using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
//using System.Linq;
//using System.Threading;
//using System.Threading.Tasks;
//using RestSharp;
//using System.Windows;

//namespace LitRes.Services.Connectivity
//{
//    internal class RestClientService : IRestClientService
//    {
//        public Task<IRestResponse> ProcessRequest(IRestRequest request, bool secureConnection, CancellationToken cancellationToken, string url = "win10-ebook.litres.ru")
//        {
//            //url = "whub.litres.ru"; // test domen
//            var client = new RestClient(string.Format("http{0}://{1}/pages", secureConnection ? "s" : "", url));
//#if DEBUG
//            RequestToString(request, client);
//#endif
//            return client.ExecuteRequestAsync(request, cancellationToken);
//        }

//        public Task<IRestResponse> ProcessStaticRequest(IRestRequest request, CancellationToken cancellationToken)
//        {
//            var client = new RestClient("http://win10-ebook.litres.ru/static");

//            return client.ExecuteRequestAsync(request, cancellationToken);
//        }

//        public Task<IRestResponse> ProcessCustomRequest(IRestRequest request, string url, CancellationToken cancellationToken)
//        {
//            var client = new RestClient(url);

//            client.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows Phone 8.0; Trident/6.0; IEMobile/10.0; ARM; Touch; NOKIA; Lumia 920)";

//            return client.ExecuteRequestAsync(request, cancellationToken);
//        }
//#if DEBUG
//        private void RequestToString(IRestRequest request, RestClient client)
//        {
//            var testUri = string.Format("{0}?", client.BuildUri(request).ToString());
//            request.Parameters.ForEach(parameter => testUri += parameter.ToString() + "&");
//            Debug.WriteLine(testUri);
//        }
//#endif
//    }
//}

using System.Threading;
using System.Threading.Tasks;
using RestSharp;

namespace LitRes.Services.Connectivity
{
    internal class RestClientService : IRestClientService
    {
        public Task<IRestResponse> ProcessRequest(IRestRequest request, bool secureConnection, CancellationToken cancellationToken, string url = "win10-ebook.litres.ru")
        {
            //url = "whub.litres.ru"; // test domen
            var client = new RestClient(string.Format("http{0}://{1}/pages", secureConnection ? "s" : "", url));
#if DEBUG
            RequestToString(request, client);
#endif
            return client.ExecuteAsync(request, cancellationToken);
        }

        public Task<IRestResponse> ProcessStaticRequest(IRestRequest request, CancellationToken cancellationToken)
        {
            var client = new RestClient("http://win10-ebook.litres.ru/static");
#if DEBUG
            RequestToString(request, client);
#endif
            return client.ExecuteAsync(request, cancellationToken);
        }

        public Task<IRestResponse> ProcessCustomRequest(IRestRequest request, string url, CancellationToken cancellationToken)
        {
            var client = new RestClient(url);

            //client.UserAgent = "Mozilla/5.0 (compatible; MSIE 10.0; Windows Phone 8.0; Trident/6.0; IEMobile/10.0; ARM; Touch; NOKIA; Lumia 920)";

            return client.ExecuteAsync(request, cancellationToken);
        }
#if DEBUG
        private void RequestToString(IRestRequest request, RestClient client)
        {
            //var testUri = string.Format("{0}/{1}/?", client.BaseUrl, request.Resource.Substring(0,request.Resource.Length));
            var testUri = string.Format("{0}/{1}/?", client.BaseUrl, request.Resource);
            testUri = testUri.Replace(@"\", string.Empty);
            foreach (var parameter in request.Parameters)
            {
                testUri += parameter.Name + "=" + parameter.Value + "&";
            }
            //request.Parameters.ForEach(parameter => testUri += parameter.ToString() + "&");
            Debug.WriteLine(Encoding.UTF8.GetString(Encoding.UTF8.GetBytes(testUri)));
        }
#endif
    }
}
