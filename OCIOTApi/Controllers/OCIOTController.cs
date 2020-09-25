using NEG.N6.IFP.Yyqu.JIC;
using Newtonsoft.Json;
using REG.N6.IFI.Yyqu.Model;
using REG.N6.IFT.TwpFControl.Model;
using System;
using System.Web.Http;
using OCIOTApi.Models;
using NEG.N6.IFI.Yyqu.JIC;
using REG.N6.IFT.TwpFControl.BLL;
using REG.N6.IFT.TwpFControl.Common;
using OCIOTApi.Http;
using System.Configuration;

namespace OCIOTApi.Controllers
{
    public class OCIOTController : ApiController
    {
        /// <summary>
        /// 接收电信OC平台推送信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseJson OCIOTMsg([FromBody]OCIOTModel model)
        {
            //暂时去掉该注释
            Utility.WriteLog("调我接口了："+DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")+"---:"+ JsonConvert.SerializeObject(model));
            ResponseJson json = null;
            try
            {
                FireControl_MpArTable_BLL MpBll = new FireControl_MpArTable_BLL();
                REG.N6.IFT.TwpFControl.Model.ResultDis<bool> Waterresult = null;
                FireControl_MpArTable info = new FireControl_MpArTable();
                info.AccID = CommHelper.CreatePKID("acc");
                info.RawData = JsonConvert.SerializeObject(model);
                info.ReportDate = DateTime.MinValue;

                //保存传入数据                   //修改

                //判断是否有这条消息
                Waterresult = MpBll.AddObj(info);
                if (Waterresult.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                {
                    Utility.WriteLog("数据保存成功：" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                }
                else
                {
                    Utility.WriteLog(Waterresult.ResultContent);
                    Utility.WriteLog("传入数据系统生成时间：" + info.RawData);
                }

                OCIOTServiceData data = new OCIOTServiceData();
                string alamdate = string.Empty;
                if (model != null)
                {
                    if (model.service != null)
                    {
                        OCIOTService services = new OCIOTService();
                        services = model.service;
                        if (services != null)
                        {
                            data = services.data;
                            //事件上报时间
                            alamdate = services.eventTime;
                            if (!string.IsNullOrEmpty(alamdate))
                            {
                                string year = alamdate.Substring(0, 4);
                                string month = alamdate.Substring(4, 2);
                                string day = alamdate.Substring(6, 2);
                                string t = alamdate.Substring(8, 1);
                                string hour = alamdate.Substring(9, 2);
                                string minute = alamdate.Substring(11, 2);
                                string second = alamdate.Substring(13, 2);
                                string z = alamdate.Substring(15, 1);
                                alamdate = year + "-" + month + "-" + day + t + hour + ":" + minute + ":" + second + z;
                            }
                        }
                    }
                }
                if (data != null)
                {
                    OCIOTData ociotdata = new OCIOTData();
                    Utility.WriteLog("数据保存成功报文的内置时间：" + DateTime.Parse(alamdate).ToString("yyyy-MM-dd HH:mm:ss:fff"));
                    ociotdata.GetData(data.rawData, alamdate, info.AccID);
                }
                json = new ResponseJson(ResponseCode.Nomal, "成功");
            }
            catch (Exception ex)
            {
                Utility.WriteLog(ex.Message);
                json = new ResponseJson(ResponseCode.Err, "失败");
            }
            return json;
        }

        /// <summary>
        /// 获取授权token
        /// </summary>
        /// <returns></returns>
        public string GetToken()
        {
            Utility.WriteLog("获取token");
            ContentType Content = new ContentType();
            var Token = Utility.GetCache("accessToken"); ;
            if (string.IsNullOrWhiteSpace(Token.ToString()))
            {
                string phoneApi = ConfigurationManager.AppSettings["DeviceLogin"].ToString();//登陆授权地址
                TelecomModel Telecom = new TelecomModel();
                Telecom.appId = ConfigurationManager.AppSettings["DeviceAppID"].ToString();
                Telecom.secret = ConfigurationManager.AppSettings["DeviceSecret"].ToString();
                var TeleData = JsonConvert.SerializeObject(Telecom);
                RestApiClient Mode = new RestApiClient(phoneApi, HttpVerbNew.POST, Content.Urlencoded, TeleData);
                var LoginResult = Mode.MakeRequest("", "");

                var OCIOTTokenModel = JsonConvert.DeserializeObject<OCIOTToken>(LoginResult);
                if (OCIOTTokenModel != null && !string.IsNullOrWhiteSpace(OCIOTTokenModel.accessToken))
                {
                    // 存储授权token 过期时间1天 和 刷新token 过期时间1个月
                    Token = OCIOTTokenModel.accessToken;
                    Utility.SetCache("accessToken", OCIOTTokenModel.accessToken,Convert.ToInt32(Math.Round(Convert.ToInt32(OCIOTTokenModel.expiresIn)*0.75,0)));
                    Utility.SetCache("refreshToken", OCIOTTokenModel.refreshToken, 2000000);
                }
            }
            return Token.ToString();
        }


        /// <summary>
        /// 下发消音指令
        /// </summary>
        /// <param name="DeviceIOTID">设备的电信平台id</param>
        /// <returns></returns>
        public ResponseJson<bool> Silencing([FromBody]OCIOTCommend CommendModel)
        {

            ResponseJson<bool> json = null;
            try {
                if (CommendModel == null || string.IsNullOrWhiteSpace(CommendModel.DeviceID) || string.IsNullOrWhiteSpace(CommendModel.DeviceIOTID))
                {
                    return new ResponseJson<bool>(ResponseCode.Nomal, "参数错误", false);
                }
                Utility.WriteLog("消音开始");
                ContentType Content = new ContentType();
                var Token = Utility.GetCache("accessToken");
                 Utility.WriteLog(Token.ToString());
                if (string.IsNullOrWhiteSpace(Token.ToString()))
                {//未找到可能已过期
                 //从新获取
                    Token = GetToken();
                }
                //消音命令入参
                JsonFr FireModel = new JsonFr();

                string SilencingApi = ConfigurationManager.AppSettings["DeviceInstructions"].ToString();//消音地址
                var AppKey = ConfigurationManager.AppSettings["DeviceAppID"].ToString();
                //Telecom.secret = ConfigurationManager.AppSettings["DeviceSecret"].ToString();
                FireModel.deviceId = CommendModel.DeviceIOTID;//设备平台ID
                FireModel.expireTime = 0;
                command command = new command();
                command.serviceId = "IOTPlatForm";//固定值
                command.method = "CLOUD_COMMOND";//固定值
                paras paras = new paras();
                paras.rawData = SilencingModel.SilencingGenerate(CommendModel.DeviceID);//命令报文

                command.paras = paras;
                FireModel.command = command;
                var TeleData = JsonConvert.SerializeObject(FireModel);
                Utility.WriteLog("消音下发数据：" + TeleData);

                RestApiClient Mode = new RestApiClient(SilencingApi, HttpVerbNew.POST, Content.JSON, TeleData);
                var LoginResult = Mode.MakeRequest(AppKey, Token.ToString());
                if (!string.IsNullOrWhiteSpace(LoginResult))
                {
                   var ResultModel =   JsonConvert.DeserializeObject<SilencingError>(LoginResult);
                    if (ResultModel != null && !string.IsNullOrWhiteSpace(ResultModel.resultcode))
                    {
                        if (ResultModel.resultcode == "1010005" || ResultModel.resultcode == "100002")//token错误
                        {//重新获取下token
                            Utility.RemoveCache("accessToken");
                            Token = GetToken();
                            LoginResult = Mode.MakeRequest(AppKey, Token.ToString());
                        }
                    }
                    else {
                        json = new ResponseJson<bool>(ResponseCode.Nomal, LoginResult,true); 
                        return json;
                    }
                }

                return null;

            }
            catch (Exception ex)
            {
                var ss = ex.Message;


                Utility.WriteLog(ss);
                return null;
            }
         
        }
    }
}

