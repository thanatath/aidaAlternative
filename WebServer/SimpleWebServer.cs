using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;

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
            listener.Prefixes.Add($"http://*:{port}/");
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

        public event Action? ImagesChanged;        private void ServeGallery(HttpListenerResponse resp)
        {
            var sb = new StringBuilder();
            sb.Append("<html lang='en'><head><meta charset='UTF-8'><meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            sb.Append("<script src='https://cdn.tailwindcss.com'></script>");
            sb.Append("<title>Gallery</title>");
            sb.Append("<style>");
            sb.Append("input[type='file'] { width: 100%; }");
            sb.Append("@media (max-width: 640px) { .mobile-full { width: 100% !important; } }");
            sb.Append("</style>");
            sb.Append("</head><body class='bg-gray-900 text-white min-h-screen flex flex-col items-center p-4'>");
            sb.Append("<div class='w-full max-w-3xl'>");
            sb.Append("<h1 class='text-3xl font-bold mb-6 text-center'>Gallery</h1>");
            sb.Append("<form method='post' enctype='multipart/form-data' action='/upload' class='flex flex-col gap-4 items-center justify-center mb-8'>");
            sb.Append("<input type='file' name='file' accept='image/*' capture='environment' class='mobile-full file:mr-4 file:py-2 file:px-4 file:rounded-full file:border-0 file:text-sm file:font-semibold file:bg-blue-50 file:text-blue-700 hover:file:bg-blue-100' required />");
            sb.Append("<input type='submit' value='Upload' class='mobile-full bg-blue-600 hover:bg-blue-700 text-white font-bold py-2 px-4 rounded cursor-pointer' />");
            sb.Append("</form></div><hr class='border-gray-700 mb-8'>");
            sb.Append("<div class='grid grid-cols-1 sm:grid-cols-2 md:grid-cols-3 gap-8 w-full max-w-5xl mx-auto'>");
            foreach (var file in Directory.GetFiles(imagesDir))
            {
                var name = Path.GetFileName(file);
                var encoded = WebUtility.UrlEncode(name);
                sb.Append($"<div class='bg-gray-800 rounded-lg shadow-lg p-4 flex flex-col items-center'>");
                sb.Append($"<img src='/images/{encoded}' class='rounded max-w-full max-h-48 object-contain mb-4 border border-gray-700' alt='{encoded}' loading='lazy'/>");
                sb.Append($"<form method='post' action='/delete?name={encoded}' class='w-full flex justify-center'><input type='submit' value='Delete' class='bg-red-600 hover:bg-red-700 text-white font-bold py-1 px-4 rounded cursor-pointer'/></form>");
                sb.Append("</div>");
            }
            sb.Append("</div>");
            sb.Append("<script>");
            sb.Append("document.querySelector('form').addEventListener('submit', function(e) {");
            sb.Append("  const fileInput = document.querySelector('input[type=\"file\"]');");
            sb.Append("  if (fileInput.files.length === 0) { alert('Please select a file'); e.preventDefault(); return; }");
            sb.Append("  const submitBtn = document.querySelector('input[type=\"submit\"]');");
            sb.Append("  submitBtn.value = 'Uploading...'; submitBtn.disabled = true;");
            sb.Append("});");
            sb.Append("</script>");
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
        }        private void HandleUpload(HttpListenerRequest req, HttpListenerResponse resp)
        {
            try
            {
                if (!req.HasEntityBody)
                {
                    Console.WriteLine("Upload failed: No entity body");
                    resp.StatusCode = 400;
                    resp.Close();
                    return;
                }

                var boundary = GetBoundary(req.ContentType);
                if (boundary == null)
                {
                    Console.WriteLine("Upload failed: No boundary found");
                    resp.StatusCode = 400;
                    resp.Close();
                    return;
                }

                using var ms = new MemoryStream();
                req.InputStream.CopyTo(ms);
                var data = ms.ToArray();

                // Parse multipart data properly without converting binary to string
                var boundaryBytes = Encoding.UTF8.GetBytes(boundary);
                var filename = GetFileNameFromBytes(data, boundaryBytes);
                
                if (string.IsNullOrEmpty(filename))
                {
                    Console.WriteLine("Upload failed: No filename found");
                    resp.StatusCode = 400;
                    resp.Close();
                    return;
                }

                var fileData = ExtractFileData(data, boundaryBytes);
                if (fileData == null || fileData.Length == 0)
                {
                    Console.WriteLine("Upload failed: No file data found");
                    resp.StatusCode = 400;
                    resp.Close();
                    return;
                }

                // Ensure filename is safe
                filename = Path.GetFileName(filename);
                if (string.IsNullOrEmpty(filename))
                {
                    filename = $"uploaded_{DateTime.Now:yyyyMMdd_HHmmss}.jpg";
                }

                var filePath = Path.Combine(imagesDir, filename);
                File.WriteAllBytes(filePath, fileData);
                Console.WriteLine($"Successfully uploaded: {filename} ({fileData.Length} bytes)");
                
                ImagesChanged?.Invoke();

                // Send a proper redirect response
                resp.StatusCode = 302;
                resp.Headers.Add("Location", "/gallery");
                resp.ContentType = "text/html";
                var redirectHtml = "<html><head><meta http-equiv='refresh' content='0;url=/gallery'></head><body>Upload successful. <a href='/gallery'>Click here if not redirected</a></body></html>";
                var redirectBytes = Encoding.UTF8.GetBytes(redirectHtml);
                resp.ContentLength64 = redirectBytes.Length;
                resp.OutputStream.Write(redirectBytes, 0, redirectBytes.Length);
                resp.Close();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Upload error: {ex.Message}");
                try
                {
                    resp.StatusCode = 500;
                    resp.ContentType = "text/html";
                    var errorHtml = $"<html><body>Upload failed: {ex.Message}. <a href='/gallery'>Go back</a></body></html>";
                    var errorBytes = Encoding.UTF8.GetBytes(errorHtml);
                    resp.ContentLength64 = errorBytes.Length;
                    resp.OutputStream.Write(errorBytes, 0, errorBytes.Length);
                    resp.Close();
                }
                catch { }
            }
        }        private string? GetBoundary(string? contentType)
        {
            if (string.IsNullOrEmpty(contentType)) return null;
            
            var idx = contentType.IndexOf("boundary=");
            if (idx >= 0)
            {
                return "--" + contentType[(idx + 9)..];
            }
            return null;
        }private string? GetFileNameFromBytes(byte[] data, byte[] boundaryBytes)
        {
            try
            {
                // Convert only the header part to string to find filename
                var headerEnd = FindSequence(data, Encoding.UTF8.GetBytes("\r\n\r\n"));
                if (headerEnd == -1) return null;

                var headerString = Encoding.UTF8.GetString(data, 0, headerEnd);
                
                // Look for filename in Content-Disposition header
                var filenamePattern = "filename=\"";
                var idx = headerString.IndexOf(filenamePattern);
                if (idx >= 0)
                {
                    idx += filenamePattern.Length;
                    var end = headerString.IndexOf('"', idx);
                    if (end > idx)
                    {
                        return headerString.Substring(idx, end - idx);
                    }
                }

                // Alternative: look for filename*= (RFC 5987)
                filenamePattern = "filename*=UTF-8''";
                idx = headerString.IndexOf(filenamePattern);
                if (idx >= 0)
                {
                    idx += filenamePattern.Length;
                    var end = headerString.IndexOfAny(new char[] { ';', '\r', '\n' }, idx);
                    if (end == -1) end = headerString.Length;
                    if (end > idx)
                    {
                        return Uri.UnescapeDataString(headerString.Substring(idx, end - idx));
                    }
                }

                return null;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error parsing filename: {ex.Message}");
                return null;
            }
        }

        private byte[]? ExtractFileData(byte[] data, byte[] boundaryBytes)
        {
            try
            {
                // Find the start of file data (after \r\n\r\n)
                var headerEnd = FindSequence(data, Encoding.UTF8.GetBytes("\r\n\r\n"));
                if (headerEnd == -1) return null;

                var dataStart = headerEnd + 4; // Skip \r\n\r\n

                // Find the end boundary
                var endBoundary = new byte[boundaryBytes.Length + 2]; // Add \r\n before boundary
                endBoundary[0] = (byte)'\r';
                endBoundary[1] = (byte)'\n';
                Array.Copy(boundaryBytes, 0, endBoundary, 2, boundaryBytes.Length);

                var dataEnd = FindSequence(data, endBoundary, dataStart);
                if (dataEnd == -1)
                {
                    // Try without \r\n prefix (some clients might not include it)
                    dataEnd = FindSequence(data, boundaryBytes, dataStart);
                    if (dataEnd == -1) return null;
                }

                var fileDataLength = dataEnd - dataStart;
                if (fileDataLength <= 0) return null;

                var fileData = new byte[fileDataLength];
                Array.Copy(data, dataStart, fileData, 0, fileDataLength);
                return fileData;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error extracting file data: {ex.Message}");
                return null;
            }
        }

        private int FindSequence(byte[] data, byte[] sequence, int startIndex = 0)
        {
            for (int i = startIndex; i <= data.Length - sequence.Length; i++)
            {
                bool found = true;
                for (int j = 0; j < sequence.Length; j++)
                {
                    if (data[i + j] != sequence[j])
                    {
                        found = false;
                        break;
                    }
                }
                if (found) return i;
            }
            return -1;
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
