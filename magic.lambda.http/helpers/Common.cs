/*
 * Magic, Copyright(c) Thomas Hansen 2019 - 2020, thomas@servergardens.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using magic.node;
using magic.http.contracts;
using magic.node.extensions;

namespace magic.lambda.http.helpers
{
    /*
     * Helper class to create some sort of Node result out of an HTTP response.
     */
    internal static class Common
    {
        /*
         * Creates a common lambda structure out of an HTTP Response, adding
         * the HTTP headers, status code, and content into the lambda structure.
         */
        internal static void CreateResponse<T>(Node input, Response<T> response)
        {
            input.Clear();
            input.Value = (int)response.Status;
            input.Add(new Node("headers", null, response.Headers.Select(x => new Node(x.Key, x.Value))));
            input.Add(new Node("content", response.Content));
        }

        /*
         * Sanity checks input arguments to verify no unsupported arguments are given
         * for an HTTP REST invocation.
         */
        internal static void SanityCheckInput(Node input, bool allowPayload = false)
        {
            if (allowPayload)
            {
                if (input.Children.Count() > 2 || input.Children.Any(x => x.Name != "token" && x.Name != "headers" && x.Name != "payload"))
                    throw new ArgumentException("[http.xxx] can only handle one [token] or alternatively [headers] child node in addition to a [payload] argument");
            }
            else
            {
                if (input.Children.Count() > 1 || input.Children.Any(x => x.Name != "token" && x.Name != "headers"))
                    throw new ArgumentException("[http.xxx] can only handle one [token] or alternatively [headers] child node");
            }
        }

        /*
         * Retrieves common arguments to invocation.
         */
        internal static (string Url, string Token, Dictionary<string, string> Headers) GetCommonArgs(Node input)
        {
            var url = input.GetEx<string>();
            var token = input.Children.FirstOrDefault(x => x.Name == "token")?.GetEx<string>();
            var headers = input.Children.FirstOrDefault(x => x.Name == "headers")?.Children
                .ToDictionary(x1 => x1.Name, x2 => x2.GetEx<string>());
            return (url, token, headers);
        }

        /*
         * Returns payload to caller as string.
         */
        internal static string GetPayload(Node input)
        {
            return input.Children.FirstOrDefault(x => x.Name == "payload")?.GetEx<string>() ??
                throw new ArgumentException("No [payload] supplied to [http.xxx]");
        }

        /*
         * Returns payload to caller as bytes.
         */
        internal static byte[] GetBytesPayload(Node input)
        {
            var result = input.Children.FirstOrDefault(x => x.Name == "payload")?.GetEx<object>() ??
                throw new ArgumentException("No [payload] supplied to [http.xxx]");
            if (result is byte[] resultBytes)
                return resultBytes;
            else
                return Encoding.UTF8.GetBytes(result as string);
        }
    }
}
