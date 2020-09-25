using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCIOTApi.Models
{
    public class TelecomModel
    {
       public string appId { get; set; }

        public string secret { get; set; }

    }

    public class OCIOTToken {
        //{"accessToken":"15621b58cef958ec998342bbffb53f35",
        //"tokenType":"bearer","refreshToken":"c5d7beb4a40f148ba85f323d201931",
        //"expiresIn":3600,"scope":"default"}
        public string accessToken { get; set; }
        public string tokenType { get; set; }
        public string refreshToken { get; set; }
        public string expiresIn { get; set; }
        public string scope { get; set; }
    }


    public class command
    {
        public string serviceId { get; set; }
        public string method { get; set; }
        public paras paras { get; set; }
    }
    public class paras
    {
        public string rawData { get; set; }
    }
    public class JsonFr
    {
        public string deviceId { get; set; }
        public int expireTime { get; set; }
        public command command { get; set; }

    }

    public class SilencingError
    {
        public string resultcode { get; set; }
        public string resultmsg { get; set; }
    }


}