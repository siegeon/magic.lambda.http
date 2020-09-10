/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using magic.node;
using magic.http.contracts;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.lambda.http
{
    /// <summary>
    /// Invokes the HTTP POST verb towards some resource.
    /// </summary>
    [Slot(Name = "http.post")]
    [Slot(Name = "wait.http.post")]
    public class HttpPost : ISlot, ISlotAsync
    {
        readonly IHttpClient _httpClient;

        /// <summary>
        /// Creates an instance of your class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for invocation.</param>
        public HttpPost(IHttpClient httpClient)
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
            Common.SanityCheckInput(input, true);

            // Retrieving URL and (optional) token or headers.
            var args = Common.GetCommonArgs(input);
            var payload = input.Children.FirstOrDefault(x => x.Name == "payload")?.GetEx<string>() ??
                throw new ArgumentException("No [payload] supplied to [http.post]");

            // Invoking endpoint, passing in payload, and returning result as value of root node.
            var response = args.Token == null ?
                await _httpClient.PostAsync<string, string>(args.Url, payload, args.Headers) :
                await _httpClient.PostAsync<string, string>(args.Url, payload, args.Token);
            Common.CreateResponse(input, response);
        }

        #endregion
    }
}
