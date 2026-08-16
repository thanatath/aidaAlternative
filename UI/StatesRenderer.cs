using aidaAlternative.Models;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace aidaAlternative.UI
{
    public static class StatsRenderer
    {
        // Clawd, the Claude Code pixel crab. 'O' = shell, 'K' = eyes.
        private static readonly string[] Mascot =
        {
            "...OOOOOOOOOOOOOO...",
            "...OOOOOOOOOOOOOO...",
            "...OOOOOOOOOOOOOO...",
            "...OOKKOOOOOOKKOO...",
            "OOOOOKKOOOOOOKKOOOOO",
            "OOOOOOOOOOOOOOOOOOOO",
            "OOOOOOOOOOOOOOOOOOOO",
            "...OOOOOOOOOOOOOO...",
            "...OOOOOOOOOOOOOO...",
            "....O..O....O..O....",
            "....O..O....O..O....",
            "....O..O....O..O....",
        };

        [Conditional("DEBUG")]
        internal static void AssertAssetsLoaded() => Debug.Assert(Mascot.Length == 12 && Array.TrueForAll(Mascot, row => row.Length == 20));

        public static void Render(PaintEventArgs e, List<StatItem> statItems, AiUsageSnapshot usage, float fadeOpacity)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            using (var brush = new LinearGradientBrush(e.ClipRectangle, Color.Black, Color.FromArgb(50, 50, 50), 45f))
            {
                g.FillRectangle(brush, e.ClipRectangle);
            }

            foreach (var item in statItems)
            {
                var rect = item.Bounds;
                using (var path = RoundedRectangle(rect, 12))
                using (var brush = new SolidBrush(Color.FromArgb(200, 22, 22, 24)))
                using (var pen = new Pen(Color.FromArgb(90, 255, 255, 255)))
                {
                    g.FillPath(brush, path);
                    g.DrawPath(pen, path);
                }

                if (item.Percentage.HasValue)
                {
                    var barRect = new Rectangle(rect.X + 14, rect.Bottom - 24, rect.Width - 28, 14);
                    using var barPath = RoundedRectangle(barRect, 7);
                    using (var trackBrush = new SolidBrush(Color.FromArgb(55, 255, 255, 255)))
                    {
                        g.FillPath(trackBrush, barPath);
                    }
                    var pct = Math.Clamp(item.Percentage.Value, 0f, 1f);
                    if (pct > 0f)
                    {
                        g.SetClip(barPath);
                        using (var brush = new LinearGradientBrush(barRect, Color.MediumSeaGreen, Color.OrangeRed, 0f))
                        {
                            g.FillRectangle(brush, barRect.X, barRect.Y, barRect.Width * pct, barRect.Height);
                        }
                        g.ResetClip();
                    }
                    using (var pen = new Pen(Color.FromArgb(130, 255, 255, 255)))
                    {
                        g.DrawPath(pen, barPath);
                    }
                }

                using (var font = new Font("Consolas", 20, FontStyle.Bold))
                using (var textBrush = new SolidBrush(Color.FromArgb((int)(255 * fadeOpacity), 255, 255, 255)))
                {
                    var textY = item.Percentage.HasValue ? rect.Y + 10 : rect.Y + (rect.Height - 27) / 2;
                    g.DrawString($"{item.Name}: {item.Value}", font, textBrush, new PointF(rect.X + 14, textY));
                }
            }

            /* Slideshow renderer retained for possible restoration.
            var slideshowRect = new Rectangle(500, 50, 440, 540);
            if (slideshowImage != null)
            {
                float scale = 1.0f;
                try {
                    scale = Math.Min((float)slideshowRect.Width / slideshowImage.Width, (float)slideshowRect.Height / slideshowImage.Height);
                }
                catch (Exception ex)
                {
                    return;
                }
                int scaledWidth = (int)(slideshowImage.Width * scale);
                int scaledHeight = (int)(slideshowImage.Height * scale);
                int x = slideshowRect.X + (slideshowRect.Width - scaledWidth) / 2;
                int y = slideshowRect.Y + (slideshowRect.Height - scaledHeight) / 2;

                using (var attributes = new System.Drawing.Imaging.ImageAttributes())
                {
                    var matrix = new System.Drawing.Imaging.ColorMatrix { Matrix33 = slideshowOpacity };
                    attributes.SetColorMatrix(matrix);
                    g.DrawImage(slideshowImage, new Rectangle(x, y, scaledWidth, scaledHeight),
                        0, 0, slideshowImage.Width, slideshowImage.Height, GraphicsUnit.Pixel, attributes);
                }

                using (var pen = new Pen(Color.White, 2))
                {
                    using (var path = new GraphicsPath())
                    {
                        int radius = 10;
                        path.AddArc(slideshowRect.X, slideshowRect.Y, radius, radius, 180, 90);
                        path.AddArc(slideshowRect.Width - radius + slideshowRect.X, slideshowRect.Y, radius, radius, 270, 90);
                        path.AddArc(slideshowRect.Width - radius + slideshowRect.X, slideshowRect.Height - radius + slideshowRect.Y, radius, radius, 0, 90);
                        path.AddArc(slideshowRect.X, slideshowRect.Height - radius + slideshowRect.Y, radius, radius, 90, 90);
                        path.CloseFigure();
                        g.DrawPath(pen, path);
                    }
                }
            }
            else
            {
                using (var brush = new SolidBrush(Color.FromArgb(200, 20, 20, 20)))
                {
                    g.FillRectangle(brush, slideshowRect);
                }
                using (var pen = new Pen(Color.LimeGreen, 2))
                {
                    g.DrawRectangle(pen, slideshowRect);
                }
                using (var font = new Font("Consolas", 12))
                {
                    g.DrawString("No images in /images", font, Brushes.White, slideshowRect.X + 10, slideshowRect.Y + 10);
                }
            }
            */

            RenderUsage(g, usage);
        }

        private static void RenderUsage(Graphics g, AiUsageSnapshot usage)
        {
            var panel = new Rectangle(500, 50, 440, 540);
            using (var path = RoundedRectangle(panel, 14))
            using (var brush = new SolidBrush(Color.FromArgb(225, 24, 24, 27)))
            using (var pen = new Pen(Color.FromArgb(80, 255, 255, 255)))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            DrawMascot(g, panel.X + 24, panel.Y + 30, 4);

            using (var titleFont = new Font("Segoe UI", 22, FontStyle.Bold))
            using (var subtitleFont = new Font("Segoe UI", 10, FontStyle.Regular))
            {
                g.DrawString("Claude Code", titleFont, Brushes.White, panel.X + 118, panel.Y + 24);
                g.DrawString("Plan usage · refreshes every 5 min", subtitleFont, Brushes.Gainsboro, panel.X + 120, panel.Y + 57);
            }

            // ponytail: one hero number was redundant with the "5-hour plan" card right below it —
            // dropped in favor of giving the three cards real spacing instead of piling them up.
            const int cardHeight = 130;
            const int gap = 20;
            var top = panel.Y + 96;
            DrawLimit(g, new Rectangle(panel.X + 28, top, panel.Width - 56, cardHeight), "5-hour plan", usage.ClaudeFiveHourUsedPercent, usage.ClaudeFiveHourResetsAt);
            top += cardHeight + gap;
            DrawLimit(g, new Rectangle(panel.X + 28, top, panel.Width - 56, cardHeight), "7-day plan", usage.ClaudeSevenDayUsedPercent, usage.ClaudeSevenDayResetsAt);
            top += cardHeight + gap;
            DrawLimit(g, new Rectangle(panel.X + 28, top, panel.Width - 56, cardHeight), $"{usage.ScopedWeeklyLabel ?? "Model"} weekly", usage.ScopedWeeklyUsedPercent, usage.ScopedWeeklyResetsAt);

        }

        private static void DrawLimit(Graphics g, Rectangle bounds, string label, float? usedPercent, DateTime? resetsAtUtc)
        {
            using (var path = RoundedRectangle(bounds, 10))
            using (var brush = new SolidBrush(Color.FromArgb(245, 37, 37, 42)))
            using (var pen = new Pen(Color.FromArgb(60, 255, 255, 255)))
            {
                g.FillPath(brush, path);
                g.DrawPath(pen, path);
            }

            var barColor = usedPercent switch
            {
                >= 80f => Color.FromArgb(239, 68, 68),
                >= 50f => Color.FromArgb(245, 158, 11),
                not null => Color.FromArgb(34, 197, 94),
                null => Color.Gray,
            };

            using (var labelFont = new Font("Segoe UI", 15, FontStyle.Regular))
            using (var etaFont = new Font("Segoe UI", 11, FontStyle.Regular))
            using (var valueFont = new Font("Segoe UI", 34, FontStyle.Bold))
            using (var valueBrush = new SolidBrush(barColor))
            {
                g.DrawString(label, labelFont, Brushes.White, bounds.X + 18, bounds.Y + 16);
                g.DrawString(FormatEta(resetsAtUtc), etaFont, Brushes.Gainsboro, bounds.X + 18, bounds.Y + 46);
                var percentText = usedPercent is float percent ? $"{Math.Clamp(percent, 0, 100):0}%" : "—";
                var valueSize = g.MeasureString(percentText, valueFont);
                g.DrawString(percentText, valueFont, valueBrush, bounds.Right - valueSize.Width - 18, bounds.Y + 12);
            }

            var track = new Rectangle(bounds.X + 18, bounds.Bottom - 40, bounds.Width - 36, 24);
            using var trackPath = RoundedRectangle(track, 12);
            using (var trackBrush = new SolidBrush(Color.FromArgb(70, 255, 255, 255)))
            {
                g.FillPath(trackBrush, trackPath);
            }
            if (usedPercent is float value)
            {
                value = Math.Clamp(value, 0, 100);
                g.SetClip(trackPath);
                using var fill = new SolidBrush(barColor);
                g.FillRectangle(fill, track.X, track.Y, track.Width * value / 100f, track.Height);
                g.ResetClip();
            }
            using (var trackPen = new Pen(Color.FromArgb(140, 255, 255, 255)))
            {
                g.DrawPath(trackPen, trackPath);
            }
        }

        private static string FormatEta(DateTime? resetsAtUtc)
        {
            if (resetsAtUtc is not DateTime reset) return "";

            var remaining = reset - DateTime.UtcNow;
            if (remaining <= TimeSpan.Zero) return "resetting…";
            return remaining.TotalHours >= 1
                ? $"resets in {(int)remaining.TotalHours}h {remaining.Minutes}m"
                : $"resets in {remaining.Minutes}m";
        }

        private static void DrawMascot(Graphics g, int x, int y, int cell)
        {
            using var shell = new SolidBrush(Color.FromArgb(222, 136, 108));
            for (var row = 0; row < Mascot.Length; row++)
            {
                for (var column = 0; column < Mascot[row].Length; column++)
                {
                    var brush = Mascot[row][column] switch
                    {
                        'O' => shell,
                        'K' => Brushes.Black,
                        _ => null,
                    };
                    if (brush is not null) g.FillRectangle(brush, x + column * cell, y + row * cell, cell, cell);
                }
            }
        }

        private static GraphicsPath RoundedRectangle(Rectangle rectangle, int radius)
        {
            var path = new GraphicsPath();
            path.AddArc(rectangle.Left, rectangle.Top, radius, radius, 180, 90);
            path.AddArc(rectangle.Right - radius, rectangle.Top, radius, radius, 270, 90);
            path.AddArc(rectangle.Right - radius, rectangle.Bottom - radius, radius, radius, 0, 90);
            path.AddArc(rectangle.Left, rectangle.Bottom - radius, radius, radius, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
