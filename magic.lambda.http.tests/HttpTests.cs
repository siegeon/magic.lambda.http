// #define DEEP_TESTING
/*
 * Magic Cloud, copyright Aista, Ltd. See the attached LICENSE file for details.
 */

using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using magic.node.extensions;
using Newtonsoft.Json.Linq;

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
            Assert.Equal(200, lambda.Children.First().Value);
            Assert.Equal(2, lambda.Children.First().Children.Count());
            Assert.Equal("headers", lambda.Children.First().Children.First().Name);
            Assert.True(lambda.Children.First().Children.First().Children.Count() > 5);
            Assert.Equal("content", lambda.Children.First().Children.Skip(1).First().Name);
            var json = JObject.Parse(lambda.Children.First().Children.Skip(1).First().Get<string>());
            Assert.NotNull(json);
        }

        [Fact]
        public void Throws_01()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
http.get:""https://jsonplaceholder.typicode.com/users/1""
   token:foo
   foo:throws
"));
        }

        [Fact]
        public void Throws_02()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
http.post:""https://jsonplaceholder.typicode.com/users/1""
   token:foo
   paylod:howdy
   foo:throws
"));
        }

        [Fact]
        public void Throws_03()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
http.post:""https://jsonplaceholder.typicode.com/users/1""
   token:foo
   foo:throws
"));
        }

        [Fact]
        public void Throws_04()
        {
            Assert.Throws<ArgumentException>(() => Common.Evaluate(@"
http.get:""https://jsonplaceholder.typicode.com/users/1""
   foo:throws
"));
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
http.get:""https://jsonplaceholder.typicode.com/users/1""
");
            Assert.Equal(200, lambda.Children.First().Value);
        }

        [Fact]
        public void GetWithHeaders()
        {
            var lambda = Common.Evaluate(@"
http.get:""https://jsonplaceholder.typicode.com/users/1""
   headers
      Foo:Bar");
            Assert.Equal(200, lambda.Children.First().Value);
            Assert.Equal(2, lambda.Children.First().Children.Count());
            Assert.Equal("headers", lambda.Children.First().Children.First().Name);
            Assert.True(lambda.Children.First().Children.First().Children.Count() > 5);
            Assert.Equal("content", lambda.Children.First().Children.Skip(1).First().Name);
            var json = JObject.Parse(lambda.Children.First().Children.Skip(1).First().Get<string>());
            Assert.NotNull(json);
        }

        [Fact]
        public void GetWithToken()
        {
            var lambda = Common.Evaluate(@"
http.get:""https://jsonplaceholder.typicode.com/users/1""
   token:foo-bar");
            Assert.Equal(200, lambda.Children.First().Value);
            Assert.Equal(2, lambda.Children.First().Children.Count());
            Assert.Equal("headers", lambda.Children.First().Children.First().Name);
            Assert.True(lambda.Children.First().Children.First().Children.Count() > 5);
            Assert.Equal("content", lambda.Children.First().Children.Skip(1).First().Name);
            var json = JObject.Parse(lambda.Children.First().Children.Skip(1).First().Get<string>());
            Assert.NotNull(json);
        }

        [Fact]
        public void PostJson()
        {
            var lambda = Common.Evaluate(@"
http.post:""https://jsonplaceholder.typicode.com/postsXX""
   payload:@""{""""userId"""":1, """"id"""":1}""
");
            Assert.Equal(404, lambda.Children.First().Value);
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
http.post:""https://jsonplaceholder.typicode.com/posts""
   payload:@""{""""userId"""":1, """"id"""":1}""
");
            Assert.Equal(201, lambda.Children.First().Value);
        }

        [Fact]
        public void PutJson()
        {
            var lambda = Common.Evaluate(@"
http.put:""https://jsonplaceholder.typicode.com/posts/1""
   payload:@""{""""userId"""":1, """"id"""":1}""
");
            Assert.Equal(200, lambda.Children.First().Value);
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
http.put:""https://jsonplaceholder.typicode.com/posts/1""
   payload:@""{""""userId"""":1, """"id"""":1}""
");
            Assert.Equal(200, lambda.Children.First().Value);
        }

        [Fact]
        public void DeleteJson()
        {
            var lambda = Common.Evaluate(@"
http.delete:""https://jsonplaceholder.typicode.com/posts/1""
");
            Assert.Equal(200, lambda.Children.First().Value);
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
http.delete:""https://jsonplaceholder.typicode.com/posts/1""
");
            Assert.Equal(200, lambda.Children.First().Value);
        }
    }
}
