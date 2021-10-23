﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Net.Http;
using System.Threading.Tasks;
using magic.node;
using magic.signals.contracts;

namespace magic.lambda.http
{
    /// <summary>
    /// Invokes the HTTP DELETE verb towards some resource.
    /// </summary>
    [Slot(Name = "http.delete")]
    public class HttpDelete : ISlot, ISlotAsync
    {
        readonly IHttpClientFactory _factory;

        /// <summary>
        /// Creates an instance of your class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for invocation.</param>
        public HttpDelete(IHttpClientFactory factory)
        {
            _factory = factory;
        }

        /// <summary>
        /// Implementation of signal
        /// </summary>
        /// <param name="signaler">Signaler used to signal</param>
        /// <param name="input">Parameters passed from signaler</param>
        public void Signal(ISignaler signaler, Node input)
        {
            HttpWrapper.Invoke(signaler, _factory, HttpMethod.Delete, input).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Implementation of signal
        /// </summary>
        /// <param name="signaler">Signaler used to signal</param>
        /// <param name="input">Parameters passed from signaler</param>
        /// <returns>An awaiatble task.</returns>
        public async Task SignalAsync(ISignaler signaler, Node input)
        {
            await HttpWrapper.Invoke(signaler, _factory, HttpMethod.Delete, input);
        }
    }
}
