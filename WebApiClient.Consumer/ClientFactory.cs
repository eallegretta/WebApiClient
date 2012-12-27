using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using LinFu.DynamicProxy;

namespace WebApiClient.Consumer
{
    public class ClientFactory
    {
        private static ConcurrentDictionary<Type, object> _proxies = new ConcurrentDictionary<Type, object>();
        private static ProxyFactory _proxyFactory = new ProxyFactory();

        public static T CreateClient<T>(string apiUrl)
        {
            var type = typeof (T);

            return (T)_proxies.GetOrAdd(type, t => _proxyFactory.CreateProxy(t, new WebApiInterceptor(apiUrl)));
        }
    }
}
