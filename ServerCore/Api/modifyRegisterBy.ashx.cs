using System.Collections.Generic;
using System.Web;
using System.Web.SessionState;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     modifyRegisterBy 的摘要说明
    /// </summary>
    public class modifyRegisterBy : IHttpHandler, IRequiresSessionState
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

            var name = context.Request.Form["name"];
            var privilege = context.Request.Form["privilege"];
            var validDays = context.Request.Form["validDays"];
            var allowRegister = context.Request.Form["allowRegister"];
            if (name == null && privilege == null && validDays == null && allowRegister == null)
            {
                result.code = 0x03;
                result.message = "参数错误";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            // 只能修改成2或3
            if (privilege != null && privilege != "2" && privilege != "3")
            {
                result.code = 0x03;
                result.message = "参数错误";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            var sql = "update user set #60J96o where id = ?id and registerBy = ?rid;";
            var rep = "";
            var paraList = new List<MySqlParameter>();
            paraList.Add(new MySqlParameter("?id", uid));
            paraList.Add(new MySqlParameter("?rid", context.Session["uid"].ToString()));
            if (name != null)
            {
                rep += "name = ?n,";
                paraList.Add(new MySqlParameter("?n", name));
            }

            if (privilege != null)
            {
                rep += "privilege = ?p,";
                paraList.Add(new MySqlParameter("?p", privilege));
            }

            if (validDays != null)
            {
                rep += "validDays = ?v,";
                paraList.Add(new MySqlParameter("?v", validDays));
            }

            if (allowRegister != null)
            {
                rep += "allowRegister = ?a,";
                paraList.Add(new MySqlParameter("?a", allowRegister));
            }

            // 删除最后一个','，并替换
            sql = sql.Replace("#60J96o", rep.Substring(0, rep.Length - 1));
            var paras = paraList.ToArray();
            var ret = MySQLHelper.ExecuteNonQuery(sql, paras);
            if (ret == 1)
            {
                result.code = 0x00;
                result.message = "成功";
            }
            else
            {
                result.code = 0x10;
                result.message = "修改失败，可能用户不存在或权限不足";
            }

            context.Response.Write(JsonConvert.SerializeObject(result));
        }

        public bool IsReusable => false;
    }
}