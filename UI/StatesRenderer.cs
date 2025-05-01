using aidaAlternative.Models;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace aidaAlternative.UI
{
    public static class StatsRenderer
    {
        public static void Render(PaintEventArgs e, List<StatItem> statItems, Image slideshowImage, float fadeOpacity, float slideshowOpacity)
        {
            var g = e.Graphics;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            // Draw gradient background
            using (var brush = new LinearGradientBrush(e.ClipRectangle, Color.Black, Color.FromArgb(50, 50, 50), 45f))
            {
                g.FillRectangle(brush, e.ClipRectangle);
            }

            // Draw stat items
            foreach (var item in statItems)
            {
                var rect = item.Bounds;
                using (var path = new GraphicsPath())
                {
                    int radius = 10;
                    path.AddArc(rect.X, rect.Y, radius, radius, 180, 90);
                    path.AddArc(rect.Width - radius + rect.X, rect.Y, radius, radius, 270, 90);
                    path.AddArc(rect.Width - radius + rect.X, rect.Height - radius + rect.Y, radius, radius, 0, 90);
                    path.AddArc(rect.X, rect.Height - radius + rect.Y, radius, radius, 90, 90);
                    path.CloseFigure();

                    using (var brush = new SolidBrush(Color.FromArgb(200, 20, 20, 20)))
                    {
                        g.FillPath(brush, path);
                    }
                    using (var pen = new Pen(Color.LimeGreen, 2))
                    {
                        g.DrawPath(pen, path);
                    }
                }

                if (item.Percentage.HasValue)
                {
                    var barRect = new Rectangle(rect.X + 10, rect.Y + 40, rect.Width - 20, 10);
                    using (var brush = new LinearGradientBrush(barRect, Color.LimeGreen, Color.DarkGreen, 0f))
                    {
                        g.FillRectangle(brush, barRect.X, barRect.Y, barRect.Width * item.Percentage.Value, barRect.Height);
                    }
                    using (var pen = new Pen(Color.White, 1))
                    {
                        g.DrawRectangle(pen, barRect);
                    }
                }

                using (var font = new Font("Consolas", 20, FontStyle.Bold))
                {
                    var text = $"{item.Name}: {item.Value}";
                    var textPoint = new PointF(rect.X + 10, rect.Y + 10);
                    using (var textBrush = new SolidBrush(Color.FromArgb((int)(255 * fadeOpacity), 255, 255, 255)))
                    {
                        g.DrawString(text, font, textBrush, textPoint);
                    }
                }
            }

            // Draw slideshow
            var slideshowRect = new Rectangle(500, 50, 440, 540);
            if (slideshowImage != null)
            {
                float scale = Math.Min((float)slideshowRect.Width / slideshowImage.Width, (float)slideshowRect.Height / slideshowImage.Height);
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

                using (var pen = new Pen(Color.LimeGreen, 2))
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
        }
    }
}