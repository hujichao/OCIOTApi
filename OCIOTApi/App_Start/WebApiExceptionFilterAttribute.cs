using log4net;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Web;
using System.Web.Http.Filters;

namespace OCIOTApi.App_Start
{
    public class WebApiExceptionFilterAttribute : ExceptionFilterAttribute
    {

        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            ILog log = LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
            //异常日志记录
            HttpRequestMessage htm = actionExecutedContext.Request;
            StringBuilder remarkStr = new StringBuilder();
            remarkStr.AppendLine("Url:" + htm.RequestUri.ToString());
            remarkStr.AppendLine("Method:" + htm.Method.Method);
            remarkStr.AppendLine("Version:" + htm.Version.ToString());
            log.Error(remarkStr.ToString(), actionExecutedContext.Exception);

            ManageErrorInfo(actionExecutedContext);

            base.OnException(actionExecutedContext);
        }

        private void ManageErrorInfo(HttpActionExecutedContext actionExecutedContext)
        {
            //if (actionExecutedContext.Exception is NotImplementedException)
            //{
            //    var oResponse = new HttpResponseMessage(HttpStatusCode.NotImplemented);
            //    oResponse.Content = new StringContent("方法不被支持");
            //    oResponse.ReasonPhrase = "This Func is Not Supported";
            //    actionExecutedContext.Response = oResponse;
            //}
        }
    }
}