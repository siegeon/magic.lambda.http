﻿/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2021, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Net.Http;
using System.Threading.Tasks;
using magic.node;
using magic.signals.contracts;

namespace magic.lambda.http.contracts
{
    /// <summary>
    /// Interface encapsulating invocations towards HTTP endpoints.
    /// </summary>
    public interface IMagicHttp
    {
        /// <summary>
        /// Creates an HTTP invocation of the specified type, where its arguments are supplied as a Node.
        /// </summary>
        /// <param name="signaler">ISignaler implementation, needed to invoke signals to other slots</param>
        /// <param name="method">Method type fo create, can be 'GET', 'POST', 'PUT', 'DELETE' or 'PATCH'</param>
        /// <param name="input">Arguments used to decorate HTTP request and also to return data to caller</param>
        /// <returns></returns>
        Task Invoke(ISignaler signaler, HttpMethod method, Node input);
    }
}
