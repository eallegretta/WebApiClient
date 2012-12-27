using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using LinFu.DynamicProxy;

namespace WebApiClient.Consumer
{
    public class WebApiInterceptor: IInvokeWrapper
    {
        private readonly HttpClient _httpClient;

        public WebApiInterceptor(string baseUri)
        {
            _httpClient = new HttpClient();

            if (!baseUri.EndsWith("/"))
                baseUri += "/";

            _httpClient.BaseAddress = new Uri(baseUri);
        }

        public void AfterInvoke(InvocationInfo info, object returnValue)
        {
        }

        public void BeforeInvoke(InvocationInfo info)
        {
        }

        public object DoInvoke(InvocationInfo info)
        {
            if (IsAsyncMethod(info.TargetMethod))
            {
                return PerformAsyncInvoke(info);
            }
            else
            {

                return PerformInvoke(info);
            }
        }

        private object PerformInvoke(InvocationInfo info)
        {
            var response = PerformWebApiCall(info).Result;

            return ReadResponseContent(info, response).Result;
        }

        private Task PerformAsyncInvoke(InvocationInfo info)
        {
            var taskArgType = info.TargetMethod.ReturnType.GetGenericArguments().First();

            var responseCall = new ResponseCall(info, PerformWebApiCall(info));

            var instanceExpr = Expression.Constant(responseCall);

            var callExpr = Expression.Call(instanceExpr, "PerformCall", null);

            var castExpr = Expression.Convert(callExpr, taskArgType);

            var funcType = typeof(Func<>).MakeGenericType(taskArgType);

            var lambda = Expression.Lambda(funcType, castExpr);

            var taskFactoryExpr = Expression.Constant(Task.Factory);

            var taskFactoryStartNewExpr = Expression.Call(taskFactoryExpr, "StartNew", new[] { taskArgType }, lambda);

            return taskFactoryStartNewExpr.Method.Invoke(Task.Factory, new object[] { lambda.Compile() }) as Task;
        }



        private static Task<object> ReadResponseContent(InvocationInfo info, HttpResponseMessage response)
        {
            if (!response.IsSuccessStatusCode)
                throw new Exception(response.ReasonPhrase);

            var webApiMethodAttr = GetWebApiMethodAttribute(info);

            var formatters = GlobalConfiguration.MediaTypeFormatters;

            if (webApiMethodAttr != null)
                formatters = new[] { Activator.CreateInstance(webApiMethodAttr.MediaTypeFormatter) as MediaTypeFormatter };

            return response.Content.ReadAsAsync(info.TargetMethod.ReturnType, formatters);
        }

        private Task<HttpResponseMessage> PerformWebApiCall(InvocationInfo info)
        {
            var webApiMethodAttr = GetWebApiMethodAttribute(info);

            if (webApiMethodAttr == null)
            {
                string httpMethod = "get";

                if (info.TargetMethod.Name.In(StringComparison.OrdinalIgnoreCase, "post", "put", "delete", "get"))
                {
                    httpMethod = info.TargetMethod.Name.ToLowerInvariant();
                }

                return GetWebApiResponse(info, httpMethod, info.TargetMethod.Name, new JsonMediaTypeFormatter());
            }

            var formatter = Activator.CreateInstance(webApiMethodAttr.MediaTypeFormatter) as MediaTypeFormatter;

            string uri = string.IsNullOrWhiteSpace(webApiMethodAttr.Uri)
                             ? info.TargetMethod.Name
                             : webApiMethodAttr.Uri;

            return GetWebApiResponse(info, webApiMethodAttr.HttpMethod, uri, formatter);
        }

        private static WebApiMethodAttribute GetWebApiMethodAttribute(InvocationInfo info)
        {
            var webApiMethodAttr =
                info.TargetMethod.GetCustomAttributes(typeof(WebApiMethodAttribute), true).FirstOrDefault() as
                WebApiMethodAttribute;
            return webApiMethodAttr;
        }

        private Task<HttpResponseMessage> GetWebApiResponse(InvocationInfo info, string httpMethod, string uri, MediaTypeFormatter formatter)
        {
            switch (httpMethod.ToLowerInvariant())
            {
                case "post":
                    return _httpClient.PostAsync(GetRequestUri(info, uri, false), GetObjectContentFromArguments(info, formatter));
                case "put":
                    return _httpClient.PutAsync(GetRequestUri(info, uri, false), GetObjectContentFromArguments(info, formatter));
                case "delete":
                    return _httpClient.DeleteAsync(GetRequestUri(info, uri));
                case "get":
                default:
                    return _httpClient.GetAsync(GetRequestUri(info, uri));
            }
        }

        private string GetRequestUri(InvocationInfo info, string baseUri = null, bool addArgumentsToQueryString = true)
        {
            var uri = new StringBuilder();

            if (baseUri == info.TargetMethod.Name || string.IsNullOrWhiteSpace(baseUri))
            {
                string targetMethod = info.TargetMethod.Name;

                if (targetMethod.EndsWith("Async"))
                    targetMethod = targetMethod.Substring(0, targetMethod.Length - 5);

                uri.Append(targetMethod);
            }
            else
                uri.Append(baseUri);

            if (addArgumentsToQueryString)
            {

                int argumentsCount = info.TypeArguments.Length;

                if (argumentsCount > 0)
                    uri.Append("?");

                for (int index = 0; index < argumentsCount; index++)
                {
                    uri.Append(info.TypeArguments[index].Name.ToLower() + "=" + info.Arguments[index]);

                    if (index < argumentsCount - 1)
                        uri.Append("&");
                }
            }

            return GlobalConfiguration.LowercaseUris ? uri.ToString().ToLowerInvariant() : uri.ToString();
        }

        private HttpContent GetObjectContentFromArguments(InvocationInfo info, MediaTypeFormatter formatter = null)
        {
            if (formatter == null)
                formatter = GlobalConfiguration.MediaTypeFormatters.First();

            var data = new Dictionary<string, object>();

            for (int index = 0; index < info.TypeArguments.Length; index++)
            {
                data.Add(info.TypeArguments[index].Name, info.Arguments[index]);
            }

            return new ObjectContent<Dictionary<string, object>>(data, formatter);
        }

        private static bool IsAsyncMethod(System.Reflection.MethodInfo methodInfo)
        {
            bool methodEndsWithAsync = methodInfo.Name.EndsWith("Async");
            bool methodHasTaskAsReturnValue = typeof(Task).IsAssignableFrom(methodInfo.ReturnType);

            if (methodEndsWithAsync && methodHasTaskAsReturnValue)
                return true;
            else if (methodEndsWithAsync)
                throw new Exception("The method name is not compliant with async standards, the method name ends with the Async word but its return type if not a System.Threading.Tasks.Task or System.Threading.Tasks.Task<T>");
            else if (methodHasTaskAsReturnValue)
                throw new Exception("The method name is not compliant with async standards, the method return type is a System.Concurrent.Tasks.Task or inherits from it but the method name does not end with the Async keyword");
            else
                return false;
        }

        private class ResponseCall
        {
            private readonly InvocationInfo _info;
            private readonly Task<HttpResponseMessage> _response;

            public ResponseCall(InvocationInfo info, Task<HttpResponseMessage> response)
            {
                _info = info;
                _response = response;
            }

            public object PerformCall()
            {
                return _response.ContinueWith(t => ReadResponseContent(_info, t.Result)).Result.Result;
            }
        }
    }
}
