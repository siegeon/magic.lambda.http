/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@gaiasoul.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

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
            input.Add(new Node("status", (int)response.Status));
            foreach (var idxHeader in response.Headers)
            {
                input.Add(new Node(idxHeader.Key, string.Join(";", idxHeader.Value)));
            }
            if ((int)response.Status >= 200 && (int)response.Status < 400)
            {
                // Success
                input.Value = response.Content;
            }
            else
            {
                input.Value = null;
                input.Add(new Node("error", response.Error));
            }
        }
    }
}
