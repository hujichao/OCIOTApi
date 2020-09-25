using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;

namespace OCIOTApi.Models
{
    public class Utility
    {
        #region 写日志
        internal static void WriteLog(string content, string fileName = "")
        {
            string path = AppDomain.CurrentDomain.BaseDirectory;
            string logDir = "log";
            path = Path.Combine(path, logDir);
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            try
            {
                StringBuilder log = new StringBuilder();
                log.Append("================BEGIN");
                log.Append(content);
                log.Append("================END ");
                log.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                log.Append("================");
                log.Append(Environment.NewLine);

                fileName = string.IsNullOrWhiteSpace(fileName) ? DateTime.Now.ToString("yyyyMMdd")+ "日" : fileName;
                fileName = fileName + ".txt";
                string file = Path.Combine(path, fileName);
                System.IO.File.AppendAllText(file, log.ToString(), System.Text.Encoding.Default);
            }
            catch { }
        }
        #endregion

        #region 字节数据组与字符串互转
        /// <summary>
        /// 字符串转字节数组
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static byte[] ConvertStringToBytes(string str)
        {
            List<byte> bytes = new List<byte>();
            for (int index = 0; index < str.Length; index += 2)
            {
                string s = str.Substring(index, 2);
                bytes.Add((byte)Convert.ToInt32(s, 16));

            }
            return bytes.ToArray();
        }
        /// <summary>
        /// 字节数据组转字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <param name="split">字节数组分隔符</param>
        /// <returns></returns>
        public static string ConvertBytesToString(byte[] bytes, string split = "")
        {
            StringBuilder str = new StringBuilder();
            foreach (byte b in bytes)
            {
                str.Append(int.Parse(b.ToString()).ToString("X").PadLeft(2, '0'));
                str.Append(split);
            }
            return str.ToString();
        }

        #endregion

        #region 比较字符数组是否相同
        /// <summary>
        /// 比较字符数组是否相同
        /// </summary>
        /// <param name="data1"></param>
        /// <param name="data2"></param>
        /// <returns></returns>
        public static bool EqualsBytes(byte[] data1, byte[] data2)
        {
            if (data1 == null && data2 == null)
            {
                return true;
            }
            if (data1 == null || data2 == null)
            {
                return false;
            }
            if (data1 == data2)
            {
                return true;
            }

            string d1 = ConvertBytesToString(data1);
            string d2 = ConvertBytesToString(data2);

            return d1 == d2;
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

        #region 解析帧长度 
        /// <summary>
        /// 解析帧长度 
        /// </summary>
        /// <param name="inbyte"></param>
        /// <returns></returns>
        public static int byte2ControlLength(byte[] inbyte)
        {
            if (inbyte.Length != 2)
            {
                return 0;
            }
            string signal = inbyte[1].ToString() + inbyte[0].ToString();
            int str = Convert.ToInt32(signal);
            return str;
        }
        #endregion

        #region 解析数据块计数 
        /// <summary>
        /// 解析数据块计数
        /// </summary>
        /// <param name="inbyte"></param>
        /// <returns></returns>
        public static int byte2SignalIntensity(byte[] inbyte)
        {
            if (inbyte.Length != 1)
            {
                return 0;
            }
            string signal = ConvertString(inbyte[0], 16);
            int str = Convert.ToInt32(signal); ;
            return str;
        }
        #endregion

        #region 解析IMEI
        /// <summary>
        /// 解析IMEI
        /// </summary>
        /// <param name="inbyte"></param>
        /// <returns></returns>
        public static string byte2IMEI(byte[] inbyte)
        {
            if (inbyte.Length != 6)
            {
                return null;
            }
            Array.Reverse(inbyte);
            string str = ToHexString(inbyte);
            return str;
        }
        #endregion

        #region 解析设备类型
        /// <summary>
        /// 解析设备类型
        /// </summary>
        /// <param name="inbyte"></param>
        /// <returns></returns>
        public static string byte2DeviceType(byte[] inbyte)
        {
            if (inbyte.Length != 2)
            {
                return null;
            }
            Array.Reverse(inbyte);
            //string ip2 = ConvertString(inbyte[1].ToString(), 10, 16);
            //string ip1 = ConvertString(inbyte[0].ToString(), 10, 16);
            string str = ToHexString(inbyte);
            return str;
        }
        #endregion

        #region 解析Identity
        /// <summary>
        /// 解析Identity
        /// </summary>
        /// <param name="inbyte"></param>
        /// <returns></returns>
        public static string byte2Identity(byte[] inbyte)
        {
            if (inbyte.Length != 2)
            {
                return null;
            }

            string ip1 = ConvertString(inbyte[0].ToString(), 10, 16);
            string ip2 = ConvertString(inbyte[1].ToString(), 10, 16);
            string str = ip2 + ip1;
            return str;
        }
        #endregion

        #region 解析Version
        /// <summary>
        /// 解析Version
        /// </summary>
        /// <param name="inbyte"></param>
        /// <returns></returns>
        public static string byte2Version(byte[] inbyte)
        {
            if (inbyte.Length != 4)
            {
                return null;
            }

            string ip1 = inbyte[0].ToString();
            string ip2 = inbyte[1].ToString();
            string ip3 = ConvertString(inbyte[2].ToString(), 10, 16);
            string ip4 = ConvertString(inbyte[3].ToString(), 10, 16);
            string str = ip4 + "." + ip3;
            return str;
        }
        #endregion

        #region  StringToHexString
        public static string StringToHexString(string str)
        {
            StringBuilder s = new StringBuilder();
            foreach (short c in str.ToCharArray())
            {
                s.Append(c.ToString("X4"));
            }
            return s.ToString();
        }
        #endregion

        #region byte[]转16进制格式string
        /// <summary>
        /// byte[]转16进制格式string
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string ToHexString(byte[] bytes) // 0xae00cf => "AE00CF "
        {
            string hexString = string.Empty;
            if (bytes != null)
            {
                StringBuilder strB = new StringBuilder();
                for (int i = 0; i < bytes.Length; i++)
                {
                    strB.Append(bytes[i].ToString("X2"));
                }
                hexString = strB.ToString();
            }
            return hexString;
        }
        #endregion

        #region 16进制字符串转byte[]数组
        /// <summary>
        /// 16进制字符串转byte[]数组
        /// </summary>
        /// <param name="hex"></param>
        /// <returns></returns>
        public static byte[] hexStringToByte(string hex)
        {
            int len = (hex.Length / 2);
            byte[] result = new byte[len];
            char[] achar = hex.ToCharArray();
            for (int i = 0; i < len; i++)
            {
                int pos = i * 2;
                result[i] = (byte)(toByte(achar[pos]) << 4 | toByte(achar[pos + 1]));
            }
            return result;
        }


        private static int toByte(char c)
        {
            byte b = (byte)"0123456789ABCDEF".IndexOf(c);
            return b;
        }
        #endregion

        public static string ConvertString(byte value, int toBase)
        {
            return Convert.ToString(value, toBase);
        }

        public static string ConvertString(string value, int fromBase, int toBase)
        {

            Int64 intValue = Convert.ToInt64(value, fromBase);

            return Convert.ToString(intValue, toBase);
        }



        /// <summary>  
        /// 设置数据缓存   num 保存时长 秒
        /// </summary>  
        public static void SetCache(string CacheKey, object objObject, int num)
        {
            System.Web.Caching.Cache objCache = HttpRuntime.Cache;
            objCache.Insert(CacheKey, objObject, null, System.DateTime.Now.AddSeconds(num), TimeSpan.Zero);
        }
        /// <summary>
        /// 获取数据缓存
        /// </summary>
        /// <param name="CacheKey"></param>
        /// <returns></returns>
        public static object GetCache(string CacheKey)
        {
            try
            {
                System.Web.Caching.Cache objCache = HttpRuntime.Cache;
                var o = objCache[CacheKey];
                return o ?? "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }
        /// <summary>
        /// 获取数据缓存
        /// </summary>
        /// <param name="CacheKey"></param>
        /// <returns></returns>
        public static object RemoveCache(string CacheKey)
        {
            try
            {
                System.Web.Caching.Cache objCache = HttpRuntime.Cache;
                var o = objCache.Remove(CacheKey);
                return o ?? "";
            }
            catch (Exception ex)
            {
                return "";
            }
        }

        /// <summary>
        /// 程序执行时间测试
        /// </summary>
        /// <param name="dateBegin">开始时间</param>
        /// <param name="dateEnd">结束时间</param>
        /// <returns>返回(秒)单位，比如: 1秒</returns>
        public static double ExecDateDiff(DateTime dateBegin, DateTime dateEnd)
        {
            TimeSpan ts1 = new TimeSpan(dateBegin.Ticks);
            TimeSpan ts2 = new TimeSpan(dateEnd.Ticks);
            TimeSpan ts3 = ts1.Subtract(ts2).Duration();
            //你想转的格式
            return ts3.TotalMinutes;
        }
    }
}