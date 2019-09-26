/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using magic.node;
using magic.http.contracts;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.lambda.http
{
    [Slot(Name = "http.put.json")]
    public class HttpPutJson : ISlot
    {
        readonly IHttpClient _httpClient;

        public HttpPutJson(IHttpClient httpClient)
        {
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        }

        public void Signal(ISignaler signaler, Node input)
        {
            if (input.Children.Count() > 2 || input.Children.Any(x => x.Name != "token" && x.Name != "payload"))
                throw new ApplicationException("[http.put.json] can only handle one [token] and one [payload] child node");

            var url = input.GetEx<string>();
            var token = input.Children.FirstOrDefault(x => x.Name == "token")?.GetEx<string>();
            var payload = input.Children.FirstOrDefault(x => x.Name == "payload")?.GetEx<string>();

            // Notice, to sanity check the result we still want to roundtrip through a JToken result.
            input.Value = _httpClient.PutAsync<string, string>(url, payload, token).Result;
            input.Clear();
        }
    }
}
