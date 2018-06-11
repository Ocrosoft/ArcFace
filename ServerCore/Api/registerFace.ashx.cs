using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.SessionState;
using MySql.Data.MySqlClient;
using Newtonsoft.Json;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     registerFace 的摘要说明
    /// </summary>
    public class registerFace : IHttpHandler, IRequiresSessionState
    {
        // 通用引擎参数
        private readonly string appId = Global.GetSettingString("appId");

        // 检测引擎参数
        private IntPtr detectEngine;

        // 引擎初始化结果
        private int detectInitCode;
        private readonly int detectSize = Global.GetSettingInt("detectSize") * 1024 * 1024;
        private readonly int nMaxFaceNum = Global.GetSettingInt("nMaxFaceNum");
        private readonly int nScale = Global.GetSettingInt("nScale");
        private IntPtr pMemDetect;
        private IntPtr pMemRecognition;

        // 识别引擎参数
        private IntPtr recognitionEngion;
        private int recognitionInitCode;
        private readonly string sdkFDKey = Global.GetSettingString("sdkFDKey");
        private readonly string sdkFRKey = Global.GetSettingString("sdkFRKey");

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

            // 邀请码
            var code = context.Request.Form["code"];
            var registerBy = 0;
            // 姓名
            var name = context.Request.Form["name"];
            // 手机
            var phone = context.Request.Form["phone"];
            // base64
            var base64 = context.Request.Form["facesImage"];
            // 
            var privilege = context.Request.Form["privilege"];
            var validDaysForm = context.Request.Form["validDays"];
            var validDays = 0;
            if (validDaysForm != null) validDays = int.Parse(validDaysForm);
            var allowRegister = context.Request.Form["allowRegister"];

            // 邀请码为空，检查Session
            if (code == null)
            {
                // 没有oid，已经注册了自己的人脸信息
                if (context.Session["oid"] == null)
                    if (name == null)
                    {
                        result.code = 0x02;
                        result.message = "权限不足，或已经注册";

                        context.Response.Write(JsonConvert.SerializeObject(result));
                        return;
                    }
                    else
                    {
                        // 任意必须参数为空
                        if (privilege == null || allowRegister == null)
                        {
                            result.code = 0x03;
                            result.message = "参数错误";

                            context.Response.Write(JsonConvert.SerializeObject(result));
                            return;
                        }
                    }
            }
            // 检查邀请码是否存在
            else
            {
                var sql =
                    "select code, startTime, userId from codes where code = ?c and unix_timestamp(startTime) + 15 * 60 > unix_timestamp(now());";
                var ds = MySQLHelper.ExecuteDataSet(sql, new MySqlParameter("?c", code));
                if (ds.Tables[0].Rows.Count == 0)
                {
                    result.code = 0x04;
                    result.message = "邀请码不存在";

                    context.Response.Write(JsonConvert.SerializeObject(result));
                    return;
                }

                registerBy = int.Parse(ds.Tables[0].Rows[0].ItemArray[2].ToString());
            }

            // 没有图片
            if (string.IsNullOrEmpty(base64) ||
                // 邀请码注册，但没有姓名
                !string.IsNullOrEmpty(code) && string.IsNullOrEmpty(name))
            {
                result.code = 0x03;
                result.message = "参数错误";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            // 人脸检测引擎初始化
            pMemDetect = Marshal.AllocHGlobal(detectSize);
            detectInitCode = ArcSoft.AFD_FSDK_InitialFaceEngine(appId, sdkFDKey, pMemDetect, detectSize,
                ref detectEngine,
                (int) ArcSoft.AFD_FSDK_OrientPriority.AFD_FSDK_OPF_0_HIGHER_EXT, nScale, nMaxFaceNum);
            if (detectInitCode != 0)
            {
                // 检测引擎初始化错误
                result.code = 0x211;
                result.message = "服务器错误";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            // 人脸识别引擎初始化
            pMemRecognition = Marshal.AllocHGlobal(detectSize);
            recognitionInitCode =
                ArcSoft.AFR_FSDK_InitialEngine(appId, sdkFRKey, pMemRecognition, detectSize, ref recognitionEngion);
            if (recognitionInitCode != 0)
            {
                // 识别引擎初始化错误
                result.code = 0x212;
                result.message = "服务器错误";

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            // 检测人脸
            Bitmap faceImage = null;
            byte[] faceImageBytes;
            int width = 0, height = 0, pitch = 0;
            faceImageBytes = Global.ProcessBase64(base64, ref faceImage, ref width, ref height, ref pitch);

            // 创建图片指针，并复制图片信息
            var imageDataPtr = Marshal.AllocHGlobal(faceImageBytes.Length);
            Marshal.Copy(faceImageBytes, 0, imageDataPtr, faceImageBytes.Length);

            // 创建图片其他信息指针，并设置内容
            var offInput = new ArcSoft.ASVLOFFSCREEN
            {
                u32PixelArrayFormat = 513,
                ppu8Plane = new IntPtr[4],
                i32Width = width,
                i32Height = height,
                pi32Pitch = new int[4]
            };
            offInput.pi32Pitch[0] = pitch;
            offInput.ppu8Plane[0] = imageDataPtr;
            var offInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(offInput));
            Marshal.StructureToPtr(offInput, offInputPtr, false);
            // 检测结果对象指针
            var faceRes = new ArcSoft.AFD_FSDK_FACERES();
            var faceResPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceRes));
            Marshal.StructureToPtr(faceRes, faceResPtr, false);
            // 进行检测
            int detectResult;
            try
            {
                detectResult = ArcSoft.AFD_FSDK_StillImageFaceDetection(detectEngine, offInputPtr, ref faceResPtr);
            }
            catch (Exception e)
            {
                result.code = -1;
                result.message = e.Message;

                context.Response.Write(JsonConvert.SerializeObject(result));

                Marshal.FreeHGlobal(imageDataPtr);
                Marshal.FreeHGlobal(offInputPtr);
                //Marshal.FreeHGlobal(faceResPtr);

                return;
            }

            // 从指针转化为对象
            faceRes = (ArcSoft.AFD_FSDK_FACERES) Marshal.PtrToStructure(faceResPtr, typeof(ArcSoft.AFD_FSDK_FACERES));

            if (detectResult == 0)
            {
                if (faceRes.nFace == 0)
                {
                    result.code = 0x11;
                    result.message = "没有检测到人脸";

                    context.Response.Write(JsonConvert.SerializeObject(result));

                    Marshal.FreeHGlobal(imageDataPtr);
                    Marshal.FreeHGlobal(offInputPtr);
                    //Marshal.FreeHGlobal(faceResPtr);
                }
                else if (faceRes.nFace > 1)
                {
                    result.code = 0x12;
                    result.message = "检测到多个人脸";

                    context.Response.Write(JsonConvert.SerializeObject(result));

                    Marshal.FreeHGlobal(imageDataPtr);
                    Marshal.FreeHGlobal(offInputPtr);
                    //Marshal.FreeHGlobal(faceResPtr);
                }
                else
                {
                    result.code = 0x0;
                    result.message = "成功";

                    // 识别模型对象和指针
                    var modelRes = new ArcSoft.AFR_FSDK_FaceModel();
                    var modelResPtr = Marshal.AllocHGlobal(Marshal.SizeOf(modelRes));
                    // 输入的人脸信息
                    var faceInput = new ArcSoft.AFR_FSDK_FaceInput();
                    // 人脸角度
                    faceInput.lOrient = (int) Marshal.PtrToStructure(faceRes.lfaceOrient, typeof(int));
                    // 人脸矩形框
                    faceInput.rcFace = (ArcSoft.MRECT) Marshal.PtrToStructure(faceRes.rcFace, typeof(ArcSoft.MRECT));
                    // 输入人脸信息指针
                    var faceInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceInput));
                    Marshal.StructureToPtr(faceInput, faceInputPtr, false);
                    // 进行识别
                    var recognitionResult =
                        ArcSoft.AFR_FSDK_ExtractFRFeature(recognitionEngion, offInputPtr, faceInputPtr,
                            modelResPtr);
                    // 从指针转化为对象
                    modelRes = (ArcSoft.AFR_FSDK_FaceModel) Marshal.PtrToStructure(modelResPtr,
                        typeof(ArcSoft.AFR_FSDK_FaceModel));

                    Marshal.FreeHGlobal(imageDataPtr);
                    Marshal.FreeHGlobal(offInputPtr);
                    ////Marshal.FreeHGlobal(faceResPtr);
                    Marshal.FreeHGlobal(faceInputPtr);
                    Marshal.FreeHGlobal(modelResPtr);

                    if (recognitionResult == 0)
                    {
                        var featureContent = new byte[modelRes.lFeatureSize];
                        Marshal.Copy(modelRes.pbFeature, featureContent, 0, modelRes.lFeatureSize);

                        var rect = (ArcSoft.MRECT) Marshal.PtrToStructure(faceRes.rcFace, typeof(ArcSoft.MRECT));
                        var cutedImage = Global.CutImage(faceImage, rect.left, rect.top, rect.bottom - rect.top,
                            rect.right - rect.left);
                        var cutedBase64 = Global.GetBase64FromImage(cutedImage, true);

                        // 没有验证码，当前登录用户注册人脸或添加新用户
                        if (code == null)
                        {
                            // 当前登录用户注册
                            if (name == null)
                            {
                                // 保存到数据库失败
                                if (SaveFace(featureContent, cutedBase64, context.Session["oid"].ToString()) ==
                                    false)
                                {
                                    result.code = 0x24;
                                    result.message = "保存失败";
                                }
                                else
                                {
                                    // 注册后，自动重新登录
                                    var sql = "select last_insert_id();";
                                    var ds = MySQLHelper.ExecuteDataSet(sql);
                                    var id = int.Parse(ds.Tables[0].Rows[0].ItemArray[0].ToString());

                                    context.Session["uid"] = id;
                                    context.Session["oid"] = null;
                                }
                            }
                            // 添加新用户
                            else
                            {
                                if (SaveFace(featureContent, name, int.Parse(privilege), validDays,
                                        int.Parse(allowRegister), phone, cutedBase64,
                                        context.Session["uid"].ToString()) ==
                                    false)
                                {
                                    result.code = 0x24;
                                    result.message = "保存失败";
                                }
                            }
                        }
                        // 属于邀请注册
                        else
                        {
                            // 保存到数据库失败
                            if (SaveFace(featureContent, name, phone, registerBy, cutedBase64, code) == false)
                            {
                                result.code = 0x24;
                                result.message = "保存失败";
                            }
                        }

                        result.message = "成功";
                        context.Response.Write(JsonConvert.SerializeObject(result));
                    }
                    else
                    {
                        result.code = 0x23;
                        result.message = "服务器错误";
                        result.data = recognitionResult;

                        context.Response.Write(JsonConvert.SerializeObject(result));
                    }
                }
            }
            else
            {
                result.code = 0x22;
                result.message = "服务器错误";
                result.data = detectResult;

                context.Response.Write(JsonConvert.SerializeObject(result));
            }
        }

        public bool IsReusable => false;

        /// <summary>
        ///     将人脸特征保存到数据库（邀请注册）
        /// </summary>
        /// <param name="feature">人脸数据</param>
        /// <param name="name">姓名</param>
        /// <param name="phone">手机</param>
        /// <param name="registerBy">邀请人</param>
        /// <param name="cutedBase64">裁剪的人脸base64编码</param>
        /// <param name="code">邀请码</param>
        /// <returns></returns>
        private bool SaveFace(byte[] feature, string name, string phone, int registerBy, string cutedBase64,
            string code)
        {
            var sql = "insert into userInRegister(" +
                      "name," +
                      "faceData," +
                      "privilege," +
                      "inTime," +
                      "phone," +
                      "registerBy," +
                      "faceImage," +
                      "code) values(" +
                      "?na," +
                      "?faD," +
                      "?pr," +
                      "now()," +
                      "?ph," +
                      "?re," +
                      "?faI," +
                      "?co);";
            var paras = new MySqlParameter[7];
            paras[0] = new MySqlParameter("?na", name);
            paras[1] = new MySqlParameter("faD", feature);
            paras[2] = new MySqlParameter("?pr", 2);
            paras[3] = new MySqlParameter("?ph", phone);
            paras[4] = new MySqlParameter("?re", registerBy);
            paras[5] = new MySqlParameter("?faI", cutedBase64);
            paras[6] = new MySqlParameter("?co", code);
            var res = MySQLHelper.ExecuteNonQuery(sql, paras);

            return res == 1;
        }

        /// <summary>
        ///     将人脸特征保存到数据库（当前登录用户自己）
        /// </summary>
        /// <param name="feature">人脸数据</param>
        /// <param name="cutedBase64">裁剪的人脸base64编码</param>
        /// <param name="id">ownerId</param>
        /// <returns></returns>
        private bool SaveFace(byte[] feature, string cutedBase64, string id)
        {
            var sql = "select * from owner where id = ?id;";
            var ds = MySQLHelper.ExecuteDataSet(sql, new MySqlParameter("?id", id));

            string name, phone;
            name = ds.Tables[0].Rows[0].ItemArray[1].ToString();
            phone = ds.Tables[0].Rows[0].ItemArray[3].ToString();

            sql = "insert into user(" +
                  "name," +
                  "ownerId," +
                  "faceData," +
                  "privilege," +
                  "inTime," +
                  "allowRegister," +
                  "phone," +
                  "faceImage) values(" +
                  "?na," +
                  "?ow," +
                  "?faD," +
                  "?pr," +
                  "now()," +
                  "?al," +
                  "?ph," +
                  "?faI);";
            var paras = new MySqlParameter[7];
            paras[0] = new MySqlParameter("?na", name);
            paras[1] = new MySqlParameter("?ow", id);
            paras[2] = new MySqlParameter("faD", feature);
            paras[3] = new MySqlParameter("?pr", 1);
            paras[4] = new MySqlParameter("?al", 1);
            paras[5] = new MySqlParameter("?ph", phone);
            paras[6] = new MySqlParameter("?faI", cutedBase64);
            var res = MySQLHelper.ExecuteNonQuery(sql, paras);

            return res == 1;
        }

        /// <summary>
        ///     将人脸特征保存到数据库（当前登录用户添加新用户）
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="name"></param>
        /// <param name="privilege"></param>
        /// <param name="validDays"></param>
        /// <param name="allowRegister"></param>
        /// <param name="phone"></param>
        /// <param name="cutedBase64"></param>
        /// <returns></returns>
        private bool SaveFace(byte[] feature, string name, int privilege, int validDays, int allowRegister,
            string phone, string cutedBase64, string registerBy)
        {
            var sql = "insert into user(" +
                      "name," +
                      "faceData," +
                      "privilege," +
                      "inTime," +
                      "validDays," +
                      "allowRegister," +
                      "phone," +
                      "faceImage," +
                      "registerBy) values(" +
                      "?na," +
                      "?faD," +
                      "?pr," +
                      "now()," +
                      "?va," +
                      "?al," +
                      "?ph," +
                      "?faI," +
                      "?re);";

            var paras = new MySqlParameter[8];
            paras[0] = new MySqlParameter("?na", name);
            paras[1] = new MySqlParameter("faD", feature);
            paras[2] = new MySqlParameter("?pr", privilege);
            paras[3] = new MySqlParameter("?va", validDays);
            paras[4] = new MySqlParameter("?al", allowRegister);
            paras[5] = new MySqlParameter("?ph", phone);
            paras[6] = new MySqlParameter("?faI", cutedBase64);
            paras[7] = new MySqlParameter("?re", registerBy);
            var res = MySQLHelper.ExecuteNonQuery(sql, paras);

            return res == 1;
        }
    }
}