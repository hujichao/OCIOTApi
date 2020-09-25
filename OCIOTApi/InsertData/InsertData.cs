using OCIOTApi.EquipmentManagement;
using OCIOTApi.Http;
using OCIOTApi.Models;
using REG.N6.IFI.Yyqu.Model;
using REG.N6.IFT.TwpFControl.BLL;
using REG.N6.IFT.TwpFControl.Common;
using REG.N6.IFT.TwpFControl.Common.Enum;
using REG.N6.IFT.TwpFControl.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;

namespace OCIOTApi
{
    public class InsertData
    {

        #region 数据
        public static void Insert(ReceiveData info, string ACID)
        {
            int deviceType = 0;
            if (info != null)
            {
                var State = false;
                switch (info.DeviceType)
                {
                    case "0100":
                        State = GasData(info, ACID);
                        deviceType = 8;
                        break;
                    case "0200":
                        State = SmokeData(info, ACID);
                        deviceType = 7;
                        break;
                    default:
                        break;
                }
                if (State)
                {
                    JudgementAlarm(info, deviceType, ACID);
                }

            }
        }

        /// <summary>
        /// 烟感解析数据
        /// </summary>
        private static bool SmokeData(ReceiveData info, string ACID)
        {
            FireControl_MpArTable_BLL MpBll = new FireControl_MpArTable_BLL();
            ResultDis smokeAnalysisResult = null;
            SmokeAnalysisMessage smokeAnalysis = new SmokeAnalysisMessage();
            if (info != null)
            {
                smokeAnalysis.IMEI = info.IMEI;
                smokeAnalysis.DeviceType = info.DeviceType.ToString();
                smokeAnalysis.DataBlockCount = info.DataBlockCount;
                smokeAnalysis.MainProgramVersionIdentity = info.MainProgramVersionIdentity;
                smokeAnalysis.MainProgramVersion = info.MainProgramVersion;
                smokeAnalysis.HardwareVersionIdentity = info.HardwareVersionIdentity;
                smokeAnalysis.HardwareVersion = info.HardwareVersion;
                smokeAnalysis.BatchDataIdentity = info.BatchDataIdentity;
                if (info.Bodydata != null)
                {
                    smokeAnalysis.DataSegmentLength = info.Bodydata.DataSegmentLength;
                    smokeAnalysis.ProductLine = info.Bodydata.ProductLine;
                    smokeAnalysis.SmokeAlarmStatus = info.Bodydata.AlarmStatus;
                    smokeAnalysis.SmokeAlarmStatusText = info.Bodydata.AlarmStatusText;
                    smokeAnalysis.Reserve1 = info.Bodydata.Reserve1;
                    smokeAnalysis.Reserve2 = info.Bodydata.Reserve2;
                    smokeAnalysis.Reserve3 = info.Bodydata.Reserve3;
                    smokeAnalysis.WirelessType = info.Bodydata.WirelessType;
                    smokeAnalysis.WirelessTypeText = info.Bodydata.WirelessTypeText;
                    smokeAnalysis.WirelessValue = info.Bodydata.WirelessValue;
                    smokeAnalysis.CollectorBatteryStatus = info.Bodydata.CollectorBatteryStatus;
                    smokeAnalysis.CollectorBatteryValue = info.Bodydata.CollectorBatteryValue;
                    smokeAnalysis.CommunicationBatteryStatus = info.Bodydata.CommunicationBatteryStatus;
                    smokeAnalysis.CommunicationBatteryValue = info.Bodydata.CommunicationBatteryValue;
                }
                smokeAnalysis.CollectTime = DateTime.Now;
            }
            var Str = ConvertData<SmokeAnalysisMessage>.GetEntityToString(smokeAnalysis);
            Utility.WriteLog("烟感解析结果：" + Str);
            var Result = MpBll.GetObjByKey(ACID);
            if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
            {
                Utility.WriteLog("开始保存设备信息：");
                var Model = Result.ResultData;
                Model.PJsonData = Str;
                Model.ReportDate = info.Bodydata.CollectTime;
                Model.DevNum = info.IMEI;
                //判断是否已经存在该设备的该事件上传时间 存在就保存传入信息后跳过不进行后续操作
                if (info != null && info.Bodydata != null && info.Bodydata.AlarmStatus == "00")
                {
                    //未报警 不用管是重复数据直接跳过
                    Utility.WriteLog("未报警 不用管是重复数据直接跳过");
                }
                MpBll.ModObj(Model);//保存解析后的json串
            }
            //修改设备信息
            FireControl_DeviceBLL buildDeviceBLL = new FireControl_DeviceBLL();
            REG.N6.IFT.TwpFControl.Model.ResultDis<FireControl_Device> resultDevice = null;
            //1、 根据设备编号 查询设备    //修改
            resultDevice = buildDeviceBLL.GetDeviceTypeDetail(info.IMEI);
            FireControl_Device buildDevice = new FireControl_Device();
            var ReportDate = DateTime.MinValue;
            if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
            {
                ReportDate = Result.ResultData.ReportDate;
            }
            if (resultDevice.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
            {
                Utility.WriteLog("该设备在数据库中存在，设备IMEI为：" + info.IMEI);
                buildDevice = resultDevice.ResultData;
                if (buildDevice.LastUploadTime == ReportDate)
                {
                    Utility.WriteLog("已存在该设备上传信息：" + Str);
                    return false;
                }
            }


            if (buildDevice != null && !string.IsNullOrEmpty(buildDevice.DevID))
            {
                if (buildDevice.ValidDate < DateTime.Now && buildDevice.ValidDate > Convert.ToDateTime("1900-01-02"))
                {
                    Utility.WriteLog("---------设备已过期等待用户充值后提醒--------");
                    return false;
                }
                buildDevice.DeviceState = Convert.ToByte(info.Bodydata.AlarmStatus.Substring(1, 1));
                buildDevice.SignalGrand = smokeAnalysis.WirelessValue.ToString();
                buildDevice.CommBoardCharge = smokeAnalysis.CommunicationBatteryValue.ToString();
                if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                {
                    buildDevice.LastUploadTime = Result.ResultData.ReportDate;
                }
                buildDevice.LastUploadTime =DateTime.Now;
                buildDevice.AcquBoardCharge = smokeAnalysis.CollectorBatteryValue.ToString();

                #region Lyp添加 电量报警
                // 采集板电池状态或者通讯板电池状态 更改为阶梯报警 分别是20%，10%，5% 根据亚伟开会决定
                if (smokeAnalysis.CollectorBatteryValue <= 5 || smokeAnalysis.CommunicationBatteryValue <= 5)
                {
                    buildDevice.ElecAlarm = "本次提示为最后一次提醒，电量低于5%，设备即将停止运行，请及时购买并更换电池。";
                }
                else if (smokeAnalysis.CollectorBatteryValue <= 10 || smokeAnalysis.CommunicationBatteryValue <= 10)
                {
                    buildDevice.ElecAlarm = "电量低于10%，请及时购买并更换电池。";
                }
                else if (smokeAnalysis.CollectorBatteryValue <= 20 || smokeAnalysis.CommunicationBatteryValue <= 20)
                {
                    buildDevice.ElecAlarm = "电量低于20%，请及时购买并更换电池。";
                }
                #endregion

                var ResultDev = buildDeviceBLL.ModObj(buildDevice);
                if (ResultDev.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                {
                    Utility.WriteLog("设备信息修改成功---------------------------------");
                }
                //resultDevice
            }

            return true;
        }


        /// <summary>
        /// 燃气解析数据
        /// </summary>
        private static bool GasData(ReceiveData info, string ACID)
        {
            try
            {

                FireControl_MpArTable_BLL MpBll = new FireControl_MpArTable_BLL();
                ResultDis gasAnalysisResult = null;
                GasAnalysisMessage gasAnalysis = new GasAnalysisMessage();
                if (info != null)
                {
                    gasAnalysis.IMEI = info.IMEI;
                    gasAnalysis.DeviceType = info.DeviceType.ToString();
                    gasAnalysis.DataBlockCount = info.DataBlockCount;
                    gasAnalysis.MainProgramVersionIdentity = info.MainProgramVersionIdentity;
                    gasAnalysis.MainProgramVersion = info.MainProgramVersion;
                    gasAnalysis.HardwareVersionIdentity = info.HardwareVersionIdentity;
                    gasAnalysis.HardwareVersion = info.HardwareVersion;
                    gasAnalysis.BatchDataIdentity = info.BatchDataIdentity;
                    if (info.Bodydata != null)
                    {
                        gasAnalysis.DataSegmentLength = info.Bodydata.DataSegmentLength;
                        gasAnalysis.ProductLine = info.Bodydata.ProductLine;
                        gasAnalysis.GasAlarmStatus = info.Bodydata.AlarmStatus;
                        gasAnalysis.GasAlarmStatusText = info.Bodydata.AlarmStatusText;
                        gasAnalysis.Reserve1 = info.Bodydata.Reserve1;
                        gasAnalysis.Reserve2 = info.Bodydata.Reserve2;
                        gasAnalysis.Reserve3 = info.Bodydata.Reserve3;
                        gasAnalysis.WirelessType = info.Bodydata.WirelessType;
                        gasAnalysis.WirelessTypeText = info.Bodydata.WirelessTypeText;
                        gasAnalysis.WirelessValue = info.Bodydata.WirelessValue;
                        gasAnalysis.CollectorBatteryStatus = info.Bodydata.CollectorBatteryStatus;
                        gasAnalysis.CollectorBatteryValue = info.Bodydata.CollectorBatteryValue;
                        gasAnalysis.CommunicationBatteryStatus = info.Bodydata.CommunicationBatteryStatus;
                        gasAnalysis.CommunicationBatteryValue = info.Bodydata.CommunicationBatteryValue;
                    }
                    gasAnalysis.CollectTime = DateTime.Now;


                    //添加解析结果
                    var Str = ConvertData<GasAnalysisMessage>.GetEntityToString(gasAnalysis);
                    Utility.WriteLog("燃气解析结果：" + Str + "////消息id" + ACID);
                    var Result = MpBll.GetObjByKey(ACID);
                    if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                    {
                        Utility.WriteLog("获取到了上传信息");
                        var Model = Result.ResultData;
                        Model.PJsonData = Str;
                        Model.ReportDate = info.Bodydata.CollectTime;
                        Model.DevNum = info.IMEI;
                        //判断是否已经存在该设备的该事件上传时间 存在就保存传入信息后跳过不进行后续操作
                        var MPTabModleResult = MpBll.GetObjByIMEI(info.IMEI, Model.ReportDate);
                        if (info != null && info.Bodydata != null && info.Bodydata.AlarmStatus == "00")
                        {
                            //未报警 不用管是重复数据直接跳过
                            Utility.WriteLog("该设备数据重复,直接跳过,设备IMEI为：" + info.IMEI);
                        }
                        else
                        {
                            if (MPTabModleResult.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                            {//有值所以保存后不进行后续操作
                                MpBll.ModObj(Model);//保存解析后的json串
                                Utility.WriteLog("已存在该设备上传信息：" + Str);
                                return false;
                            }
                        }
                        var sta = MpBll.ModObj(Model);//保存解析后的json串
                        Utility.WriteLog("燃气解析结果保存：" + sta.ResultContent + "sss:" + info.Bodydata.CollectTime);

                    }
                    //修改设备信息
                    FireControl_DeviceBLL buildDeviceBLL = new FireControl_DeviceBLL();
                    REG.N6.IFT.TwpFControl.Model.ResultDis<FireControl_Device> resultDevice = null;
                    //1、 根据设备编号 查询设备    //修改
                    resultDevice = buildDeviceBLL.GetDeviceTypeDetail(info.IMEI);
                    FireControl_Device buildDevice = new FireControl_Device();
                    var ReportDate = DateTime.MinValue;
                    if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                    {
                        ReportDate = Result.ResultData.ReportDate;
                    }
                    if (resultDevice.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                    {
                        Utility.WriteLog("该设备在数据库中存在，设备IMEI为：" + info.IMEI);
                        buildDevice = resultDevice.ResultData;
                        if (buildDevice.LastUploadTime == ReportDate)
                        {
                            Utility.WriteLog("已存在该设备上传信息：" + Str);
                            return false;
                        }
                    }


                    if (buildDevice != null && !string.IsNullOrEmpty(buildDevice.DevID))
                    {
                        if (buildDevice.ValidDate < DateTime.Now && buildDevice.ValidDate > Convert.ToDateTime("1900-01-02"))
                        {
                            Utility.WriteLog("---------------------------------设备已过期等待用户充值后提醒--------");
                            return false;
                        }


                        buildDevice.DeviceState = Convert.ToByte(info.Bodydata.AlarmStatus.Substring(1, 1));
                        buildDevice.SignalGrand = gasAnalysis.WirelessValue.ToString();
                        buildDevice.CommBoardCharge = gasAnalysis.CommunicationBatteryValue.ToString();
                        buildDevice.AcquBoardCharge = gasAnalysis.CollectorBatteryValue.ToString();
                        buildDevice.LastUploadTime = DateTime.Now;

                        #region Lyp添加 电量报警
                        // 采集板电池状态或者通讯板电池状态 更改为阶梯报警 分别是20%，10%，5% 根据亚伟开会决定
                        if (gasAnalysis.CollectorBatteryValue <= 5 || gasAnalysis.CommunicationBatteryValue <= 5)
                        {
                            buildDevice.ElecAlarm = "本次提示为最后一次提醒，电量低于5%，设备即将停止运行，请及时购买并更换电池。";
                        }
                        else if (gasAnalysis.CollectorBatteryValue <= 10 || gasAnalysis.CommunicationBatteryValue <= 10)
                        {
                            buildDevice.ElecAlarm = "电量低于10%，请及时购买并更换电池。";
                        }
                        else if (gasAnalysis.CollectorBatteryValue <= 20 || gasAnalysis.CommunicationBatteryValue <= 20)
                        {
                            buildDevice.ElecAlarm = "电量低于20%，请及时购买并更换电池。";
                        }
                        #endregion

                        var ResultDev = buildDeviceBLL.ModObj(buildDevice);
                        if (ResultDev.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                        {
                            Utility.WriteLog("设备信息修改成功---------------------------------5--------");
                        }
                        //resultDevice
                    }
                }
                return true;
            }
            catch (Exception ex)
            {
                Utility.WriteLog("燃气解析报错：" + ex.Message);
                return false;
            }
        }
        /// <summary>
        /// 判断      
        /// </summary>ACID 消息记录id
        private static void JudgementAlarm(ReceiveData info, int deviceType, string ACID)
        {
            try
            {
                Utility.WriteLog("开始---------------------------------1--------");
                if (info.Bodydata != null)
                {
                    FireControl_DeviceBLL buildDeviceBLL = new FireControl_DeviceBLL();
                    FireControl_DevBindUserBLL DevBinBll = new FireControl_DevBindUserBLL();
                    REG.N6.IFT.TwpFControl.Model.ResultDis<FireControl_Device> resultDevice = null;
                    //1、 根据设备编号 查询设备    //修改
                    resultDevice = buildDeviceBLL.GetDevIDByDevNum(info.IMEI);
                    FireControl_Device buildDevice = new FireControl_Device();
                    if (resultDevice.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                    {
                        Utility.WriteLog("该设备在数据库中存在,设备IMEI为:" + info.IMEI);
                        buildDevice = resultDevice.ResultData;
                    }
                    if (buildDevice != null && !string.IsNullOrEmpty(buildDevice.DevID))
                    {  //警情记录id  同步设备管理系统用
                        string recordID = string.Empty;
                        string AddressID = "";
                        FireControl_DeviceBLL DevBLL = new FireControl_DeviceBLL();
                        if (info.Bodydata.AlarmStatus == "01")//判断设备是否报警
                        {
                            Utility.WriteLog("该设备发生报警,设备IMEI为" + info.IMEI);
                            //8燃气 7烟感
                            //查询是否该设备已经有未关闭的报警消息
                            FireControl_WarninsPushBLL pushBll = new FireControl_WarninsPushBLL();
                            var Result = pushBll.SelectNotClose(buildDevice.DevID, Dictoary.Warnin_Warning.ToString());
                            if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                            {
                                var PushModel = Result.ResultData;
                                if (PushModel != null && !string.IsNullOrWhiteSpace(PushModel.TitleID) && PushModel.WarIn_Type == Dictoary.Warnin_Warning)
                                {
                                    //有报警的消息记录
                                    Utility.WriteLog($"还在报警报警的消息记录id{PushModel.TitleID}---------------------------------2--------");
                                    Utility.WriteLog("//////////////////////////正常结束//////////////////////");
                                    return;
                                }
                            }
                            ///查询该设备是否在报警但消音
                            var SilencingResult = pushBll.SelectNotClose(buildDevice.DevID, Dictoary.Warnin_Silencing.ToString());
                            if (SilencingResult.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                            {
                                var PushModel = SilencingResult.ResultData;
                                if (PushModel != null && !string.IsNullOrWhiteSpace(PushModel.TitleID) && PushModel.WarIn_Type == Dictoary.Warnin_Silencing)
                                {
                                    //有报警的消息记录
                                    Utility.WriteLog($"还在报警但是已经消音了{PushModel.TitleID}---------------------------------2--------");
                                    Utility.WriteLog("把状态改为报警，添加一条处理记录");
                                    //改为报警状态  并增加报警推送记录
                                    PushModel.WarIn_Type = 1;//报警  警情状态 0正常 1报警 2已正常未关闭 3报警但消音 
                                    pushBll.ModObj(PushModel.TitleID, "设备报警", "设备继续报警", "0", 1);
                                    Utility.WriteLog("结束");
                                    return;
                                }
                            }
                            //查询设备对应的人员和地址id和地址详细 

                            var PeopleResult = DevBinBll.GetDevPeople(buildDevice.DevID);
                            if (PeopleResult.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                            {
                                AddressID = PeopleResult.ResultData.AddressID;
                                var People = PeopleResult.ResultData;
                                if (string.IsNullOrWhiteSpace(People.AddressID))
                                {
                                    Utility.WriteLog("该设备的地址为空人员为：---------------------------------3--------" + People.UserName + People.Phone);
                                    Utility.WriteLog("正常结束");
                                    return;
                                }
                                //给用户发送推送  告警 
                                //先获取到消息id
                                var TitleID = CommHelper.CreatePKID("tit");
                                #region 添加主消息记录
                                var PushModel = new REG.N6.IFT.TwpFControl.Model.FireControl_WarninsPush();
                                PushModel.CreateTime = DateTime.Now;
                                PushModel.PushDate = DateTime.Now;
                                PushModel.Deleted = 0;
                                PushModel.CreatedBy = "0";
                                PushModel.TitleID = TitleID;
                                PushModel.DevID = buildDevice.DevID;
                                PushModel.NewContent = "消息详情";
                                PushModel.NewTitle = buildDevice.DevName + "发生报警";
                                PushModel.WarInAdress = People.Address;
                                PushModel.WarIn_Type = 1; //消息报警开启
                                PushModel.WarninsState = "1";
                                PushModel.RecordValue = "100";//假值 目前只有公司燃气存在浓度
                                PushModel.AddressID = AddressID;
                                var sp = pushBll.AddObj(PushModel, out recordID);
                                if (sp.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                                {
                                    Utility.WriteLog("主报警消息保存成功---------------------zzzz--------");
                                    //添加敬请消息处理记录 设备报警 推送人员记录
                                    var PeopleName = "";
                                    if (People.PeopleList != null && People.PeopleList.Any())
                                    {
                                        PeopleName = string.Join(",", People.PeopleList.Where(f => f != null).Where(f => !string.IsNullOrWhiteSpace(f.FriendName)).Distinct()?.Select(f => f.FriendName).Distinct().ToList());
                                    }
                                    AddPusUserFirst(recordID, deviceType, People.Address, PeopleName, People.UserName);

                                    FireControl_PushProcess FireControl_PushProcessModel = new FireControl_PushProcess();
                                    FireControl_PushProcessModel.CreateTime = DateTime.Now;
                                    FireControl_PushProcessModel.DevID = buildDevice.DevID;
                                    FireControl_PushProcessModel.DevName = buildDevice.DevName;
                                    FireControl_PushProcessModel.isPush = 0;
                                    FireControl_PushProcessModel.PPID = CommHelper.CreatePKID("pp");
                                    FireControl_PushProcessModel.TitleID = TitleID;
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
                                    //进行推送 推送
                                    // AlarmPush.Send(People, buildDevice.DevName, recordID, buildDevice.DevID);
                                }
                                else
                                {
                                    Utility.WriteLog("消息保存失败---------------------xxxxx--------");
                                }
                                #endregion
                            }
                            else
                            {
                                Utility.WriteLog("未找到该设备对应的人员---------------------zzzz--------");
                            }
                            #region //同步到设备管理api
                            EMModel eMModel = new EMModel();
                            eMModel.deviceNumber = buildDevice.DevNum;
                            eMModel.deviceTypeNumber = buildDevice.TypeID;
                            if (!string.IsNullOrEmpty(AddressID))
                            {
                                eMModel.unitID = EMApi.GetCommunityIDByADID(AddressID);
                            }
                            eMModel.recordID = string.IsNullOrEmpty(recordID) ? "1111111111111" : recordID;
                            EMApi.SendEM_Alert(eMModel);
                            #endregion
                        }
                        else if (info.Bodydata.AlarmStatus == "02")
                        {
                            Utility.WriteLog("设备消音----********************************--------");
                        }
                        else if (info.Bodydata.AlarmStatus == "04")
                        {
                            var PeopleResult = DevBinBll.GetDevPeople(buildDevice.DevID);
                            if (PeopleResult.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                            {
                                var People = PeopleResult.ResultData;
                                if (string.IsNullOrWhiteSpace(People.AddressID))
                                {

                                    Utility.WriteLog("该设备的地址为空人员为：---------------------------------3--------" + People.UserName + People.Phone);
                                    Utility.WriteLog("正常结束");
                                    return;
                                }
                                AlarmPush.PublicPush("", People.UserName, "1", People.Address, buildDevice.DevName, "设备故障", $"紧急通知!您的{buildDevice.DevName}发生故障告警，地址{People.Address},请尽快处理!", "", People.UserID, "0", "7");
                            }


                            Utility.WriteLog("设备信号异常----********************************--------");
                        }
                        else if (info.Bodydata.AlarmStatus == "05")
                        {
                            Utility.WriteLog("设备自检----********************************--------");
                        }
                        else
                        {
                            //查询该设备是否有未关闭的消息记录
                            //如果存在未关闭的消息 此时 添加关闭消息记录并 修改该消息状态
                            FireControl_WarninsPushBLL pushBll = new FireControl_WarninsPushBLL();
                            var Result = pushBll.SelectNotClose(buildDevice.DevID, "1");
                            if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ && Result.ResultData != null)
                            {
                                var PushModel = Result.ResultData;
                                if (PushModel == null || string.IsNullOrWhiteSpace(PushModel.TitleID))
                                {
                                    //没有未关闭的消息
                                    //做什么处理后序添加
                                    Utility.WriteLog("设备一直很正常....");
                                }
                                else
                                {
                                    Utility.WriteLog("设备变正常");
                                    //该设备有未关闭和未消音的消息
                                    PushModel.WarIn_Type = 2;//设备消音
                                    pushBll.ModObj(PushModel.TitleID, "设备已正常", "设备报警解除", "0", 2);//该方法内置添加消息处理操作

                                    #region 修改设备状态  同步到设备管理系统
                                    DevStateModel devState = new DevStateModel();
                                    devState.deviceNumber = buildDevice.DevNum;
                                    devState.recordID = PushModel.TitleID;

                                    FireControl_DeviceTypeBLL _DeviceTypeBLL = new FireControl_DeviceTypeBLL();
                                    REG.N6.IFT.TwpFControl.Model.ResultDis<FireControl_DeviceType> dis = _DeviceTypeBLL.GetObjByDeviceID(buildDevice.DevID);
                                    if (dis.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                                    {
                                        devState.deviceTypeNumber = dis.ResultData.TypeNum;
                                    }

                                    EMApi.SendEM_UpdateDevState(devState);
                                    #endregion
                                }
                            }
                            else
                            {
                                var ResultXYin = pushBll.SelectNotClose(buildDevice.DevID, "3");//消音过得设备
                                if (Result.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                                {
                                    var PushModel = Result.ResultData;
                                    if (PushModel != null && string.IsNullOrWhiteSpace(PushModel.TitleID))
                                    {
                                        //有设备消音记录
                                        Utility.WriteLog("设备变正常");
                                        //该设备有未关闭和未消音的消息
                                        PushModel.WarIn_Type = 2;//设备消音
                                        pushBll.ModObj(PushModel.TitleID, "设备已正常", "设备报警解除", "0", 2);//该方法内置添加消息处理操作
                                        #region 修改设备状态  同步到设备管理系统
                                        DevStateModel devState = new DevStateModel();
                                        devState.deviceNumber = buildDevice.DevNum;
                                        devState.recordID = PushModel.TitleID;
                                        FireControl_DeviceTypeBLL _DeviceTypeBLL = new FireControl_DeviceTypeBLL();
                                        REG.N6.IFT.TwpFControl.Model.ResultDis<FireControl_DeviceType> dis = _DeviceTypeBLL.GetObjByDeviceID(buildDevice.DevID);
                                        if (dis.ResultCode == REG.N6.IFT.TwpFControl.Model.ResultCode.Succ)
                                        {
                                            devState.deviceTypeNumber = dis.ResultData.TypeNum;
                                        }
                                        EMApi.SendEM_UpdateDevState(devState);
                                        #endregion
                                    }
                                }
                                else
                                {
                                    Utility.WriteLog("无该设备的报警消息");
                                }
                            }

                            Utility.WriteLog("设备正常未报警");
                        }
                    }
                    else
                    {
                        Utility.WriteLog("没有该设备或设备未入库：" + info.IMEI);
                    }
                }
            }
            catch (Exception ex)
            {
                Utility.WriteLog(ex.Message);
                Utility.WriteLog("//////////////////////////异常结束//////////////////////");
            }
            Utility.WriteLog("//////////////////////////正常结束//////////////////////");
        }
        #endregion
        /// <summary>
        /// 报警消息处理记录 
        /// </summary>
        /// <param name="TitleID">消息id</param>
        /// <param name="deviceType">设备类型</param>
        /// <param name="address">报警地址</param>
        /// <param name="FriendNames">关联好友</param>
        private static void AddPusUserFirst(string TitleID, int deviceType, string address, string FriendNames, string UserName)
        {
            List<FireControl_WarninsHandle> InsertModle = new List<FireControl_WarninsHandle>();
            FireControl_WarninsHandleBLL WHandBLL = new FireControl_WarninsHandleBLL();
            //初次报警记录
            FireControl_WarninsHandle Model = new FireControl_WarninsHandle();
            Model.Content = address + $"的{ConvertControlType.ConvertNameString(deviceType)}发生报警";
            Model.CreateTime = DateTime.Now;
            Model.Deleted = 0;
            Model.CreatedBy = "0";
            Model.HandlD = CommHelper.CreatePKID("hd"); ;
            Model.Hand_Date = DateTime.Now;
            Model.Title = $"{ConvertControlType.ConvertNameString(deviceType)}发生报警"; ;
            Model.TitleID = TitleID;
            Model.UserID = "0"; //初次报警 0代表系统
            Model.Hand_Mode = 0;
            Model.Hand_Type = 0;
            InsertModle.Add(Model);
            //报警推送记录
            FireControl_WarninsHandle SecondModel = new FireControl_WarninsHandle();
            SecondModel.Content = $"报警已推送给{UserName},{(FriendNames != "" ? "和" + FriendNames : "")}";
            SecondModel.CreateTime = Model.Hand_Date.AddSeconds(1);
            SecondModel.Deleted = 0;
            SecondModel.CreatedBy = "0";
            SecondModel.HandlD = CommHelper.CreatePKID("hd"); ;
            SecondModel.Hand_Date = Model.Hand_Date.AddSeconds(1);
            SecondModel.Title = $"报警推送"; ;
            SecondModel.TitleID = TitleID;
            SecondModel.UserID = "0"; //初次报警 0代表系统
            SecondModel.Hand_Mode = 0;
            SecondModel.Hand_Type = 2;
            InsertModle.Add(SecondModel);
            var ss = WHandBLL.AddAllObj(InsertModle);
            Utility.WriteLog("消息处理：" + ss.ResultContent);

        }

        /// <summary>
        /// 关闭消息记录
        /// </summary>
        /// <param name="DevID">设备ID</param>
        private static void ClosePush(string DevID)
        {
            //查询该设备是否有未关闭的报警消息



        }


    }
}