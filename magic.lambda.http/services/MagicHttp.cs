/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Collections.Generic;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;
using magic.lambda.http.contracts;

namespace magic.lambda.http.services
{
    /// <summary>
    /// Class encapsulating the invocation of a single HTTP request from a declaration
    /// specified as a Node object.
    /// </summary>
    public class MagicHttp : IMagicHttp
    {
        // Default HTTP headers for an empty HTTP request.
        static readonly Dictionary<string, string> DEFAULT_HEADERS_EMPTY_REQUEST =
            new Dictionary<string, string> {
                { "Accept", "application/json" },
            };

        // Default HTTP headers for an HTTP request with a payload.
        static readonly Dictionary<string, string> DEFAULT_HEADERS_REQUEST =
            new Dictionary<string, string> {
                { "Content-Type", "application/json" },
                { "Accept", "application/json" },
            };
        
        // Dependency injected HttpClient instance.
        readonly HttpClient _client;

        /// <summary>
        /// Creates an instance of your type.
        /// </summary>
        /// <param name="client">Actual HttpClient implementation</param>
        public MagicHttp(HttpClient client)
        {
            _client = client;
        }

        /// <inheritdoc />
        public async Task Invoke(ISignaler signaler, HttpMethod method, Node input)
        {
            // Sanitcy checking invocation.
            if (input.Children.Any(x =>
                x.Name != "payload" &&
                x.Name != "filename" &&
                x.Name != "headers" &&
                x.Name != "token"))
                throw new ArgumentException($"Only supply [payload], [filename], [headers] and [token] to [{input.Name}]");

            /*
             * Strictly speaking it should not be necessary to dispose client,
             * since it's created using IHttpClientFactory - However, to make sure
             * we keep with the standard of the language, we do it anyways.
             */
            using (var request = CreateRequestMessage(method, input, out var headers))
            {
                switch (method.Method.ToLowerInvariant())
                {
                    case "get":
                    case "delete":

                        // Empty request, sanity checking invocation, making sure no [payload] was provided.
                        if (input.Children.Any(x => x.Name == "payload" || x.Name == "filename"))
                            throw new ArgumentException($"Do not supply a [payload] or [filename] argument to [{input.Name}]");
                        using (var response = await _client.SendAsync(request))
                        {
                            await GetResponse(response, input);
                        }
                        break;

                    default:

                        // Content request, implying 'PUT', 'POST' or 'DELETE' request.
                        using (var content = GetRequestContent(signaler, input))
                        {
                            AddContentHeaders(content, headers);
                            request.Content = content;
                            using (var response = await _client.SendAsync(request))
                            {
                                await GetResponse(response, input);
                            }
                        }
                        break;
                }
            }
        }

        #region [ -- Private helper methods -- ]

        /*
         * Creates an HTTP request message and returns to caller, correctly decorating
         * the HTTP headers of the message.
         */
        static HttpRequestMessage CreateRequestMessage(
            HttpMethod method,
            Node input,
            out Dictionary<string, string> headers)
        {
            // Retrieving arguments to invocation.
            var url = input.GetEx<string>();
            headers = input.Children
                .FirstOrDefault(x => x.Name == "headers")?
                .Children
                .ToDictionary(lhs => lhs.Name, rhs => rhs.GetEx<string>()) ?? new Dictionary<string, string>();

            // Applying default headers if no [headers] argument was supplied.
            if (headers.Count == 0)
            {
                switch (method.Method.ToLowerInvariant())
                {
                    case "get":
                    case "delete":
                        foreach (var idx in DEFAULT_HEADERS_EMPTY_REQUEST)
                        {
                            headers[idx.Key] = idx.Value;
                        }
                        break;

                    default:
                        foreach (var idx in DEFAULT_HEADERS_REQUEST)
                        {
                            headers[idx.Key] = idx.Value;
                        }
                        break;
                }
            }

            // Applying Bearer token if specified.
            var token = input.Children.FirstOrDefault(x => x.Name == "token")?.GetEx<string>();
            if (token != null)
                headers["Authorization"] = $"Bearer {token}";

            // Creating our request message.
            var result = new HttpRequestMessage(method, new Uri(url));

            // Associating each non-content HTTP header with the request message.
            foreach (var idx in headers.Keys)
            {
                switch (idx)
                {
                    case "Allow":
                    case "Content-Disposition":
                    case "Content-Encoding":
                    case "Content-Language":
                    case "Content-Length":
                    case "Content-Location":
                    case "Content-MD5":
                    case "Content-Range":
                    case "Content-Type":
                    case "Expires":
                    case "Last-Modified":

                        // These HTTP headers we simply ignore, since they're added to the content object later.
                        // However, if such headers are added to GET or DELETE invocation, it's considered a bug.
                        if (method.Method.ToLowerInvariant() == "get" || method.Method.ToLowerInvariant() == "delete")
                            throw new ArgumentException($"You cannot decorate an HTTP GET or DELETE request with content type of headers");
                        break;

                    default:

                        // These are the only HTTP headers we're interested in adding to the request message itself.
                        result.Headers.Add(idx, headers[idx]);
                        break;
                }
            }
            return result;
        }

        /*
         * Creates an HTTP content object for HTTP invocations requiring such things (POST, PUT and PATCH).
         */
        static HttpContent GetRequestContent(ISignaler signaler, Node input)
        {
            // Buffer to hold content
            object content = null;

            // Using [payload] node if available.
            var payloadNode = input.Children.FirstOrDefault(x => x.Name == "payload");

            // Prioritising [content] node.
            if (payloadNode == null)
            {
                // If no [content] was given we check if caller supplied a [filename] argument.
                var filename = input.Children.FirstOrDefault(x => x.Name == "filename")?.GetEx<string>();
                if (filename == null)
                    throw new ArgumentException($"No [payload] or [filename] argument supplied to [{input.Name}]");

                // Caller supplied a [filename] argument, hence using it as a stream content object.
                var rootFolderNode = new Node();
                signaler.Signal(".io.folder.root", rootFolderNode);
                var fullpath = rootFolderNode.Get<string>().TrimEnd('/') + "/" + filename.TrimStart('/');
                if (File.Exists(fullpath))
                    content = File.OpenRead(fullpath);
                else
                    throw new ArgumentException($"File supplied as [filename] argument to [{input.Name}] doesn't exist");
            }
            else
            {
                if (payloadNode.Value == null)
                {
                    /*
                     * Checking if caller provided children nodes to [payload],
                     * at which point we automatically transform from Lambda to JSON.
                     */
                    if (!payloadNode.Children.Any())
                        throw new ArgumentException($"No [payload] value supplied to [{input.Name}]");

                    /*
                     * Automatically [unerap]'ing all nodes, since there are no reasons why you'd want
                     * to pass in an expression as JSON to any endpoints.
                     */
                    foreach (var idx in payloadNode.Children)
                    {
                        Unwrap(idx, input.Name);
                    }

                    // Using JSON slots to transform nodes to JSON.
                    signaler.Signal("lambda2json", payloadNode);
                    content = payloadNode.Get<object>();
                }
                else
                {
                    // [payload] is either JSON or an expression leading to JSON.
                    content = payloadNode?.GetEx<object>();
                    if (content == null)
                        throw new ArgumentException($"No [payload] value supplied to [{input.Name}]");
                }
            }

            // Making sure we support our 3 primary content types.
            if (content is Stream stream)
                return new StreamContent(stream);
            if (content is byte[] bytes)
                return new ByteArrayContent(bytes);
            return new StringContent(content as string);
        }

        /*
         * Helper method to [unwrap] all nodes passed in as a lambda object.
         */
        static void Unwrap(Node node, string slotName)
        {
            if (node.Value is Expression)
            {
                var exp = node.Evaluate();
                if (exp.Count() > 1)
                    throw new ArgumentException($"Multiple sources found for node in lambda object supplied to [{slotName}]");
                node.Value = exp.FirstOrDefault()?.Value;
            }
            foreach (var idx in node.Children)
            {
                Unwrap(idx, slotName);
            }
        }

        /*
         * Adds the HTTP content headers to the specified content object (POST, PUT and PATCH).
         */
        static void AddContentHeaders(HttpContent content, Dictionary<string, string> headers)
        {
            // Associating each content HTTP header with the content.
            foreach (var idx in headers.Keys)
            {
                switch (idx)
                {
                    case "Allow":
                    case "Content-Disposition":
                    case "Content-Encoding":
                    case "Content-Language":
                    case "Content-Length":
                    case "Content-Location":
                    case "Content-MD5":
                    case "Content-Range":
                    case "Content-Type":
                    case "Expires":
                    case "Last-Modified":

                        // These are the only HTTP headers relevant to our HTTP content object.
                        if (content.Headers.Contains(idx))
                            content.Headers.Remove(idx);
                        content.Headers.Add(idx, headers[idx]);
                        break;
                }
            }
        }

        /*
         * Extracts the response content from the specified response message, and
         * puts it into the specified node.
         */
        static async Task GetResponse(HttpResponseMessage response, Node result)
        {
            // House cleaning.
            result.Clear();

            // Status code.
            result.Value = (int)response.StatusCode;

            // Ensuring we clean up after ourselves.
            using (var content = response.Content)
            {
                // HTTP headers.
                if (response.Headers.Any() || content.Headers.Any())
                {
                    var headers = new Node("headers");
                    foreach (var idx in response.Headers)
                    {
                        headers.Add(new Node(idx.Key, string.Join(";", idx.Value)));
                    }
                    foreach (var idx in content.Headers)
                    {
                        headers.Add(new Node(idx.Key, string.Join(";", idx.Value)));
                    }
                    result.Add(headers);
                }

                // HTTP content, defaulting Content-Type to 'application/json'.
                var contentType = "application/json";
                if (content.Headers.Any(x => x.Key.ToLowerInvariant() == "content-type"))
                {
                    var rawHeader = content.Headers.First(x => x.Key.ToLowerInvariant() == "content-type");
                    contentType = rawHeader.Value.First()?.Split(';')?.FirstOrDefault() ?? "application/json";
                }
                switch (contentType)
                {
                    // Common text types of MIME types.
                    case "application/json":
                    case "application/x-www-form-urlencoded":
                    case "application/x-hyperlambda":
                    case "application/rss+xml":
                    case "application/xml":
                        result.Add(new Node("content", await content.ReadAsStringAsync()));
                        break;

                    // Anything but the above.
                    default:

                        // Checking if this MIME type starts with "text/" something, at which point we treat it as text.
                        if (contentType.StartsWith("text/"))
                            result.Add(new Node("content", await content.ReadAsStringAsync()));
                        else
                            result.Add(new Node("content", await content.ReadAsByteArrayAsync()));
                        break;
                }
            }
        }

        #endregion
    }
}
