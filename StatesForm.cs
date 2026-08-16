using aidaAlternative.Models;
using aidaAlternative.Services;
using aidaAlternative.SystemTray;
using aidaAlternative.UI;
using aidaAlternative.WebServer;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Timer = System.Windows.Forms.Timer;

namespace aidaAlternative
{
    public class StatsForm : Form
    {
        private readonly PerformanceMonitor performanceMonitor;
        /* Slideshow retained for possible restoration.
        private readonly SlideshowManager slideshowManager;
        */
        private readonly SystemTrayManager systemTrayManager;
        /* Slideshow gallery retained for possible restoration.
        private readonly SimpleWebServer webServer;
        */
        private readonly DisplayGuard displayGuard;
        private readonly AiUsageMonitor aiUsageMonitor;
        private readonly List<StatItem> statItems;
        private readonly Timer updateTimer;
        private readonly Timer fadeTimer;
        /* Slideshow timers retained for possible restoration.
        private readonly Timer slideshowTimer;
        private readonly Timer slideshowFadeTimer;
        */
        private float fadeOpacity = 1f;
        private bool fadeIn = false;
        /* Slideshow fade state retained for possible restoration.
        private float slideshowOpacity = 1f;
        private bool slideshowFadeIn = false;
        */
        private bool allowClose = false;
        private bool userHidden = false;

        public StatsForm(Screen targetScreen)
        {
            FormBorderStyle = FormBorderStyle.None;
            StartPosition = FormStartPosition.Manual;
            Bounds = targetScreen.Bounds;
            TopMost = true;
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            ShowInTaskbar = false;

            performanceMonitor = new PerformanceMonitor();
            /* Slideshow retained for possible restoration.
            slideshowManager = new SlideshowManager();
            */
            systemTrayManager = new SystemTrayManager(ShowForm, HideForm, ExitApplication);
            displayGuard = new DisplayGuard();
            aiUsageMonitor = new AiUsageMonitor();
            aiUsageMonitor.RefreshIfDue();
            /* Slideshow gallery retained for possible restoration.
            webServer = new SimpleWebServer();
            webServer.ImagesChanged += () =>
            {
                StatsRenderer.PauseRendering = true;
                try
                {
                    slideshowManager.ReloadImages();
                }
                finally
                {
                    StatsRenderer.PauseRendering = false;
                }
                Invalidate();
            };
            webServer.Start();
            */
            statItems = new List<StatItem>();
            InitializeStatItems();

            updateTimer = new Timer { Interval = 1000 };
            updateTimer.Tick += UpdateStats;
            updateTimer.Start();

            fadeTimer = new Timer { Interval = 50 };
            fadeTimer.Tick += (s, e) =>
            {
                if (fadeIn)
                {
                    fadeOpacity += 0.1f;
                    if (fadeOpacity >= 1f) { fadeOpacity = 1f; fadeIn = false; }
                }
                else
                {
                    fadeOpacity -= 0.1f;
                    if (fadeOpacity <= 0.8f) { fadeOpacity = 0.8f; fadeIn = true; }
                }
                Invalidate();
            };
            fadeTimer.Start();

            /* Slideshow timers retained for possible restoration.
            slideshowTimer = new Timer { Interval = 5000 };
            slideshowTimer.Tick += (s, e) =>
            {
                if (slideshowManager.HasImages)
                {
                    slideshowManager.NextImage();
                    slideshowOpacity = 0f;
                    slideshowFadeIn = true;
                }
                Invalidate();
            };
            slideshowTimer.Start();

            slideshowFadeTimer = new Timer { Interval = 50 };
            slideshowFadeTimer.Tick += (s, e) =>
            {
                if (slideshowFadeIn)
                {
                    slideshowOpacity += 0.1f;
                    if (slideshowOpacity >= 1f) { slideshowOpacity = 1f; slideshowFadeIn = false; }
                    Invalidate();
                }
            };
            slideshowFadeTimer.Start();
            */
        }

        private void InitializeStatItems()
        {
            int x = 20, y = 50, spacing = 84;
            int width = 450, height = 64;

            statItems.Add(new StatItem { Name = "CPU", Bounds = new Rectangle(x, y, width, height) });
            y += spacing;
            statItems.Add(new StatItem { Name = "RAM", Bounds = new Rectangle(x, y, width, height) });
            y += spacing;
            statItems.Add(new StatItem { Name = "Download", Bounds = new Rectangle(x, y, width, height) });
            y += spacing;
            statItems.Add(new StatItem { Name = "Upload", Bounds = new Rectangle(x, y, width, height) });
            y += spacing;
            statItems.Add(new StatItem { Name = "GPU Usage", Bounds = new Rectangle(x, y, width, height) });
            y += spacing;
            statItems.Add(new StatItem { Name = "GPU VRAM", Bounds = new Rectangle(x, y, width, height) });
            y += spacing;
            statItems.Add(new StatItem { Name = "GPU Temp", Bounds = new Rectangle(x, y, width, height) });
        }

        // ponytail: re-asserted every second instead of hooking DisplaySettingsChanged,
        // Windows fires that event before it finishes shuffling windows back
        private void SnapToTargetScreen()
        {
            var screen = Program.FindTargetScreen();
            if (screen == null)
            {
                if (Visible) Hide();
                return;
            }
            if (Bounds != screen.Bounds)
            {
                Bounds = screen.Bounds;
                TopMost = true;
            }
            if (!Visible && !userHidden) Show();
        }

        private void UpdateStats(object sender, EventArgs e)
        {
            SnapToTargetScreen();
            aiUsageMonitor.RefreshIfDue();
            var stats = performanceMonitor.GetStats();
            foreach (var stat in stats)
            {
                var item = statItems.FirstOrDefault(s => s.Name == stat.Key);
                if (item != null)
                {
                    item.Value = stat.Value.Value;
                    item.Percentage = stat.Value.Percentage;
                }
            }
            Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            /* Slideshow render retained for possible restoration.
            StatsRenderer.Render(e, statItems, slideshowManager.GetCurrentImage(), fadeOpacity, slideshowOpacity);
            */
            StatsRenderer.Render(e, statItems, aiUsageMonitor.Snapshot, fadeOpacity);
        }

        private void ShowForm()
        {
            userHidden = false;
            Show();
            WindowState = FormWindowState.Normal;
            TopMost = true;
            systemTrayManager.DisableShow();
        }

        private void HideForm()
        {
            userHidden = true;
            Hide();
            systemTrayManager.EnableShow();
        }

        private void ExitApplication()
        {
            allowClose = true;
            systemTrayManager.Dispose();
            Close();
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (!allowClose && e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                HideForm();
            }
            base.OnFormClosing(e);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                updateTimer?.Dispose();
                fadeTimer?.Dispose();
                /* Slideshow timers retained for possible restoration.
                slideshowTimer?.Dispose();
                slideshowFadeTimer?.Dispose();
                */
                performanceMonitor?.Dispose();
                /* Slideshow retained for possible restoration.
                slideshowManager?.Dispose();
                */
                systemTrayManager?.Dispose();
                displayGuard?.Dispose();
                /* Slideshow gallery retained for possible restoration.
                webServer?.Dispose();
                */
            }
            base.Dispose(disposing);
        }
    }
}
