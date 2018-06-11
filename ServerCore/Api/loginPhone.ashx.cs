using System.Web;
using System.Web.SessionState;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     loginPhone 的摘要说明
    /// </summary>
    public class loginPhone : IHttpHandler, IRequiresSessionState
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

            var phone = context.Request.Form["phone"];
            var code = context.Request.Form["code"];

            if (phone == null || code == null)
            {
                result.code = 0x03;
                result.message = "参数错误";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            var returnStruct = new ReturnStruct();

            var sql = "select count(*) from user where phone = ?p;";
            var ret = int.Parse(MySQLHelper.ExecuteScalar(sql, new MySqlParameter("?p", phone)).ToString());
            // 存在于user表中
            if (ret == 1)
            {
                returnStruct.firstLogin = false;
                sql = "select id, name from user where phone = ?p;";
                var ds = MySQLHelper.ExecuteDataSet(sql, new MySqlParameter("?p", phone));
                returnStruct.userId = int.Parse(ds.Tables[0].Rows[0].ItemArray[0].ToString());
                returnStruct.name = ds.Tables[0].Rows[0].ItemArray[1].ToString();

                context.Session["uid"] = returnStruct.userId;

                result.message = "成功";
                result.data = returnStruct;
            }
            else
            {
                sql = "select count(*) from owner where phone = ?p;";
                ret = int.Parse(MySQLHelper.ExecuteScalar(sql, new MySqlParameter("?p", phone)).ToString());
                // 存在于owner表中
                if (ret == 1)
                {
                    returnStruct.firstLogin = true;
                    returnStruct.userId = 0;
                    sql = "select name, id from owner where phone = ?p;";
                    var ds = MySQLHelper.ExecuteDataSet(sql, new MySqlParameter("?p", phone));
                    returnStruct.name = ds.Tables[0].Rows[0].ItemArray[0].ToString();

                    context.Session["oid"] = ds.Tables[0].Rows[0].ItemArray[1].ToString();

                    result.message = "成功";
                    result.data = returnStruct;
                }
                else
                {
                    result.message = "成功";
                    result.data = null;
                }
            }

            context.Response.Write(JsonConvert.SerializeObject(result));
        }

        public bool IsReusable => false;

        private struct ReturnStruct
        {
            public bool firstLogin;
            public int userId;
            public string name;
        }
    }
}