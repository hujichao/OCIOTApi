#region 代码说明
/*******************************类说明********************************
 * 作者：zhaodongyue
 * 日期：2017/8/17 11:00:49
 * 版权：2017-2015
 * CLR版本：4.0.30319.42000
*******************************修改说明*******************************
 *日期：2017/8/17 11:00:49
 *版本：v1.0
 *说明：新增
**********************************************************************/
#endregion
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Formatting;
using System.Net.Http.Headers;
using System.Web;

namespace OCIOTApi.App_Start
{
    public class JsonContentNegotiator : IContentNegotiator
    {
        #region 变量
        private readonly JsonMediaTypeFormatter _jsonFormatter;
        #endregion

        #region 属性

        #endregion

        #region 构造

        #endregion

        #region 公有方法
        public JsonContentNegotiator(JsonMediaTypeFormatter formatter)
        {
            _jsonFormatter = formatter;
        }

        public ContentNegotiationResult Negotiate(Type type, HttpRequestMessage request, IEnumerable<MediaTypeFormatter> formatters)
        {
            var result = new ContentNegotiationResult(_jsonFormatter, new MediaTypeHeaderValue("application/json"));
            return result;
        }
        #endregion

        #region 私有方法

        #endregion

        #region 事件

        #endregion
    }
}