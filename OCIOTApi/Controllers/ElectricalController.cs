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
    /// <summary>
    /// 电气对接接口控制器
    /// </summary>
    public class ElectricalController : ApiController
    {
        //// GET: Electrical
        //public ActionResult Index()
        //{
        //    return View();
        //}

        #region 接收电气平台推送的实时数据
        /// <summary>
        /// 接收电气平台推送的实时数据
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseJson<bool> GetTemporaryData([FromBody]FireControl_RealTimeData model)
        {
            Utility.WriteLog("实时数据接受到的编号为:" + model.DevNum);

            //1.接收实时数据存入数据库
            ResponseJson<bool> json = null;
            ResultDis<bool> result = null;
            try
            {
                var str = JsonConvert.SerializeObject(model);
                //添加记录
                AddMpArTable(str, model.DevNum);
                Utility.WriteLog(str);
                FireControl_RealTimeData_BLL objBLL = new FireControl_RealTimeData_BLL();
                FireControl_DeviceBLL dll = new FireControl_DeviceBLL();
                ResultDis<FireControl_Device> item = dll.GetDevIDByDevNum(model.DevNum);
                if (item.ResultData != null && item.ResultCode == ResultCode.Succ)
                {
                    if (item.ResultData.ValidDate < DateTime.Now)//过期时间大于当前时间)   
                    {
                        Utility.WriteLog("设备已过期，设备编号为:" + model.DevNum);
                        json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                        return json;
                    }
                    //A相电流
                    if (!string.IsNullOrEmpty(model.ACurrent))
                    {
                        var ACurrent = Convert.ToString(Convert.ToDouble(model.ACurrent));
                        model.ACurrent = string.IsNullOrWhiteSpace(ACurrent) ? model.ACurrent : ACurrent;
                    }
                    //B相电流
                    if (!string.IsNullOrEmpty(model.Bcurrent))
                    {
                        model.Bcurrent = Convert.ToString(Convert.ToDouble(model.Bcurrent));
                    }
                    //C相电流
                    if (!string.IsNullOrEmpty(model.Ccurrent))
                    {
                        model.Ccurrent = Convert.ToString(Convert.ToDouble(model.Ccurrent));
                    }
                    //温度1
                    if (!string.IsNullOrEmpty(model.Temperature1))
                    {
                        model.Temperature1 = Convert.ToString(Convert.ToDouble(model.Temperature1));
                    }
                    //温度2
                    if (!string.IsNullOrEmpty(model.Temperature2))
                    {
                        model.Temperature2 = Convert.ToString(Convert.ToDouble(model.Temperature2));
                    }
                    //温度3
                    if (!string.IsNullOrEmpty(model.Temperature3))
                    {
                        model.Temperature3 = Convert.ToString(Convert.ToDouble(model.Temperature3));
                    }
                    //温度4
                    if (!string.IsNullOrEmpty(model.Temperature4))
                    {
                        model.Temperature4 = Convert.ToString(Convert.ToDouble(model.Temperature4));
                    }
                    //状态
                    if (!string.IsNullOrEmpty(model.AlarmState))
                    {
                        model.AlarmState = Convert.ToString(model.AlarmState);
                    }
                    //信号
                    if (!string.IsNullOrEmpty(model.Signal))
                    {//信号直接加入到设备表中
                        item.ResultData.SignalGrand = model.Signal;
                    }

                    //如果该设备处于报警或者故障状态时,并且状态码为正常时将该设备修改为正常状态
                    Utility.WriteLog(item.ResultData.DevID);
                    model.DevID = item.ResultData.DevID;
                    ResultDis<FireControl_RealTimeData> resultDis = objBLL.GetObjByKey(item.ResultData.DevID);
                    FireControl_DQUpperLimitBLL objDQUBLL = new FireControl_DQUpperLimitBLL();
                    if (resultDis.ResultData == null)
                    {
                        Utility.WriteLog("未找到该电气设备的实时数据:电气设备ID=" + model.DevID);
                        return new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                        /* var UpDataResult =  objDQUBLL.GetList();
                          if (UpDataResult.ResultCode == ResultCode.Succ)
                          {
                              var UpData = UpDataResult.ResultData.FirstOrDefault();
                              model.ACurrentUpper = UpData.ACurrentUpper;
                              model.AVoltageUpper = UpData.AVoltageUpper;
                              model.BCurrentUpper = UpData.BCurrentUpper;
                              model.BVoltageUpper = UpData.BVoltageUpper;
                              model.CCurrentUpper = UpData.CCurrentUpper;
                              model.CVoltageUpper = UpData.CVoltageUpper;
                              model.LeakageUpper = UpData.LeakageUpper;
                              model.Temper1Upper = UpData.Temper1Upper;
                              model.Temper2Upper = UpData.Temper2Upper;
                              model.Temper3Upper = UpData.Temper3Upper;
                              model.Temper4Upper = UpData.Temper4Upper;
                              model.VoltageLowerLimit = UpData.VoltageLowerLimit;
                          }
                          result = objBLL.AddObj(model);*/
                    }
                    else
                    {
                        //如果新的实时数据为空那么不赋值
                        Utility.WriteLog(model.AlarmState);
                        var state = UpdateRealTimeData(model, 0);
                        if (state)
                        {
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                        }
                        else
                        {
                            Utility.WriteLog("电气实时数据更新失败");
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                        }
                    }

                    //用于判断该设备返回的数据状态码是否是正常状态
                    Regex regExp = new Regex("^[0 - 0]*$");
                    //如果只包含0，证明该设备的状态为正常状态
                    Utility.WriteLog("设备状态：" + model.AlarmState + ";设备编号：" + model.DevNum);
                    if (regExp.IsMatch(model.AlarmState))
                    {
                        //如果该设备正处于异常状态（非正常状态）改为正常状态
                        if (!item.ResultData.DeviceState.ToString().Contains("0"))
                        {
                            //修改设备状态为正常状态
                            item.ResultData.DeviceState = 0;
                            //并且修改报警状态给报警状态增加一条已经正常的记录
                            //找到所有未消音的报警记录

                            FireControl_WarninsPushBLL pushBll = new FireControl_WarninsPushBLL();
                            var Result = pushBll.SelectNotCloseList(item.ResultData.DevID, "1");
                            if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                            {
                                var PushList = Result.ResultData;
                                if (PushList == null || !PushList.Any())
                                {
                                    //没有未关闭的消息
                                    //做什么处理后序添加
                                    Utility.WriteLog("设备一直很正常....");
                                }
                                else
                                {
                                    Utility.WriteLog("设备变正常");
                                    foreach (var PushModel in PushList)
                                    {
                                        //该设备有未关闭和未消音的消息
                                        PushModel.WarIn_Type = 2;//设备消音
                                        pushBll.ModObj(PushModel.TitleID, "设备已正常", "设备报警解除", "0", 2);//该方法内置添加消息处理操作

                                        #region 修改设备状态  同步到设备管理系统
                                        DevStateModel devState = new DevStateModel();
                                        devState.deviceNumber = item.ResultData.DevNum;
                                        devState.recordID = PushModel.TitleID;

                                        FireControl_DeviceTypeBLL _DeviceTypeBLL = new FireControl_DeviceTypeBLL();
                                        ResultDis<FireControl_DeviceType> dis = _DeviceTypeBLL.GetObjByDeviceID(item.ResultData.DevID);
                                        if (dis.ResultCode == ResultCode.Succ)
                                        {
                                            devState.deviceTypeNumber = dis.ResultData.TypeNum;
                                        }

                                        EMApi.SendEM_UpdateDevState(devState);
                                        #endregion
                                    }
                                }
                            }
                            else
                            {
                                Utility.WriteLog("无该设备的未报警消息");
                            }
                        }
                    }
                    ///给设备记录最有后一次上传数据的时间
                    item.ResultData.LastUploadTime = DateTime.Now;
                    FireControl_DeviceBLL devdll = new FireControl_DeviceBLL();
                    devdll.ModObj(item.ResultData);
                }
                else
                {
                    Utility.WriteLog("在获取实时数据时,未能在全民消防数据库中查到该设备，设备编号为:" + model.DevNum);
                    json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                }

            }
            catch (Exception ex)
            {
                //ex.WriteErrLog();
                Utility.WriteLog(ex.Message);
                json = new ResponseJson<bool>(ResponseCode.Err, ex.Message);
                //log.Error("", ex);
            }
            return json;
        }

        #endregion

        #region 接收电气平台推送的故障信息
        /// <summary>
        /// 接收电气平台推送的故障信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseJson<bool> GetFaultData([FromBody]JObject jObject)
        {
            Utility.WriteLog("设备故障:");
            ResponseJson<bool> json = null;
            ResultDis<bool> result = null;
            try
            {
                //1.根据设备编号获取设备基本信息(判断该设备是否是我们的设备)
                FireControl_DeviceBLL dll = new FireControl_DeviceBLL();
                FireControl_WarninsPush model = new FireControl_WarninsPush();
                FireControl_DevBindUserBLL devUserdll = new FireControl_DevBindUserBLL();
                JToken jitem = "";
                foreach (var a in jObject)
                {
                    if (string.IsNullOrEmpty(a.Key))
                    {
                        jitem = a.Value;
                    }
                    else
                    {
                        jitem = a.Key;
                    }
                }
                Utility.WriteLog(jitem.ToString());
                JObject jo = (JObject)JsonConvert.DeserializeObject(jitem.ToString());
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
                model.WarninsState = "2";
                model.WarninsRepresent = string.IsNullOrEmpty(jo["WarninsRepresent"].ToString()) ? "" : jo["WarninsRepresent"].ToString();
                model.WarninsVoltage = string.IsNullOrEmpty(jo["WarninsVoltage"].ToString()) ? "" : jo["WarninsVoltage"].ToString();
                model.WarninsCurrent = string.IsNullOrEmpty(jo["WarninsCurrent"].ToString()) ? "" : jo["WarninsCurrent"].ToString();
                model.WarninsTemper = string.IsNullOrEmpty(jo["WarninsTemper"].ToString()) ? "" : jo["WarninsTemper"].ToString();
                model.WarninsTemper2 = string.IsNullOrEmpty(jo["WarninsTemper2"].ToString()) ? "" : jo["WarninsTemper2"].ToString();
                model.WarninsArc = string.IsNullOrEmpty(jo["WarninsArc"].ToString()) ? "" : jo["WarninsArc"].ToString();
                model.Warninsleakage = string.IsNullOrEmpty(jo["Warninsleakage"].ToString()) ? "" : jo["Warninsleakage"].ToString();
                model.DevNum = string.IsNullOrEmpty(jo["DevNum"].ToString()) ? "" : jo["DevNum"].ToString();
                Utility.WriteLog("设备故障:" + model.DevNum);

                if (!string.IsNullOrEmpty(model.DevNum))
                {
                    //警情记录id  同步设备管理系统用
                    string recordID = string.Empty;

                    string DevTypeNum = string.Empty;
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
                            Utility.WriteLog("设备继续报警:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "继续报警");
                            return json;
                        }


                        #region 将故障信息插入到数据库
                        //1.接收故障信息存入数据库
                        model.NewTitle = model.DevNum + "发生故障";
                        model.NewContent = model.DevNum + "因为" + model.WarninsRepresent + "发生故障";
                        model.PushDate = DateTime.Now;
                        model.DevID = item.ResultData.DevID;
                        model.WarninsState = "2";
                        //根据设备ID获取我的设备绑定地址
                        string AddressID = "";
                        ResultDis<GetDevPeopleUser> usermodel = devUserdll.GetDevPeople(model.DevID);
                        #region 将设备改为故障状态
                        //修改设备状态为故障状态
                        Utility.WriteLog("设备故障------" + model.DevNum);
                        item.ResultData.DeviceState = 2;
                        FireControl_DeviceBLL devdll = new FireControl_DeviceBLL();
                        devdll.ModObj(item.ResultData);
                        #endregion
                        if (!string.IsNullOrWhiteSpace(usermodel.ResultData.AddressID))
                        {
                            AddressID = usermodel.ResultData.AddressID;
                            model.TitleID = CommHelper.CreatePKID("tit");
                            #region 添加处理流程数据
                            FireControl_WarninsHandleBLL clbll = new FireControl_WarninsHandleBLL();
                            FireControl_WarninsHandle handle = new FireControl_WarninsHandle();
                            handle.HandlD = CommHelper.CreatePKID("han");
                            handle.Title = item.ResultData.DevName + "电气设备发生故障";
                            handle.Content = usermodel.ResultData.Address + "的电气设备发生故障";
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
                                json = new ResponseJson<bool>(ResponseCode.Err, result.ResultContent);
                            }
                            #endregion

                            #region 向客户发推送故障消息
                            //将故障信息推送给用户
                            var PeopleModel = usermodel.ResultData;
                            var People = new People();
                            People.Address = PeopleModel.Address;
                            People.Area = PeopleModel.Area;
                            People.City = PeopleModel.City;
                            People.Name = PeopleModel.UserName;
                            People.Phone = PeopleModel.Phone;
                            People.Province = PeopleModel.Province;
                            People.StreetName = PeopleModel.StreetName;
                            People.UserID = PeopleModel.UserID;
                            People.VillageName = PeopleModel.VillageName;
                            AlarmPush.GetPushStr(People, model.DevID, model.TitleID);
                            #endregion
                        }
                        else
                        {
                            Utility.WriteLog("地址不存在，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                        }
                        #region //同步到设备管理api



                        EMModel eMModel = new EMModel();
                        eMModel.deviceNumber = model.DevNum;
                        eMModel.deviceTypeNumber = DevTypeNum;
                        if (!string.IsNullOrEmpty(AddressID))
                        {
                            eMModel.unitID = EMApi.GetCommunityIDByADID(AddressID);
                        }
                        eMModel.recordID = string.IsNullOrEmpty(recordID) ? "1111111111111" : recordID;
                        EMApi.SendEM_Fault(eMModel);
                        #endregion

                    }
                }
                else
                {
                    Utility.WriteLog("在获取故障信息时,未能在全民消防数据库中查到该设备，设备编号为:" + model.DevNum);
                    json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                }

            }
            catch (Exception ex)
            {
                //ex.WriteErrLog();
                Utility.WriteLog(ex.Message);
                json = new ResponseJson<bool>(ResponseCode.Err, ex.Message);
                //log.Error("", ex);
            }
            return json;
        }
        #endregion

        #region 接收电气平台推送的上线状态
        /// <summary>
        /// 接收电气平台推送的上线状态
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseJson<bool> GetGoOnlineData([FromBody]JObject jObject)
        {
            ResponseJson<bool> json = null;
            ResultDis<bool> result = null;
            try
            {
                //1.根据设备编号获取设备基本信息(判断该设备是否是我们的设备)
                FireControl_DeviceBLL dll = new FireControl_DeviceBLL();
                FireControl_WarninsPush model = new FireControl_WarninsPush();
                FireControl_DevBindUserBLL devUserdll = new FireControl_DevBindUserBLL();
                JToken jitem = "";
                foreach (var a in jObject)
                {
                    if (string.IsNullOrEmpty(a.Key))
                    {
                        jitem = a.Value;
                    }
                    else
                    {
                        jitem = a.Key;
                    }
                }
                JObject jo = (JObject)JsonConvert.DeserializeObject(jitem.ToString());
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
                model.WarninsState = "4";
                model.WarninsRepresent = string.IsNullOrEmpty(jo["WarninsRepresent"].ToString()) ? "" : jo["WarninsRepresent"].ToString(); ;
                model.WarninsVoltage = "";
                model.WarninsCurrent = "";
                model.WarninsTemper = "";
                model.WarninsTemper2 = "";
                model.WarninsArc = "";
                model.Warninsleakage = "";
                model.DevNum = string.IsNullOrEmpty(jo["DevNum"].ToString()) ? "" : jo["DevNum"].ToString();


                if (!string.IsNullOrEmpty(model.DevNum))
                {

                    ResultDis<FireControl_Device> item = dll.GetDevIDByDevNum(model.DevNum);
                    if (item.ResultData != null && item.ResultCode == ResultCode.Succ)
                    {


                        if (item.ResultData.ValidDate < DateTime.Now)//过期时间大于当前时间)   
                        {
                            Utility.WriteLog("设备已过期，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                            return json;
                        }
                        //1.接收故障信息存入数据库
                        //标题
                        model.NewTitle = model.DevNum + "上线";
                        model.NewContent = model.DevNum + "上线";
                        model.PushDate = DateTime.Now;
                        model.DevID = item.ResultData.DevID;
                        model.WarninsState = "4";
                        model.TitleID = CommHelper.CreatePKID("pu");// TitleID;
                        //根据设备ID获取我的设备绑定地址
                        ResultDis<GetDevPeopleUser> usermodel = devUserdll.GetDevPeople(model.DevID);
                        //修改设备状态为上线状态(正常)
                        if (item.ResultData.DeviceState != 1 && item.ResultData.DeviceState != 2)
                        {
                            item.ResultData.DeviceState = 0;
                        }
                        FireControl_DeviceBLL devdll = new FireControl_DeviceBLL();
                        devdll.ModObj(item.ResultData);
                        if (!string.IsNullOrWhiteSpace(usermodel.ResultData.AddressID))
                        {
                            if (usermodel.ResultData != null)
                            {
                                model.WarInAdress = usermodel.ResultData.Address;
                            }
                            model.WarIn_Type = 0;
                            model.CreateTime = DateTime.Now;
                            model.CreatedBy = "System";
                            model.Deleted = 0;
                            model.AddressID = usermodel.ResultData.AddressID;
                            FireControl_WarninsPushBLL push = new FireControl_WarninsPushBLL();
                            result = push.AddObj(model);
                            if (result.ResultCode == ResultCode.Succ)
                            {
                                json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");

                            }
                            else
                            {
                                json = new ResponseJson<bool>(ResponseCode.Err, result.ResultContent);
                            }

                        }
                        else
                        {
                            Utility.WriteLog("地址不存在，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                        }

                    }
                    else
                    {
                        Utility.WriteLog("在获取上线状态信息时,未能在全民消防数据库中查到该设备，设备编号为:" + model.DevNum);
                        json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                    }

                }
                else
                {
                    Utility.WriteLog("在获取上线状态信息时,未能在全民消防数据库中查到该设备，设备编号为:" + model.DevNum);
                    json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                }

            }
            catch (Exception ex)
            {
                //ex.WriteErrLog();
                Utility.WriteLog(ex.Message);
                json = new ResponseJson<bool>(ResponseCode.Err, ex.Message);
                //log.Error("", ex);
            }
            return json;
        }

        #endregion

        #region 接收电气平台推送的下线状态
        /// <summary>
        /// 接收电气平台推送的下线状态
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseJson<bool> GetGoOfflineData([FromBody]JObject jObject)
        {
            ResponseJson<bool> json = null;
            ResultDis<bool> result = null;
            try
            {
                //1.根据设备编号获取设备基本信息(判断该设备是否是我们的设备)


                FireControl_DeviceBLL dll = new FireControl_DeviceBLL();
                FireControl_DevBindUserBLL devUserdll = new FireControl_DevBindUserBLL();
                FireControl_WarninsPush model = new FireControl_WarninsPush();
                JToken jitem = "";
                foreach (var a in jObject)
                {
                    if (string.IsNullOrEmpty(a.Key))
                    {
                        jitem = a.Value;
                    }
                    else
                    {
                        jitem = a.Key;
                    }
                }
                JObject jo = (JObject)JsonConvert.DeserializeObject(jitem.ToString());
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
                model.WarninsState = "";
                model.WarninsRepresent = string.IsNullOrEmpty(jo["WarninsRepresent"].ToString()) ? "" : jo["WarninsRepresent"].ToString(); ;
                model.WarninsVoltage = "";
                model.WarninsCurrent = "";
                model.WarninsTemper = "";
                model.WarninsTemper2 = "";
                model.WarninsArc = "";
                model.Warninsleakage = "";
                model.DevNum = string.IsNullOrEmpty(jo["DevNum"].ToString()) ? "" : jo["DevNum"].ToString();
                if (!string.IsNullOrEmpty(model.DevNum))
                {
                    ResultDis<FireControl_Device> item = dll.GetDevIDByDevNum(model.DevNum);
                    if (item.ResultData != null && item.ResultCode == ResultCode.Succ)
                    {
                        if (item.ResultData.ValidDate < DateTime.Now)//过期时间大于当前时间)   
                        {
                            Utility.WriteLog("设备已过期，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                            return json;
                        }
                        #region 将故障信息插入到数据库
                        //1.接收故障信息存入数据库
                        model.TitleID = CommHelper.CreatePKID("tit");
                        model.NewTitle = model.DevNum + "下线";
                        model.NewContent = model.DevNum + "下线";
                        model.PushDate = DateTime.Now;
                        model.DevID = item.ResultData.DevID;
                        model.WarninsState = "3";
                        //根据设备ID获取我的设备绑定地址
                        ResultDis<GetDevPeopleUser> usermodel = devUserdll.GetDevPeople(model.DevID);
                        #region  修改设备状态为掉线状态
                        //修改设备状态为掉线状态
                        if (item.ResultData.DeviceState != 1 && item.ResultData.DeviceState != 2)
                        {//设备正在报警
                            item.ResultData.DeviceState = 7;
                        }

                        FireControl_DeviceBLL devdll = new FireControl_DeviceBLL();
                        devdll.ModObj(item.ResultData);
                        #endregion
                        if (!string.IsNullOrWhiteSpace(usermodel.ResultData.AddressID))
                        {
                            #region 添加处理流程数据
                            FireControl_WarninsHandleBLL clbll = new FireControl_WarninsHandleBLL();
                            FireControl_WarninsHandle handle = new FireControl_WarninsHandle();
                            handle.HandlD = CommHelper.CreatePKID("han");
                            handle.Title = "电气设备离线";
                            handle.Content = usermodel.ResultData.Address + item.ResultData.DevName + "的电气设备离线";
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
                            model.WarIn_Type = 0;
                            model.CreateTime = DateTime.Now;
                            model.CreatedBy = "System";
                            model.Deleted = 0;
                            model.AddressID = usermodel.ResultData.AddressID;
                            FireControl_WarninsPushBLL push = new FireControl_WarninsPushBLL();
                            result = push.AddObj(model);
                            if (result.ResultCode == ResultCode.Succ)
                            {
                                json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");

                            }
                            else
                            {
                                json = new ResponseJson<bool>(ResponseCode.Err, result.ResultContent);
                            }
                            #endregion



                            #region 向客户发推送下线消息
                            AlarmPush.PublicPush(usermodel.ResultData.Phone, usermodel.ResultData.UserName, "1", usermodel.ResultData.Address, item.ResultData.DevName, "设备掉线", $"紧急通知!您的{item.ResultData.DevName}已经下线，地址{usermodel.ResultData.Address},请尽快处理!", "6", usermodel.ResultData.UserID, model.TitleID, "7");
                            #endregion

                            #region 将推送消息添加到推送记录表
                            //添加推送记录表
                            FireControl_WarninsPUserBLL puBll = new FireControl_WarninsPUserBLL();
                            var Model = new FireControl_WarninsPUser();
                            Model.CreatedBy = "0";
                            Model.CreateTime = DateTime.Now;
                            Model.Deleted = 0;
                            Model.PUID = CommHelper.CreatePKID("pu");
                            Model.TitleID = model.TitleID;// TitleID;
                            Model.UserID = usermodel.ResultData.UserID;
                            Model.UserPhone = usermodel.ResultData.Phone;
                            puBll.AddObj(Model);
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
                        Utility.WriteLog("在获取装备下线信息时,未能在全民消防数据库中查到该设备，设备编号为:" + model.DevNum);
                        json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                    }
                }
                else
                {
                    Utility.WriteLog("在获取装备下线信息时,未能在全民消防数据库中查到该设备，设备编号为:" + model.DevNum);
                    json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                }

            }
            catch (Exception ex)
            {
                //ex.WriteErrLog();
                Utility.WriteLog(ex.Message);
                json = new ResponseJson<bool>(ResponseCode.Err, ex.Message);
                Utility.WriteLog(ex.Message);
            }
            return json;
        }

        #endregion

        #region 接收电气平台推送的服务掉线状态
        /// <summary>
        /// 接收电气平台推送的故障信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseJson<bool> GetServiceData([FromBody]JObject jObject)
        {
            ResponseJson<bool> json = null;
            ResultDis<bool> result = null;
            try
            {
                FireControl_DeviceBLL dll = new FireControl_DeviceBLL();
                FireControl_WarninsPush model = new FireControl_WarninsPush();
                FireControl_DevBindUserBLL devUserdll = new FireControl_DevBindUserBLL();
                JToken jitem = "";
                foreach (var a in jObject)
                {
                    if (string.IsNullOrEmpty(a.Key))
                    {
                        jitem = a.Value;
                    }
                    else
                    {
                        jitem = a.Key;
                    }
                }
                JObject jo = (JObject)JsonConvert.DeserializeObject(jitem.ToString());
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
                model.WarninsState = "";
                model.WarninsRepresent = string.IsNullOrEmpty(jo["WarninsRepresent"].ToString()) ? "" : jo["WarninsRepresent"].ToString(); ;
                model.WarninsVoltage = "";
                model.WarninsCurrent = "";
                model.WarninsTemper = "";
                model.WarninsTemper2 = "";
                model.WarninsArc = "";
                model.Warninsleakage = "";
                model.DevNum = string.IsNullOrEmpty(jo["DevNum"].ToString()) ? "" : jo["DevNum"].ToString();
                //1.根据设备编号获取设备基本信息(判断该设备是否是我们的设备)
                Utility.WriteLog("因服务掉线将该设备改为故障状态，设备编号为：" + model.DevNum);
                if (!string.IsNullOrEmpty(model.DevNum))
                {
                    ResultDis<FireControl_Device> item = dll.GetDevIDByDevNum(model.DevNum);
                    if (item.ResultData != null && item.ResultCode == ResultCode.Succ)
                    {
                        if (item.ResultData.ValidDate < DateTime.Now)//过期时间大于当前时间)   
                        {
                            Utility.WriteLog("设备已过期，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                            return json;
                        }
                        //1.接收故障信息存入数据库
                        model.NewTitle = "因服务掉线导致" + model.DevNum + "下线";
                        model.NewContent = "因服务掉线导致" + model.DevNum + "下线";
                        model.PushDate = DateTime.Now;
                        model.DevID = item.ResultData.DevID;
                        model.TitleID = CommHelper.CreatePKID("tit");
                        model.WarninsState = "5";
                        //根据设备ID获取我的设备绑定地址
                        ResultDis<GetDevPeopleUser> usermodel = devUserdll.GetDevPeople(model.DevID);
                        //修改设备状态为下线状态
                        item.ResultData.DeviceState = 4;
                        FireControl_DeviceBLL devdll = new FireControl_DeviceBLL();
                        devdll.ModObj(item.ResultData);
                        if (!string.IsNullOrWhiteSpace(usermodel.ResultData.AddressID))
                        {
                            if (usermodel.ResultData != null)
                            {
                                model.WarInAdress = usermodel.ResultData.Address;
                            }
                            model.WarIn_Type = 0;
                            model.CreateTime = DateTime.Now;
                            model.CreatedBy = "System";
                            model.Deleted = 0;
                            model.AddressID = usermodel.ResultData.AddressID;
                            FireControl_WarninsPushBLL push = new FireControl_WarninsPushBLL();
                            result = push.AddObj(model);
                            if (result.ResultCode == ResultCode.Succ)
                            {
                                json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");

                            }
                            else
                            {
                                json = new ResponseJson<bool>(ResponseCode.Err, result.ResultContent);
                            }

                        }
                        else
                        {
                            Utility.WriteLog("地址不存在，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                        }
                    }
                    else
                    {
                        Utility.WriteLog("该设备在全民消防中不存在：" + model.DevNum);
                        json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                    }
                }
                else
                {
                    Utility.WriteLog("该设备在全民消防中不存在：" + model.DevNum);
                    json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                }

            }
            catch (Exception ex)
            {
                //  ex.WriteErrLog();
                Utility.WriteLog(ex.Message);
                json = new ResponseJson<bool>(ResponseCode.Err, ex.Message);
                //log.Error("", ex);
            }
            return json;
        }

        #endregion

        #region 接收电气平台推送的告警状态
        /// <summary>
        /// 接收电气平台推送的故障信息
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        [HttpPost]
        public ResponseJson<bool> GetCallThePoliceData([FromBody]JObject jObject)
        {
            string userIP1 = HttpContext.Current.Request.UserHostAddress;

            Utility.WriteLog("请求GetCallThePoliceData的访问者ip----------------------------------------" + userIP1);
            ResponseJson<bool> json = null;
            ResultDis<bool> result = null;
            try
            {
                //1.根据设备编号获取设备基本信息(判断该设备是否是我们的设备)
                FireControl_DeviceBLL dll = new FireControl_DeviceBLL();
                FireControl_DevBindUserBLL devUserdll = new FireControl_DevBindUserBLL();
                FireControl_WarninsPush model = new FireControl_WarninsPush();
                JToken jitem = "";
                foreach (var a in jObject)
                {
                    if (string.IsNullOrEmpty(a.Key))
                    {
                        jitem = a.Value;
                    }
                    else
                    {
                        jitem = a.Key;
                    }
                }
                Utility.WriteLog(jitem.ToString());
                JObject jo = (JObject)JsonConvert.DeserializeObject(jitem.ToString());
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
                                devdlle.ModObj(item.ResultData);
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

        #endregion
      



       
     

        #region 设置设备的报警上限值
        [HttpPost]
        public ResponseJson<bool> UpdateDeviceWarningCap([FromBody]JObject jObject)
        {
            ResponseJson<bool> json = null;
            ResultDis<bool> result = null;
            try
            {
                //1.根据设备编号获取设备基本信息(判断该设备是否是我们的设备)
                FireControl_DeviceBLL dll = new FireControl_DeviceBLL();
                FireControl_RealTimeData model = new FireControl_RealTimeData();
                Utility.WriteLog(jObject.ToString());
                JObject jo = (JObject)JsonConvert.DeserializeObject(jObject.ToString());
                //添加记录
                AddMpArTable(jObject.ToString(), jo["DevNum"].ToString());

                if (jo["PortType"].ToString().IsNullOrWhiteSpace())
                {
                    json = new ResponseJson<bool>(ResponseCode.Nomal, "PortType参数不能为空");
                    return json;
                }
                if (jo["PortNum"].ToString().IsNullOrWhiteSpace())
                {
                    json = new ResponseJson<bool>(ResponseCode.Nomal, "PortNum参数不能为空");
                    return json;
                }
                //                //{
                //                "DevNum": "0303190800001",
                //  "PortType": "Voltage",
                //  "LimitUpper": "260",
                //  "PortNum": "1",
                //  "LimitLower": "100"
                //}
                var PortType = jo["PortType"].ToString();//端口类型
                var PortNum = jo["PortNum"].ToString();//端口号
                var LimitUpper = jo["LimitUpper"].ToString();//上限
                var LimitLower = jo["LimitLower"].ToString();//下限
                model.DevNum = string.IsNullOrEmpty(jo["DevNum"].ToString()) ? "" : jo["DevNum"].ToString();
                if (!string.IsNullOrEmpty(model.DevNum))
                {
                    #region 将上限数据录入

                    FireControl_RealTimeData_BLL objBLL = new FireControl_RealTimeData_BLL();
                    FireControl_DeviceBLL Devdll = new FireControl_DeviceBLL();
                    ResultDis<FireControl_Device> itemResult = dll.GetDevIDByDevNum(model.DevNum);
                    if (itemResult.ResultData != null && itemResult.ResultCode == ResultCode.Succ)
                    {
                        if (itemResult.ResultData.ValidDate < DateTime.Now)//过期时间大于当前时间)   
                        {
                            Utility.WriteLog("设备已过期，设备编号为:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                            return json;
                        }
                        ResultDis<FireControl_RealTimeData> resultDis = objBLL.GetObjByKey(itemResult.ResultData.DevID);
                        if (resultDis.ResultData == null)
                        {
                            #region 没找到历史信息数据不对
                            Utility.WriteLog("设置设备的上限时设备数据未找到:" + model.DevNum);
                            json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                            return json;
                            #endregion
                        }
                        else
                        {
                            model.DevID = itemResult.ResultData.DevID;
                            #region 赋值
                            switch (PortType)
                            {
                                case "0":
                                    if (PortNum == "1")
                                    {
                                        model.AVoltageUpper = LimitUpper;//电压
                                        model.VoltageLowerLimit = LimitLower;//下限
                                    }
                                    break;
                                case "1":
                                    if (PortNum == "1")
                                    {
                                        model.ACurrentUpper = LimitUpper;//电流
                                    }
                                    break;
                                case "2":
                                    if (PortNum == "1")
                                    {
                                        model.LeakageUpper = LimitUpper;//漏电
                                    }
                                    break;
                                case "3":
                                    if (PortNum == "1")
                                    {
                                        model.Temper1Upper = LimitUpper;//温度
                                    }
                                    else if (PortNum == "2")
                                    {
                                        model.Temper2Upper = LimitUpper;//温度
                                    }
                                    else if (PortNum == "3")
                                    {
                                        model.Temper3Upper = LimitUpper;//温度
                                    }
                                    else if (PortNum == "4")
                                    {
                                        model.Temper4Upper = LimitUpper;//温度
                                    }

                                    break;
                                case "4":
                                    //功率
                                    break;
                                case "5":
                                    //功率累计
                                    break;
                                case "6":
                                //断路器
                                case "7":
                                    //信号
                                    break;

                            }
                            var state = UpdateRealTimeData(model, 0);
                            if (state)
                            {
                                json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                            }
                            else
                            {

                                Utility.WriteLog("电气实时数据更新失败");
                                json = new ResponseJson<bool>(ResponseCode.Nomal, "添加成功");
                            }

                            #endregion
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
                    Utility.WriteLog("设备编号为空");
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


        #endregion

        #region 保存上传数据

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

        /*
         
         */

        #endregion


        #region 更新电气实时数据

        public bool UpdateRealTimeData(FireControl_RealTimeData Data, int i)
        {
            i++;
            if (i < 5) //同一条更新重复次数大于5结束本次更新
            {
                FireControl_RealTimeData_BLL objBLL = new FireControl_RealTimeData_BLL();

                var OldModelResult = objBLL.GetObjByKey(Data.DevID);
                if (OldModelResult.ResultCode == ResultCode.Succ)
                {
                    var OldModle = OldModelResult.ResultData;
                    #region 更新实体
                    if (!string.IsNullOrWhiteSpace(Data.ACurrent))
                    {
                        OldModle.ACurrent = Data.ACurrent;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.ACurrentUpper))
                    {
                        OldModle.ACurrentUpper = Data.ACurrentUpper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.AlarmState))
                    {
                        OldModle.AlarmState = Data.AlarmState;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.ArcState))
                    {
                        OldModle.ArcState = Data.ArcState;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.AVoltage))
                    {
                        OldModle.AVoltage = Data.AVoltage;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.AVoltageUpper))
                    {
                        OldModle.AVoltageUpper = Data.AVoltageUpper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Bcurrent))
                    {
                        OldModle.Bcurrent = Data.Bcurrent;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.BCurrentUpper))
                    {
                        OldModle.BCurrentUpper = Data.BCurrentUpper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.BVoltage))
                    {
                        OldModle.BVoltage = Data.BVoltage;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.BVoltageUpper))
                    {
                        OldModle.BVoltageUpper = Data.BVoltageUpper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Ccurrent))
                    {
                        OldModle.Ccurrent = Data.Ccurrent;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.CCurrentUpper))
                    {
                        OldModle.CCurrentUpper = Data.CCurrentUpper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.CVoltage))
                    {
                        OldModle.CVoltage = Data.CVoltage;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.CVoltageUpper))
                    {
                        OldModle.CVoltageUpper = Data.CVoltageUpper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.DI1State))
                    {
                        OldModle.DI1State = Data.DI1State;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.DOState))
                    {
                        OldModle.DOState = Data.DOState;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.ForceTrip))
                    {
                        OldModle.ForceTrip = Data.ForceTrip;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.FunctionState))
                    {
                        OldModle.FunctionState = Data.FunctionState;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.IP1))
                    {
                        OldModle.IP1 = Data.IP1;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.IP2))
                    {
                        OldModle.IP2 = Data.IP2;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.IP3))
                    {
                        OldModle.IP3 = Data.IP3;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.IP4))
                    {
                        OldModle.IP4 = Data.IP4;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Leakage))
                    {
                        OldModle.Leakage = Data.Leakage;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.LeakageUpper))
                    {
                        OldModle.LeakageUpper = Data.LeakageUpper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.PORT))
                    {
                        OldModle.PORT = Data.PORT;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Reset))
                    {
                        OldModle.Reset = Data.Reset;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.SelfCheck))
                    {
                        OldModle.SelfCheck = Data.SelfCheck;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Silencing))
                    {
                        OldModle.Silencing = Data.Silencing;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Temper1Upper))
                    {
                        OldModle.Temper1Upper = Data.Temper1Upper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Temper2Upper))
                    {
                        OldModle.Temper2Upper = Data.Temper2Upper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Temper3Upper))
                    {
                        OldModle.Temper3Upper = Data.Temper3Upper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Temper4Upper))
                    {
                        OldModle.Temper4Upper = Data.Temper4Upper;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Temperature1))
                    {
                        OldModle.Temperature1 = Data.Temperature1;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Temperature2))
                    {
                        OldModle.Temperature2 = Data.Temperature2;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Temperature3))
                    {
                        OldModle.Temperature3 = Data.Temperature3;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Temperature4))
                    {
                        OldModle.Temperature4 = Data.Temperature4;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.TemperatureCLLS))
                    {
                        OldModle.TemperatureCLLS = Data.TemperatureCLLS;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.TripFunction))
                    {
                        OldModle.TripFunction = Data.TripFunction;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.TypeID))
                    {
                        OldModle.TypeID = Data.TypeID;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.VoltageLowerLimit))
                    {
                        OldModle.VoltageLowerLimit = Data.VoltageLowerLimit;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Power))
                    {
                        OldModle.Power = Data.Power;
                    }
                    if (!string.IsNullOrWhiteSpace(Data.Signal))
                    {
                        OldModle.Signal = Data.Signal;
                    }
                    #endregion
                    var UpdateStateResult = objBLL.ModObj(OldModle);
                    if (UpdateStateResult.ResultCode == ResultCode.Succ)
                    {
                        return true;
                    }
                    if (UpdateStateResult.ResultCode == ResultCode.Exists)
                    {
                        UpdateRealTimeData(Data, i);
                    }
                }
            }
            return false;
        }

        #endregion


    }
}