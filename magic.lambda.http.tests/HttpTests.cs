/*
 * Magic, Copyright(c) Thomas Hansen 2019, thomas@gaiasoul.com, all rights reserved.
 * See the enclosed LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using magic.node.extensions;

namespace magic.lambda.http.tests
{
    public class HttpTests
    {
        [Fact]
        public void GetJson()
        {
            var lambda = Common.Evaluate(@"
http.get:""https://jsonplaceholder.typicode.com/users/1""
");
            Assert.True(lambda.Children.First().Get<string>().Length > 0);
        }

        [Fact]
        public void GetJsonPayload_Throws()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
http.get:""https://jsonplaceholder.typicode.com/users/1""
   payload:foo
"));
        }

        [Fact]
        public async Task GetJsonAsync()
        {
            var lambda = await Common.EvaluateAsync(@"
wait.http.get:""https://jsonplaceholder.typicode.com/users/1""
");
            Assert.True(lambda.Children.First().Get<string>().Length > 0);
        }

        [Fact]
        public void PostJson()
        {
            var lambda = Common.Evaluate(@"
http.post:""https://jsonplaceholder.typicode.com/posts""
   payload:@""{""""userId"""":1, """"id"""":1}""
");
            Assert.Equal(404, lambda.Children.First().Children.First().Value);
        }

        [Fact]
        public void PostJsonNoPayload_Throws()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
http.post:""https://jsonplaceholder.typicode.com/posts""
"));
        }

        [Fact]
        public async Task PostJsonAsync()
        {
            var lambda = await Common.EvaluateAsync(@"
wait.http.post:""https://jsonplaceholder.typicode.com/posts""
   payload:@""{""""userId"""":1, """"id"""":1}""
");
            Assert.True(lambda.Children.First().Get<string>().Length > 0);
        }

        [Fact]
        public void PutJson()
        {
            var lambda = Common.Evaluate(@"
http.put:""https://jsonplaceholder.typicode.com/posts/1""
   payload:@""{""""userId"""":1, """"id"""":1}""
");
            Assert.True(lambda.Children.First().Get<string>().Length > 0);
        }

        [Fact]
        public void PutJsonNoPayload_Throws()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
http.put:""https://jsonplaceholder.typicode.com/posts/1""
"));
        }

        [Fact]
        public async Task PutJsonAsync()
        {
            var lambda = await Common.EvaluateAsync(@"
wait.http.put:""https://jsonplaceholder.typicode.com/posts/1""
   payload:@""{""""userId"""":1, """"id"""":1}""
");
            Assert.True(lambda.Children.First().Get<string>().Length > 0);
        }

        [Fact]
        public void DeleteJson()
        {
            var lambda = Common.Evaluate(@"
http.delete:""https://jsonplaceholder.typicode.com/posts/1""
");
            Assert.True(lambda.Children.First().Get<string>().Length > 0);
        }

        [Fact]
        public void DeleteJsonPayload_Throws()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
http.delete:""https://jsonplaceholder.typicode.com/posts/1""
   payload:foo
"));
        }

        [Fact]
        public async Task DeleteJsonAsync()
        {
            var lambda = await Common.EvaluateAsync(@"
wait.http.delete:""https://jsonplaceholder.typicode.com/posts/1""
");
            Assert.True(lambda.Children.First().Get<string>().Length > 0);
        }
    }
}
