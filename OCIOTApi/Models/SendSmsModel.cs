using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCIOTApi.Models
{
    /// <summary>
    /// 短信发送信息存储类
    /// </summary>
    public class SendSmsModel
    {
        /// <summary>
        /// 设备简称
        /// </summary>
        public string DevName { get; set; }
        /// <summary>
        /// 用户名
        /// </summary>
        public string UserName { get; set; }
        /// <summary>
        /// 我的小区名称
        /// </summary>
        public string VillageName { get; set; }
        /// <summary>
        /// 地址详细简单版没省市区和小区名
        /// </summary>
        public string AddressDetail { get; set; }
        /// <summary>
        /// 我的地址完整版
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 报警描述
        /// </summary>
        public string PushName { get; set; }

        /// <summary>
        /// 简单处理方案
        /// </summary>
        public string Message { get; set; }
        /// <summary>
        /// 电话
        /// </summary>
        public string Phone { get; set; }

    }
}