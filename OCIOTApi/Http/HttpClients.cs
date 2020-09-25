using Newtonsoft.Json;
using OCIOTApi.Models;
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
    public enum HttpVerbNew
    {
        GET,            //method  常用的就这几样，可以添加其他的   get：获取    post：修改    put：写入    delete：删除
        POST,
        PUT,
        DELETE
    }


    public class ContentType//根据Postman整理，可以添加
    {
        public string Text = "text/plain";
        public string JSON = "application/json";
        public string Javascript = "application/javascript";
        public string XML = "application/xml";
        public string TextXML = "text/xml";
        public string HTML = "text/html";
        public string Urlencoded = "application/x-www-form-urlencoded";
    }


    public class RestApiClient
    {
        public string EndPoint { get; set; }    //请求的url地址  
        public HttpVerbNew Method { get; set; }    //请求的方法
        public string ContentType { get; set; } //格式类型
        public string PostData { get; set; }    //传送的数据


        public RestApiClient()
        {
            EndPoint = "";
            Method = HttpVerbNew.GET;
            ContentType = "text/xml";
            PostData = "";
        }
        public RestApiClient(string endpoint, string contentType)
        {
            EndPoint = endpoint;
            Method = HttpVerbNew.GET;
            ContentType = contentType;
            PostData = "";
        }
        public RestApiClient(string endpoint, HttpVerbNew method, string contentType)
        {
            EndPoint = endpoint;
            Method = method;
            ContentType = contentType;
            PostData = "";
        }


        public RestApiClient(string endpoint, HttpVerbNew method, string contentType, string postData)
        {
            EndPoint = endpoint;
            Method = method;
            ContentType = contentType;
            PostData = postData;
        }


        // 添加https
        private static readonly string DefaultUserAgent = "Mozilla/4.0 (compatible; MSIE 6.0; Windows NT 5.2; SV1; .NET CLR 1.1.4322; .NET CLR 2.0.50727)";


        private static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {

            return true;

        }
        // end添加https


        public string MakeRequest(string app_key, string authorization)
        {
            var Html = MakeRequest("", app_key, authorization);
            return Html;
        }


        public string MakeRequest(string parameters, string app_key, string authorization)
        {
        


            // 添加https
            Utility.WriteLog("开始");
            if (EndPoint.Substring(0, 8) == "https://")
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                ServicePointManager.SecurityProtocol = SecurityProtocolType.Ssl3 | SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            }
            var request = (HttpWebRequest)HttpWebRequest.Create(EndPoint + parameters);
            if (EndPoint.Substring(0, 8) == "https://")
            {

                //   string clientp12path = @"E:/电信平台证书/outgoing.CertwithKey.pkcs12";
                //   string clientp12PassWord = "IoM@1234";
                //     string clientp12path2 = @"E:\kry\server.cer";
                // //     string clientp12PassWord2 = "1qaz2wsx";
                X509Store certStore = new X509Store(StoreName.My, StoreLocation.LocalMachine);
                certStore.Open(OpenFlags.ReadOnly);
                //  X509Certificate2 cer = new X509Certificate2(clientp12path, clientp12PassWord);
                //    X509Certificate2 cer2 = new X509Certificate2(clientp12path2, clientp12PassWord2);
                //   request.ClientCertificates.Add(cer);
                // request.ClientCertificates.Add(cer2);
                Utility.WriteLog("证书");
                var Address = AppDomain.CurrentDomain.BaseDirectory;
                Utility.WriteLog(Address);
                X509Certificate2 cert = new X509Certificate2(Address + "outgoing.CertwithKey.pkcs12", "IoM@1234", X509KeyStorageFlags.PersistKeySet | X509KeyStorageFlags.MachineKeySet);
                request.ClientCertificates.Add(cert);
                Utility.WriteLog("证书添加");
                ServicePointManager.ServerCertificateValidationCallback += (object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors) =>
                {
                    return true;
                };
                Utility.WriteLog("证书确认");
                request.UserAgent = DefaultUserAgent;
            }


            // end添加https
            request.Method = Method.ToString();
            request.ContentLength = 0;
            request.ContentType = ContentType;
            request.ProtocolVersion = HttpVersion.Version10;
            if (!string.IsNullOrWhiteSpace(authorization))
            {
                request.Headers.Add("app_key", app_key);
                request.Headers.Add("Authorization", "bearer " + authorization);
         
            }


            ContentType Content = new ContentType();
            if (ContentType== Content.Urlencoded)//登录授权和下发命令使用的不是一种发送格式 
            {
                StringBuilder buffer = new StringBuilder();
                var Datae = JsonConvert.DeserializeObject<Dictionary<string, string>>(PostData);
                int i = 0;
                foreach (string key in Datae.Keys)
                {
                    if (i > 0)
                    {
                        buffer.AppendFormat("&{0}={1}", key, Datae[key]);
                    }
                    else
                    {
                        buffer.AppendFormat("{0}={1}", key, Datae[key]);
                    }
                    i++;
                }

                byte[] data = Encoding.UTF8.GetBytes(buffer.ToString());
                request.ContentLength = data.Length;

                using (Stream stream = request.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
           //     var response = request.GetResponse();
           //     Stream streame = response.GetResponseStream();   //获取响应的字符串流  
            //    StreamReader sr = new StreamReader(streame); //创建一个stream读取流  
             //   string html = sr.ReadToEnd();
               // Utility.WriteLog(html);
               // return html;

                try
                {
                    Utility.WriteLog("开始请求");
                    var responsed = request.GetResponse();
                    Stream streamed = responsed.GetResponseStream();   //获取响应的字符串流  
                    StreamReader sre = new StreamReader(streamed); //创建一个stream读取流  
                    string html1 = sre.ReadToEnd();
                    Utility.WriteLog(html1);
                    return html1;
                }
                catch (WebException e)
                {
                    Utility.WriteLog("异常1");
                    var stream = e.Response.GetResponseStream();
                    StreamReader sre = new StreamReader(stream); //创建一个stream读取流  
                    string html1 = sre.ReadToEnd();
                    Utility.WriteLog(html1);
                    return html1;


                }
            }
            else {
                if (!string.IsNullOrEmpty(PostData) && Method == HttpVerbNew.POST)//如果传送的数据不为空，并且方法是post
                {
                    var encoding = new UTF8Encoding();
                    var bytes = Encoding.UTF8.GetBytes(PostData);//编码方式按自己需求进行更改，我在项目中使用的是UTF-8
                    request.ContentLength = bytes.Length;
                    using (var writeStream = request.GetRequestStream())
                    {
                        writeStream.Write(bytes, 0, bytes.Length);
                    }
                }
                else if (!string.IsNullOrEmpty(PostData) && Method == HttpVerbNew.PUT)//如果传送的数据不为空，并且方法是put
                {
                    var encoding = new UTF8Encoding();
                    var bytes = Encoding.UTF8.GetBytes(PostData);//编码方式按自己需求进行更改，我在项目中使用的是UTF-8
                    request.ContentLength = bytes.Length;
                    using (var writeStream = request.GetRequestStream())
                    {
                        writeStream.Write(bytes, 0, bytes.Length);
                    }
                }
            }
            try {
                Utility.WriteLog("开始请求2");
                var responsed = request.GetResponse();
                Stream streamed = responsed.GetResponseStream();   //获取响应的字符串流  
                StreamReader sre = new StreamReader(streamed); //创建一个stream读取流  
                string html1 = sre.ReadToEnd();
                Utility.WriteLog(html1);
                return html1;
            }
            catch (WebException e)
            {
                Utility.WriteLog("异常2");
                var stream = e.Response.GetResponseStream();
                StreamReader sre = new StreamReader(stream); //创建一个stream读取流  
                string html1 = sre.ReadToEnd();
                Utility.WriteLog(html1);
                return html1;
           

            }
               
        }

        public bool CheckUrl(string parameters)
        {
            bool bResult = true;
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(EndPoint + parameters);
            myRequest.Method = Method.ToString();             //设置提交方式可以为＂ｇｅｔ＂，＂ｈｅａｄ＂等
            myRequest.Timeout = 10000;　             //设置网页响应时间长度
            myRequest.AllowAutoRedirect = false;//是否允许自动重定向
            HttpWebResponse myResponse = (HttpWebResponse)myRequest.GetResponse();
            bResult = (myResponse.StatusCode == HttpStatusCode.OK);//返回响应的状态
            return bResult;
        }
    }

}