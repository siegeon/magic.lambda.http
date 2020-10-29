/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Threading.Tasks;
using magic.node;
using magic.http.contracts;
using magic.signals.contracts;
using magic.lambda.http.helpers;

namespace magic.lambda.http
{
    /// <summary>
    /// Invokes the HTTP PATCH verb towards some resource.
    /// </summary>
    [Slot(Name = "http.patch")]
    public class HttpPatch : HttpBase
    {
        /// <summary>
        /// Creates an instance of your class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for invocation.</param>
        public HttpPatch(IHttpClient httpClient)
            : base (httpClient)
        { }

        #region [ -- Overridden base class methods -- ]

        /// <inheritdoc/>
        protected async override Task Implementation(Node input)
        {
            // Sanity checking input arguments.
            Common.SanityCheckInput(input, true);

            // Retrieving URL and (optional) token or headers.
            var (Url, Token, Headers) = Common.GetCommonArgs(input);
            var payload = Common.GetPayload<object>(input);

            // Invoking endpoint, passing in payload, and returning result as value of root node.
            var response = Token == null ?
                await HttpClient.PatchAsync<object, object>(Url, payload, Headers) :
                await HttpClient.PatchAsync<object, object>(Url, payload, Token);
            Common.CreateResponse(input, response);
        }

        #endregion
    }
}
