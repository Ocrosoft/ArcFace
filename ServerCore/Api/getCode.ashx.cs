using System.Web;
using System.Web.SessionState;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using Ocrosoft;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     getCode 的摘要说明
    /// </summary>
    public class getCode : IHttpHandler, IRequiresSessionState
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

            // 删除所有过期的邀请码
            var sql = "delete from codes where unix_timestamp(startTime) + 15*60 <= unix_timestamp(now());";
            MySQLHelper.ExecuteNonQuery(sql);

            // 查询该用户是否有邀请码
            sql = "select count(*) from codes where userId = ?uid;";
            var ret = int.Parse(MySQLHelper.ExecuteScalar(sql, new MySqlParameter("?uid", uid)).ToString());
            var code = "";
            if (ret == 0)
            {
                sql = "select allowRegister from user where id = ?uid;";
                var ds = MySQLHelper.ExecuteDataSet(sql, new MySqlParameter("?uid", uid));
                if (ds.Tables[0].Rows.Count == 0)
                {
                    result.code = 0x03;
                    result.message = "用户不存在";

                    context.Response.Write(JsonConvert.SerializeObject(result));
                    return;
                }

                if (ds.Tables[0].Rows[0].ItemArray[0].ToString() != "1")
                {
                    result.code = 0x02;
                    result.message = "没有生成邀请码的权限";

                    context.Response.Write(JsonConvert.SerializeObject(result));
                    return;
                }

                // 生成新的6位邀请码
                code = OSecurity.GetRandomString(6);
                sql = "insert into codes value(?code, ?uid, now());";
                var paras = new MySqlParameter[2];
                paras[0] = new MySqlParameter("?code", code);
                paras[1] = new MySqlParameter("?uid", uid);
                // 插入到数据库
                var insertResult = MySQLHelper.ExecuteNonQuery(sql, paras);
                // 失败返回空字符串
                if (insertResult != 1) code = "";
            }
            else
            {
                sql = "select code from codes where userId = ?uid;";
                var ds = MySQLHelper.ExecuteDataSet(sql, new MySqlParameter("?uid", uid));
                code = ds.Tables[0].Rows[0].ItemArray[0].ToString();
            }

            result.message = "成功";
            result.data = new ReturnStruct
            {
                code = code,
                userId = int.Parse(uid)
            };
            context.Response.Write(JsonConvert.SerializeObject(result));
        }

        public bool IsReusable => false;

        private struct ReturnStruct
        {
            public string code;
            public int userId;
        }
    }
}