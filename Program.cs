using System;
using System.Linq;
using System.Windows.Forms;

namespace aidaAlternative
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Screen targetScreen = null;
            while (true)
            {
                // Find the monitor with 960x640 resolution
                targetScreen = Screen.AllScreens
                    .FirstOrDefault(x => x.Bounds.Width == 960 && x.Bounds.Height == 640);

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
    }
}