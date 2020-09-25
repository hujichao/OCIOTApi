using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Web;

namespace OCIOTApi.Http
{
    public class HttpsSend
    {
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";
        //DeviceLogin
        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true; //总是接受  
        }

        public static HttpWebResponse CreatePostHttpResponse(string url, IDictionary<string, string> parameters, Encoding charset, string type = "1", string Token = "", string JsonStr = "")
        {
            HttpWebRequest request = null;
            //HTTPSQ请求  
            ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
            //ServicePointManager.ServerCertificateValidationCallback = delegate { return true; };
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            request = WebRequest.Create(url) as HttpWebRequest;
            X509Store certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
            certStore.Open(OpenFlags.ReadOnly);
            // X509Certificate2Collection certCollection = certStore.Certificates.Find(X509FindType.FindBySubjectName, "安装的证书名字", false);


            var cer = new X509Certificate2("E:/电信平台证书/outgoing.CertwithKey.pkcs12", "IoM@1234");
            request.ClientCertificates.Add(cer);
            ServicePointManager.ServerCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
            {
                return true;
            };
            if (type == "2")
            {
                request.Headers.Add("app_key", "WZfm9GApPGiPE8rMGuaT36HOh2Ya");
                request.Headers.Add("Authorization", "Bearer " + Token);
                request.ContentType = "application/json";


                //Content-Type:application/json
            }
            else
            {

                request.ContentType = "application/x-www-form-urlencoded";
            }


            request.ProtocolVersion = HttpVersion.Version10;
            request.Method = "POST";

            request.UserAgent = DefaultUserAgent;
            if (type == "2")
            {
                using (var streamWriter = new StreamWriter(request.GetRequestStream()))
                {
                    streamWriter.Write(JsonStr);
                }

                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream()))
                {
                    var result = streamReader.ReadToEnd();
                }

                return null;
            }
            else
            {

                //如果需要POST数据 
                if (!(parameters == null || parameters.Count == 0))
                {
                    StringBuilder buffer = new StringBuilder();
                    int i = 0;
                    foreach (string key in parameters.Keys)
                    {
                        if (i > 0)
                        {
                            buffer.AppendFormat("&{0}={1}", key, parameters[key]);
                        }
                        else
                        {
                            buffer.AppendFormat("{0}={1}", key, parameters[key]);
                        }
                        i++;
                    }
                    byte[] data = charset.GetBytes(buffer.ToString());
                    using (Stream stream = request.GetRequestStream())
                    {
                        stream.Write(data, 0, data.Length);
                    }
                }
                return request.GetResponse() as HttpWebResponse;
            }



        }
    }
}