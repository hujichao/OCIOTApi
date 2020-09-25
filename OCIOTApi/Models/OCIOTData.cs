using OCIOTApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCIOTApi
{
    public class OCIOTData
    {
        #region 变量
        protected byte[] _head = new byte[] { 0xAA };//开始字节
        protected byte[] _end = new byte[] { 0xC3 };//结束字节
        #endregion

        /// <summary>
        /// 协议头
        /// </summary>
        internal byte[] Head
        {
            get;
            set;
        }

        /// <summary>
        /// 协议尾
        /// </summary>
        internal byte[] End
        {
            get;
            set;
        }

        /// <summary>
        /// 协议校验合
        /// </summary>
        internal byte LRC
        {
            get;
            set;
        }

        /// <summary>
        /// 控制符
        /// </summary>
        internal string ControlCharacter
        {
            get;
            set;
        }

        /// <summary>
        /// 长度
        /// </summary>
        internal int ControlLength
        {
            get;
            set;
        }

        /// <summary>
        /// 指令类型
        /// </summary>
        internal byte[] Control
        {
            get;
            set;
        }

        /// <summary>
        /// 指令类型
        /// </summary>
        internal ControlType ControlType
        {
            get;
            set;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="dataString">发送数据</param>
        /// <param name="alamdate">硬件发送时间</param>
        /// <param name="ACID">传入数据记录id</param>
        /// <returns></returns>
        public bool GetData(string dataString,string alamdate,string ACID)
        {
            bool flag = false;
            int state = 0;
            if (string.IsNullOrEmpty(dataString))
            {
                flag = false;
            }
            else
            {
                byte[] data = Utility.hexStringToByte(dataString);
                flag = ParseHead(data);
                if (!flag)
                {
                    return false;
                }
                flag = ParseEnd(data,out state);
                if (!flag)
                {
                    return false;
                }
                flag = ParseLRC(data);
                if (!flag)
                {
                    return false;
                }
                flag = ParseControl(data, state);
                if (!flag)
                {
                    return false;
                }
                flag = ParseControlCharacter(data,state);
                if (!flag)
                {
                    return false;
                }
                if (state == 0)
                {//设备报文
                    flag = new ReceiveData().Parse(data, alamdate, ACID);
                }
                else {
                    //设备回复命令帧
                    flag = new ReceiveData().Parse(data, alamdate, ACID);
                }
           
            }
            return flag;
        }


        #region 解析头
        /// <summary>
        /// 解析头
        /// </summary>
        /// <param name="data"></param>
        private bool ParseHead(byte[] data)
        {
            string msgType = "[解析开始字节异常]{0}:原始协议(" + Utility.ConvertBytesToString(data, " ") + ")";
            if (data.Length < 1)
            {
                Utility.WriteLog(string.Format(msgType, "协议长度不够"));
                return false;
            }
            this.Head = new byte[1];
            Array.Copy(data, this.Head, 1);
            if (Utility.EqualsBytes(this.Head, this._head))
            {
                return true;
            }
            else
            {
                Utility.WriteLog(string.Format(msgType, "未找到开始字节"));
                return false;
            }
        }
        #endregion

        #region 解析尾
        /// <summary>
        /// 解析尾
        /// </summary>
        /// <param name="data"></param>
        private bool ParseEnd(byte[] data,out int state)
        {
            state = 0;
            string msgType = "[解析结束字节异常]{0}:原始协议(" + Utility.ConvertBytesToString(data, " ") + ")";
            if (data.Length < 42)
            {
                if (data.Length < 21)
                {
                    Utility.WriteLog(string.Format(msgType, "协议长度不够"));
                    return false;
                }
                else {
                    state = 1;
                    //第二种协议 （设备返回命令帧）
                    byte[] ControlLengthByte = new byte[2];
                    Array.Copy(data, 1, ControlLengthByte, 0, 2);

                    ControlLength = Utility.byte2ControlLength(ControlLengthByte);
                    this.End = new byte[1];
                    if (ControlLength + 2 > data.Length)
                    {
                        Utility.WriteLog(string.Format(msgType, "数据长度计算错误"));
                        return false;
                    }
                    Array.Copy(data, 20, this.End, 0, 1);
                    if (Utility.EqualsBytes(this.End, this._end))
                    {
                        return true;
                    }
                    else
                    {
                        Utility.WriteLog(string.Format(msgType, "未找到结束字节"));
                        return false;
                    }
                }
        
            }
            else {
         
                byte[] ControlLengthByte = new byte[2];
                Array.Copy(data, 1, ControlLengthByte, 0, 2);

                ControlLength = Utility.byte2ControlLength(ControlLengthByte);
                this.End = new byte[1];
                if (ControlLength + 2 > data.Length)
                {
                    Utility.WriteLog(string.Format(msgType, "数据长度计算错误"));
                    return false;
                }
                Array.Copy(data, 41, this.End, 0, 1);
                if (Utility.EqualsBytes(this.End, this._end))
                {
                    return true;
                }
                else
                {
                    Utility.WriteLog(string.Format(msgType, "未找到结束字节"));
                    return false;
                }
            }
         
        }
        #endregion

        #region LRC校验
        /// <summary>
        ///  LRC校验
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ParseLRC(byte[] data)
        {
            string msgType = "[解析CRC字节异常]{0}:原始协议(" + Utility.ConvertBytesToString(data, " ") + ")";
            if (data.Length < 29)//12计算是协议到结果最小协议长度，这个数需要根据实际进行修改
            {
                if (data.Length < 19)
                {
                    Utility.WriteLog(string.Format(msgType, "协议长度不够"));
                    return false;
                }
                else {
                    //第二种协议 （设备返回命令帧）
                    this.LRC = data[ControlLength];//获取原数据中的CRC

                    int lrcFhDataLen = ControlLength;//crcDataLen是指协议数据中需要CRC校验部分的数据长度,需要通过协议说明文档说明进行计算
                    byte[] crcFhData = new byte[lrcFhDataLen - 1];
                    Array.Copy(data, 1, crcFhData, 0, lrcFhDataLen - 1);

                    byte crcFhResult = Utility.LRC(crcFhData);//计算CRC结果

                    if (this.LRC == crcFhResult)
                    {
                        return true;
                    }
                    else
                    {
                        Utility.WriteLog(string.Format(msgType, "CRC校验和不正确"));
                        return false;
                    }
                }


            }
            this.LRC = data[ControlLength];//获取原数据中的CRC

            int lrcDataLen = ControlLength;//crcDataLen是指协议数据中需要CRC校验部分的数据长度,需要通过协议说明文档说明进行计算
            byte[] crcData = new byte[lrcDataLen - 1];
            Array.Copy(data, 1, crcData, 0, lrcDataLen - 1);

            byte crcResult = Utility.LRC(crcData);//计算CRC结果

            if (this.LRC == crcResult)
            {
                return true;
            }
            else
            {
                Utility.WriteLog(string.Format(msgType, "CRC校验和不正确"));
                return false;
            }
        }
        #endregion

        #region 解析设备类型
        /// <summary>
        /// 解析设备类型
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ParseControl(byte[] data,int state)
        {
            string msgType = "[解析控制类型字节异常]{0}:原始协议(" + Utility.ConvertBytesToString(data, " ") + ")";
            if (data.Length < 6)//12计算是协议到结果最小协议长度，这个数需要根据实际进行修改
            {
                Utility.WriteLog(string.Format(msgType, "协议长度不够"));
                return false;
            }
            this.Control = new byte[2];
            Array.Copy(data, 4, this.Control, 0, 2);
            this.ControlType = ConvertControlType.ConvertString(Utility.byte2DeviceType(this.Control));
            if (this.ControlType == ControlType.UnKnown)
            {
                Utility.WriteLog(string.Format(msgType, "未找到对应的控制类型"));
                return false;
            }
            else
            {
                return true;
            }
        }
        #endregion

        #region 解析控制符 
        /// <summary>
        /// 解析设备类型
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private bool ParseControlCharacter(byte[] data,int state)
        {
            string msgType = "[解析控制类型字节异常]{0}:原始协议(" + Utility.ConvertBytesToString(data, " ") + ")";
            if (data.Length < 6)//12计算是协议到结果最小协议长度，这个数需要根据实际进行修改
            {
                Utility.WriteLog(string.Format(msgType, "协议长度不够"));
                return false;
            }
            byte[] ControlCharacterByte = new byte[1];
            Array.Copy(data, 3, ControlCharacterByte, 0, 1);
            this.ControlCharacter = Utility.ToHexString(ControlCharacterByte);
            return true;
        }
        #endregion

    }

}