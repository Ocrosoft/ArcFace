using System;
using System.Runtime.InteropServices;

namespace ServerCore.Models
{
    public class ArcSoft
    {
        /// <summary>
        ///     基于逆时针的脸部方向枚举值
        /// </summary>
        public enum AFD_FSDK_OrientCode
        {
            AFD_FSDK_FOC_0 = 1,
            AFD_FSDK_FOC_90 = 2,
            AFD_FSDK_FOC_270 = 3,
            AFD_FSDK_FOC_180 = 4,
            AFD_FSDK_FOC_30 = 5,
            AFD_FSDK_FOC_60 = 6,
            AFD_FSDK_FOC_120 = 7,
            AFD_FSDK_FOC_150 = 8,
            AFD_FSDK_FOC_210 = 9,
            AFD_FSDK_FOC_240 = 10,
            AFD_FSDK_FOC_300 = 11,
            AFD_FSDK_FOC_330 = 12
        }

        /// <summary>
        ///     定义脸部检测角度的优先级
        /// </summary>
        public enum AFD_FSDK_OrientPriority
        {
            /// 检测0度方向
            AFD_FSDK_OPF_0_ONLY = 1,

            /// 检测90度方向
            AFD_FSDK_OPF_90_ONLY = 2,

            /// 检测270度方向
            AFD_FSDK_OPF_270_ONLY = 3,

            /// 检测180度方向
            AFD_FSDK_OPF_180_ONLY = 4,

            /// 检测0, 90, 180, 270四个方向,0度更优先
            AFD_FSDK_OPF_0_HIGHER_EXT = 5
        }

        /// <summary>
        ///     基于逆时针的脸部方向枚举值
        /// </summary>
        public enum AFR_FSDK_OrientCode
        {
            AFD_FSDK_FOC_0 = 1,
            AFD_FSDK_FOC_90 = 2,
            AFD_FSDK_FOC_270 = 3,
            AFD_FSDK_FOC_180 = 4,
            AFD_FSDK_FOC_30 = 5,
            AFD_FSDK_FOC_60 = 6,
            AFD_FSDK_FOC_120 = 7,
            AFD_FSDK_FOC_150 = 8,
            AFD_FSDK_FOC_210 = 9,
            AFD_FSDK_FOC_240 = 10,
            AFD_FSDK_FOC_300 = 11,
            AFD_FSDK_FOC_330 = 12
        }

        /// <summary>
        ///     初始化脸部检测引擎
        /// </summary>
        /// <param name="appId">用户申请SDK时获取的App Id</param>
        /// <param name="sdkKey">用户申请SDK时获取的SDK Key</param>
        /// <param name="pMem">分配给引擎使用的内存地址</param>
        /// <param name="lMemSize">分配给引擎使用的内存大小</param>
        /// <param name="pEngine">引擎handle</param>
        /// <param name="iOrientPriority">期望的脸部检测角度的优先级</param>
        /// <param name="nScale">用于数值表示的最小人脸尺寸 有效范围[2, 50] 推荐值16</param>
        /// <param name="nMaxFaceNum">用户期望引擎最多能检测出的人脸数 有效值范围[1, 100]</param>
        /// <returns>
        ///     成功返回MOK，否则返回失败code。失败codes如下所列：
        ///     MERR_INVALID_PARAM 参数输入非法
        ///     MERR_NO_MOMORY 内存不足
        /// </returns>
        [DllImport("libarcsoft_fsdk_face_detection.dll", EntryPoint = "AFD_FSDK_InitialFaceEngine",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFD_FSDK_InitialFaceEngine(string appId, string sdkKey, IntPtr pMem, int lMemSize,
            ref IntPtr pEngine, int iOrientPriority, int nScale, int nMaxFaceNum);

        /// <summary>
        ///     根据输入的图像检测出人脸位置，一般用于静态图像检测
        /// </summary>
        /// <param name="pEngine">引擎handle</param>
        /// <param name="pImgData">待检测图像信息</param>
        /// <param name="pFaceRes">人脸检测结果</param>
        /// <returns></returns>
        [DllImport("libarcsoft_fsdk_face_detection.dll", EntryPoint = "AFD_FSDK_StillImageFaceDetection",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFD_FSDK_StillImageFaceDetection(IntPtr pEngine, IntPtr pImgData, ref IntPtr pFaceRes);

        /// <summary>
        ///     销毁引擎，释放相应资源
        /// </summary>
        /// <param name="pEngine">引擎handle</param>
        /// <returns>
        ///     成功返回MOK，否则返回失败code。失败codes如下所列：
        ///     MERR_INVALID_PARAM 输入参数非法
        /// </returns>
        [DllImport("libarcsoft_fsdk_face_detection.dll", EntryPoint = "AFD_FSDK_UninitialFaceEngine",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFD_FSDK_UninitialFaceEngine(IntPtr pEngine);

        /// <summary>
        ///     获取SDK版本信息
        /// </summary>
        /// <param name="pEngine">引擎handle</param>
        /// <returns></returns>
        [DllImport("libarcsoft_fsdk_face_detection.dll", EntryPoint = "AFD_FSDK_GetVersion",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AFD_FSDK_GetVersion(IntPtr pEngine);

        /// <summary>
        ///     初始化引擎
        /// </summary>
        /// <param name="AppId">用户申请SDK时获取的id</param>
        /// <param name="SDKKey">用户申请SDK时获取的id</param>
        /// <param name="pMem">分配给引擎使用的内存地址</param>
        /// <param name="lMemSize">分配给引擎使用的内存大小</param>
        /// <param name="phEngine">引擎handle</param>
        /// <returns>
        ///     成功返回MOK，否则返回失败code。失败codes如下所列：
        ///     MERR_INVALID_PARAM 参数输入非法
        ///     MERR_NO_MEMORY 内存不足
        /// </returns>
        [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_InitialEngine",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFR_FSDK_InitialEngine(string AppId, string SDKKey, IntPtr pMem, int lMemSize,
            ref IntPtr phEngine);

        /// <summary>
        ///     获取脸部特征
        /// </summary>
        /// <param name="hEngine">引擎handle</param>
        /// <param name="pInputImage">输入的图像数据</param>
        /// <param name="pFaceRes">已检测到的脸部信息</param>
        /// <param name="pFaceModels">提取的脸部特征信息</param>
        /// <returns>
        ///     成功返回MOK，否则返回失败code。失败codes如下所列：
        ///     MERR_INVALID_PARAM 参数输入非法
        ///     MERR_NO_MEMORY 内存不足
        /// </returns>
        [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_ExtractFRFeature",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFR_FSDK_ExtractFRFeature(IntPtr hEngine, IntPtr pInputImage, IntPtr pFaceRes,
            IntPtr pFaceModels);

        /// <summary>
        ///     脸部特征比较
        /// </summary>
        /// <param name="hEngine">引擎handle</param>
        /// <param name="reffeature">已有脸部特征信息</param>
        /// <param name="probefeature">被比较的脸部特征信息</param>
        /// <param name="pfSimilScore">相似程度数值</param>
        /// <returns>
        ///     成功返回MOK，否则返回失败code。失败codes如下所列：
        ///     MERR_INVALID_PARAM 输入参数非法
        ///     MERR_NO_MEMORY 内存不足
        /// </returns>
        [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_FacePairMatching",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFR_FSDK_FacePairMatching(IntPtr hEngine, IntPtr reffeature, IntPtr probefeature,
            ref float pfSimilScore);

        /// <summary>
        ///     结束引擎
        /// </summary>
        /// <param name="hEngine">引擎handle</param>
        /// <returns>
        ///     成功返回MOK，否则返回失败code。失败codes如下所列：
        ///     MERR_INVALID_PARAM 参数输入非法
        /// </returns>
        [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_UninitialEngine",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern int AFR_FSDK_UninitialEngine(IntPtr hEngine);

        /// <summary>
        ///     获取引擎版本信息
        /// </summary>
        /// <param name="hEngine">引擎handle</param>
        /// <returns></returns>
        [DllImport("libarcsoft_fsdk_face_recognition.dll", EntryPoint = "AFR_FSDK_GetVersion",
            CallingConvention = CallingConvention.Cdecl)]
        public static extern IntPtr AFR_FSDK_GetVersion(IntPtr hEngine);

        /*******************************************************************************************
        FaceDetection 人脸检测
        *******************************************************************************************/
        /// <summary>
        ///     检测到的脸部信息
        /// </summary>
        public struct AFD_FSDK_FACERES
        {
            /// 人脸个数
            public int nFace;

            /// 人脸矩形框信息
            public IntPtr rcFace;

            /// 人脸角度信息
            public IntPtr lfaceOrient;
        }

        /// <summary>
        ///     AFD_FSDK_FACERES.rcFace
        /// </summary>
        public struct MRECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        /// <summary>
        ///     SDK 版本信息
        /// </summary>
        public struct AFD_FSDK_Version
        {
            /// 代码库版本号
            public int lCodeBase;

            /// 主版本号
            public int lMajor;

            /// 次版本号
            public int lMinor;

            /// 编译版本号，递增
            public int lBuild;

            /// 字符串形式的版本号
            public IntPtr Version;

            /// 编译时间
            public IntPtr BuildDate;

            /// copyright
            public IntPtr CopyRight;
        }

        /// <summary>
        ///     asvloffscreen.h
        /// </summary>
        public struct ASVLOFFSCREEN
        {
            public int u32PixelArrayFormat;
            public int i32Width;
            public int i32Height;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.SysUInt)]
            public IntPtr[] ppu8Plane;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.I4)]
            public int[] pi32Pitch;
        }

        /*******************************************************************************************
        FaceRecognition 人脸识别
        *******************************************************************************************/
        /// <summary>
        ///     脸部信息
        /// </summary>
        public struct AFR_FSDK_FaceInput
        {
            /// 脸部矩形框信息
            public MRECT rcFace;

            /// 脸部旋转角度
            public int lOrient;
        }

        /// <summary>
        ///     脸部特征信息
        /// </summary>
        public struct AFR_FSDK_FaceModel
        {
            /// 提取到的脸部特征
            public IntPtr pbFeature;

            /// 特征信息长度
            public int lFeatureSize;
        }

        /// <summary>
        ///     引擎版本信息
        /// </summary>
        public struct AFR_FSDK_Version
        {
            /// 代码库版本号
            public int lCodebase;

            /// 主版本号
            public int lMajor;

            /// 次版本号
            public int lMinor;

            /// 编译版本号，递增
            public int lBuild;

            /// 特征库版本号
            public int lFeatureLevel;

            /// 字符串形式的版本号
            public string Version;

            /// 编译时间
            public string BuildDate;

            /// Copyright
            public string CopyRight;
        }
    }
}