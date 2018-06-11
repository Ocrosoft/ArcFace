using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Text.RegularExpressions;
using MySql.Data.MySqlClient;

namespace ServerCore
{
    public class MySQLHelper
    {
        public static string connectionString = ConfigurationManager.ConnectionStrings["Conn"].ConnectionString;

        /// <summary>
        ///     获取分页数据 在不用存储过程情况下
        /// </summary>
        /// <param name="recordCount">总记录条数</param>
        /// <param name="selectList">选择的列逗号隔开,支持top num</param>
        /// <param name="tableName">表名字</param>
        /// <param name="whereStr">条件字符 必须前加 and</param>
        /// <param name="orderExpression">排序 例如 ID</param>
        /// <param name="pageIdex">当前索引页</param>
        /// <param name="pageSize">每页记录数</param>
        /// <returns></returns>
        public static DataTable getPager(out int recordCount, string selectList, string tableName, string whereStr,
            string orderExpression, int pageIdex, int pageSize)
        {
            var rows = 0;
            var dt = new DataTable();
            var matchs = Regex.Matches(selectList, @"top\s+\d{1,}", RegexOptions.IgnoreCase); //含有top 
            string sqlStr =
                sqlStr = string.Format("select {0} from {1} where 1=1 {2}", selectList, tableName, whereStr);
            if (!string.IsNullOrEmpty(orderExpression)) sqlStr += string.Format(" Order by {0}", orderExpression);
            if (matchs.Count > 0) //含有top的时候 
            {
                var dtTemp = ExecuteDataSet(sqlStr).Tables[0];
                rows = dtTemp.Rows.Count;
            }
            else //不含有top的时候 
            {
                var sqlCount = string.Format("select count(*) from {0} where 1=1 {1} ", tableName, whereStr);
                //获取行数 
                var obj = ExecuteScalar(sqlCount);
                if (obj != null) rows = Convert.ToInt32(obj);
            }

            dt = ExecuteDataSet(sqlStr, (pageIdex - 1) * pageSize, pageSize).Tables[0];
            recordCount = rows;
            return dt;
        }

        #region 创建command

        private static void PrepareCommand(MySqlCommand cmd, MySqlConnection conn, MySqlTransaction trans,
            string cmdText, MySqlParameter[] cmdParms)
        {
            if (conn.State != ConnectionState.Open)
                conn.Open();
            cmd.Connection = conn;
            cmd.CommandText = cmdText;
            if (trans != null)
                cmd.Transaction = trans;
            cmd.CommandType = CommandType.Text; //cmdType; 
            if (cmdParms != null)
                foreach (var parameter in cmdParms)
                {
                    if ((parameter.Direction == ParameterDirection.InputOutput ||
                         parameter.Direction == ParameterDirection.Input) &&
                        parameter.Value == null)
                        parameter.Value = DBNull.Value;
                    cmd.Parameters.Add(parameter);
                }
        }

        #endregion

        #region ExecuteNonQuery

        /// <summary>
        ///     执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteNonQuery(string SQLString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                using (var cmd = new MySqlCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        var rows = cmd.ExecuteNonQuery();
                        return rows;
                    }
                    catch (MySqlException e)
                    {
                        connection.Close();
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        ///     执行SQL语句，返回影响的记录数
        /// </summary>
        /// <param name="SQLString">SQL语句</param>
        /// <returns>影响的记录数</returns>
        public static int ExecuteNonQuery(string SQLString, params MySqlParameter[] cmdParms)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                using (var cmd = new MySqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, connection, null, SQLString, cmdParms);
                        var rows = cmd.ExecuteNonQuery();
                        cmd.Parameters.Clear();
                        return rows;
                    }
                    catch (MySqlException e)
                    {
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        ///     执行多条SQL语句，实现数据库事务。
        /// </summary>
        /// <param name="SQLStringList">多条SQL语句</param>
        public static bool ExecuteNoQueryTran(List<string> SQLStringList)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand();
                cmd.Connection = conn;
                var tx = conn.BeginTransaction();
                cmd.Transaction = tx;
                try
                {
                    for (var n = 0; n < SQLStringList.Count; n++)
                    {
                        var strsql = SQLStringList[n];
                        if (strsql.Trim().Length > 1)
                        {
                            cmd.CommandText = strsql;
                            PrepareCommand(cmd, conn, tx, strsql, null);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    cmd.ExecuteNonQuery();
                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
                    return false;
                }
            }
        }

        /// <summary>
        ///     执行多条带Parameter的SQL语句
        /// </summary>
        /// <param name="SQLStringList"></param>
        /// <param name="SQLParaList"></param>
        /// <returns></returns>
        public static bool ExecuteNoQueryTran(List<string> SQLStringList, List<MySqlParameter[]> SQLParaList)
        {
            using (var conn = new MySqlConnection(connectionString))
            {
                conn.Open();
                var cmd = new MySqlCommand();
                //cmd.CommandTimeout = 0;
                cmd.Connection = conn;
                var tx = conn.BeginTransaction();
                cmd.Transaction = tx;
                try
                {
                    for (var n = 0; n < SQLStringList.Count; n++)
                    {
                        var strsql = SQLStringList[n];
                        if (strsql.Trim().Length > 1)
                        {
                            cmd.CommandText = strsql;
                            PrepareCommand(cmd, conn, tx, strsql, SQLParaList[n]);
                            cmd.ExecuteNonQuery();
                        }
                    }

                    cmd.ExecuteNonQuery();
                    tx.Commit();
                    return true;
                }
                catch
                {
                    tx.Rollback();
                    return false;
                }
            }
        }

        #endregion

        #region ExecuteScalar

        /// <summary>
        ///     执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="SQLString">计算查询结果语句</param>
        /// <returns>查询结果（object）</returns>
        public static object ExecuteScalar(string SQLString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                using (var cmd = new MySqlCommand(SQLString, connection))
                {
                    try
                    {
                        connection.Open();
                        var obj = cmd.ExecuteScalar();
                        if (Equals(obj, null) || Equals(obj, DBNull.Value))
                            return null;
                        return obj;
                    }
                    catch (MySqlException e)
                    {
                        connection.Close();
                        throw e;
                    }
                }
            }
        }

        /// <summary>
        ///     执行一条计算查询结果语句，返回查询结果（object）。
        /// </summary>
        /// <param name="SQLString">计算查询结果语句</param>
        /// <returns>查询结果（object）</returns>
        public static object ExecuteScalar(string SQLString, params MySqlParameter[] cmdParms)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                using (var cmd = new MySqlCommand())
                {
                    try
                    {
                        PrepareCommand(cmd, connection, null, SQLString, cmdParms);
                        var obj = cmd.ExecuteScalar();
                        cmd.Parameters.Clear();
                        if (Equals(obj, null) || Equals(obj, DBNull.Value))
                            return null;
                        return obj;
                    }
                    catch (MySqlException e)
                    {
                        throw e;
                    }
                }
            }
        }

        #endregion

        #region ExecuteReader

        /// <summary>
        ///     执行查询语句，返回MySqlDataReader ( 注意：调用该方法后，一定要对MySqlDataReader进行Close )
        /// </summary>
        /// <param name="strSQL">查询语句</param>
        /// <returns>MySqlDataReader</returns>
        public static MySqlDataReader ExecuteReader(string strSQL)
        {
            var connection = new MySqlConnection(connectionString);
            var cmd = new MySqlCommand(strSQL, connection);
            try
            {
                connection.Open();
                var myReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                return myReader;
            }
            catch (MySqlException e)
            {
                throw e;
            }
        }

        /// <summary>
        ///     执行查询语句，返回MySqlDataReader ( 注意：调用该方法后，一定要对MySqlDataReader进行Close )
        /// </summary>
        /// <param name="strSQL">查询语句</param>
        /// <returns>MySqlDataReader</returns>
        public static MySqlDataReader ExecuteReader(string SQLString, params MySqlParameter[] cmdParms)
        {
            var connection = new MySqlConnection(connectionString);
            var cmd = new MySqlCommand();
            try
            {
                PrepareCommand(cmd, connection, null, SQLString, cmdParms);
                var myReader = cmd.ExecuteReader(CommandBehavior.CloseConnection);
                cmd.Parameters.Clear();
                return myReader;
            }
            catch (MySqlException e)
            {
                throw e;
            }

            // finally 
            // { 
            // cmd.Dispose(); 
            // connection.Close(); 
            // } 
        }

        #endregion

        #region ExecuteDataTable

        /// <summary>
        ///     执行查询语句，返回DataSet
        /// </summary>
        /// <param name="SQLString">查询语句</param>
        /// <returns>DataSet</returns>
        public static DataSet ExecuteDataSet(string SQLString)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var ds = new DataSet();
                try
                {
                    connection.Open();
                    var command = new MySqlDataAdapter(SQLString, connection);
                    command.Fill(ds, "ds");
                }
                catch (MySqlException ex)
                {
                    throw new Exception(ex.Message);
                }

                return ds;
            }
        }

        /// <summary>
        ///     执行查询语句，返回DataSet
        /// </summary>
        /// <param name="SQLString">查询语句</param>
        /// <returns>DataSet</returns>
        public static DataSet ExecuteDataSet(string SQLString, params MySqlParameter[] cmdParms)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var cmd = new MySqlCommand();
                PrepareCommand(cmd, connection, null, SQLString, cmdParms);
                using (var da = new MySqlDataAdapter(cmd))
                {
                    var ds = new DataSet();
                    try
                    {
                        da.Fill(ds, "ds");
                        cmd.Parameters.Clear();
                    }
                    catch (MySqlException ex)
                    {
                        throw new Exception(ex.Message);
                    }

                    return ds;
                }
            }
        }

        //获取起始页码和结束页码 
        public static DataSet ExecuteDataSet(string cmdText, int startResord, int maxRecord)
        {
            using (var connection = new MySqlConnection(connectionString))
            {
                var ds = new DataSet();
                try
                {
                    connection.Open();
                    var command = new MySqlDataAdapter(cmdText, connection);
                    command.Fill(ds, startResord, maxRecord, "ds");
                }
                catch (MySqlException ex)
                {
                    throw new Exception(ex.Message);
                }

                return ds;
            }
        }

        #endregion
    }
}