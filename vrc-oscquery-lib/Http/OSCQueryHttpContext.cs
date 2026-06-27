using System;
using System.IO;
using System.Net;

namespace VRC.OSCQuery
{
    /// <summary>
    /// Transport-agnostic HTTP context passed through the middleware chain. Replaces the
    /// <see cref="HttpListenerContext"/> previously used so the server can run on a raw socket,
    /// which binds to any interface without the http.sys URL ACL that HttpListener requires.
    /// </summary>
    public class OSCQueryHttpContext
    {
        public OSCQueryHttpRequest Request { get; }
        public OSCQueryHttpResponse Response { get; }

        public OSCQueryHttpContext(OSCQueryHttpRequest request)
        {
            Request = request;
            Response = new OSCQueryHttpResponse();
        }
    }

    public class OSCQueryHttpRequest
    {
        /// <summary>Raw request target as sent on the request line, e.g. "/foo?explorer".</summary>
        public string RawUrl { get; }

        /// <summary>Parsed absolute URL, exposing <see cref="Uri.LocalPath"/> and <see cref="Uri.Query"/>.</summary>
        public Uri Url { get; }

        public string HttpMethod { get; }

        public OSCQueryHttpRequest(string httpMethod, string rawUrl, Uri url)
        {
            HttpMethod = httpMethod;
            RawUrl = rawUrl;
            Url = url;
        }
    }

    public class OSCQueryHttpResponse
    {
        public WebHeaderCollection Headers { get; } = new WebHeaderCollection();
        public string ContentType { get; set; }

        /// <summary>
        /// Retained for API compatibility with the previous HttpListener-based context.
        /// The actual Content-Length sent is derived from the bytes written to <see cref="OutputStream"/>.
        /// </summary>
        public long ContentLength64 { get; set; }

        public int StatusCode { get; set; } = 200;

        /// <summary>
        /// Buffer that middleware writes the response body into. Buffering lets us send an accurate
        /// Content-Length and ignores Dispose so middleware using `using (new StreamWriter(...))` does
        /// not tear down the buffer before the server reads it.
        /// </summary>
        public Stream OutputStream => _body;

        private readonly BodyStream _body = new BodyStream();

        public byte[] GetBodyBytes() => _body.ToArray();

        private sealed class BodyStream : MemoryStream
        {
            protected override void Dispose(bool disposing)
            {
                // No-op: the server owns this buffer's lifecycle and reads it after the
                // middleware chain completes, including middleware that disposes its writer.
            }
        }
    }
}
