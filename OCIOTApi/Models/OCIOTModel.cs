using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCIOTApi
{
    //public class OCIOTModel
    //{
    //    /// <summary>
    //    /// 类型
    //    /// </summary>
    //    public string notifyType { get; set; }
    //    /// <summary>
    //    /// 设备ID
    //    /// </summary>
    //    public string deviceId { get; set; }
    //    /// <summary>
    //    /// 网关ID
    //    /// </summary>
    //    public string gatewayId { get; set; }
    //    /// <summary>
    //    /// 
    //    /// </summary>
    //    public string requestId { get; set; }
    //    /// <summary>
    //    /// 数据
    //    /// </summary>
    //    public OCIOTService service { get; set; }
    //}

    //public class OCIOTService
    //{
    //    public string serviceId { get; set; }
    //    public string serviceType { get; set; }
    //    public OCIOTServiceData data { get; set; }
    //    public string eventTime { get; set; }
    //}
    //public class OCIOTServiceData
    //{
    //    public string rawData { get; set; }
    //}

    public class OCIOTModel
    {
        public string notifyType { get; set; }
        public string deviceId { get; set; }
        public string gatewayId { get; set; }
        public object requestId { get; set; }
        public OCIOTService service { get; set; }
    }

    public class OCIOTService
    {
        public string serviceId { get; set; }
        public string serviceType { get; set; }
        public OCIOTServiceData data { get; set; }
        public string eventTime { get; set; }
    }

    public class OCIOTServiceData
    {
        public string rawData { get; set; }
    }

    public class OCIOTCommend{
        /// <summary>
        /// 电信平台ID
        /// </summary>
        public string DeviceIOTID { get; set; }
        public string DeviceID { get; set; }
       
    }

}