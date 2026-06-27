using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VRC.OSCQuery
{
    public class OSCQueryHttpServer : IDisposable
    {
        private readonly TcpListener _listener;
        private bool _shouldProcessHttp;

        // HTTP Middleware
        private List<Func<OSCQueryHttpContext, Action, Task>> _preMiddleware;
        private List<Func<OSCQueryHttpContext, Action, Task>> _middleware = new List<Func<OSCQueryHttpContext, Action, Task>>(); // constructed here to ensure it exists even if empty
        private List<Func<OSCQueryHttpContext, Action, Task>> _postMiddleware;

        private ILogger<OSCQueryService> Logger;

        private OSCQueryService _oscQuery;

        public OSCQueryHttpServer(OSCQueryService oscQueryService, ILogger<OSCQueryService> logger)
        {
            _oscQuery = oscQueryService;
            Logger = logger;

            _preMiddleware = new List<Func<OSCQueryHttpContext, Action, Task>>
            {
                HostInfoMiddleware
            };
            _postMiddleware = new List<Func<OSCQueryHttpContext, Action, Task>>
            {
                FaviconMiddleware,
                ExplorerMiddleware,
                RootNodeMiddleware
            };

            // A raw TcpListener binds in user space, so IPAddress.Any works without the http.sys
            // URL ACL that HttpListener requires for non-loopback prefixes on Windows.
            _listener = new TcpListener(_oscQuery.HostIP, _oscQuery.TcpPort);
            _listener.Start();
            _shouldProcessHttp = true;
            _ = AcceptLoop();
        }

        public void AddMiddleware(Func<OSCQueryHttpContext, Action, Task> middleware)
        {
            _middleware.Add(middleware);
        }

        private async Task AcceptLoop()
        {
            while (_shouldProcessHttp)
            {
                TcpClient client;
                try
                {
                    client = await _listener.AcceptTcpClientAsync();
                }
                catch (ObjectDisposedException)
                {
                    return; // Listener stopped during Dispose
                }
                catch (SocketException) when (!_shouldProcessHttp)
                {
                    return;
                }

                _ = HandleConnection(client);
            }
        }

        private async Task HandleConnection(TcpClient client)
        {
            try
            {
                using (client)
                using (var stream = client.GetStream())
                {
                    var request = await ReadRequest(stream);
                    if (request == null) return;

                    var context = new OSCQueryHttpContext(request);
                    await RunMiddleware(context);
                    await WriteResponse(stream, context.Response);
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Error handling HTTP connection: {e.Message}");
            }
        }

        private async Task RunMiddleware(OSCQueryHttpContext context)
        {
            foreach (var stage in new[] { _preMiddleware, _middleware, _postMiddleware })
            {
                foreach (var middleware in stage)
                {
                    var move = false;
                    await middleware(context, () => move = true);
                    if (!move) return;
                }
            }
        }

        /// <summary>
        /// Reads an HTTP request line and headers from the stream. The OSCQuery server is GET-only,
        /// so the request body is not consumed.
        /// </summary>
        private async Task<OSCQueryHttpRequest> ReadRequest(NetworkStream stream)
        {
            var headerBytes = new List<byte>(1024);
            var buffer = new byte[1];
            var matched = 0; // number of trailing CRLFCRLF bytes matched
            var terminator = new byte[] { (byte)'\r', (byte)'\n', (byte)'\r', (byte)'\n' };

            while (matched < terminator.Length)
            {
                var read = await stream.ReadAsync(buffer, 0, 1);
                if (read == 0) return null; // connection closed before a full request

                var b = buffer[0];
                headerBytes.Add(b);
                matched = b == terminator[matched] ? matched + 1 : (b == terminator[0] ? 1 : 0);

                if (headerBytes.Count > MaxHeaderBytes)
                {
                    Logger.LogWarning("HTTP request headers exceeded maximum size, dropping connection.");
                    return null;
                }
            }

            var headerText = Encoding.ASCII.GetString(headerBytes.ToArray());
            var requestLine = headerText.Substring(0, headerText.IndexOf("\r\n", StringComparison.Ordinal));
            var parts = requestLine.Split(' ');
            if (parts.Length < 2) return null;

            var method = parts[0];
            var rawUrl = parts[1];

            // Build an absolute Uri so middleware can read LocalPath and Query. The authority is
            // irrelevant to routing here, so a fixed placeholder keeps parsing simple.
            var url = new Uri(new Uri("http://localhost"), rawUrl);

            return new OSCQueryHttpRequest(method, rawUrl, url);
        }

        private async Task WriteResponse(NetworkStream stream, OSCQueryHttpResponse response)
        {
            var body = response.GetBodyBytes();

            var head = new StringBuilder();
            head.Append($"HTTP/1.1 {response.StatusCode} {ReasonPhrase(response.StatusCode)}\r\n");

            if (!string.IsNullOrEmpty(response.ContentType))
                head.Append($"Content-Type: {response.ContentType}\r\n");

            foreach (var key in response.Headers.AllKeys)
                head.Append($"{key}: {response.Headers[key]}\r\n");

            head.Append($"Content-Length: {body.Length}\r\n");
            head.Append("Connection: close\r\n");
            head.Append("\r\n");

            var headBytes = Encoding.ASCII.GetBytes(head.ToString());
            await stream.WriteAsync(headBytes, 0, headBytes.Length);
            if (body.Length > 0)
                await stream.WriteAsync(body, 0, body.Length);
            await stream.FlushAsync();
        }

        private const int MaxHeaderBytes = 8192;

        private static string ReasonPhrase(int statusCode)
        {
            switch (statusCode)
            {
                case 200: return "OK";
                case 404: return "Not Found";
                default: return "Unknown";
            }
        }

        private async Task HostInfoMiddleware(OSCQueryHttpContext context, Action next)
        {
            if (!context.Request.RawUrl.Contains(Attributes.HOST_INFO))
            {
                next();
                return;
            }

            try
            {
                // Serve Host Info for requests with "HOST_INFO" in them
                var hostInfoString = _oscQuery.HostInfo.ToString();

                // Send Response
                context.Response.Headers.Add("pragma:no-cache");

                context.Response.ContentType = "application/json";
                using (var sw = new StreamWriter(context.Response.OutputStream))
                {
                    await sw.WriteAsync(hostInfoString);
                    await sw.FlushAsync();
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not construct and send Host Info: {e.Message}");
            }
        }

        private static string _pathToResources;

        private static string PathToResources
        {
            get
            {
                if (string.IsNullOrWhiteSpace(_pathToResources))
                {
                    var dllLocation = Path.Combine(System.Reflection.Assembly.GetExecutingAssembly().Location);
                    _pathToResources = Path.Combine(new DirectoryInfo(dllLocation).Parent?.FullName ?? string.Empty, "Resources");
                }
                return _pathToResources;
            }
        }
        private async Task ExplorerMiddleware(OSCQueryHttpContext context, Action next)
        {
            if (!context.Request.Url.Query.Contains(Attributes.EXPLORER))
            {
                next();
                return;
            }

            var path = Path.Combine(PathToResources, "OSCQueryExplorer.html");
            if (!File.Exists(path))
            {
                Logger.LogError($"Cannot find file at {path} to serve.");
                next();
                return;
            }
            await Extensions.ServeStaticFile(path, "text/html", context);
        }

        private async Task FaviconMiddleware(OSCQueryHttpContext context, Action next)
        {
            if (!context.Request.RawUrl.Contains("favicon.ico"))
            {
                next();
                return;
            }

            var path = Path.Combine(PathToResources, "favicon.ico");
            if (!File.Exists(path))
            {
                Logger.LogError($"Cannot find file at {path} to serve.");
                next();
                return;
            }

            await Extensions.ServeStaticFile(path, "image/x-icon", context);
        }

        private async Task RootNodeMiddleware(OSCQueryHttpContext context, Action next)
        {
            var path = context.Request.Url.LocalPath;
            var matchedNode = _oscQuery.RootNode.GetNodeWithPath(path);
            if (matchedNode == null)
            {
                const string err = "OSC Path not found";

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                using (var sw = new StreamWriter(context.Response.OutputStream))
                {
                    await sw.WriteAsync(err);
                    await sw.FlushAsync();
                }

                return;
            }

            string stringResponse = "";
            try
            {
                stringResponse = matchedNode.ToString();
            }
            catch (Exception e)
            {
                Logger.LogError($"Could not serialize node {matchedNode.Name}: {e.Message}");
            }

            // Send Response
            context.Response.Headers.Add("pragma:no-cache");

            context.Response.ContentType = "application/json";
            using (var sw = new StreamWriter(context.Response.OutputStream))
            {
                await sw.WriteAsync(stringResponse);
                await sw.FlushAsync();
            }
        }

        public void Dispose()
        {
            _shouldProcessHttp = false;
            _listener?.Stop();
        }
    }
}
