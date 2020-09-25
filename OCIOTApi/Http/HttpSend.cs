using System.Linq;
using System.Text;
using System.Threading.Tasks;
using REG.N6.IFT.TwpFControl.Model;
using Newtonsoft.Json;
using System.Net.Http;
using System.IO;
using System;
using System.Configuration;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.Net.Security;
using System.Web;
using System.Collections.Generic;
using OCIOTApi.Models;

namespace OCIOTApi.Http
{
    class HttpSend
    {
        public static string phoneApi = ConfigurationManager.AppSettings["YYPhoneAPI"].ToString();//语音电话API地址
        public static string phoneApiP1 = ConfigurationManager.AppSettings["YYPhoneAPIP1"].ToString();//语音电话API地址
        //public static void Send(Alerts_FaultInfo mminfo)
        //{
        //    try
        //    {
        //        string urlGetRun = ConfigurationManager.AppSettings["ApiUrl"].ToString();
        //        if (!string.IsNullOrWhiteSpace(urlGetRun))
        //        {
        //            var handler = new HttpClientHandler();
        //            //创建HttpClient（注意传入HttpClientHandler）
        //            using (var client = new HttpClient())
        //            {
        //                string jsonString = JsonConvert.SerializeObject(mminfo);
        //                byte[] bytes = Encoding.UTF8.GetBytes(jsonString);
        //                using (StreamContent sc = new StreamContent(new MemoryStream(bytes)))
        //                {
        //                    sc.Headers.ContentLength = bytes.Length;
        //                    sc.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");
        //                    var result = client.PostAsync(urlGetRun, sc).Result;
        //                    string json = result.Content.ReadAsStringAsync().Result;
        //                }
        //            }
        //        }
        //    }
        //    catch (Exception)
        //    {
        //    }
        //}

        #region 短信提醒
        #region 短信通知常量
        private const String host = "https://ali-sms.showapi.com";
        private const String path = "/sendSms";
        private const String method = "GET";
        private const String appcode = "77f2475cbda6453685310da7b4da03a6";
        #endregion 
        //async
        /// <summary>
        /// 短信通知
        /// </summary>
        /// <param name="num"></param>
        /// <param name="phone"></param>
        public static async void ShortMessageSend(string phone, string Address, string DevName, string UserName, string BeUserName, string type)
        {
            if (string.IsNullOrWhiteSpace(phone))
            {
                Utility.WriteLog("联系人发短信异常---手机号为空" + type);
                return;
            }
            //{'address':'" + Address + "'}
            string jsonArrayText = ""; //"[{'a':'a1','b':'b1'}]
            var tNum = "";
            if (type == "1")
            {//业主
                jsonArrayText = "{'UserName':'" + UserName + "','address':'" + Address + "','DivName':'" + DevName + "'}";
                tNum = "T170317004089";
            }
            else
            {
                jsonArrayText = "{'BeUserName':'" + BeUserName + "','UserName':'" + UserName + "','address':'" + Address + "','DivName':'" + DevName + "'}";
                tNum = "T170317004062";
            }
            String querys = $"content=" + jsonArrayText + "&mobile=" + phone + "&tNum=" + tNum;
            String bodys = "";
            String url = host + path;
            HttpWebRequest httpRequest = null;
            HttpWebResponse httpResponse = null;

            if (0 < querys.Length)
            {
                url = url + "?" + querys;
            }

            if (host.Contains("https://"))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                httpRequest = (HttpWebRequest)WebRequest.CreateDefault(new Uri(url));
            }
            else
            {
                httpRequest = (HttpWebRequest)WebRequest.Create(url);
            }
            httpRequest.Method = method;
            httpRequest.Headers.Add("Authorization", "APPCODE " + appcode);
            if (0 < bodys.Length)
            {
                byte[] data = Encoding.UTF8.GetBytes(bodys);
                using (Stream stream = httpRequest.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            try
            {//换为异步执行
                httpResponse = await httpRequest.GetResponseAsync() as HttpWebResponse;
                //  httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            }
            catch (WebException ex)
            {
                httpResponse = (HttpWebResponse)ex.Response;
            }

            Console.WriteLine(httpResponse.StatusCode);
            Console.WriteLine(httpResponse.Method);
            Console.WriteLine(httpResponse.Headers);
            Stream st = httpResponse.GetResponseStream();
            StreamReader reader = new StreamReader(st, Encoding.GetEncoding("utf-8"));
            // Console.WriteLine(reader.ReadToEnd());
            Utility.WriteLog(reader.ReadToEnd());
            Console.WriteLine("\n");
        }
        public static bool CheckValidationResult(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors errors)
        {
            return true;
        }
        /// <summary>
        /// 打电话
        /// </summary>
        /// <param name="coent"></param>
        /// <param name="phone">电话号码</param>
        /// <param name="mobanID">模板id</param>
        /// WarinType 报警类型
        public static void TalePhone(string divName, string phone, string address, string name, string type, string BeUserName = "", string DevID = "", string WarinType = "")
        {
            Utility.WriteLog("拨打电话：" + phone + name);

            Dictionary<string, string> dic1 = new Dictionary<string, string>();
            dic1.Add("Phone", phone);
            dic1.Add("State", type);
            dic1.Add("Address", address);
            dic1.Add("Facility", divName);
            if (type == "1")
            {
                dic1.Add("Name", name);
            }
            else
            {
                dic1.Add("Name", BeUserName);
                dic1.Add("yzname", name);
            }
            dic1.Add("DevID", DevID);
            dic1.Add("WarinType", WarinType);
            new Task(() =>
            {
                GetByRequest(phoneApi, dic1, null);
            }).Start();
        }


        #region Reuqst获取数据
        /// <summary>
        /// Reuqst获取数据
        /// </summary>
        /// <param name="url">请求链接</param>
        /// <param name="parames">参数async</param>
        /// <returns></returns>
        public static async void GetByRequest(string url, Dictionary<string, string> parames = null, Dictionary<string, string> headers = null, bool UTF = false)
        {
            string resultObj = string.Empty;

            if (!string.IsNullOrEmpty(url))
            {
                string param = "";
                if (parames != null)
                {
                    foreach (var item in parames)
                    {
                        if (UTF)
                        {
                            Encoding myEncoding = Encoding.GetEncoding("UTF-8");
                            param += HttpUtility.UrlEncode(item.Key, myEncoding) + "=" + HttpUtility.UrlEncode(item.Value, myEncoding) + "&";
                        }
                        else
                        {
                            Encoding myEncoding = Encoding.GetEncoding("gb2312");
                            param += HttpUtility.UrlEncode(item.Key, myEncoding) + "=" + HttpUtility.UrlEncode(item.Value, myEncoding) + "&";
                        }
                    }
                }

                param = param.TrimEnd('&');
                Utility.WriteLog("数据参数：" + param);
                byte[] postBytes = Encoding.ASCII.GetBytes(param);
                HttpWebRequest req = (HttpWebRequest)HttpWebRequest.Create(url);
                req.Method = "POST";
                if (headers != null)
                {
                    foreach (var item in headers)
                    {
                        req.Headers.Add(item.Key, item.Value);
                    }
                }
                req.ContentType = "application/x-www-form-urlencoded;charset=gb2312";
                req.ContentLength = postBytes.Length;
                using (Stream reqStream = req.GetRequestStream())
                {
                    reqStream.Write(postBytes, 0, postBytes.Length);
                }
                //using (WebResponse wr = req.GetResponse())
                //{
                //    using (StreamReader reader = new StreamReader(wr.GetResponseStream(), myEncoding))
                //    {
                //        resultObj = reader.ReadToEnd();
                //    }
                //}

                HttpWebResponse res;
                try
                {//换为异步执行
                    res = await req.GetResponseAsync() as HttpWebResponse;
                    // res = (HttpWebResponse)req.GetResponse();
                }
                catch (WebException ex)
                {
                    res = (HttpWebResponse)ex.Response;
                }
                if (UTF)
                {
                    StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("UTF-8"));

                    resultObj = sr.ReadToEnd();
                }
                else
                {
                    StreamReader sr = new StreamReader(res.GetResponseStream(), Encoding.GetEncoding("gb2312"));

                    resultObj = sr.ReadToEnd();
                }

                Utility.WriteLog(resultObj);
            }


        }
        #endregion

        //p1 新版通知 只包括业主的推送电话和短信

        #region P1短信发送
        /// <summary>
        /// 
        /// </summary>
        /// <param name="phone"></param>
        /// <param name="Address"></param>
        /// <param name="DevName"></param>
        /// <param name="UserName"></param>
        /// <param name="BeUserName"></param>
        /// <param name="type"></param>
        public static async void sendSmsP1(SendSmsModel SenModel)
        {
            if (string.IsNullOrWhiteSpace(SenModel.Phone))
            {
                Utility.WriteLog("联系人发短信异常---手机号为空");
                return;
            }
            string jsonArrayText = ""; //[address]的[DevName][PushName],友情提示[Message] 
            jsonArrayText = "{'PushName':'" + SenModel.PushName + "','address':'" + SenModel.VillageName+ SenModel.AddressDetail + "','Message':'" + SenModel.Message + "','DevName':'" + SenModel.DevName + "'}";
            var tNum = "T170317005189";
            String querys = $"content=" + jsonArrayText + "&mobile=" + SenModel.Phone + "&tNum=" + tNum;
            String bodys = "";
            String url = host + path;
            HttpWebRequest httpRequest = null;
            HttpWebResponse httpResponse = null;

            if (0 < querys.Length)
            {
                url = url + "?" + querys;
            }

            if (host.Contains("https://"))
            {
                ServicePointManager.ServerCertificateValidationCallback = new RemoteCertificateValidationCallback(CheckValidationResult);
                httpRequest = (HttpWebRequest)WebRequest.CreateDefault(new Uri(url));
            }
            else
            {
                httpRequest = (HttpWebRequest)WebRequest.Create(url);
            }
            httpRequest.Method = method;
            httpRequest.Headers.Add("Authorization", "APPCODE " + appcode);
            if (0 < bodys.Length)
            {
                byte[] data = Encoding.UTF8.GetBytes(bodys);
                using (Stream stream = httpRequest.GetRequestStream())
                {
                    stream.Write(data, 0, data.Length);
                }
            }
            try
            {//换为异步执行
                httpResponse = await httpRequest.GetResponseAsync() as HttpWebResponse;
                //  httpResponse = (HttpWebResponse)httpRequest.GetResponse();
            }
            catch (WebException ex)
            {
                httpResponse = (HttpWebResponse)ex.Response;
            }

            Console.WriteLine(httpResponse.StatusCode);
            Console.WriteLine(httpResponse.Method);
            Console.WriteLine(httpResponse.Headers);
            Stream st = httpResponse.GetResponseStream();
            StreamReader reader = new StreamReader(st, Encoding.GetEncoding("utf-8"));
            // Console.WriteLine(reader.ReadToEnd());
            Utility.WriteLog(reader.ReadToEnd());
            Console.WriteLine("\n");
        }

        #endregion

        #region P1语音电话

       /// <summary>
       /// 语音电话
       /// </summary>
       /// <param name="divName">设备昵称</param>
       /// <param name="phone">电话</param>
       /// <param name="address">地址</param>
       /// <param name="name">用户名称</param>
       /// <param name="DevID">设备ID</param>
       /// <param name="WarinType">推送类型</param>
       /// <param name="Message">主体内容</param>
        public static void TalePhoneP1(string divName, string phone, string address, string name, string DevID, string WarinType,string Message)
        {
            Utility.WriteLog("拨打电话：" + phone + name);
            Dictionary<string, string> dic1 = new Dictionary<string, string>();
            dic1.Add("Phone", phone);
            dic1.Add("Address", address);
            dic1.Add("facility", divName);
            dic1.Add("name", name);
            dic1.Add("DevID", DevID);
            dic1.Add("WarinType", WarinType);
            dic1.Add("Message", Message);
            new Task(() =>
            {
                GetByRequest(phoneApiP1, dic1, null);
            }).Start();
        }



        #endregion


        #endregion
    }
}