/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@gaiasoul.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System.Linq;
using magic.node;
using magic.http.contracts;

namespace magic.lambda.http
{
    /*
     * Helper class to create some sort of Node result out of an HTTP response.
     */
    internal static class Common
    {
        public static void CreateResponse(Node input, Response<string> response)
        {
            input.Clear();
            input.Value = (int)response.Status;
            input.Add(new Node("headers", null, response.Headers.Select(x => new Node(x.Key, x.Value))));
            input.Add(new Node("content", response.Content));
        }
    }
}
