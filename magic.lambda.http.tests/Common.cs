/*
 * Magic, Copyright(c) Thomas Hansen 2019 - thomas@gaiasoul.com
 * Licensed as Affero GPL unless an explicitly proprietary license has been obtained.
 */

using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using magic.node;
using magic.http.services;
using magic.http.contracts;
using magic.signals.services;
using magic.signals.contracts;
using magic.node.extensions.hyperlambda;

namespace magic.lambda.http.tests
{
    public static class Common
    {
        [Slot(Name = "foo")]
        public class FooSlot : ISlot
        {
            public void Signal(ISignaler signaler, Node input)
            {
                input.Value = "OK";
            }
        }

        static public Node Evaluate(string hl)
        {
            var services = Initialize();
            var lambda = new Parser(hl).Lambda();
            var signaler = services.GetService(typeof(ISignaler)) as ISignaler;
            signaler.Signal("eval", lambda);
            return lambda;
        }

        static public ISignaler GetSignaler()
        {
            var services = Initialize();
            return services.GetService(typeof(ISignaler)) as ISignaler;
        }

        #region [ -- Private helper methods -- ]

        static IServiceProvider Initialize()
        {
            var configuration = new ConfigurationBuilder().Build();
            var services = new ServiceCollection();
            services.AddTransient<IConfiguration>((svc) => configuration);
            services.AddTransient<ISignaler, Signaler>();
            services.AddTransient<IHttpClient, HttpClient>();
            var types = new SignalsProvider(InstantiateAllTypes<ISlot>(services));
            services.AddTransient<ISignalsProvider>((svc) => types);
            var provider = services.BuildServiceProvider();
            return provider;
        }

        static IEnumerable<Type> InstantiateAllTypes<T>(ServiceCollection services) where T : class
        {
            var type = typeof(T);
            var result = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);

            foreach (var idx in result)
            {
                services.AddTransient(idx);
            }
            return result;
        }

        #endregion
    }
}
