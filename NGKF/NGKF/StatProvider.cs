using Fortnite_API;
using SharpDX.Direct2D1;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Linq;

namespace NGKF
{
    internal class StatProvider
    {
        public event EventHandler StatAvailable;
        private readonly OCR _ocr;
        private readonly int _ocrScanIntervalMillis;
        private readonly int _sameNameAppearanceFrequencySeconds;
        private readonly FortniteApiClient _client;
        private readonly Dictionary<string, DateTime> _handledNames = new Dictionary<string, DateTime>();
        private readonly List<string> _killFeedTexts = new List<string>();
        private readonly List<string> _observedPlayers = new List<string>();

        public virtual void OnStatAvailable(FeedLine line, EventArgs e)
        {
            StatAvailable?.Invoke(line, e);
        }

        public StatProvider()
        {
            _killFeedTexts = new List<string>(ConfigurationManager.AppSettings.Get("KillFeedLines").Split(new char[] { ';' }));
            _observedPlayers = new List<string>(ConfigurationManager.AppSettings.Get("ObservedPlayers").Split(new char[] { ';' }));

            _ocr = new OCR();
            _ocrScanIntervalMillis = int.Parse(ConfigurationManager.AppSettings.Get("OcrScanIntervalMillis"));
            _sameNameAppearanceFrequencySeconds = int.Parse(ConfigurationManager.AppSettings.Get("SameNameAppearanceFrequencySeconds"));
            string apiKey = ConfigurationManager.AppSettings.Get("FortniteAPIKey");
            _client = new FortniteApiClient(apiKey);
        }

        public void StartScanning()
        {
            //System.Timers.Timer aTimer = new System.Timers.Timer();
            //aTimer.Elapsed += new ElapsedEventHandler(OnTimedEvent);
            //aTimer.Interval = _ocrScanIntervalMillis;
            //aTimer.Enabled = true;
            OnTimedEvent(null, null);
        }

        private void OnTimedEvent(object? sender, ElapsedEventArgs e)
        {
            List<string> lines = _ocr.Capture();
            List<string> searchNames = new List<string>();
            foreach (string line in lines)
            {
                if (string.IsNullOrEmpty(line) || string.IsNullOrWhiteSpace(line))
                {
                    continue;
                }

                foreach (string feedText in _killFeedTexts)
                {
                    string feedTextWithoutNames = feedText.Replace("#PLAYER#", "").Replace("#TARGET#", "");
                    if (!string.IsNullOrWhiteSpace(feedTextWithoutNames) && line.Contains(feedTextWithoutNames))
                    {
                        int start = line.IndexOf(feedTextWithoutNames);
                        string playerName = line.Substring(0, start).Trim();
                        string playerNameNoLevel = Regex.Replace(playerName, @"(\([0-9]*\))$", "").Trim('\\').Trim();
                        if (Regex.IsMatch(playerNameNoLevel, @"(\[[0-9]*\])$"))
                        {
                            continue;
                        }

                        string enemyName = line.Substring(start + feedTextWithoutNames.Length).Trim();
                        string enemyNameNoLevel = Regex.Replace(enemyName, @"(\([0-9]*\))$", "").Trim('\\').Trim();
                        if (Regex.IsMatch(enemyNameNoLevel, @"(\[[0-9]*\])$"))
                        {
                            continue;
                        }

                        if (enemyNameNoLevel != null && _observedPlayers.Contains(playerNameNoLevel))
                        {
                            searchNames.Add(enemyNameNoLevel);
                        }
                        else if (playerNameNoLevel != null && _observedPlayers.Contains(enemyNameNoLevel))
                        {
                            searchNames.Add(playerNameNoLevel);
                        }
                    }
                }
            }
            if (searchNames.Count == 0)
            {
                return;
            }

            DateTime now = DateTime.Now;
            foreach (string name in searchNames)
            {
                if (_handledNames.TryGetValue(name, out DateTime prevTime))
                {
                    if ((now - prevTime).TotalSeconds < _sameNameAppearanceFrequencySeconds)
                    {
                        continue;
                    }
                }

                _handledNames[name] = now;

                Console.WriteLine(name);
                var statsV2V1 = _client.V1.Stats.GetBrV2Id(x =>
                {
                    x.Name = name;
                });

                if (statsV2V1 != null && statsV2V1.IsSuccess)
                {
                    FeedLine line = new FeedLine(statsV2V1.Data, DateTime.UtcNow, Guid.NewGuid().ToString());
                    OnStatAvailable(line, null);
                }
            }
        }
    }
}
