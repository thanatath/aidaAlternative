using System;
using System.Linq;
using System.Windows.Forms;
using aidaAlternative.Services;
using aidaAlternative.UI;

namespace aidaAlternative
{
    internal static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            DisplayGuard.AssertPositionMapping();
            StatsRenderer.AssertAssetsLoaded();
            if (args.Contains("--self-check"))
            {
                var usageMonitor = new AiUsageMonitor();
                usageMonitor.RefreshIfDue();
                return;
            }

            Screen? targetScreen = null;
            while (true)
            {
                targetScreen = FindTargetScreen();

                // Debug: List all screens for verification
                Console.WriteLine("Available screens:");
                foreach (var screen in Screen.AllScreens)
                {
                    Console.WriteLine($"Screen: {screen.DeviceName}, Resolution: {screen.Bounds.Width}x{screen.Bounds.Height}, " +
                                      $"Location: {screen.Bounds.X},{screen.Bounds.Y}, Primary: {screen.Primary}");
                }

                if (targetScreen != null)
                {
                    Console.WriteLine($"Selected screen: {targetScreen.DeviceName}, Resolution: {targetScreen.Bounds.Width}x{targetScreen.Bounds.Height}, " +
                                      $"Location: {targetScreen.Bounds.X},{targetScreen.Bounds.Y}");
                    break;
                }
                else
                {
                    // No warning message, just wait and rescan
                    System.Threading.Thread.Sleep(60000); // Wait 1 minute before rescanning
                }
            }

            using (var form = new StatsForm(targetScreen))
            {
                Application.Run(form);
            }
        }

        // ponytail: re-enumerated on every call, Screen objects are snapshots and go stale on unplug/replug
        internal static Screen? FindTargetScreen() =>
            Screen.AllScreens.FirstOrDefault(x => x.Bounds.Width == 960 && x.Bounds.Height == 640);
    }
}
