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

            // Find the monitor with 960x640 resolution
            Screen targetScreen = Screen.AllScreens
                .FirstOrDefault(x => x.Bounds.Width == 960 && x.Bounds.Height == 640)
                ?? Screen.PrimaryScreen;

            // Debug: List all screens for verification
            Console.WriteLine("Available screens:");
            foreach (var screen in Screen.AllScreens)
            {
                Console.WriteLine($"Screen: {screen.DeviceName}, Resolution: {screen.Bounds.Width}x{screen.Bounds.Height}, " +
                                  $"Location: {screen.Bounds.X},{screen.Bounds.Y}, Primary: {screen.Primary}");
            }
            Console.WriteLine($"Selected screen: {targetScreen.DeviceName}, Resolution: {targetScreen.Bounds.Width}x{targetScreen.Bounds.Height}, " +
                              $"Location: {targetScreen.Bounds.X},{targetScreen.Bounds.Y}");

            // Check if the target screen matches 960x640
            if (targetScreen.Bounds.Width != 960 || targetScreen.Bounds.Height != 640)
            {
                MessageBox.Show(
                    $"Monitor with 960x640 resolution not found. Using {targetScreen.DeviceName} ({targetScreen.Bounds.Width}x{targetScreen.Bounds.Height}). " +
                    "Check Windows Display Settings.",
                    "Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }

            using (var form = new StatsForm(targetScreen))
            {
                Application.Run(form);
            }
        }
    }
}