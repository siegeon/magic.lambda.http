/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
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
    public class HttpPost : ISlot, ISlotAsync
    {
        readonly IHttpClient _httpClient;

        /// <summary>
        /// Creates an instance of your class.
        /// </summary>
        /// <param name="httpClient">HTTP client to use for invocation.</param>
        public HttpPost(IHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        /// <summary>
        /// Implementation of your slot.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to your slot.</param>
        public void Signal(ISignaler signaler, Node input)
        {
            if (input.Children.Count() > 2 || input.Children.Any(x => x.Name != "token" && x.Name != "payload"))
                throw new ArgumentException("[http.post] can only handle one [token] and one [payload] child node");

            var url = input.GetEx<string>();
            var token = input.Children.FirstOrDefault(x => x.Name == "token")?.GetEx<string>();
            var payload = input.Children.FirstOrDefault(x => x.Name == "payload")?.GetEx<string>() ?? 
                throw new ArgumentException("No [payload] supplied to [http.post]");

            // Invoking endpoint, passing in payload, and returning result as value of root node.
            if (token == null)
                input.Value = _httpClient.PostAsync<string, string>(url, payload).Result;
            else
                input.Value = _httpClient.PostAsync<string, string>(url, payload, token).Result;
            input.Clear();
        }

        /// <summary>
        /// Implementation of your slot.
        /// </summary>
        /// <param name="signaler">Signaler that raised the signal.</param>
        /// <param name="input">Arguments to your slot.</param>
        /// <returns>An awaitable task.</returns>
        public async Task SignalAsync(ISignaler signaler, Node input)
        {
            if (input.Children.Count() > 2 || input.Children.Any(x => x.Name != "token" && x.Name != "payload"))
                throw new ApplicationException("[http.post] can only handle one [token] and one [payload] child node");

            var url = input.GetEx<string>();
            var token = input.Children.FirstOrDefault(x => x.Name == "token")?.GetEx<string>();
            var payload = input.Children.FirstOrDefault(x => x.Name == "payload")?.GetEx<string>() ??
                throw new ArgumentException("No [payload] supplied to [http.post]");

            // Invoking endpoint, passing in payload, and returning result as value of root node.
            if (token == null)
                input.Value = await _httpClient.PostAsync<string, string>(url, payload);
            else
                input.Value = await _httpClient.PostAsync<string, string>(url, payload, token);
            input.Clear();
        }
    }
}
