using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Web;
using Newtonsoft.Json;
using ServerCore.Models;

namespace ServerCore.Api
{
    /// <summary>
    ///     faceDetect 的摘要说明
    /// </summary>
    public class faceDetect : IHttpHandler
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

            // base64
            var base64 = context.Request.Form["facesImage"];

            // 没有图片
            if (string.IsNullOrEmpty(base64))
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

                    return;
                }

                if (faceRes.nFace > 1)
                {
                    result.code = 0x12;
                    result.message = "检测到多个人脸";

                    context.Response.Write(JsonConvert.SerializeObject(result));

                    Marshal.FreeHGlobal(imageDataPtr);
                    Marshal.FreeHGlobal(offInputPtr);
                    //Marshal.FreeHGlobal(faceResPtr);

                    return;
                }

                var rect = (ArcSoft.MRECT) Marshal.PtrToStructure(faceRes.rcFace, typeof(ArcSoft.MRECT));
                var cutedImage = Global.CutImage(faceImage, rect.left, rect.top, rect.bottom - rect.top,
                    rect.right - rect.left);

                /*Graphics g = Graphics.FromImage(faceImage);
                    Brush brush = new SolidBrush(Color.Red);
                    Pen pen = new Pen(brush, 2);
                    pen.DashStyle = DashStyle.Dash;
                    g.DrawRectangle(pen,
                        new Rectangle(rect.left, rect.top, rect.bottom - rect.top, rect.right - rect.left));
                    g.Dispose();*/


                result.data = Global.GetBase64FromImage(cutedImage, true);

                context.Response.Write(JsonConvert.SerializeObject(result));
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
    }
}