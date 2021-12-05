﻿/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System.IO;
using System.Web;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using MimeKit;
using magic.node;
using magic.node.extensions;
using magic.signals.contracts;

namespace magic.lambda.http.services.helpers
{
    /*
     * Helper class to transform an HTTP request content object semantically declared as a
     * lambda to a content object of some sort.
     */
    internal static class RequestTransformers
    {
        /*
         * Transforms a a [payload] node into JSON string and returns to caller.
         */
        internal static object TransformToJson(
            ISignaler signaler,
            Dictionary<string, string> headers,
            Node payloadNode,
            string slotName)
        {
            /*
             * Automatically [unwrap]'ing all nodes, since there are no reasons why you'd want
             * to pass in an expression as JSON token to an endpoint.
             */
            foreach (var idx in payloadNode.Children)
            {
                Unwrap(idx, slotName, true);
            }

            // Using JSON slot to transform nodes to JSON.
            signaler.Signal("lambda2json", payloadNode);
            return payloadNode.Value;
        }

        /*
         * Transforms a [payload] node into a Hyperlambda string and returns to caller.
         */
        internal static object TransformToHyperlambda(
            ISignaler signaler,
            Dictionary<string, string> headers,
            Node payloadNode,
            string slotName)
        {
            // Using Hyperlambda slot to transform nodes to string.
            signaler.Signal("lambda2hyper", payloadNode);

            // Returning Hyperlambda to caller.
            return payloadNode.Value;
        }

        /*
         * Transforms a [payload] node into a URL encoded string and returns to caller.
         */
        internal static object TransformToUrlEncoded(
            ISignaler signaler,
            Dictionary<string, string> headers,
            Node payloadNode,
            string slotName)
        {
            // Creating a URL encoded request payload body.
            var builder = new StringBuilder();
            foreach (var idx in payloadNode.Children)
            {
                if (idx.Children.Any())
                    throw new HyperlambdaException($"'application/x-www-form-urlencoded' requests can only handle one level of arguments, and node '{idx.Name}' had children");

                if (builder.Length > 0)
                    builder.Append("&");

                builder.Append(idx.Name).Append("=").Append(HttpUtility.UrlEncode(idx.GetEx<string>()));
            }

            // Returning URL encoded key/value pairs to caller.
            return builder.ToString();
        }

        /*
         * Transforms a [payload] node into a URL encoded string and returns to caller.
         */
        internal static object MultipartFormData(
            ISignaler signaler,
            Dictionary<string, string> headers,
            Node payloadNode,
            string slotName)
        {
            // Invoking slot responsible for creating our MIME entity.
            payloadNode.Value = headers["Content-Type"];
            signaler.Signal(".mime.create", payloadNode);
            var entity = payloadNode.Get<MimeEntity>();

            // Figuring out correct Content-Type of MIME multipart, including its boundary parts, and modifying headers collection.
            var contentType = entity.ContentType.MimeType;
            foreach (var idx in entity.ContentType.Parameters)
            {
                contentType += "; " + idx.Name + "=\"" + idx.Value + "\"";
            }
            headers["Content-Type"] = contentType;

            // Serialising MIME entity *without* its headers and returning it to caller.
            using (var stream = new MemoryStream())
            {
                entity.WriteTo(new FormatOptions { MaxLineLength = 100, NewLineFormat = NewLineFormat.Dos }, stream, true);
                stream.Position = 0;

                // House cleaning.
                DisposeEntity(entity);

                // Returning string wrapping entire MIME entity to caller.
                using (var reader = new StreamReader(stream))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        #region [ -- Private helper methods -- ]

        /*
         * Internal helper method to dispose all streams inside all entities.
         */
        static void DisposeEntity(MimeEntity entity)
        {
            if (entity is MimePart part)
            {
                part.Content?.Stream?.Dispose();
            }
            else if (entity is Multipart multi)
            {
                foreach (var idx in multi)
                {
                    DisposeEntity(idx);
                }
            }
        }

        /*
         * Helper method to [unwrap] all nodes passed in as a lambda object.
         */
        static void Unwrap(Node node, string slotName, bool recurse)
        {
            // Checking if value of node is an expression, and if so, [unwrap]'ign it.
            if (node.Value is Expression)
            {
                var exp = node.Evaluate();
                if (exp.Count() > 1)
                    throw new HyperlambdaException($"Multiple sources found for node in lambda object supplied to [{slotName}]");
                node.Value = exp.FirstOrDefault()?.Value;
            }

            // Recursively iterating through all children of currently iterated node, but only if caller wants us to.
            if (recurse)
            {
                foreach (var idx in node.Children)
                {
                    Unwrap(idx, slotName, recurse);
                }
            }
        }

        #endregion
    }
}
