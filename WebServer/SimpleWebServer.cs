using System;
using System.IO;
using System.Net;
using aidaAlternative.Services;
using System.Text;
using System.Threading;
using System.Net;

namespace aidaAlternative.WebServer
{
    public class SimpleWebServer : IDisposable
    {
        private readonly HttpListener listener;
        private readonly string imagesDir;
        private readonly Action? galleryChanged;
        private Thread? serverThread;
        private bool isRunning;

        public SimpleWebServer(int port = 8080, Action? onGalleryChanged = null)
        {
            imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            Directory.CreateDirectory(imagesDir);
            listener = new HttpListener();
            listener.Prefixes.Add($"http://*:{port}/");
            galleryChanged = onGalleryChanged;
        }

        public void Start()
        {
            if (isRunning) return;
            isRunning = true;
            listener.Start();
            serverThread = new Thread(Listen) { IsBackground = true };
            serverThread.Start();
        }

        private void Listen()
        {
            while (isRunning)
            {
                try
                {
                    var context = listener.GetContext();
                    ThreadPool.QueueUserWorkItem(_ => HandleRequest(context));
                }
                catch (HttpListenerException)
                {
                    // Listener was stopped
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"WebServer error: {ex.Message}");
                }
            }
        }

        private void HandleRequest(HttpListenerContext context)
        {
            var req = context.Request;
            var resp = context.Response;
            try
            {
                if (req.HttpMethod == "GET" && (req.Url?.AbsolutePath == "/" || req.Url?.AbsolutePath == "/gallery"))
                {
                    ServeGallery(resp);
                }
                else if (req.HttpMethod == "GET" && req.Url?.AbsolutePath.StartsWith("/images/") == true)
                {
                    ServeImage(resp, req.Url.AbsolutePath.Substring("/images/".Length));
                }
                else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/upload")
                {
                    HandleUpload(req, resp);
                }
                else if (req.HttpMethod == "POST" && req.Url?.AbsolutePath == "/delete")
                {
                    var name = req.QueryString["name"];
                    HandleDelete(resp, name);
                }
                else
                {
                    resp.StatusCode = 404;
                    resp.Close();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"WebServer request error: {ex.Message}");
                try { resp.StatusCode = 500; resp.Close(); } catch { }
            }
        }

        private void ServeGallery(HttpListenerResponse resp)
        {
            var sb = new StringBuilder();
            sb.Append("<html><head><title>Slideshow Gallery</title>");
            sb.Append("<script src='https://cdn.tailwindcss.com'></script></head>");
            sb.Append("<body class='bg-gray-100 p-4'>");
            sb.Append("<div class='container mx-auto'>");
            sb.Append("<h1 class='text-2xl font-bold mb-4 text-center'>Slideshow Gallery</h1>");
            sb.Append("<form class='mb-6 flex flex-col sm:flex-row items-center justify-center gap-2' method='post' enctype='multipart/form-data' action='/upload'>");
            sb.Append("<input type='file' name='file' class='file:mr-4 file:py-2 file:px-4 file:rounded file:border-0 file:bg-blue-600 file:text-white file:cursor-pointer'/>");
            sb.Append("<button type='submit' class='bg-green-500 text-white px-4 py-2 rounded'>Upload</button>");
            sb.Append("</form>");
            sb.Append("<div class='grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-4'>");
            foreach (var file in Directory.GetFiles(imagesDir))
            {
                var name = Path.GetFileName(file);
                var encoded = WebUtility.UrlEncode(name);
                sb.Append("<div class='bg-white rounded shadow p-4'>");
                sb.Append($"<img class='w-full h-auto object-contain mb-2' src='/images/{encoded}' />");
                sb.Append($"<form method='post' action='/delete?name={encoded}'>");
                sb.Append("<button class='bg-red-500 text-white px-2 py-1 rounded w-full'>Delete</button>");
                sb.Append("</form></div>");
            }
            sb.Append("</div></div></body></html>");
            var bytes = Encoding.UTF8.GetBytes(sb.ToString());
            resp.ContentType = "text/html";
            resp.ContentLength64 = bytes.Length;
            resp.OutputStream.Write(bytes, 0, bytes.Length);
            resp.Close();
        }

        private void ServeImage(HttpListenerResponse resp, string filename)
        {
            var path = Path.Combine(imagesDir, filename);
            if (File.Exists(path))
            {
                try
                {
                    var bytes = File.ReadAllBytes(path);
                    resp.ContentType = "image/png";
                    resp.ContentLength64 = bytes.Length;
                    resp.OutputStream.Write(bytes, 0, bytes.Length);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error serving image: {ex.Message}");
                    resp.StatusCode = 500;
                }
            }
            else
            {
                resp.StatusCode = 404;
            }
            resp.Close();
        }

        private void HandleUpload(HttpListenerRequest req, HttpListenerResponse resp)
        {
            if (!req.HasEntityBody)
            {
                resp.StatusCode = 400;
                resp.Close();
                return;
            }
            var boundary = GetBoundary(req.ContentType);
            if (boundary == null)
            {
                resp.StatusCode = 400;
                resp.Close();
                return;
            }
            using var ms = new MemoryStream();
            req.InputStream.CopyTo(ms);
            var data = ms.ToArray();
            var content = Encoding.UTF8.GetString(data);
            var filename = GetFileName(content);
            if (filename == null)
            {
                resp.StatusCode = 400;
                resp.Close();
                return;
            }
            var start = content.IndexOf("\r\n\r\n") + 4;
            var end = content.IndexOf(boundary, start) - 4;
            var fileData = data.AsSpan(start, end - start).ToArray();
            File.WriteAllBytes(Path.Combine(imagesDir, filename), fileData);
            galleryChanged?.Invoke();
            resp.Redirect("/gallery");
            resp.Close();
        }

        private string? GetBoundary(string contentType)
        {
            var idx = contentType?.IndexOf("boundary=") ?? -1;
            if (idx >= 0)
            {
                return "--" + contentType[(idx + 9)..];
            }
            return null;
        }

        private string? GetFileName(string content)
        {
            var idx = content.IndexOf("filename=\"");
            if (idx >= 0)
            {
                idx += 10;
                var end = content.IndexOf('"', idx);
                if (end > idx)
                {
                    return Path.GetFileName(content[idx..end]);
                }
            }
            return null;
        }

        private void HandleDelete(HttpListenerResponse resp, string? name)
        {
            if (!string.IsNullOrEmpty(name))
            {
                var path = Path.Combine(imagesDir, name);
                if (File.Exists(path))
                {
                    File.Delete(path);
                    galleryChanged?.Invoke();
                }
            }
            resp.Redirect("/gallery");
            resp.Close();
        }

        public void Stop()
        {
            if (!isRunning) return;
            isRunning = false;
            listener.Stop();
        }

        public void Dispose()
        {
            Stop();
            listener.Close();
        }
    }
}
