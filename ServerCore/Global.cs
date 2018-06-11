using System;
using System.Configuration;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;

namespace ServerCore
{
    public static class Global
    {
        private static DataTable libraryFeatures;
        private static DateTime lastUpdateTime = DateTime.Now;
        private static DataTable clientMacs;

        static Global()
        {
            UpdateFeatures();
        }

        /// <summary>
        ///     将图片转换成base64编码
        /// </summary>
        /// <param name="image">图片</param>
        /// <param name="jpeg">是否用jpg压缩</param>
        /// <returns></returns>
        public static string GetBase64FromImage(Bitmap image, bool jpeg = false)
        {
            var strbaser64 = "";
            try
            {
                var ms = new MemoryStream();
                image.Save(ms, jpeg ? ImageFormat.Jpeg : ImageFormat.Bmp);
                var arr = new byte[ms.Length];
                ms.Position = 0;
                ms.Read(arr, 0, (int) ms.Length);
                ms.Close();
                strbaser64 = Convert.ToBase64String(arr);
            }
            catch (Exception e)
            {
                throw e;
            }

            return strbaser64;
        }

        /// <summary>
        ///     将base64转换成byte数组和bitmap，并计算width，height，pitch
        /// </summary>
        /// <param name="base64">base64编码的图片</param>
        /// <param name="image">返回的Bitmap</param>
        /// <param name="width">返回的width</param>
        /// <param name="height">返回的width</param>
        /// <param name="pitch">返回的pitch</param>
        /// <returns>byte[]</returns>
        public static byte[] ProcessBase64(string base64, ref Bitmap image, ref int width, ref int height,
            ref int pitch)
        {
            // 转字节数组
            var bytes = Convert.FromBase64String(base64);
            var ms = new MemoryStream(bytes);
            // 转Bitmap
            image = new Bitmap(Image.FromStream(ms));
            //将Bitmap锁定到系统内存中,获得BitmapData
            var data = image.LockBits(new Rectangle(0, 0, image.Width, image.Height), ImageLockMode.ReadOnly,
                PixelFormat.Format24bppRgb);
            //位图中第一个像素数据的地址。它也可以看成是位图中的第一个扫描行
            var ptr = data.Scan0;
            //定义数组长度
            var soureBitArrayLength = data.Height * Math.Abs(data.Stride);
            var sourceBitArray = new byte[soureBitArrayLength];
            //将bitmap中的内容拷贝到ptr_bgr数组中
            Marshal.Copy(ptr, sourceBitArray, 0, soureBitArrayLength);
            width = data.Width;
            height = data.Height;
            pitch = Math.Abs(data.Stride);

            var line = width * 3;
            var bgr_len = line * height;
            var destBitArray = new byte[bgr_len];
            for (var i = 0; i < height; ++i) Array.Copy(sourceBitArray, i * pitch, destBitArray, i * line, line);
            pitch = line;
            image.UnlockBits(data);
            return destBitArray;
        }

        public static Bitmap CutImage(Bitmap image, int left, int top, int width, int height)
        {
            var destBitmap = new Bitmap(width, height); //目标图
            var destRect = new Rectangle(0, 0, width, height); //矩形容器
            var srcRect = new Rectangle(left, top, width, height);

            var g = Graphics.FromImage(destBitmap);
            g.DrawImage(image, destRect, srcRect, GraphicsUnit.Pixel);

            return destBitmap;
        }

        /// <summary>
        ///     获取配置文件中的配置并转为int
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static int GetSettingInt(string key)
        {
            return int.Parse(GetSettingString(key));
        }

        /// <summary>
        ///     获取配置文件中的配置
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static string GetSettingString(string key)
        {
            return ConfigurationManager.AppSettings[key];
        }

        /// <summary>
        ///     获取配置文件中的配置并转为float
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public static float GetSettingFloat(string key)
        {
            return float.Parse(GetSettingString(key));
        }

        /// <summary>
        ///     获取数据库中保存的人脸特征信息
        /// </summary>
        /// <returns></returns>
        public static DataTable GetlibraryFeatures()
        {
            if ((DateTime.Now - lastUpdateTime).TotalMinutes > 5) UpdateFeatures();

            return libraryFeatures;
        }

        /// <summary>
        ///     获取数据库中保存的门禁客户端MAC信息
        /// </summary>
        /// <returns></returns>
        public static DataTable GetClientMacs()
        {
            if ((DateTime.Now - lastUpdateTime).TotalMinutes > 5) UpdateFeatures();

            return clientMacs;
        }

        /// <summary>
        ///     更新内存中的人脸特征信息、门禁MAC信息
        /// </summary>
        private static void UpdateFeatures()
        {
            // 获取数据库人脸特征信息
            var sql = "select faceData, id, name, privilege, inTime, validDays from user;";
            var ds = MySQLHelper.ExecuteDataSet(sql);
            libraryFeatures = ds.Tables[0];
            // 获取门禁客户端信息
            sql = "select mac, id from client;";
            ds = MySQLHelper.ExecuteDataSet(sql);
            clientMacs = ds.Tables[0];
            // 最后更新时间修改为现在
            lastUpdateTime = DateTime.Now;
        }
    }
}