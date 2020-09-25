namespace OCIOTApi.EquipmentManagement
{
    public class EMModel
    {
        /// <summary>
        /// 平台类型（1智慧消防 2 全民消防）
        /// </summary>
        public int platType { get; set; } = 2;
        /// <summary>
        /// 设备编号
        /// </summary>
        public string deviceNumber { get; set; }
        /// <summary>
        /// 设备类型编号
        /// </summary>
        public string deviceTypeNumber { get; set; }

        /// <summary>
        /// 报警id（全民消防警情id）
        /// </summary>
        public string recordID { get; set; }
        /// <summary>
        /// 社会单位ID
        /// </summary>
        public string unitID { get; set; }
        /// <summary>
        /// 告警信息状态：1、未处理；2、处理中；3、已处理；
        /// </summary>
        public int status { get; set; } = 1;

        /// <summary>
        /// 隐患等级
        /// </summary>
        public int recordLV { get; set; } = 1;
        /// <summary>
        /// 状态等级
        /// </summary>
        public int statusLV { get; set; } = 1;
        /// <summary>
        /// 0 非智能设备 1、智能设备
        /// </summary>
        public int fromtype { get; set; } = 1;
    }



    public class DevStateModel
    {
        /// <summary>
        /// 平台类型（1智慧消防 2 全民消防）
        /// </summary>
        public int platType { get; set; } = 2;
        /// <summary>
        /// 设备编号
        /// </summary>
        public string deviceNumber { get; set; }

        /// <summary>
        /// 设备类型编号
        /// </summary>
        public string deviceTypeNumber { get; set; }
        /// <summary>
        /// 报警（隐患）ID  
        /// </summary>
        public string recordID { get; set; }

        /// <summary>
        /// 告警信息状态：1、未处理；3、已处理；
        /// </summary>
        public int status { get; set; } = 3;


    }

}