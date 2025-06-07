using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;

namespace aidaAlternative.Services
{
    public class SlideshowManager
    {
        private List<Image> images;
        private int currentIndex;

        public SlideshowManager()
        {
            images = LoadImages();
            currentIndex = 0;
        }

        private List<Image> LoadImages()
        {
            var images = new List<Image>();
            try
            {
                string imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
                if (Directory.Exists(imagesDir))
                {
                    var supportedExtensions = new[] { ".png", ".jpg", ".jpeg", ".bmp" };
                    foreach (var file in Directory.GetFiles(imagesDir)
                        .Where(f => supportedExtensions.Contains(Path.GetExtension(f).ToLower())))
                    {
                        try
                        {
                            // Fix: Load image into memory to avoid file lock
                            using (var fs = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                            using (var ms = new MemoryStream())
                            {
                                fs.CopyTo(ms);
                                ms.Position = 0;
                                images.Add(Image.FromStream(ms));
                            }
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"Failed to load image {file}: {ex.Message}");
                        }
                    }
                }
                else
                {
                    Directory.CreateDirectory(imagesDir);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error accessing images directory: {ex.Message}");
            }
            return images;
        }

        public Image GetCurrentImage() => images.Any() ? images[currentIndex] : null;

        public bool HasImages => images.Any();

        public void NextImage()
        {
            if (images.Any())
            {
                currentIndex = (currentIndex + 1) % images.Count;
            }
        }

        public void ReloadImages()
        {
            images?.ForEach(img => img.Dispose());
            images = LoadImages();
            currentIndex = 0;
        }

        public void Dispose()
        {
            images?.ForEach(img => img.Dispose());
        }
    }
}