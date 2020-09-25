using CsharpHttpHelper;
using CsharpHttpHelper.Enum;
using Newtonsoft.Json;
using OCIOTApi.Models;
using REG.N6.IFT.TwpFControl.BLL;
using REG.N6.IFT.TwpFControl.Model;
using System;
using System.Configuration;
using System.IO;
using System.Net;
using System.Text;

namespace OCIOTApi.EquipmentManagement
{
    public static class EMApi
    {
        #region 报警（报警和故障的参数是一样的  只是调的地址不同，具体和消防沟通）


        /// <summary>
        /// 报警（报警和故障的参数是一样的  只是调的地址不同，具体和消防沟通）
        /// </summary>
        /// <param name="model"></param>
        public static void SendEM_Alert(EMModel model)
        {
            string urlGetRun = ConfigurationManager.AppSettings["EM_AlertsApiUrl"];
            //创建HttpClient（注意传入HttpClientHandler）
            string jsonString = JsonConvert.SerializeObject(model);
            string result = SendData(urlGetRun, jsonString);
        }
        #endregion
        #region 故障（报警和故障的参数是一样的  只是调的地址不同，具体和消防沟通）


        /// <summary>
        /// 故障（报警和故障的参数是一样的  只是调的地址不同，具体和消防沟通）
        /// </summary>
        /// <param name="model"></param>
        public static void SendEM_Fault(EMModel model)
        {
            string urlGetRun = ConfigurationManager.AppSettings["EM_FaultApiUrl"];
            //创建HttpClient（注意传入HttpClientHandler）
            string jsonString = JsonConvert.SerializeObject(model);
            string result = SendData(urlGetRun, jsonString);
        }
        #endregion

        #region 设备报警和故障处理结果状态变更


        /// <summary>
        /// 设备报警和故障处理结果状态变更
        /// </summary>
        /// <param name="model"></param>
        public static void SendEM_UpdateDevState(DevStateModel model)
        {
            try {
                string urlGetRun = ConfigurationManager.AppSettings["EM_UpdateDevStateApiUrl"];
                //创建HttpClient（注意传入HttpClientHandler）
                string jsonString = JsonConvert.SerializeObject(model);
                string result = SendData(urlGetRun, jsonString);
                Utility.WriteLog("报警恢复状态变更"+ result+"入参:"+ jsonString+"地址:"+ urlGetRun);
            }
            catch (Exception ex) {

                Utility.WriteLog("报警恢复状态变更异常" + ex.Message);
            }
        }
        #endregion

        public static string SendData(string url, string jsondata)
        {
            string result = string.Empty;
            HttpWebRequest httpWebRequest = HttpWebRequest.Create(url) as HttpWebRequest;
            httpWebRequest.Method = "POST";
            httpWebRequest.ContentType = "application/json";
            byte[] data = Encoding.UTF8.GetBytes(jsondata);
            Stream requestStream = httpWebRequest.GetRequestStream();
            requestStream.Write(data, 0, data.Length);
            requestStream.Close();
            try
            {
                using (var res = httpWebRequest.GetResponse() as HttpWebResponse)
                {
                    if (res.StatusCode == HttpStatusCode.OK)
                    {
                        StreamReader streamReader = new StreamReader(res.GetResponseStream(), Encoding.UTF8);
                        result = streamReader.ReadToEnd();
                    }
                }
            }
            catch (Exception ex)
            {
                throw;
            }
            return result;

        }
        /// <summary>
        /// 根据地址id获取小区id
        /// </summary>
        /// <param name="AddressID"></param>
        /// <returns></returns>
        public static string GetCommunityIDByADID(string AddressID)
        {
            string CommunityID = string.Empty;
            FireControl_AdrBindCommunityBLL _AdrBindCommunityBLL = new FireControl_AdrBindCommunityBLL();
            ResultDis<FireControl_AdrBindCommunity> resultDiss = _AdrBindCommunityBLL.GetObjByADID(AddressID);
            if (resultDiss.ResultCode == ResultCode.Succ || resultDiss.ResultData != null)
            {
                CommunityID = resultDiss.ResultData.CommunityID;
            }
            return CommunityID;
        }

    }
}