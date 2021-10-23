/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
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

namespace magic.lambda.http
{
    /*
     * Class encapsulating the invocation of a single HTTP request from a declaration
     * specified as a Node object.
     */
    internal static class HttpWrapper
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

        /*
         * Invoke the specified HTTP request.
         */
        public static async Task Invoke(
            ISignaler signaler,
            IHttpClientFactory factory, 
            HttpMethod method,
            Node input)
        {
            // Sanitcy checking invocation.
            if (input.Children.Any(x => x.Name != "payload" && x.Name != "filename" && x.Name != "headers" && x.Name != "token"))
                throw new ArgumentException($"Only supply [payload], [filename], [headers] and [token] to [{input.Name}]");

            /*
             * Strictly speaking it should not be necessary to dispose client,
             * since it's created using IHttpClientFactory - However, to make sure
             * we keep with the standard of the language, we do it anyways.
             */
            using (var client = factory.CreateClient())
            {
                using (var request = CreateRequestMessage(method, input, out var headers))
                {
                    switch (method.Method.ToLowerInvariant())
                    {
                        case "get":
                        case "delete":

                            // Sanity checking invocation.
                            if (input.Children.Any(x => x.Name == "payload"))
                                throw new ArgumentException($"Do not supply a [payload] argument to [{input.Name}]");

                            // Empty request.
                            using (var response = await client.SendAsync(request))
                            {
                                await GetResponse(response, input);
                            }
                            break;

                        default:

                            // Content request.
                            using (var content = GetRequestContent(signaler, input))
                            {
                                AddContentHeaders(content, headers);
                                request.Content = content;
                                using (var response = await client.SendAsync(request))
                                {
                                    await GetResponse(response, input);
                                }
                            }
                            break;
                    }
                }
            }
        }

        #region [ -- Private helper methods -- ]

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

                // HTTP content.
                var contentType = "application/json";
                if (content.Headers.Any(x => x.Key.ToLowerInvariant() == "content-type"))
                {
                    var rawHeader = content.Headers.First(x => x.Key.ToLowerInvariant() == "content-type");
                    contentType = rawHeader.Value.First()?.Split(';').FirstOrDefault() ?? "application/json";
                }
                switch (contentType)
                {
                    case "application/json":
                    case "application/x-www-form-urlencoded":
                    case "application/x-hyperlambda":
                        result.Add(new Node("content", await content.ReadAsStringAsync()));
                        break;

                    default:
                        if (contentType.StartsWith("text/"))
                            result.Add(new Node("content", await content.ReadAsStringAsync()));
                        else
                            result.Add(new Node("content", await content.ReadAsByteArrayAsync()));
                        break;
                }
            }
        }

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
                .Children.ToDictionary(lhs => lhs.Name, rhs => rhs.GetEx<string>());

            // Applying default headers if no [headers] argument was supplied.
            if (headers == null)
            {
                switch (method.Method.ToLowerInvariant())
                {
                    case "get":
                    case "delete":
                        headers = DEFAULT_HEADERS_EMPTY_REQUEST;
                        break;

                    default:
                        headers = DEFAULT_HEADERS_REQUEST;
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
            // Using string/byte[] content if available.
            var content = input.Children.FirstOrDefault(x => x.Name == "payload")?.GetEx<object>();

            // If no [content] was given we check if caller supplied a [filename] argument.
            var file = input.Children.FirstOrDefault(x => x.Name == "filename")?.GetEx<string>();
            if (content == null && file != null)
            {
                // Caller supplied a [filename] argument, hence using it as a stream content object.
                var rootNode = new Node();
                signaler.Signal(".io.folder.root", rootNode);
                var fullpath = rootNode.Get<string>().TrimEnd('/') + "/" + file.TrimStart('/');
                if (File.Exists(fullpath))
                    content = File.OpenRead(fullpath);
            }

            // Sanity checking invocation.
            if (content == null)
                throw new ArgumentException($"No [payload] argument supplied to [{input.Name}]");

            // Making sure we support our 3 primary content types.
            if (content is Stream stream)
                return new StreamContent(stream);
            if (content is byte[] bytes)
                return new ByteArrayContent(bytes);
            return new StringContent(content as string);
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

        #endregion
    }
}
