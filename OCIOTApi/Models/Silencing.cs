using OCIOTApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Web;

namespace OCIOTApi
{
    /// <summary>
    /// 消音协议
    /// </summary>
    public static class SilencingModel
    {
        /// <summary>
        /// 生成消音的协议
        /// </summary>
        /// <param name="DeviceID">设备ID</param>
        /// <returns></returns>
        public static string SilencingGenerate(string DeviceID) {
            DeviceID = DeviceID.ToUpper();
            //固定起始字符
         //   var top = "AA";

          //  String Z = "1300010002 6729701090C1 01 017F 0200 0100";//100000000C0  c19010702967 4728701090C1

            StringBuilder result = new StringBuilder();
            //result.Append("3A");
            //字节需反转 
            result.Append("1300");//字符数
            result.Append("01");
            result.Append("0002");//设备类型
            var DeviceByte = Utility.hexStringToByte(DeviceID);//设备ID数组
            Array.Reverse(DeviceByte);//数组翻转
            result.Append(Utility.ToHexString(DeviceByte));//设备ID
            result.Append("01");
            result.Append("017F");
            result.Append("0200");//string.Format("{0:D4}", sael)
            result.Append("0100");
            var DLRC = Utility.hexStringToByte(result.ToString());//待加密字符
            var SRC = ParseLRC(DLRC);
            var LSRC = ToHexString(SRC);
            result.Append(LSRC);
            result.Append("C3");
            return "AA" + result.ToString();
        }
        #region LRC校验
        /// <summary>
        ///  LRC校验
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private static byte ParseLRC(byte[] data)
        {


            byte crcResult = LRC(data);//计算CRC结果
            return crcResult;
          
        }
        #endregion
        #region LRC8和校验
        /// <summary>
        /// LRC8和校验
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static byte LRC(byte[] data)
        {
            byte lrc = 0;
            foreach (byte c in data)
            {
                lrc += c;
            }
            return (byte)-lrc;
        }
        #endregion
        #region byte[]转16进制格式string
        /// <summary>
        /// byte[]转16进制格式string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToHexString(byte bytes) // 0xae00cf => "AE00CF "
        {

            string s = bytes.ToString("X2");
            return s;

        }
        #endregion
    }
}