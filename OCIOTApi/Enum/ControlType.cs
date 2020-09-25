using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCIOTApi
{
  public  enum ControlType
    {
        /// <summary>
        /// 燃气
        /// </summary>
        Gas = 0100,
        /// <summary>
        /// 烟感
        /// </summary>
        Smoke = 0200,
        /// <summary>
        /// 未知
        /// </summary>
        UnKnown = -1
    }
   
    /// <summary>
    /// 指令类型与指令协议数据转换
    /// </summary>
    class ConvertControlType
    {
        /// <summary>
        /// 指令类型转协议数据
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static byte[] ConvertTypeToBytes(ControlType type)
        {
            switch (type)
            {
                case ControlType.Gas:
                    return new byte[] { 0x01, 0x00 };
                case ControlType.Smoke:
                    return new byte[] { 0x02, 0x00 };
                case ControlType.UnKnown:
                    return new byte[] { 0x00 };
                default:
                    return new byte[] { 0x00 };
            }
        }


        /// <summary>
        /// 协议数据转指令类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static ControlType ConvertString(string buff)
        {
            switch (buff)
            {
                case "0100":
                    return ControlType.Gas;
                case "0200":
                    return ControlType.Smoke;
                default:
                    return ControlType.UnKnown;
            }
        }
        /// <summary>
        /// 协议数据转指令类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        internal static string ConvertNameString(int buff)
        {
            switch (buff)
            {
                case 8:
                    return "燃气设备";
                case 7:
                    return "烟感设备";
                default:
                    return "未知设备";
            }
        }
    }
}