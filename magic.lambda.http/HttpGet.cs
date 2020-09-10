/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Linq;
using System.Threading.Tasks;
using magic.node;
using magic.http.contracts;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.lambda.http
{
    /// <summary>
    /// Invokes the HTTP GET verb towards some resource.
    /// </summary>
    [Slot(Name = "http.get")]
    [Slot(Name = "wait.http.get")]
    public class HttpGet : ISlot, ISlotAsync
    {
        readonly IHttpClient _httpClient;

        /// <summary>
        /// Creates an instance of your class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for invocation.</param>
        public HttpGet(IHttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        /// <summary>
        /// Implementation of your slot.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to your slot.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            Implementation(input).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Implementation of your slot.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to your slot.</param>
        /// <returns>An awaitable task.</returns>
        public async Task SignalAsync(ISignaler signaler, Node input)
        {
            await Implementation(input);
        }

        #region [ -- Private helper methods -- ]

        async Task Implementation(Node input)
        {
            // Sanity checking input arguments.
            Common.SanityCheckInput(input);

            // Retrieving URL and (optional) token or headers.
            var args = Common.GetCommonArgs(input);

            // Invoking endpoint and returning result as value of root node.
            var response = args.Token == null ?
                await _httpClient.GetAsync<string>(args.Url, args.Headers) :
                await _httpClient.GetAsync<string>(args.Url, args.Token);
            Common.CreateResponse(input, response);
        }

        #endregion
    }
}
