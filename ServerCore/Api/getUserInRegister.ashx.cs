using System;
using System.Collections.Generic;
using System.Data;
using System.Web;
using System.Web.SessionState;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     getUserInRegister 的摘要说明
    /// </summary>
    public class getUserInRegister : IHttpHandler, IRequiresSessionState
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

            var list = new List<ReturnStruct>();
            var sql =
                "select id,name,privilege,inTime,validDays,allowRegister,phone,faceImage from userinregister where code = (select code from codes where userId = ?uid);";
            var para = new MySqlParameter("?uid", uid);

            var ds = MySQLHelper.ExecuteDataSet(sql, para);
            foreach (DataRow row in ds.Tables[0].Rows)
                list.Add(new ReturnStruct
                {
                    id = int.Parse(row.ItemArray[0].ToString()),
                    name = row.ItemArray[1].ToString(),
                    privilege = int.Parse(row.ItemArray[2].ToString()),
                    inTime = DateTime.Parse(row.ItemArray[3].ToString().Replace("T", " ")),
                    validDays = int.Parse(row.ItemArray[4].ToString()),
                    allowRegister = int.Parse(row.ItemArray[5].ToString()),
                    phone = row.ItemArray[6].ToString(),
                    faceImage = row.ItemArray[7].ToString()
                });

            result.message = "成功";
            result.data = list;

            context.Response.Write(JsonConvert.SerializeObject(result));
        }

        public bool IsReusable => false;

        private struct ReturnStruct
        {
            public int id;
            public string name;
            public int privilege;
            public DateTime inTime;
            public int validDays;
            public int allowRegister;
            public string phone;
            public string faceImage;
        }
    }
}