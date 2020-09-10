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
    /// Invokes the HTTP PUT verb towards some resource.
    /// </summary>
    [Slot(Name = "http.put")]
    [Slot(Name = "wait.http.put")]
    public class HttpPut : HttpBase
    {
        /// <summary>
        /// Creates a new instance of your class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for invocation.</param>
        public HttpPut(IHttpClient httpClient)
            : base (httpClient)
        { }

        #region [ -- Private helper methods -- ]

        protected async override Task Implementation(Node input)
        {
            // Sanity checking input arguments.
            Common.SanityCheckInput(input, true);

            // Retrieving URL and (optional) token or headers.
            var (Url, Token, Headers) = Common.GetCommonArgs(input);
            var payload = Common.GetPayload(input);

            // Invoking endpoint, passing in payload, and returning result as value of root node.
            var response = Token == null ?
                await HttpClient.PutAsync<string, string>(Url, payload, Headers) :
                await HttpClient.PutAsync<string, string>(Url, payload, Token);
            Common.CreateResponse(input, response);
        }

        #endregion
    }
}
