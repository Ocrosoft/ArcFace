using System;
using System.Data;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.SessionState;
using Newtonsoft.Json;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     loginFace 的摘要说明
    /// </summary>
    public class loginFace : IHttpHandler, IRequiresSessionState
    {
        // 通用引擎参数
        private readonly string appId = Global.GetSettingString("appId");

        // 检测引擎参数
        private IntPtr detectEngine;

        // 引擎初始化结果
        private int detectInitCode;
        private readonly int detectSize = Global.GetSettingInt("detectSize") * 1024 * 1024;
        private readonly float minSimilarity = Global.GetSettingFloat("minSimilarity");
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

            /*var macIsInDatabase = false;
            foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
            {
                if (nic.OperationalStatus == OperationalStatus.Up)
                {
                    var mac = nic.GetPhysicalAddress().ToString().Replace("-", "").ToUpper();
                    foreach (DataRow row in Global.GetClientMacs().Rows)
                    {
                        if (mac.Equals(row.ItemArray[0]))
                        {
                            macIsInDatabase = true;
                            break;
                        }
                    }

                    if (macIsInDatabase)
                    {
                        break;
                    }
                }
            }

            if (macIsInDatabase == false)
            {
                result.code = 0x03;
                result.message = "非法请求";

                context.Response.Write(JsonConvert.SerializeObject(result));

                return;
            }*/

            if (context.Request.HttpMethod != "POST")
            {
                result.code = 0x01;
                result.message = "需要使用POST请求";

                context.Response.Write(JsonConvert.SerializeObject(result));

                return;
            }

            // 没有图片的请求
            if (string.IsNullOrEmpty(context.Request.Form["facesImage"]))
            {
                result.code = 0x02;
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

                // 销毁检测引擎
                ArcSoft.AFD_FSDK_UninitialFaceEngine(detectEngine);

                context.Response.Write(JsonConvert.SerializeObject(result));
                return;
            }

            // base64编码的图片
            var base64 = context.Request.Form["facesImage"];

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
            offInput.ppu8Plane[0] = imageDataPtr;
            offInput.pi32Pitch[0] = pitch;
            var offInputPtr = Marshal.AllocHGlobal(Marshal.SizeOf(offInput));
            Marshal.StructureToPtr(offInput, offInputPtr, false);

            // 检测结果对象指针
            var faceRes = new ArcSoft.AFD_FSDK_FACERES();
            var faceResPtr = Marshal.AllocHGlobal(Marshal.SizeOf(faceRes));
            // 进行检测
            int detectResult;
            try
            {
                detectResult = ArcSoft.AFD_FSDK_StillImageFaceDetection(detectEngine, offInputPtr, ref faceResPtr);
            }
            catch (AccessViolationException e)
            {
                result.code = -1;
                result.message = e.Message;

                context.Response.Write(JsonConvert.SerializeObject(result));

                Marshal.FreeHGlobal(imageDataPtr);
                Marshal.FreeHGlobal(offInputPtr);
                //Marshal.FreeHGlobal(faceResPtr);

                // 销毁检测引擎
                ArcSoft.AFD_FSDK_UninitialFaceEngine(detectEngine);
                // 销毁识别引擎
                ArcSoft.AFR_FSDK_UninitialEngine(recognitionEngion);

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

                    // 销毁检测引擎
                    ArcSoft.AFD_FSDK_UninitialFaceEngine(detectEngine);
                    // 销毁识别引擎
                    ArcSoft.AFR_FSDK_UninitialEngine(recognitionEngion);
                }
                else if (faceRes.nFace > 1)
                {
                    result.code = 0x12;
                    result.message = "检测到多个人脸";

                    context.Response.Write(JsonConvert.SerializeObject(result));

                    Marshal.FreeHGlobal(imageDataPtr);
                    Marshal.FreeHGlobal(offInputPtr);
                    //Marshal.FreeHGlobal(faceResPtr);

                    // 销毁检测引擎
                    ArcSoft.AFD_FSDK_UninitialFaceEngine(detectEngine);
                    // 销毁识别引擎
                    ArcSoft.AFR_FSDK_UninitialEngine(recognitionEngion);
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
                    //Marshal.FreeHGlobal(faceResPtr);
                    Marshal.FreeHGlobal(faceInputPtr);
                    Marshal.FreeHGlobal(modelResPtr);

                    if (recognitionResult == 0)
                    {
                        // 获取人脸特征
                        var featureContent = new byte[modelRes.lFeatureSize];
                        Marshal.Copy(modelRes.pbFeature, featureContent, 0, modelRes.lFeatureSize);

                        var similarity = 0f;
                        var returnStruct = new ReturnStruct();
                        var compareRes = Compare(featureContent, ref similarity, ref returnStruct);
                        // 存在匹配的人脸
                        if (compareRes == 0)
                        {
                            result.message = "成功";
                            result.data = returnStruct;

                            context.Session["uid"] = returnStruct.userId;
                        }
                        // 不存在匹配的人脸
                        else if (compareRes == -1)
                        {
                            result.data = -1;
                        }
                        // 函数错误
                        else
                        {
                            result.code = 0x24;
                            result.message = "系统错误";
                            result.data = compareRes;
                        }

                        context.Response.Write(JsonConvert.SerializeObject(result));

                        // 销毁检测引擎
                        ArcSoft.AFD_FSDK_UninitialFaceEngine(detectEngine);
                        // 销毁识别引擎
                        ArcSoft.AFR_FSDK_UninitialEngine(recognitionEngion);
                    }
                    else
                    {
                        result.code = 0x23;
                        result.message = "服务器错误";
                        result.data = recognitionResult;

                        context.Response.Write(JsonConvert.SerializeObject(result));

                        // 销毁检测引擎
                        ArcSoft.AFD_FSDK_UninitialFaceEngine(detectEngine);
                        // 销毁识别引擎
                        ArcSoft.AFR_FSDK_UninitialEngine(recognitionEngion);
                    }
                }
            }
            else
            {
                result.code = 0x22;
                result.message = "服务器错误";
                result.data = detectResult;

                context.Response.Write(JsonConvert.SerializeObject(result));

                // 销毁检测引擎
                ArcSoft.AFD_FSDK_UninitialFaceEngine(detectEngine);
                // 销毁识别引擎
                ArcSoft.AFR_FSDK_UninitialEngine(recognitionEngion);
            }
        }

        public bool IsReusable => false;

        /// <summary>
        ///     将人脸数据与数据库的人脸数据进行一一对比
        /// </summary>
        /// <param name="feature"></param>
        /// <param name="similarity"></param>
        /// <returns>有匹配返回0， 没有匹配返回-1，失败返回code</returns>
        private int Compare(byte[] feature, ref float similarity, ref ReturnStruct returnStruct)
        {
            // 检测到的脸部信息
            var localFaceModels = new ArcSoft.AFR_FSDK_FaceModel();
            var sourceFeaturePtr = Marshal.AllocHGlobal(feature.Length);
            Marshal.Copy(feature, 0, sourceFeaturePtr, feature.Length);
            localFaceModels.lFeatureSize = feature.Length;
            localFaceModels.pbFeature = sourceFeaturePtr;
            var localFacePtr = Marshal.AllocHGlobal(Marshal.SizeOf(localFaceModels));
            Marshal.StructureToPtr(localFaceModels, localFacePtr, false);

            // 服务器的脸部信息
            var libraryFaceModel = new ArcSoft.AFR_FSDK_FaceModel();
            var libaryFeaturePtr = IntPtr.Zero;
            var libraryFacePtr = IntPtr.Zero;

            // 与数据库中每条人脸特征信息进行比较
            foreach (DataRow row in Global.GetlibraryFeatures().Rows)
            {
                var libaryFeature = (byte[]) row.ItemArray[0];

                // 准备服务器的脸部信息
                libaryFeaturePtr = Marshal.AllocHGlobal(libaryFeature.Length);
                Marshal.Copy(libaryFeature, 0, libaryFeaturePtr, libaryFeature.Length);
                libraryFaceModel.lFeatureSize = libaryFeature.Length;
                libraryFaceModel.pbFeature = libaryFeaturePtr;
                libraryFacePtr = Marshal.AllocHGlobal(Marshal.SizeOf(libraryFaceModel));
                Marshal.StructureToPtr(libraryFaceModel, libraryFacePtr, false);

                similarity = 0f;
                // 进行比较
                var ret = ArcSoft.AFR_FSDK_FacePairMatching(recognitionEngion, localFacePtr, libraryFacePtr,
                    ref similarity);

                // 释放空间
                Marshal.FreeHGlobal(libaryFeaturePtr);
                Marshal.FreeHGlobal(libraryFacePtr);

                // 比较函数正常
                if (ret == 0)
                {
                    // 找到匹配的人脸
                    if (similarity >= minSimilarity)
                    {
                        returnStruct.userId = int.Parse(row.ItemArray[1].ToString());
                        returnStruct.name = row.ItemArray[2].ToString();
                        var privilege = int.Parse(row.ItemArray[3].ToString());
                        // 如果是访客
                        if (privilege == 3)
                        {
                            var inTime = DateTime.Parse(row.ItemArray[4].ToString().Replace("T", " "));
                            var validDays = int.Parse(row.ItemArray[5].ToString());
                            // 已到时间
                            if (inTime.AddDays(validDays) <= DateTime.Now) continue;
                        }

                        return 0;
                    }
                }
                // 函数错误，停止比较
                else if (ret != 0)
                {
                    return ret;
                }
            }

            // 没有找到匹配的人脸
            return -1;
        }

        private struct ReturnStruct
        {
            public int userId;
            public string name;
        }
    }
}