using NEG.N6.IFI.Yyqu.JIC;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using OCIOTApi.EquipmentManagement;
using OCIOTApi.Models;
using REG.N6.IFT.TwpFControl.BLL;
using REG.N6.IFT.TwpFControl.Common;
using REG.N6.IFT.TwpFControl.Model;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using HttpPostAttribute = System.Web.Http.HttpPostAttribute;

namespace OCIOTApi.Controllers
{
    public class testController : ApiController
    {
        #region 接收电气平台推送的告警状态  本地测试使用
        /// <summary>
        /// 接收电气平台推送的故障信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseJson<bool> GetCallThePoliceData([FromBody] sdfsdfsdf jitem)//JObject jObject
        {
            string userIP;
            // HttpRequest Request = HttpContext.Current.Request;  
            HttpRequest Request = System.Web.HttpContext.Current.Request; // 如果使用代理，获取真实IP  
            if (Request.ServerVariables["HTTP_X_FORWARDED_FOR"] != "")
                userIP = Request.ServerVariables["REMOTE_ADDR"];
            else
                userIP = Request.ServerVariables["HTTP_X_FORWARDED_FOR"];
            if (userIP == null || userIP == "")
                userIP = Request.UserHostAddress;

            string userIP1 = HttpContext.Current.Request.UserHostAddress;

            Utility.WriteLog("请求GetCallThePoliceData的访问者ip------------" + userIP + "----------------------------" + userIP1);

            ResponseJson<bool> json = null;
            ResultDis<bool> result = null;
            try
            {
                //1.根据设备编号获取设备基本信息(判断该设备是否是我们的设备)
                FireControl_DeviceBLL dll = new FireControl_DeviceBLL();
                FireControl_DevBindUserBLL devUserdll = new FireControl_DevBindUserBLL();
                FireControl_WarninsPush model = new FireControl_WarninsPush();
                //JToken jitem = "";
                //foreach (var a in jObject.Children())
                //{

                //    //if (string.IsNullOrEmpty(a.Next.n))
                //    //{
                //    //    jitem = a.Value;
                //    //}
                //    //else
                //    //{
                //    //    jitem = a.Key;
                //    //}
                //}
                //Utility.WriteLog(jitem.ToString());

                string jsons = JsonConvert.SerializeObject(jitem);
                JObject jo = JObject.Parse(jsons);

                //添加记录
                AddMpArTable(jitem.ToString(), jo["DevNum"].ToString());
                model.SeqNum = 0;
                model.TitleID = "";
                model.NewTitle = "";
                model.NewContent = "";
                model.PushDate = DateTime.Now;
                model.DevID = "";
                model.WarInAdress = "";
                model.WarIn_Type = 0;
                model.CreateTime = DateTime.Now;
                model.CreatedBy = "";
                model.Deleted = 0;
                model.PushState = string.IsNullOrEmpty(jo["PushState"].ToString()) ? "" : jo["PushState"].ToString();
                model.WarninsState = "1";
                model.WarninsRepresent = string.IsNullOrEmpty(jo["WarninsRepresent"].ToString()) ? "" : jo["WarninsRepresent"].ToString();
                model.WarninsVoltage = string.IsNullOrEmpty(jo["WarninsVoltage"].ToString()) ? "" : Convert.ToString(Convert.ToDouble(jo["WarninsVoltage"].ToString()));
                model.WarninsCurrent = string.IsNullOrEmpty(jo["WarninsCurrent"].ToString()) ? "" : Convert.ToString(Convert.ToDouble(jo["WarninsCurrent"].ToString()));
                model.WarninsTemper = string.IsNullOrEmpty(jo["WarninsTemper"].ToString()) ? "" : Convert.ToString(Convert.ToDouble(jo["WarninsTemper"].ToString()));
                model.WarninsTemper2 = string.IsNullOrEmpty(jo["WarninsTemper2"].ToString()) ? "" : Convert.ToString(Convert.ToDouble(jo["WarninsTemper2"].ToString()));
                model.WarninsArc = string.IsNullOrEmpty(jo["WarninsArc"].ToString()) ? "" : jo["WarninsArc"].ToString();
                model.Warninsleakage = string.IsNullOrEmpty(jo["Warninsleakage"].ToString()) ? "" : jo["Warninsleakage"].ToString();
                model.DevNum = string.IsNullOrEmpty(jo["DevNum"].ToString()) ? "" : jo["DevNum"].ToString();
                if (!string.IsNullOrEmpty(model.DevNum))
                {
                    string AddressID = "";
                    string recordID = string.Empty;
                    string DevTypeNum = string.Empty;
                    #region 将告警信息插入到数据库
                    ResultDis<FireControl_Device> item = dll.GetDevIDByDevNum(model.DevNum);

                    if (item.ResultData != null && item.ResultCode == ResultCode.Succ)
                    {
                        FireControl_DeviceTypeBLL _DeviceTypeBLL = new FireControl_DeviceTypeBLL();
                        ResultDis<FireControl_DeviceType> dis = _DeviceTypeBLL.GetObjByDeviceID(item.ResultData.DevID);
                        if (dis.ResultCode == ResultCode.Succ)
                        {
                            DevTypeNum = dis.ResultData.TypeNum;
                        }
                        if (item.ResultData.ValidDate < DateTime.Now)//过期时间大于当前时间)   
                        {
                            Utility.WriteLog("设备已过期，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                            return json;
                        }
                        //判断设备是否正在报警
                        FireControl_WarninsPushBLL pushBll = new FireControl_WarninsPushBLL();
                        //  model.DevNum
                        var OldData = pushBll.SelectNotCloseState(item.ResultData.DevID, "1", model.PushState);
                        if (OldData.ResultCode == ResultCode.Succ && !string.IsNullOrWhiteSpace(OldData.ResultData.DevID))
                        {
                            if (item.ResultData.DeviceState == 1)
                            {
                                Utility.WriteLog("设备继续报警:" + model.DevNum);
                                json = new ResponseJson<bool>(ResponseCode.Nomal, "继续报警");
                                return json;
                            }
                            else
                            {
                                item.ResultData.DeviceState = 1;
                                FireControl_DeviceBLL devdlle = new FireControl_DeviceBLL();
                                ResultDis<bool> dis1 = devdlle.ModObj(item.ResultData);
                                if (dis1.ResultCode == ResultCode.Succ)
                                {
                                    Utility.WriteLog("设备继续报警,但状态刚才是正常----修改状态成功:" + model.DevNum);
                                }
                                Utility.WriteLog("设备继续报警,但状态刚才是正常:" + model.DevNum);
                                json = new ResponseJson<bool>(ResponseCode.Nomal, "继续报警");
                                return json;
                            }
                        }
                        FireControl_FaultSolution_BLL fabll = new FireControl_FaultSolution_BLL();
                        ResultDis<FireControl_FaultSolution> list = fabll.GetListByCode(model.PushState);
                        //1.接收故障信息存入数据库
                        model.NewTitle = list.ResultData.FaultName;
                        model.NewContent = list.ResultData.Causes;
                        model.PushDate = DateTime.Now;
                        model.DevID = item.ResultData.DevID;
                        model.TitleID = CommHelper.CreatePKID("tit");
                        //根据设备ID获取我的设备绑定地址 
                        ResultDis<GetDevPeopleUser> usermodel = devUserdll.GetDevPeople(model.DevID);
                        //修改设备状态为报警状态
                        item.ResultData.DeviceState = 1;
                        FireControl_DeviceBLL devdll = new FireControl_DeviceBLL();
                        devdll.ModObj(item.ResultData);

                        if (!string.IsNullOrWhiteSpace(usermodel.ResultData.AddressID))
                        {
                            AddressID = usermodel.ResultData.AddressID;
                            #region 添加处理流程数据
                            FireControl_WarninsHandleBLL clbll = new FireControl_WarninsHandleBLL();
                            FireControl_WarninsHandle handle = new FireControl_WarninsHandle();
                            handle.HandlD = CommHelper.CreatePKID("han");
                            handle.Title = "电气设备发生报警";
                            handle.Content = usermodel.ResultData.Address + "的电气设备发生报警";
                            handle.TitleID = model.TitleID;
                            handle.Hand_Type = 0;
                            handle.UserID = usermodel.ResultData.UserID;
                            handle.Hand_Mode = 0;
                            handle.Hand_Date = DateTime.Now;
                            handle.CreateTime = DateTime.Now;
                            handle.CreatedBy = "System";
                            handle.Deleted = 0;
                            clbll.AddObj(handle);
                            #endregion
                            if (usermodel.ResultData != null)
                            {
                                model.WarInAdress = usermodel.ResultData.Address;
                            }
                            model.WarIn_Type = 1;
                            model.CreateTime = DateTime.Now;
                            model.CreatedBy = "System";
                            model.Deleted = 0;
                            model.AddressID = usermodel.ResultData.AddressID;
                            FireControl_WarninsPushBLL push = new FireControl_WarninsPushBLL();
                            result = push.AddObj(model, out recordID);
                            if (result.ResultCode == ResultCode.Succ)
                            {
                                json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");

                            }
                            else
                            {
                                Utility.WriteLog("设备报警记录添加失败:" + model.DevNum);
                                json = new ResponseJson<bool>(ResponseCode.Err, result.ResultContent);
                            }

                            #region 向客户发推送故障消息
                            var PeopleModel = usermodel.ResultData;
                            #region 推送入库
                            FireControl_PushProcess FireControl_PushProcessModel = new FireControl_PushProcess();
                            FireControl_PushProcessModel.CreateTime = DateTime.Now;
                            FireControl_PushProcessModel.DevID = model.DevID;
                            FireControl_PushProcessModel.DevName = "电气";
                            FireControl_PushProcessModel.isPush = 0;
                            FireControl_PushProcessModel.PPID = CommHelper.CreatePKID("pp");
                            FireControl_PushProcessModel.TitleID = model.TitleID;
                            FireControl_PushProcessBLL proBll = new FireControl_PushProcessBLL();
                            var proResult = proBll.AddObj(FireControl_PushProcessModel);
                            if (proResult.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                            {
                                Utility.WriteLog("消息保存推送成功---------------------xxxxx--------");
                            }
                            else
                            {
                                Utility.WriteLog("保存推送失败-----------------------------");
                            }

                            #endregion
                            #endregion

                            #region 将推送消息添加到推送记录表
                            //添加推送记录表
                            //FireControl_WarninsPUserBLL puBll = new FireControl_WarninsPUserBLL();
                            //var Model = new FireControl_WarninsPUser();
                            //Model.CreatedBy = "0";
                            //Model.CreateTime = DateTime.Now;
                            //Model.Deleted = 0;
                            //Model.PUID = CommHelper.CreatePKID("pu");
                            //Model.TitleID = model.TitleID;// TitleID;
                            //Model.UserID = usermodel.ResultData.UserID;
                            //Model.UserPhone = usermodel.ResultData.Phone;
                            //puBll.AddObj(Model);
                            #endregion

                            #region //同步到设备管理api
                            EMModel eMModel = new EMModel();
                            eMModel.deviceNumber = model.DevNum;
                            eMModel.deviceTypeNumber = DevTypeNum;
                            eMModel.recordID = string.IsNullOrEmpty(recordID) ? "1111111111111" : recordID;
                            if (!string.IsNullOrEmpty(AddressID))
                            {
                                eMModel.unitID = EMApi.GetCommunityIDByADID(AddressID);
                            }
                            EMApi.SendEM_Alert(eMModel);
                            #endregion
                        }
                        else
                        {
                            Utility.WriteLog("地址不存在，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                        }

                    }
                    else
                    {
                        Utility.WriteLog("在获取装备告警信息时,未能在全民消防数据库中查到该设备，设备编号为:" + model.DevNum);
                        json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                    }
                    #endregion

                }
                else
                {
                    Utility.WriteLog("在获取装备告警信息时,未能在全民消防数据库中查到该设备，设备编号为:" + model.DevNum);
                    json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                }

            }
            catch (Exception ex)
            {
                ex.WriteErrLog();
                Utility.WriteLog(ex.Message);
                json = new ResponseJson<bool>(ResponseCode.Err, ex.Message);
                //log.Error("", ex);
            }
            return json;
        }
        private void AddMpArTable(string data, string DevNum)
        {
            FireControl_MpArTable_BLL MpBll = new FireControl_MpArTable_BLL();
            REG.N6.IFT.TwpFControl.Model.ResultDis<bool> Waterresult = null;
            FireControl_MpArTable info = new FireControl_MpArTable();
            info.AccID = CommHelper.CreatePKID("acc");
            info.RawData = data;
            info.ReportDate = DateTime.MinValue;
            info.DevNum = DevNum;
            //保存传入数据                   //修改
            info.CreateTime = DateTime.Now;
            //判断是否有这条消息
            Waterresult = MpBll.AddObj(info);
        }
        #endregion
        public class sdfsdfsdf
        {
            public string WarninsVoltage { get; set; }
            public string WarninsCurrent { get; set; }
            public string WarninsTemper { get; set; }
            public string WarninsTemper2 { get; set; }
            public string WarninsArc { get; set; }


            public string Warninsleakage { get; set; }

            public string Signal { get; set; }


            public string PushState { get; set; }
            public string WarninsRepresent { get; set; }
            public string DevNum { get; set; }
        }
    }
}
