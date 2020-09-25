using NEG.N6.IFI.Yyqu.JIC;
using Newtonsoft.Json;
using OCIOTApi.Common;
using OCIOTApi.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace OCIOTApi.Controllers
{
    /// <summary>
    /// 新版燃起(公司自研)
    /// </summary>
    public class NewFuelGasController : ApiController
    {
        #region 属性

        protected string RedisKey = "FuelGas";
        private readonly IRedis redis;
        #endregion

        #region 构造

        ///// <summary>
        ///// 注入tcp通道类
        ///// </summary>
        ///// <param name="redis"></param>
        public NewFuelGasController(IRedis redis)
        {
            this.redis = redis;

        }
        #endregion

        #region MyRegion

        [HttpPost]
        //[HttpPost, Route("api/NewFuelGas/AddFuelGasMsg")]
        public ResponseJson<string> AddFuelGasMsg([FromBody]FuelGasMsgModel model)
        {
            ResponseJson<string> json = null;
            try
            {

                if (model != null)
                {
                    Utility.WriteLog(JsonConvert.SerializeObject(model), "燃气" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    Random rd = new Random();
                    model.randomNum = rd.Next().ToString();
                    model.actionType = "FuelGas_Handle";
                    //写入redis中
                    string key = string.Format("{0}:{1}:{2}", model.deviceNumber, model.deviceType, model.randomNum);
                    bool sdf = redis.HashSet(RedisKey, key, JsonConvert.SerializeObject(model));
                    Utility.WriteLog("获取token", "燃气" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    json = new ResponseJson<string>(ResponseCode.Nomal, "成功");
                }

            }
            catch (Exception ex)
            {
                return json = new ResponseJson<string>(ResponseCode.Err, ex.Message);
                throw;
            }
            return json;

        }
        /// <summary>
        /// 下线接口
        /// </summary>
        /// <param name="model"></param>
        /// <returns></returns>
        public ResponseJson<string> FuelGasOffline([FromBody] FuelGasMsgModel model)
        {
            ResponseJson<string> json = null;
            try
            {

                if (model != null)
                {
                    Utility.WriteLog(JsonConvert.SerializeObject(model), "燃气" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    Random rd = new Random();
                    model.randomNum = rd.Next().ToString();
                    model.actionType = "FuelGas_Handle";
                    //写入redis中
                    string key = string.Format("{0}:{1}:{2}", model.deviceNumber, model.deviceType, model.randomNum);
                    bool sdf = redis.HashSet(RedisKey, key, JsonConvert.SerializeObject(model));
                    Utility.WriteLog("获取token", "燃气" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                    json = new ResponseJson<string>(ResponseCode.Nomal, "成功");
                }

            }
            catch (Exception ex)
            {
                return json = new ResponseJson<string>(ResponseCode.Err, ex.Message);
                throw;
            }
            return json;

        }


        public class FuelGasMsgModel
        {/// <summary>
         /// 数据类型
         /// </summary>
            public string actionType { get; set; }
            /// <summary>
            /// 随机序号
            /// </summary>
            public string randomNum { get; set; }
            /// <summary>
            /// 设备编号
            /// </summary>
            public string deviceNumber { get; set; }
            /// <summary>
            /// 设备类型
            /// </summary>
            public string deviceType { get; set; }
            /// <summary>
            /// 信号
            /// </summary>
            public string signal { get; set; }

            /// <summary>
            /// 燃气浓度
            /// </summary>
            public string concentration { get; set; }

            /// <summary>
            /// 时间
            /// </summary>
            public string time { get; set; }

            /// <summary>
            /// 是否报警(0:正常 ；1：报警)
            /// </summary>
            public string runningState { get; set; }
            /// <summary>
            /// 数据类型 1 实时数据和报警数据 2设备报警数据 3设备下线 4设备上线 5设备故障 
            /// </summary>
            public string DataState { get; set; }
        }
        #endregion
    }





}
