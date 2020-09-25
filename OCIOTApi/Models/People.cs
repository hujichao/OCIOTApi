using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace OCIOTApi.Models
{
    public class People
    {
        public string UserID { get; set; }
        public string Name { get; set; }
        public string Phone { get; set; }
        /// <summary>
        /// 省
        /// </summary>
        public string Province { get; set; }
        /// <summary>
        /// 市
        /// </summary>
        public string City { get; set; }
        /// <summary>
        /// 区
        /// </summary>
        public string Area { get; set; }
        /// <summary>
        /// 街道
        /// </summary>
        public string StreetName { get; set; }

        /// <summary>
        /// 地址名称
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 小区名称
        /// </summary>
        public string VillageName { get; set; }

    }
}