using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FallGuysStats.Entities {
    public class Content {
        public class ContentGameRules {
            public int duration { get; }
            public bool isFinal { get; }

            public ContentGameRules(int duration, bool isFinal) {
                this.duration = duration;
                this.isFinal = isFinal;
            }
        }

        // basic obfuscation to avoid search engine based trolls
        private string CONTENT_FILENAME = (("c" + (("on" + "te") + "n") + "t") + '_') + (("v1"));
        private string contentPath;
        private byte[] contentXorKeyA = new byte[] {0xa1, 0x66, 0x59 - 1, 0x63 + 16, 0x4A, 0x34, 0x69, 0x64, 0x9b, 0x2c, 0xA9, 0x97, 0x6d, 0xa3, 0x6b, 0x3e + 0x01};
        private byte[] contentXorKeyB = new byte[] {0xc0, 0x43 + 2, 0x79, 0x00, 0x09, 0x04, 0x45, 0x4a, 0xfa, 0x0f, 0x88, 0xe4, 0x2e, 0x93, 0x47, 0x13 - 0x02};
        private JsonClass contentDict;
        private Dictionary<string, ContentGameRules> allGameRules = new Dictionary<string, ContentGameRules>();
        FileSystemWatcher watcher;
        public Content(string contentFolder) {
            contentPath = Path.Combine(contentFolder, CONTENT_FILENAME);
            watcher = new FileSystemWatcher(contentFolder);
            watcher.Filter = CONTENT_FILENAME;
            watcher.Changed += OnChanged;
            watcher.Created += OnChanged;
            watcher.Deleted += OnChanged;
            watcher.EnableRaisingEvents = true;
            reloadData();
        }

        ~Content() {
            watcher.EnableRaisingEvents = false;
        }

        private void OnChanged(object sender, FileSystemEventArgs e) {
            reloadData();
        }
        private void reloadData() {
            if (File.Exists(contentPath)) {
                byte[] fileBytes = File.ReadAllBytes(contentPath);
                for (int i = 0; i < fileBytes.Length; ++i) {
                    fileBytes[i] ^= contentXorKeyA[i % 16];
                }
                for (int i = 0; i < fileBytes.Length; ++i) {
                    fileBytes[i] ^= contentXorKeyB[i % 16];
                }
                lock (this) {
                    contentDict = Json.Read(fileBytes) as JsonClass;
                    processGameRules();
                }
            } else {
                lock (this) {
                    allGameRules.Clear();
                }
            }
        }
        private void processGameRules() {
            allGameRules.Clear();
            if (contentDict != null) {
                JsonArray levelRounds = contentDict["game_rules"] as JsonArray;
                if (levelRounds != null) {
                    int count = levelRounds.Count;
                    for (int i = 0; i < count; i++) {
                        if (levelRounds[i] is JsonClass) {
                            JsonClass levelClass = levelRounds[i] as JsonClass;
                            string LevelId = levelClass["id"].AsString();
                            if (LevelId != null) {
                                int duration = levelClass["duration"].AsInt();
                                bool isFinal = levelClass["is_final_round"].AsBool();
                                ContentGameRules level = new ContentGameRules(duration, isFinal);
                                allGameRules[LevelId] = level;
                            }
                        }
                    }
                }
            }
        }
        public ContentGameRules getGameRulesForRound(string roundId) {
            lock (this) {
                return allGameRules.ContainsKey(roundId) ? allGameRules[roundId] : null;
            }
        }
    }
}
