using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using REG.N6.IFI.Yyqu.Model;

namespace OCIOTApi.Models
{
    public class ReceiveData
    {
        #region 属性
        /// <summary>
        /// 设备IMEI 唯一标识编码
        /// </summary>
        public string IMEI { get; set; }
        /// <summary>
        /// 设备类型
        /// </summary>
        public string DeviceType { get; set; }
        /// <summary>
        /// 数据块计数  
        /// </summary>
        public int DataBlockCount { get; set; }
        /// <summary>
        /// 主程序版本的标识段为0x0005
        /// </summary>
        public string MainProgramVersionIdentity { get; set; }
        /// <summary>
        /// 主程序版本
        /// </summary>
        public string MainProgramVersion { get; set; }
        /// <summary>
        /// 硬件版本的标识段为0x0006
        /// </summary>
        public string HardwareVersionIdentity { get; set; }
        /// <summary>
        /// 硬件版本
        /// </summary>
        public string HardwareVersion { get; set; }

        /// <summary>
        /// 批量数据传送的标识段为0x7F00
        /// </summary>
        public string BatchDataIdentity { get; set; }
        /// <summary>
        /// 消息体信息
        /// </summary>
        public BodyData Bodydata { get; set; }


        #endregion

        public bool Parse(byte[] data,string alamdate, string ACID)
        {
            bool flag = true;
            try
            {
                byte[] deviceType = new byte[2];
                Array.ConstrainedCopy(data, 4, deviceType, 0, 2);
                DeviceType = Utility.byte2DeviceType(deviceType);//设备类型

                byte[] imei = new byte[6];
                Array.ConstrainedCopy(data, 6, imei, 0, 6);
                IMEI = Utility.byte2IMEI(imei);//设备编码


                byte[] dataBlockCount = new byte[1];
                Array.ConstrainedCopy(data, 12, dataBlockCount, 0, 1);
                DataBlockCount = byte2WirelessValue(dataBlockCount); //主程序版本、硬件版本、批量数据传送共3个数据块


                byte[] mainProgramVersionIdentity = new byte[2];
                Array.ConstrainedCopy(data, 13, mainProgramVersionIdentity, 0, 2);
                Array.Reverse(mainProgramVersionIdentity);
                MainProgramVersionIdentity = Utility.ToHexString(mainProgramVersionIdentity);
                if (MainProgramVersionIdentity == "0005")
                {
                    byte[] mainProgramVersion = new byte[4];
                    Array.ConstrainedCopy(data, 15, mainProgramVersion, 0, 4);
                    MainProgramVersion = Utility.byte2Version(mainProgramVersion);
                }


                byte[] hardwareVersionIdentity = new byte[2];
                Array.ConstrainedCopy(data, 19, hardwareVersionIdentity, 0, 2);
                Array.Reverse(hardwareVersionIdentity);
                HardwareVersionIdentity = Utility.ToHexString(hardwareVersionIdentity);
                if (HardwareVersionIdentity == "0006")
                {
                    byte[] hardwareVersion = new byte[4];
                    Array.ConstrainedCopy(data, 21, hardwareVersion, 0, 4);
                    HardwareVersion = Utility.byte2Version(hardwareVersion);
                }


                byte[] batchDataIdentity = new byte[2];
                Array.ConstrainedCopy(data, 25, batchDataIdentity, 0, 2);
                Array.Reverse(batchDataIdentity);
                BatchDataIdentity = Utility.ToHexString(batchDataIdentity);
                if (BatchDataIdentity == "7F00")
                {
                    byte[] bytedatastatistics = new byte[13];
                    Array.ConstrainedCopy(data, 27, bytedatastatistics, 0, 13);
                    Bodydata = new BodyData();
                    Bodydata = byte2DataStatistics(bytedatastatistics, DeviceType);
                    Bodydata.CollectTime = DateTime.Parse(alamdate);
                }

                ReceiveData my = new ReceiveData();
                my.IMEI = IMEI;
                my.DeviceType = DeviceType;
                my.DataBlockCount = DataBlockCount;
                my.MainProgramVersionIdentity = MainProgramVersionIdentity;
                my.MainProgramVersion = MainProgramVersion;
                my.HardwareVersionIdentity = HardwareVersionIdentity;
                my.HardwareVersion = HardwareVersion;
                my.BatchDataIdentity = BatchDataIdentity;
                my.Bodydata = new BodyData();
                my.Bodydata = Bodydata;
                InsertData.Insert(my, ACID);
            }
            catch (Exception ex)
            {
                flag = false;
            }
            return flag;
        }
        #region 解析数据
        /// <summary>
        /// 解析数据
        /// </summary>
        /// <param name="inbyte"></param>
        /// <param name="deviceType">设备类型</param>
        /// <returns></returns>
        private static BodyData byte2DataStatistics(byte[] inbyte,string deviceType)
        {
            if (inbyte.Length < 13)
            {
                return null;
            }
            BodyData data = new BodyData();
            byte[] dataSegmentLength = new byte[2];
            Array.ConstrainedCopy(inbyte, 0, dataSegmentLength, 0, 2);
            data.DataSegmentLength = ConvertDataLength(dataSegmentLength);
            if (data.DataSegmentLength == "11")
            {
                byte[] productLine = new byte[1];
                Array.ConstrainedCopy(inbyte, 2, productLine, 0, 1);
                data.ProductLine = Utility.ToHexString(productLine);

                byte[] smokeAlarmStatus = new byte[1];
                Array.ConstrainedCopy(inbyte, 3, smokeAlarmStatus, 0, 1);
                data.AlarmStatus = Utility.ToHexString(smokeAlarmStatus);
                data.AlarmStatusText = ConvertAlarmStatusText(data.AlarmStatus, deviceType);

                byte[] reserve1 = new byte[1];
                Array.ConstrainedCopy(inbyte, 4, reserve1, 0, 1);
                data.Reserve1 = Utility.ConvertString(reserve1[0], 10);

                byte[] reserve2 = new byte[1];
                Array.ConstrainedCopy(inbyte, 5, reserve2, 0, 1);
                data.Reserve2 = Utility.ConvertString(reserve2[0], 10);

                byte[] reserve3 = new byte[1];
                Array.ConstrainedCopy(inbyte, 6, reserve3, 0, 1);
                data.Reserve3 = Utility.ConvertString(reserve3[0], 10);

                byte[] wirelessType = new byte[1];
                Array.ConstrainedCopy(inbyte, 7, wirelessType, 0, 1);
                data.WirelessType = Utility.ToHexString(wirelessType);
                data.WirelessTypeText = ConvertWirelessTypeText(data.WirelessType);

                byte[] wirelessValue = new byte[1];
                Array.ConstrainedCopy(inbyte, 8, wirelessValue, 0, 1);
                data.WirelessValue = byte2WirelessValue(wirelessValue); 

                byte[] collectorBatteryStatus = new byte[1];
                Array.ConstrainedCopy(inbyte, 9, collectorBatteryStatus, 0, 1);
                data.CollectorBatteryStatus = Convert.ToInt32(collectorBatteryStatus[0].ToString());

                byte[] collectorBatteryValue = new byte[1];
                Array.ConstrainedCopy(inbyte, 10, collectorBatteryValue, 0, 1);
                data.CollectorBatteryValue = byte2WirelessValue(collectorBatteryValue);

                byte[] communicationBatteryStatus = new byte[1];
                Array.ConstrainedCopy(inbyte, 11, communicationBatteryStatus, 0, 1);
                data.CommunicationBatteryStatus = Convert.ToInt32(communicationBatteryStatus[0].ToString());

                byte[] communicationBatteryValue = new byte[1];
                Array.ConstrainedCopy(inbyte, 12, communicationBatteryValue, 0, 1);
                data.CommunicationBatteryValue = Convert.ToInt32(communicationBatteryValue[0].ToString());
            }

            return data;
        }
        #endregion

        #region 数据段长度
        /// <summary>
        /// 数据段长度 
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        private static string ConvertDataLength(byte[] inbyte)
        {
            if (inbyte.Length != 2)
            {
                return null;
            }

            string ip1 = inbyte[0].ToString();
            string ip2 = inbyte[1].ToString();
            byte length = Convert.ToByte(ip2 + ip1);
            string str = Utility.ConvertString(length, 10);
            return str;
        }
        #endregion

        #region 产品系列编号
        /// <summary>
        /// 产品系列编号 
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        private static string ConvertProductLine(byte[] inbyte)
        {
            if (inbyte.Length != 1)
            {
                return null;
            }
            string str = inbyte[0].ToString();
            return str;
        }
        #endregion

        #region 根据字节换取状态信息
        /// <summary>
        /// 根据字节换取状态信息
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        private static string ConvertAlarmStatusText(string status,string deviceType)
        {
            if (deviceType == "0100")
            {
                switch (status)
                {
                    case "00":
                        return "无报警";
                    case "01":
                        return "燃气报警";
                    case "02":
                        return "消音";
                    case "03":
                        return "预留";
                    case "04":
                        return "信号异常报警";
                    case "05":
                        return "自检";
                    case "06":
                        return "预留";
                    default:
                        return "";
                }
            }
            else if (deviceType == "0200")
            {
                switch (status)
                {
                    case "00":
                        return "无报警";
                    case "01":
                        return "烟雾报警";
                    case "02":
                        return "消音";
                    case "03":
                        return "预留";
                    case "04":
                        return "信号异常报警";
                    case "05":
                        return "自检";
                    case "06":
                        return "预留";
                    default:
                        return "";
                }
            }
            return "";
        }
        #endregion

        #region 根据字节换取状态信息
        /// <summary>
        /// 根据字节换取状态信息
        /// </summary>
        /// <param name="company"></param>
        /// <returns></returns>
        private static string ConvertWirelessTypeText(string type)
        {

            switch (type)
            {
                case "00":
                    return "无";
                case "01":
                    return "ZigBee";
                case "02":
                    return "NB-IoT";
                case "03":
                    return "Lora";
                case "04":
                    return "GPRS";
                case "05":
                    return "Z-Wave";
                default:
                    return "";
            }
        }
        #endregion

        #region 解析无线模块信号值
        /// <summary>
        /// 解析无线模块信号值
        /// </summary>
        /// <param name="inbyte"></param>
        /// <returns></returns>
        public static int byte2WirelessValue(byte[] inbyte)
        {
            if (inbyte.Length != 1)
            {
                return 0;
            }
            string signal = inbyte[0].ToString();
            int str = Convert.ToInt32(signal);
            return str;
        }
        #endregion

        #region 解析采集板电池状态
        /// <summary>
        /// 解析采集板电池状态
        /// </summary>
        /// <param name="inbyte"></param>
        /// <returns></returns>
        public static int byte2CollectorBatteryStatus(byte[] inbyte)
        {
            if (inbyte.Length != 1)
            {
                return 0;
            }
            string signal = Utility.ConvertString(inbyte[0], 16);
            int str = Convert.ToInt32(signal);
            return str;
        }
        #endregion
    }

    public class BodyData
    {
        /// <summary>
        /// 数据段长度:值固定为11Byte
        /// </summary>
        public string DataSegmentLength { get; set; }
        /// <summary>
        /// 产品系列编号:值固定位0x00
        /// </summary>
        public string ProductLine { get; set; }
        /// <summary>
        /// 烟雾报警状态
        /// </summary>
        public string AlarmStatus { get; set; }
        /// <summary>
        /// 烟雾报警状态展示
        /// </summary>
        public string AlarmStatusText { get; set; }
        /// <summary>
        /// 预留1
        /// </summary>
        public string Reserve1 { get; set; }

        /// <summary>
        /// 预留2
        /// </summary>
        public string Reserve2 { get; set; }
        /// <summary>
        /// 预留3
        /// </summary>
        public string Reserve3 { get; set; }
        /// <summary>
        /// 无线模块类型 00=无,01=预留,02=NB-IoT
        /// </summary>
        public string WirelessType { get; set; }
        /// <summary>
        /// 无线模块类型内容 00=无,01=预留,02=NB-IoT
        /// </summary>
        public string WirelessTypeText { get; set; }
        /// <summary>
        /// 无线模块信号值 信号强度，有效范围0-31，最大值99
        /// </summary>
        public int WirelessValue { get; set; }
        /// <summary>
        /// 采集板电池状态 0-电池电量低报警；1-电池电量高
        /// </summary>
        public int CollectorBatteryStatus { get; set; }
        /// <summary>
        /// 采集板电池电量 百分比形式0到100
        /// </summary>
        public int CollectorBatteryValue { get; set; }
        /// <summary>
        /// 通讯板电池状态 0-电池电量低报警；1-电池电量高
        /// </summary>
        public int CommunicationBatteryStatus { get; set; }
        /// <summary>
        /// 通讯板电池电量 百分比形式0到100
        /// </summary>
        public int CommunicationBatteryValue { get; set; }
        /// <summary>
        /// 时间
        /// </summary>
        public DateTime CollectTime { get; set; }

    }
}