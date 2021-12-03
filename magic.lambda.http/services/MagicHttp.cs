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
using magic.lambda.http.services.helpers;

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
        
        // Lambda to request object converters, to semantically transform from a lambda object to an object value of some sort.
        readonly static Dictionary<string, Func<ISignaler, Node, string, object>> _requestTransformers =
            new Dictionary<string, Func<ISignaler, Node, string, object>>
            {
                {
                    "application/json", RequestTransformers.TransformToJson
                },
                {
                    "application/x-json", RequestTransformers.TransformToJson
                },
                {
                    "application/hyperlambda", RequestTransformers.TransformToHyperlambda
                },
                {
                    "application/x-hyperlambda", RequestTransformers.TransformToHyperlambda
                },
                {
                    "application/www-form-urlencoded", RequestTransformers.TransformToUrlEncoded
                },
                {
                    "application/x-www-form-urlencoded", RequestTransformers.TransformToUrlEncoded
                },
                {
                    "multipart/form-data", RequestTransformers.MultipartFormData
                },
            };
        
        // Response object to lambda converters, to semantically transform from a response object to a lambda object.
        readonly static Dictionary<string, Func<ISignaler, HttpContent, Task<Node>>> _responseTransformers =
            new Dictionary<string, Func<ISignaler, HttpContent, Task<Node>>>
        {
            {
                "application/json", ResponseTransformers.TransformFromJson
            },
            {
                "application/x-json", ResponseTransformers.TransformFromJson
            },
            {
                "application/hyperlambda", ResponseTransformers.TransformFromHyperlambda
            },
            {
                "application/x-hyperlambda", ResponseTransformers.TransformFromHyperlambda
            },
            {
                "application/www-form-urlencoded", ResponseTransformers.TransformFromUrlEncoded
            },
            {
                "application/x-www-form-urlencoded", ResponseTransformers.TransformFromUrlEncoded
            },
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
        public async Task Invoke(
            ISignaler signaler,
            HttpMethod method,
            Node input)
        {
            // Sanitcy checking invocation.
            if (input.Children.Any(x =>
                x.Name != "payload" &&
                x.Name != "filename" &&
                x.Name != "headers" &&
                x.Name != "token" &&
                x.Name != "convert"))
                throw new HyperlambdaException($"Only supply [payload], [filename], [headers], [convert] and [token] to [{input.Name}]");

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
                            throw new HyperlambdaException($"Do not supply a [payload] or [filename] argument to [{input.Name}]");

                        using (var response = await _client.SendAsync(request))
                        {
                            await GetResponse(signaler, response, input);
                        }
                        break;

                    default:

                        // Content request, implying 'PUT', 'POST' or 'DELETE' request.
                        using (var content = GetRequestContent(signaler, input, headers))
                        {
                            AddContentHeaders(content, headers);
                            request.Content = content;
                            using (var response = await _client.SendAsync(request))
                            {
                                await GetResponse(signaler, response, input);
                            }
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// Registers a semantic request Content-Type handler allowing you to transform from a lambda object
        /// to some sort of request content object automatically.
        /// </summary>
        /// <param name="contentType">Content-Type to handle</param>
        /// <param name="functor">Function to create a request object for Content-Type</param>
        public static void AddRequestHandler(string contentType, Func<ISignaler, Node, string, object> functor)
        {
            _requestTransformers[contentType] = functor;
        }

        /// <summary>
        /// Registers a semantic response Content-Type handler allowing you to transform from a content object
        /// to a lambda object automatically.
        /// </summary>
        /// <param name="contentType">Content-Type to handle</param>
        /// <param name="functor">Function to create a lambda object for Content-Type</param>
        public static void AddResponseHandler(string contentType, Func<ISignaler, HttpContent, Task<Node>> functor)
        {
            _responseTransformers[contentType] = functor;
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
                            throw new HyperlambdaException($"You cannot decorate an HTTP GET or DELETE request with content type of headers");
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
        static HttpContent GetRequestContent(
            ISignaler signaler,
            Node input,
            Dictionary<string, string> headers)
        {
            // Buffer to hold content
            object content = null;

            // Using [payload] node if available.
            var payloadNode = input.Children.FirstOrDefault(x => x.Name == "payload");

            // Prioritising [payload] argument.
            if (payloadNode != null)
                content = GetRequestContentContent(signaler, input, payloadNode, headers);
            else
                content = GetRequestFileContent(signaler, input);

            // Making sure we support our 3 primary content types.
            if (content is Stream stream)
                return new StreamContent(stream);
            if (content is byte[] bytes)
                return new ByteArrayContent(bytes);
            return new StringContent(content as string);
        }

        /*
         * Creates an HTTP content object from some sort of inline, and/or stream content object.
         */
        static object GetRequestContentContent(
            ISignaler signaler,
            Node input,
            Node payloadNode,
            Dictionary<string, string> headers)
        {
            // Checking if caller supplied a [payload] value or not, alternatives are structured lambda object.
            if (payloadNode.Value == null)
            {
                // Verifying user supplied some sort of structured lambda object type of content.
                if (!payloadNode.Children.Any())
                    throw new HyperlambdaException($"No [payload] value or children supplied to [{input.Name}]");

                // Figuring out Content-Type of request payload to make sure we correctly transform into the specified value.
                var contentType = headers.ContainsKey("Content-Type") ?
                    headers["Content-Type"]?.Split(';').First().Trim() ?? "application/json" :
                    "application/json";

                if (_requestTransformers.TryGetValue(contentType, out var functor))
                {
                    // Invoking function responsible for creating payload.
                    var result = functor(signaler, payloadNode, input.Name);

                    // Checking if some sort of "structured" result was returned from request transformer.
                    if (result is Node nodeResult)
                    {
                        var contentNode = nodeResult.Children.First(x => x.Name == "content");
                        contentNode.UnTie();
                        foreach (var idx in contentNode.Children)
                        {
                            headers[idx.Name] = idx.Get<string>();
                        }
                        return contentNode.Value;
                    }
                    return result;
                }

                // No transformer for specified Content-Type exists.
                throw new HyperlambdaException($"I don't know how to transform a lambda object to Content-Type of '{contentType}'");
            }
            else
            {
                // [payload] contains a value object of some sort.
                return payloadNode?.GetEx<object>() ??
                    throw new HyperlambdaException($"No [payload] value supplied to [{input.Name}]");
            }
        }

        /*
         * Creates an HTTP content object wrapping a file.
         */
        static object GetRequestFileContent(ISignaler signaler, Node input)
        {
            // If no [content] was given we check if caller supplied a [filename] argument.
            var filename = input.Children.FirstOrDefault(x => x.Name == "filename")?.GetEx<string>();
            if (filename == null)
                throw new HyperlambdaException($"No [payload] or [filename] argument supplied to [{input.Name}]");

            // Caller supplied a [filename] argument, hence using it as a stream content object.
            var rootFolderNode = new Node();
            signaler.Signal(".io.folder.root", rootFolderNode);
            var fullpath = rootFolderNode.Get<string>().TrimEnd('/') + "/" + filename.TrimStart('/');
            if (File.Exists(fullpath))
                return File.OpenRead(fullpath);

            // File doesn't exist.
            throw new HyperlambdaException($"File supplied as [filename] argument to [{input.Name}] doesn't exist");
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
        static async Task GetResponse(
            ISignaler signaler,
            HttpResponseMessage response,
            Node result)
        {
            // Checking if caller wants to automatically convert result to lambda.
            var convert = result.Children.FirstOrDefault(x => x.Name == "convert")?.GetEx<bool>() ?? false;

            // House cleaning.
            result.Clear();

            // Status code.
            result.Value = (int)response.StatusCode;

            // Ensuring we clean up after ourselves.
            using (var content = response.Content)
            {
                // HTTP content, defaulting Content-Type to 'application/json' unless explicitly overridden by endpoint.
                var contentType = "application/json";

                // HTTP headers.
                if (response.Headers.Any() || content.Headers.Any())
                {
                    var headers = new Node("headers");
                    foreach (var idx in response.Headers)
                    {
                        headers.Add(new Node(idx.Key, string.Join(", ", idx.Value)));
                    }
                    foreach (var idx in content.Headers)
                    {
                        if (idx.Key.ToLowerInvariant() == "content-type")
                            contentType = idx.Value.First().Split(';').First();
                        headers.Add(new Node(idx.Key, string.Join(", ", idx.Value)));
                    }
                    result.Add(headers);
                }

                // Checking if caller wants to automatically convert and if we've got response converters registered for Content-Type.
                if (convert && _responseTransformers.TryGetValue(contentType, out var functor))
                {
                    result.Add(await functor(signaler, content));
                }
                else
                {
                    // Checking if this MIME type starts with "text/" something, at which point we treat it as text.
                    if (contentType.StartsWith("text/"))
                        result.Add(new Node("content", await content.ReadAsStringAsync()));
                    else
                        result.Add(new Node("content", await content.ReadAsByteArrayAsync()));
                }
            }
        }

        #endregion
    }
}
