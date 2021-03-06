﻿using System.Web;
using System.Web.SessionState;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     deleteRegister 的摘要说明
    /// </summary>
    public class deleteRegister : IHttpHandler, IRequiresSessionState
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

            // 要修改的用户iuid
            var uid = context.Request.Form["userId"];
            if (uid == null)
            {
                result.code = 0x03;
                result.message = "参数错误";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            var sql = "delete from userInRegister where id = ?uid;";
            var ret = MySQLHelper.ExecuteNonQuery(sql, new MySqlParameter("?uid", uid));
            if (ret == 1)
            {
                result.code = 0x00;
                result.message = "成功";
            }
            else
            {
                result.code = 0x10;
                result.message = "删除失败，可能用户不存在";
            }

            context.Response.Write(JsonConvert.SerializeObject(result));
        }

        public bool IsReusable => false;
    }
}