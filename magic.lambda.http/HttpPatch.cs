﻿/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System.Net.Http;
using System.Threading.Tasks;
using magic.node;
using magic.signals.contracts;
using magic.lambda.http.contracts;

namespace magic.lambda.http
{
    /// <summary>
    /// Invokes the HTTP PATCH verb towards some resource.
    /// </summary>
    [Slot(Name = "http.patch")]
    public class HttpPatch : ISlot, ISlotAsync
    {
        readonly IMagicHttp _service;

        /// <summary>
        /// Creates an instance of your class.
        /// </summary>
        /// <param name="service">Actual implementation.</param>
        public HttpPatch(IMagicHttp service)
        {
            _service = service;
        }

        /// <summary>
        /// Implementation of signal
        /// </summary>
        /// <param name="signaler">Signaler used to signal</param>
        /// <param name="input">Parameters passed from signaler</param>
        public void Signal(ISignaler signaler, Node input)
        {
            _service.Invoke(signaler, new HttpMethod("PATCH"), input).GetAwaiter().GetResult();
        }

        /// <summary>
        /// Implementation of signal
        /// </summary>
        /// <param name="signaler">Signaler used to signal</param>
        /// <param name="input">Parameters passed from signaler</param>
        /// <returns>An awaiatble task.</returns>
        public async Task SignalAsync(ISignaler signaler, Node input)
        {
            await _service.Invoke(signaler, new HttpMethod("PATCH"), input);
        }
    }
}
