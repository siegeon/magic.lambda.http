/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Threading.Tasks;
using magic.node;
using magic.http.contracts;
using magic.signals.contracts;

namespace magic.lambda.http
{
    /// <summary>
    /// Generic base class implementing the actual slot implemntations.
    /// </summary>
    public abstract class HttpBase : ISlot, ISlotAsync
    {
        /// <summary>
        /// Creates a new instance of your class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for invocation.</param>
        public HttpBase(IHttpClient httpClient)
        {
            HttpClient = httpClient;
        }

        /// <summary>
        /// Returns HTTP client to derived class.
        /// </summary>
        protected IHttpClient HttpClient { get; }

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

        protected abstract Task Implementation(Node input);
    }
}
