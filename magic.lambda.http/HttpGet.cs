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
    /// Invokes the HTTP GET verb towards some resource.
    /// </summary>
    [Slot(Name = "http.get")]
    [Slot(Name = "wait.http.get")]
    public class HttpGet : HttpBase
    {
        /// <summary>
        /// Creates an instance of your class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for invocation.</param>
        public HttpGet(IHttpClient httpClient)
            : base (httpClient)
        { }

        #region [ -- Overridden base class methods -- ]

        /// <inheritdoc/>
        protected async override Task Implementation(Node input)
        {
            // Sanity checking input arguments.
            Common.SanityCheckInput(input);

            // Retrieving URL and (optional) token or headers.
            var (Url, Token, Headers) = Common.GetCommonArgs(input);

            // Invoking endpoint and returning result as value of root node.
            var response = Token == null ?
                await HttpClient.GetAsync<string>(Url, Headers) :
                await HttpClient.GetAsync<string>(Url, Token);
            Common.CreateResponse(input, response);
        }

        #endregion
    }
}
