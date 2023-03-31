using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace VRC.OSCQuery
{
    public class OSCQueryHttpServer : IDisposable
    {
        private HttpListener _listener;
        private bool _shouldProcessHttp;
        
        // HTTP Middleware
        private List<Func<HttpListenerContext, Action, Task>> _preMiddleware;
        private List<Func<HttpListenerContext, Action, Task>> _middleware = new List<Func<HttpListenerContext, Action, Task>>(); // constructed here to ensure it exists even if empty
        private List<Func<HttpListenerContext, Action, Task>> _postMiddleware;

        private ILogger<OSCQueryService> Logger;

        private OSCQueryService _oscQuery;
        
        public OSCQueryHttpServer(OSCQueryService oscQueryService, ILogger<OSCQueryService> logger)
        {
            _oscQuery = oscQueryService;
            Logger = logger;
            
            // Create and start HTTPListener
            _listener = new HttpListener();

            string prefix = $"http://{_oscQuery.HostIP}:{_oscQuery.TcpPort}/";
            _listener.Prefixes.Add(prefix);
            _preMiddleware = new List<Func<HttpListenerContext, Action, Task>>
            {
                HostInfoMiddleware
            };
            _postMiddleware = new List<Func<HttpListenerContext, Action, Task>>
            {
                FaviconMiddleware,
                ExplorerMiddleware,
                RootNodeMiddleware
            };
            _listener.Start();
            _listener.BeginGetContext(HttpListenerLoop, _listener);
            _shouldProcessHttp = true;
        }
        
        public void AddMiddleware(Func<HttpListenerContext, Action, Task> middleware)
        {
            _middleware.Add(middleware);
        }
        
        /// <summary>
        /// Process and responds to incoming HTTP queries
        /// </summary>
        private void HttpListenerLoop(IAsyncResult result)
        {
            if (!_shouldProcessHttp) return;
            
            var context = _listener.EndGetContext(result);
            _listener.BeginGetContext(HttpListenerLoop, _listener);
            Task.Run(async () =>
            {
                // Pre middleware
                foreach (var middleware in _preMiddleware)
                {
                    var move = false;
                    await middleware(context, () => move = true);
                    if (!move) return;
                }
                
                // User middleware
                foreach (var middleware in _middleware)
                {
                    var move = false;
                    await middleware(context, () => move = true);
                    if (!move) return;
                }
                
                // Post middleware
                foreach (var middleware in _postMiddleware)
                {
                    var move = false;
                    await middleware(context, () => move = true);
                    if (!move) return;
                }
            }).ConfigureAwait(false);
        }

        private async Task HostInfoMiddleware(HttpListenerContext context, Action next)
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
                context.Response.ContentLength64 = hostInfoString.Length;
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
        private async Task ExplorerMiddleware(HttpListenerContext context, Action next)
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

        private async Task FaviconMiddleware(HttpListenerContext context, Action next)
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

        private async Task RootNodeMiddleware(HttpListenerContext context, Action next)
        {
            var path = context.Request.Url.LocalPath;
            var matchedNode = _oscQuery.RootNode.GetNodeWithPath(path);
            if (matchedNode == null)
            {
                const string err = "OSC Path not found";

                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                context.Response.ContentLength64 = err.Length;
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
            context.Response.ContentLength64 = stringResponse.Length;
            using (var sw = new StreamWriter(context.Response.OutputStream))
            {
                await sw.WriteAsync(stringResponse);
                await sw.FlushAsync();
            }
        }

        public void Dispose()
        {
            _shouldProcessHttp = false;
            // HttpListener teardown
            if (_listener != null)
            {
                if (_listener.IsListening)
                    _listener.Stop();
                
                _listener.Close();
            }

        }
    }
}