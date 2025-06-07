using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Net;

namespace aidaAlternative.WebServer
{
    public class SimpleWebServer : IDisposable
    {
        private readonly HttpListener listener;
        private readonly string imagesDir;
        private Thread? serverThread;
        private bool isRunning;

        public SimpleWebServer(int port = 5001)
        {
            imagesDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images");
            Directory.CreateDirectory(imagesDir);
            listener = new HttpListener();
            listener.Prefixes.Add($"http://127.0.0.1:{port}/");
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

        public event Action? ImagesChanged;

        private void ServeGallery(HttpListenerResponse resp)
        {
            var sb = new StringBuilder();
            sb.Append("<html lang='en'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.Append("<script src='https://cdn.tailwindcss.com'></script>");
            sb.Append("<title>Gallery</title></head><body class='bg-gray-900 text-white min-h-screen flex flex-col items-center p-4'>");
            sb.Append("<div class='w-full max-w-3xl'>");
            sb.Append("<h1 class='text-3xl font-bold mb-6 text-center'>Gallery</h1>");
            sb.Append("<form method='post' enctype='multipart/form-data' action='/upload' class='flex flex-col sm:flex-row gap-2 items-center justify-center mb-8'>");
            sb.Append("<input type='file' name='file' class='file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100' required />");
            sb.Append("<input type='submit' value='Upload' class='bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded cursor-pointer' />");
            sb.Append("</form></div><hr class='border-gray-700 mb-8'>");
            sb.Append("<div class='grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-8 w-full max-w-5xl mx-auto'>");
            foreach (var file in Directory.GetFiles(imagesDir))
            {
                var name = Path.GetFileName(file);
                var encoded = WebUtility.UrlEncode(name);
                sb.Append($"<div class='bg-gray-800 rounded-lg shadow-lg p-4 flex flex-col items-center'>");
                sb.Append($"<img src='/images/{encoded}' class='rounded max-w-full max-h-48 object-contain mb-4 border border-gray-700' alt='{encoded}'/>");
                sb.Append($"<form method='post' action='/delete?name={encoded}' class='w-full flex justify-center'><input type='submit' value='Delete' class='bg-red-600 hover:bg-red-700 text-white font-bold py-1 px-4 rounded cursor-pointer'/></form>");
                sb.Append("</div>");
            }
            sb.Append("</div>");
            sb.Append("</body></html>");
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
            ImagesChanged?.Invoke();
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
                try
                {
                    if (File.Exists(path))
                    {
                        File.Delete(path);
                        ImagesChanged?.Invoke();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error deleting file '{path}': {ex}");
                    resp.StatusCode = 500;
                    resp.Close();
                    return;
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
