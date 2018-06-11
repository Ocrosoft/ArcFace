using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using Newtonsoft.Json;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     acceptRegister 的摘要说明
    /// </summary>
    public class acceptRegister : IHttpHandler, IRequiresSessionState
    {
        public void ProcessRequest(HttpContext context)
        {
            context.Response.ContentType = "text/json";

            var result = new ApiResult();
            if (context.Request.HttpMethod != "POST")
            {
                result.code = 0x01;
                result.message = "需要使用POST请求";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            if (context.Session["uid"] == null)
            {
                result.code = 0x02;
                result.message = "权限不足";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            var uid = context.Session["uid"].ToString();
            var sqlList = new List<string>();
            // Tran不支持paras
            var sql =
                "insert into user(name, faceData, privilege, inTime, validDays, allowRegister, phone, registerBy, faceImage) " +
                "select name, faceData, privilege, inTime, validDays, allowRegister, phone, registerBy, faceImage from userInRegister " +
                "where code = (select code from codes where userId = " + uid + ");";
            sqlList.Add(sql);
            sql = "delete from userInRegister where code = (select code from codes where userId = " + uid + ");";
            sqlList.Add(sql);

            var ret = MySQLHelper.ExecuteNoQueryTran(sqlList);
            if (ret)
            {
                result.code = 0x00;
                result.message = "成功";
            }
            else
            {
                result.code = 0x10;
                result.message = "服务器错误";
            }

            context.Response.Write(JsonConvert.SerializeObject(result));
        }

        public bool IsReusable => false;
    }
}