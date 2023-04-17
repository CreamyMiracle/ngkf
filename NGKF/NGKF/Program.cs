
using System.Configuration;
using System.Collections.Specialized;

namespace NGKF
{
    internal class Program
    {
        static void Main(string[] args)
        {
            GameOverlay.TimerService.EnableHighPrecisionTimers();
            using (var feed = new Feed())
            {
                StatProvider statProvider = new StatProvider();
                statProvider.StatAvailable += feed.StatHandler;
                statProvider.StartScanning();
                feed.Run();
            }
        }
    }
}