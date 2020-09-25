using OCIOTApi.Http;
using OCIOTApi.Models;
using REG.N6.IFT.TwpFControl.BLL;
using REG.N6.IFT.TwpFControl.Common;
using REG.N6.IFT.TwpFControl.Common.Enum;
using REG.N6.IFT.TwpFControl.Model;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace OCIOTApi
{
    /// <summary>
    /// 设备报警推送处理类 把该类迁移到服务端 
    /// </summary>
    public class AlarmPush
    {
        private static string JGApi = ConfigurationManager.AppSettings["JGCHApi"].ToString();//修改为公司统一API
        /// <summary>
        /// 提醒
        /// </summary>
        /// <param name="People">人员信息</param>
        /// <param name="DevName">设备名称</param>
        /// <param name="TitleID">消息推送主id</param>
        /// <param name="DevID">设备ID</param> 2019年9月16日16:29:06 增加
        /// 
        public static void Send(GetDevPeopleUser People, string DevName, string TitleID, string DevID)//
        {
            FireControl_WarninsPUserBLL puBll = new FireControl_WarninsPUserBLL();

            var indexF = People.Address.IndexOf(People.Area) + People.Area.Length;
            var Address = People.Address.Substring(indexF);
            Utility.WriteLog("给业主发进行三种推送-----------------------------------------" + People.Phone + Address + People.UserName + DevName);
            #region 处理业主推送
            //   var Content = $"紧急通知!您的{DevName}发生报警，地址{People.Address},请尽快处理!";
            // 燃气报警 烟感报警  
            var PeopleModel = new People();
            PeopleModel.Name = People.UserName;
            PeopleModel.Phone = People.Phone;
            PeopleModel.Province = People.Province;
            PeopleModel.StreetName = People.StreetName;
            PeopleModel.Address = People.Address;
            PeopleModel.Area = People.Area;
            PeopleModel.City = People.City;
            PeopleModel.VillageName = People.VillageName;
            PeopleModel.UserID = People.UserID;
            GetPushStr(PeopleModel, DevID, TitleID);
            //新短信模板   T170317005189     [address]的[DevName][PushName],友情提示[Message]
            #endregion
            //紧急联系人推送,电话和短信不变
            if (People.PeopleList != null && People.PeopleList.Any())
            {
                //联系人
                var BeUserIDList = string.Join(",", People.PeopleList.Select(f => f.FriendID).Where(f => !string.IsNullOrWhiteSpace(f)).Distinct().ToList());
                var PeoPleFirendList = People.PeopleList.Select(f => new People { Name = f.FriendName, Phone = f.FriendPhone, UserID = f.FriendID }).ToList();
                PeoPleFirendList = PeoPleFirendList.Distinct().ToList();
                var FoList = new List<People>();

                foreach (var mod in PeoPleFirendList)
                {
                    if (FoList.Where(f => f.Phone == mod.Phone).Any())
                    {
                        Utility.WriteLog("联系人重复---------------------------------AS3--------" + mod.Name + People.Address + mod.Phone);
                        continue;
                    }
                    FoList.Add(mod);
                    Utility.WriteLog("联系人发短信---------------------------------AS3--------" + mod.Name + People.Address + mod.Phone);
                    //发送短信
                    HttpSend.ShortMessageSend(mod.Phone, Address, DevName, People.UserName, mod.Name, "2");

                    //打电话
                    sendTalePoone(mod.Phone, People.UserName, "2", People.Address, DevName, mod.Name, DevID);

                    //添加推送记录表

                    var Models = new FireControl_WarninsPUser();
                    Models.CreatedBy = "0";
                    Models.CreateTime = DateTime.Now;
                    Models.Deleted = 0;
                    Models.PUID = CommHelper.CreatePKID("pu");
                    Models.TitleID = TitleID;// TitleID;
                    Models.UserID = mod.UserID;
                    Models.UserPhone = mod.Phone;
                    puBll.AddObj(Models);
                }
            }
            //添加推送记录表

            var Model = new FireControl_WarninsPUser();
            Model.CreatedBy = "0";
            Model.CreateTime = DateTime.Now;
            Model.Deleted = 0;
            Model.PUID = CommHelper.CreatePKID("pu");
            Model.TitleID = TitleID;// TitleID;
            Model.UserID = People.UserID;
            Model.UserPhone = People.Phone;
            puBll.AddObj(Model);

        }
      
        /// <summary>
        /// 打电话
        /// </summary>
        /// <param name="Phone"></param>
        /// <param name="userName"></param>
        /// <param name="type">类型 1业主打电话 2给互助联系人 </param>
        /// <param name="address"></param>
        public static void sendTalePoone(string Phone, string userName, string type, string address, string DivName, string BeUserName = "", string DevID = "", string WarinType = "")
        {
            HttpSend.TalePhone(DivName, Phone, address, userName, type, BeUserName, DevID, WarinType);
        }


        /// <summary>
        /// 推送
        /// </summary>
        /// <param name="Phone"></param>
        /// <param name="userName"></param>
        /// <param name="type">类型 1业主打电话 2给互助联系人 </param>
        /// <param name="address"></param>
        /// NewType 消息类型 1 燃起烟感报警 6电气报警
        public static void PublicPush(string Phone, string userName, string type, string address, string DivName, string title, string content, string cucode, string BeUserID = "", string titleID = "", string NewType = "1")
        {//目前推送智慧给自己推送
            if (type == "1")
            {
                FireControl_WarninsHandleBLL WHandBLL = new FireControl_WarninsHandleBLL();
                Utility.WriteLog("极光--1----");
                Dictionary<string, string> dic1 = new Dictionary<string, string>();
                dic1.Add("content", content);
                dic1.Add("title", title);
                dic1.Add("type", NewType);
                dic1.Add("msgid", titleID);
                dic1.Add("cucode", BeUserID);
                dic1.Add("application", "8");
                dic1.Add("platformType", "3");
                dic1.Add("isCustomMsg", "0");
                Utility.WriteLog("极光--1推----" + BeUserID);
                new Task(() =>
                {
                    HttpSend.GetByRequest(JGApi, dic1, null, true);
                }).Start();
                if (NewType == "6")
                {
                    //报警推送记录
                    List<FireControl_WarninsHandle> InsertModle = new List<FireControl_WarninsHandle>();
                    FireControl_WarninsHandle SecondModel = new FireControl_WarninsHandle();
                    SecondModel.Content = $"报警已推送给{(string.IsNullOrWhiteSpace(userName) ? Phone : userName)}";
                    SecondModel.CreateTime = DateTime.Now;
                    SecondModel.Deleted = 0;
                    SecondModel.CreatedBy = "0";
                    SecondModel.HandlD = CommHelper.CreatePKID("hd");
                    SecondModel.Hand_Date = DateTime.Now.AddSeconds(1);
                    SecondModel.Title = $"报警推送"; ;
                    SecondModel.TitleID = titleID;
                    SecondModel.UserID = "0"; //初次报警 0代表系统
                    SecondModel.Hand_Mode = 0;
                    SecondModel.Hand_Type = 0;
                    InsertModle.Add(SecondModel);
                    var ss = WHandBLL.AddAllObj(InsertModle);
                    Utility.WriteLog("消息处理：" + ss.ResultContent);
                }
            }
        }


        /// <summary>
        /// 业主设备报警推送处理 (电话短信+极光推送)
        /// </summary>
        /// <param name="UserModel">业主信息</param>
        /// <param name="DevID">设备ID</param>
        /// <returns></returns>
        public static string GetPushStr(People UserModel, string DevID, string TitleID)
        {
            //语音电话模板是通过集合方式发送请求的 可以在数据库中添加模板 ,后期维护的话可直接改成数据表查询方式
            //先判断设备类型 
            var NewType = "";
            var DevName = ""; //设备名称简称
            FireControl_DeviceBLL DevBll = new FireControl_DeviceBLL();
            var str = "";
            var Message = ""; //短信报警解释
            var PhoneMessage = "";
            var DevResult = DevBll.GetObjByKey(DevID);
            if (DevResult.ResultCode == ResultCode.Succ)
            {
                var Device = DevResult.ResultData;
                DeviceType deviceType = CommHelper.StringToDeviceType(Device.TypeID);
                switch (deviceType)
                {
                    case DeviceType.单相电气火灾探测器:
                        //电气 不同报警类型提示文字不同
                        //电气开始时间 
                        var DQStart = "09:00";
                        //电气推送结束时间
                        var DQEnd = "22:00";
                        str = "";
                        NewType = "6";
                        DevName = "电气";
                        if (Convert.ToDateTime(DQStart) > DateTime.Now && DateTime.Now < Convert.ToDateTime(DQEnd))
                        {//可以进行推送
                         // 同一个设备一天只推送两次 间隔最少一小时
                            FireControl_PushRecordBLL pushRecBLL = new FireControl_PushRecordBLL();
                            var PushRecResult = pushRecBLL.GetPushRecordOneDayByDevIDAndUserID(DevID, UserModel.UserID);
                            if (PushRecResult.ResultCode == ResultCode.UnSucc)
                            {
                                var pushRec = PushRecResult.ResultData;
                                //找到记录 极光一天两次 间隔最少一小时   短信和电话一天一次
                                if (pushRec.Count == 1)
                                {//只有极光可以发送
                                    //还需要判断该报警是否是一小时以前的
                                    var time = pushRec.FirstOrDefault().CreateTime;
                                    var timeDiff = Utility.ExecDateDiff(DateTime.Now, time);
                                    if (timeDiff > 60)
                                    {//此时可以再推送一次
                                        SendDQ(UserModel, TitleID, NewType, DevName, DevID, "2");
                                    }
                                }
                                else if (pushRec.Count == 0)
                                {
                                    //都可以发送
                                    SendDQ(UserModel, TitleID, NewType, DevName, DevID, "1");
                                }
                            }
                            else
                            {
                                //未找到记录直接推送
                                SendDQ(UserModel, TitleID, NewType, DevName, DevID, "1");
                            }
                        }
                        break;
                    case DeviceType.独立式光电感烟火灾探测报警器:
                        // 烟感  1-4个烟感同时报警 三种推送的文字不同
                        FireControl_DevBindUserBLL devUserBLL = new FireControl_DevBindUserBLL();
                        var NumberResult = devUserBLL.GetYGAlarmNumberByDevID(DevID);//
                        var Number = Convert.ToInt32(NumberResult.ResultData);

                        str = "";
                        NewType = "1";
                        DevName = "烟感";
                        //推送
                        APPPush(UserModel, TitleID, NewType, DevName, DevID);
                        if (Number <= 1)
                        {
                            PhoneMessage =UserModel.Name.Replace("&","")+"&" + UserModel.Address.Replace(UserModel.Province, "").Replace("&", "") + "&" + "烟感探测器&正在报警&置小火于不顾终会酿成大灾,";
                            //玉龙小区1号楼二单元403室 的感烟探测器正在报警，友情提示电气火灾先断电、燃气火灾关阀门、家具火灾用水泼
                            DevName = "烟感设备";
                            Message = "电气火灾先断电、燃气火灾关阀门、家具火灾用水泼";
                        }
                        else if (Number == 2)
                        {
                            PhoneMessage = UserModel.Name.Replace("&", "") + "&" + UserModel.Address.Replace(UserModel.Province, "").Replace("&", "") + "&" + "两个烟感探测器&正在报警&";
                            //玉龙小区1号楼二单元403室 的两个感烟探测器同时在报警，友情提示应争分夺秒扑灭初期火灾，火势凶猛要迅速逃离现场
                            DevName = "两个感烟设备";
                            Message = "应争分夺秒扑灭初期火灾，火势凶猛要迅速逃离现场";
                        }
                        else if (Number >= 3)
                        {
                            PhoneMessage = UserModel.Name.Replace("&", "") + "&" + UserModel.Address.Replace(UserModel.Province, "").Replace("&", "") + "&" + "三个烟感探测器&正在报警&";
                            //玉龙小区1号楼二单元403室的三个感烟探测器同时在报警，友情提示您先冷静判断火灾类型、火势大小，报警电话要讲全
                            DevName = "多个感烟设备";
                            Message = "您先冷静判断火灾类型、火势大小，报警电话要讲全";
                        }

                        //短信
                        #region 短信
                        SendSmsModel SendSmsModel = new SendSmsModel();
                        SendSmsModel.AddressDetail = UserModel.Address.Replace(UserModel.Province + UserModel.City + UserModel.Area+UserModel.StreetName + UserModel.VillageName, "");
                        SendSmsModel.DevName = DevName;
                        SendSmsModel.PushName = "正在报警";
                        SendSmsModel.Phone = UserModel.Phone;
                        SendSmsModel.VillageName = UserModel.VillageName;
                        SendSmsModel.Message = Message;
                        HttpSend.sendSmsP1(SendSmsModel);
                        #endregion

                        #region 电话
                        //地址去掉省
                        HttpSend.TalePhoneP1(DevName, UserModel.Phone, UserModel.Address.Replace(UserModel.Province, ""), UserModel.Name, DevID, "1", PhoneMessage);

                        #endregion

                        //首页弹窗
                        FireControl_YGAlarmPromptBLL YGBLL = new FireControl_YGAlarmPromptBLL();
                        var YGList =  YGBLL.GetList();
                        if (YGList.ResultCode == ResultCode.Succ)
                        {
                            var YGText = YGList.ResultData;
                            var mod = YGText.Where(f => f.Number == Number).FirstOrDefault();
                            if (mod == null || string.IsNullOrWhiteSpace(mod.APID))
                            {
                                mod = YGText.OrderBy(f => f.Number).LastOrDefault();
                            }
                            FireControl_AlarmWindow AlarmWindow = new FireControl_AlarmWindow();
                            AlarmWindow.Address = UserModel.Address.Replace(UserModel.Province + UserModel.City + UserModel.Area + UserModel.StreetName, ""); //小区名+详细地址
                            AlarmWindow.AWID = CommHelper.CreatePKID("aw");
                            AlarmWindow.DevID = DevID;
                            AlarmWindow.DevName = DevName;
                            AlarmWindow.DevNum = Device.DevNum;
                            AlarmWindow.DevTypeID = Device.TypeID;
                            AlarmWindow.DevTypeName = "";
                            AlarmWindow.PromptText = mod.PromptText;
                            AlarmWindow.RecordID = TitleID;
                            AlarmWindow.Title = "您的" + DevName + "正在报警";
                            AlarmWindow.UserID = UserModel.UserID;

                            AddAlarmWindow(AlarmWindow);
                        }
                        break;
                    case DeviceType.独立式可燃气体探测器:
                        var PushName = "";
                        str = "";
                        NewType = "1";
                        DevName = "燃气";
                        //燃气 不同浓度报警提示不同
                        //没浓度的燃气报警显示最高报警信息
                        //查询当前报警的浓度
                        FireControl_WarninsPushBLL pushBll = new FireControl_WarninsPushBLL();
                        var PushResult = pushBll.GetObjByKey(TitleID);
                        var PushModel = PushResult.ResultData;
                        var RecordValue = Convert.ToInt32(PushModel.RecordValue); //浓度值

                        if (RecordValue >= 0 && RecordValue < 5)
                        {
                            /*
                        报警语音（王志您好，您家玉龙小区的燃气正在泄漏，如遇到明火会引起微型火灾，请尽快处理！）
                        报警短信（【全民消防】玉龙小区1号楼二单元403室的燃气正在泄漏，友情提示您立即关闭燃气阀门，开窗疏散燃气浓度）
                         */
                            PushName = "正在泄漏";
                            Message = "您立即关闭燃气阀门，开窗疏散燃气浓度";
                            PhoneMessage = UserModel.Name.Replace("&", "") + "&" + UserModel.Address.Replace(UserModel.Province, "").Replace("&", "") + "&"+ "燃气&正在泄漏&如遇到明火会引起微型火灾,";
                        }
                        else if (RecordValue >= 5 && RecordValue < 15)
                        {
                            /*
                         报警语音（王志您好，石家庄市玉龙小区1号楼二单元403室的燃气泄漏浓度已超过5%，如遇热源和明火有爆炸的危险，请尽快处理！）
                         报警短信（【全民消防】玉龙小区1号楼二单元403室的燃气泄漏浓度已超过5%，友情提示您不要开关电器，泄漏区不要出现热源和明火    
                         */
                            PushName = "泄漏浓度已超过5%";
                            Message = "您不要开关电器，泄漏区不要出现热源和明火";
                            PhoneMessage = UserModel.Name.Replace("&", "") + "&" + UserModel.Address.Replace(UserModel.Province, "").Replace("&", "") + "&" + "燃气&泄漏浓度已超过百分之五&如遇热源和明火有爆炸的危险,";
                        }
                        else if (RecordValue >= 15 && RecordValue < 30)
                        {
                            /*
                         报警语音（王志您好，石家庄市玉龙小区1号楼二单元403室的燃气泄漏浓度已超过15%，如吸入大量甲烷会使人窒息，请尽快处理！）
                         报警短信（【全民消防】玉龙小区1号楼二单元403室的燃气泄漏浓度已超过15%，友情提示您立即关闭燃气阀门，离开燃气泄漏区）    
                         */
                            PushName = "泄漏浓度已超过15%";
                            Message = "您立即关闭燃气阀门，离开燃气泄漏区";
                            PhoneMessage = UserModel.Name.Replace("&", "") + "&" + UserModel.Address.Replace(UserModel.Province, "").Replace("&", "") + "&" + "燃气&泄漏浓度已超过百分之十五&如吸入大量甲烷会使人窒息,";
                        }
                        else if (RecordValue >= 30 && RecordValue <= 100)
                        {
                            /*
                                 报警语音（王志您好，石家庄市玉龙小区1号楼二单元403室的燃气泄漏浓度已超过30%，有中毒窒息和火灾爆炸的双重危险，请尽快处理！）
                                 报警短信（【全民消防】玉龙小区1号楼二单元403室的燃气泄漏浓度已超过30%，友情提示您立即远离泄漏区打电话给物业处理）
                         */
                            PushName = "泄漏浓度已超过30%";
                            Message = "您立即远离泄漏区打电话给物业处理";
                            PhoneMessage = UserModel.Name.Replace("&", "") + "&" + UserModel.Address.Replace(UserModel.Province, "").Replace("&", "") + "&" + "燃气&泄漏浓度已超过百分之三十&有中毒窒息和火灾爆炸的双重危险,";
                        }
                        //推送
                        APPPush(UserModel, TitleID, NewType, DevName, DevID);


                        #region 短信
                        SendSmsModel SendSmsRQModel = new SendSmsModel();
                        SendSmsRQModel.AddressDetail = UserModel.Address.Replace(UserModel.Province + UserModel.City + UserModel.Area + UserModel.StreetName + UserModel.VillageName, "");
                        SendSmsRQModel.DevName = DevName;
                        SendSmsRQModel.PushName = PushName;
                        SendSmsRQModel.Phone = UserModel.Phone;
                        SendSmsRQModel.VillageName = UserModel.VillageName;
                        SendSmsRQModel.Message = Message;
                        HttpSend.sendSmsP1(SendSmsRQModel);
                        #endregion

                        #region 电话
                        HttpSend.TalePhoneP1(DevName, UserModel.Phone, UserModel.Address.Replace(UserModel.Province, ""), UserModel.Name, DevID, "2", PhoneMessage);
                        #endregion


                        //首页弹窗
                        //添加弹窗一条记录
                        //增加一个自定义推送

                        FireControl_RQAlarmPromptBLL RQBLL = new FireControl_RQAlarmPromptBLL();
                        var RQList = RQBLL.GetList();
                        if (RQList.ResultCode == ResultCode.Succ)
                        {
                            var RQText = RQList.ResultData;
                            var mod = RQText.Where(f => f.MinValue< RecordValue&&f.MaxValue>= RecordValue).FirstOrDefault();
                            if (mod == null || string.IsNullOrWhiteSpace(mod.APID))
                            {
                                mod = RQText.OrderBy(f => f.MaxValue).LastOrDefault();
                            }
                            FireControl_AlarmWindow AlarmWindow = new FireControl_AlarmWindow();
                            AlarmWindow.Address = UserModel.Address.Replace(UserModel.Province + UserModel.City + UserModel.Area + UserModel.StreetName, ""); ; //小区名+详细地址
                            AlarmWindow.AWID = CommHelper.CreatePKID("aw");
                            AlarmWindow.DevID = DevID;
                            AlarmWindow.DevName = DevName;
                            AlarmWindow.DevNum = Device.DevNum;
                            AlarmWindow.DevTypeID = Device.TypeID;
                            AlarmWindow.DevTypeName = "";
                            AlarmWindow.PromptText = mod.PromptText;
                            AlarmWindow.RecordID = TitleID;
                            AlarmWindow.Title = "您的" + DevName + PushName;
                            AlarmWindow.UserID = UserModel.UserID;
                            AddAlarmWindow(AlarmWindow);
                        }
                        break;
                }

                //所有设备的不同报警极光推送都一样 
                //紧急通知!您的｛1｝设备发生报警，地址｛2｝,请尽快处理｛1.设备类型简称；2.省市区＋小区名称；
            }


            return "";
        }

        #region 语音电话

        #endregion

        #region P1极光推送

        /// <summary>
        /// P1新版极光推送
        /// </summary>
        /// <param name="UserModel">用户基础信息</param>
        /// <param name="TitleID">消息ID</param>
        /// <param name="NewType">消息类型 1 燃起烟感报警 6电气报警</param>
        /// <param name="DevName"></param>
        public static void APPPush(People UserModel, string TitleID, string NewType, string DevName, string DevID)
        {//目前推送智慧给自己推送
         //添加推送记录
            var content = $"紧急通知!您的{DevName}设备发生报警，地址{UserModel.Province + UserModel.City + UserModel.Area + UserModel.VillageName},请尽快处理";
            FireControl_PushRecordBLL pushReBLL = new FireControl_PushRecordBLL();
            FireControl_PushRecord pushRe = new FireControl_PushRecord();
            pushRe.DevID = DevID;
            pushRe.Message = content;
            pushRe.Phone = UserModel.Phone;
            pushRe.PRID = CommHelper.CreatePKID("fr");
            pushRe.SendType = 1;
            pushRe.TitleID = TitleID;
            pushRe.UserID = UserModel.UserID;
            pushReBLL.AddObj(pushRe);
            Dictionary<string, string> dic1 = new Dictionary<string, string>();
            dic1.Add("content", content);
            dic1.Add("title", "设备报警");
            dic1.Add("type", NewType); //1为烟感燃气 6为电气
            dic1.Add("msgid", TitleID);
            dic1.Add("cucode", UserModel.UserID);
            dic1.Add("application", "8");
            dic1.Add("platformType", "3");
            dic1.Add("isCustomMsg", "0");
            Utility.WriteLog("极光--1推----" + UserModel.UserID + UserModel.Phone);
            new Task(() =>
            {
                HttpSend.GetByRequest(JGApi, dic1, null, true);
            }).Start();
            //添加极光推送记录
            if (NewType == "6")
            {
                FireControl_WarninsHandleBLL WHandBLL = new FireControl_WarninsHandleBLL();
                //报警推送记录
                List<FireControl_WarninsHandle> InsertModle = new List<FireControl_WarninsHandle>();
                FireControl_WarninsHandle SecondModel = new FireControl_WarninsHandle();
                SecondModel.Content = $"报警已推送给{(string.IsNullOrWhiteSpace(UserModel.Name) ? UserModel.Phone : UserModel.Name)}";
                SecondModel.CreateTime = DateTime.Now;
                SecondModel.Deleted = 0;
                SecondModel.CreatedBy = "0";
                SecondModel.HandlD = CommHelper.CreatePKID("hd");
                SecondModel.Hand_Date = DateTime.Now.AddSeconds(1);
                SecondModel.Title = $"报警推送"; ;
                SecondModel.TitleID = TitleID;
                SecondModel.UserID = "0"; //初次报警 0代表系统
                SecondModel.Hand_Mode = 0;
                SecondModel.Hand_Type = 0;
                InsertModle.Add(SecondModel);
                var ss = WHandBLL.AddAllObj(InsertModle);
                Utility.WriteLog("消息处理：" + ss.ResultContent);
            }

        }
        #endregion

        #region 短信

        #endregion

        #region 电气三方推送
        /// <summary>
        /// 
        /// </summary>
        /// <param name="UserModel"></param>
        /// <param name="TitleID"></param>
        /// <param name="NewType">消息类型 1 燃起烟感报警 6电气报警</param>
        /// <param name="DevName"></param>
        /// <param name="DevID"></param>
        /// <param name="state">类型 1 发送全部 2只发送极光</param>
        private static void SendDQ(People UserModel, string TitleID, string NewType, string DevName, string DevID, string state)
        {
            //极光
            APPPush(UserModel, TitleID, NewType, DevName, DevID);
            if (state == "1")
            {
                //短信
                #region 短信
                SendSmsModel SendSmsModel = new SendSmsModel();
                SendSmsModel.AddressDetail = UserModel.Address.Replace(UserModel.Province + UserModel.City + UserModel.Area + UserModel.VillageName, "");
                SendSmsModel.DevName = DevName;
                SendSmsModel.PushName = "正在报警";
                SendSmsModel.Phone = UserModel.Phone;
                SendSmsModel.VillageName = UserModel.VillageName;
                SendSmsModel.Message = "请合理使用电器，定期检查线路";
                HttpSend.sendSmsP1(SendSmsModel);
                #endregion

                //【全民消防】玉龙小区1号楼二单元403室的电气探测器正在报警，友情提示请合理使用电器，定期检查线路
                //电话

                #region 电话
                var PhoneMessage = UserModel.Name.Replace("&","")+"&"+ UserModel.Address.Replace(UserModel.Province, "").Replace("&","") + "&"+"电气探测器&正在报警&不予处理可能会引起电气火灾";
                HttpSend.TalePhoneP1(DevName, UserModel.Phone, UserModel.Address.Replace(UserModel.Province, ""), UserModel.Name, DevID, "3", PhoneMessage);

                #endregion

                //王志您好，石家庄市玉龙小区1号楼二单元403室的电气探测器正在报警，不予处理可能会引起电气火灾，请尽快处理
            }
        }


        #endregion


        #region 自定义消息推送
        /// <summary>
        /// 自定义消息推送
        /// </summary>
        public static void  AddAlarmWindow(FireControl_AlarmWindow model) {
            //添加弹窗window
            FireControl_AlarmWindowBLL alarmWinBLL = new FireControl_AlarmWindowBLL();
            model.AWID = CommHelper.CreatePKID("aw");
            var stateResult = alarmWinBLL.AddObj(model);
            if (stateResult.ResultCode == ResultCode.Succ)
            {
                Dictionary<string, string> dic1 = new Dictionary<string, string>();
                dic1.Add("type", "9");
                dic1.Add("cucode", model.UserID);
                dic1.Add("content", "消息");
                dic1.Add("title", "消息");
                dic1.Add("msgid", model.RecordID);
                dic1.Add("application", "8");
                dic1.Add("platformType", "0");
                dic1.Add("isCustomMsg", "1");
                new Task(() =>
                {
                    HttpSend.GetByRequest(JGApi, dic1, null, true);
                }).Start();
            }
        }

        #endregion
    }
}