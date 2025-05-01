using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace aidaAlternative.SystemTray
{
    public class SystemTrayManager : IDisposable
    {
        private readonly NotifyIcon notifyIcon;
        private readonly Action showForm;
        private readonly Action hideForm;
        private readonly Action exitApplication;

        public SystemTrayManager(Action showForm, Action hideForm, Action exitApplication)
        {
            this.showForm = showForm;
            this.hideForm = hideForm;
            this.exitApplication = exitApplication;

            notifyIcon = new NotifyIcon
            {
                Visible = true,
                Text = "System Stats Monitor"
            };

            // Load icon
            try
            {
                string iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "app.ico");
                if (File.Exists(iconPath))
                {
                    notifyIcon.Icon = new Icon(iconPath);
                }
                else
                {
                    notifyIcon.Icon = SystemIcons.Application;
                    Console.WriteLine("app.ico not found; using default icon.");
                }
            }
            catch (Exception ex)
            {
                notifyIcon.Icon = SystemIcons.Application;
                Console.WriteLine($"Error loading icon: {ex.Message}");
            }

            // Context menu
            var contextMenu = new ContextMenuStrip();
            contextMenu.Items.Add("Show", null, (s, e) => showForm());
            contextMenu.Items.Add("Hide", null, (s, e) => hideForm());
            contextMenu.Items.Add("Exit", null, (s, e) => exitApplication());
            notifyIcon.ContextMenuStrip = contextMenu;

            notifyIcon.DoubleClick += (s, e) => ToggleFormVisibility();
        }

        private void ToggleFormVisibility()
        {
            if (notifyIcon.ContextMenuStrip.Items[0].Enabled) // Check if "Show" is enabled
                showForm();
            else
                hideForm();
        }

        public void DisableShow() => notifyIcon.ContextMenuStrip.Items[0].Enabled = false;
        public void EnableShow() => notifyIcon.ContextMenuStrip.Items[0].Enabled = true;

        public void Dispose()
        {
            notifyIcon.Visible = false;
            notifyIcon.Dispose();
        }
    }
}