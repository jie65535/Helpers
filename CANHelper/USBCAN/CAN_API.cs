using System;
using System.Runtime.InteropServices;


namespace USBCAN
{
    public sealed class CAN_API
    {
        #region 数据类型定义
        /// <summary>
        /// 接口卡类型定义
        /// </summary>
        public enum PCIDeviceType
        {
            VCI_PCI5121 = 1,
            VCI_PCI9810 = 2,
            VCI_USBCAN1 = 3,
            VCI_USBCAN2 = 4,
            VCI_PCI9820 = 5,
            VCI_CAN232 = 6,
            VCI_PCI5110 = 7,
            VCI_CANLITE = 8,
            VCI_ISA9620 = 9,
            VCI_ISA5420 = 10,
            VCI_PC104CAN = 11,
            VCI_CANETE = 12,
            VCI_DNP9810 = 13,
            VCI_PCI9840 = 14,
            VCI_PCI9820I = 16
        }

        //函数调用返回状态值
        /// <summary>
        /// 正常状态
        /// </summary>
        public static readonly int STATUS_OK = 1;
        /// <summary>
        /// 发生错误
        /// </summary>
        public static readonly int STATUS_ERR = 0;

        /// <summary>
        /// 错误类型
        /// </summary>
        public enum ErrorType
        {
            // --------------- CAN错误码 -------------------
            /// <summary>
            /// CAN错误码:CAN控制器内部FIFO溢出
            /// </summary>
            ERR_CAN_OVERFLOW = 0x0001,
            /// <summary>
            /// CAN错误码:CAN控制器错误报警
            /// </summary>
            ERR_CAN_ERRALARM = 0x0002,
            /// <summary>
            /// CAN错误码:CAN控制器消极错误
            /// </summary>
            ERR_CAN_PASSIVE = 0x0004,
            /// <summary>
            /// CAN错误码:CAN控制器仲裁丢失
            /// </summary>
            ERR_CAN_LOSE = 0x0008,
            /// <summary>
            /// CAN错误码:CAN控制器总线错误
            /// </summary>
            ERR_CAN_BUSERR = 0x0010,

            // --------------- 通用错误码 -------------------
            /// <summary>
            /// 通用错误码:设备已经打开
            /// </summary>
            ERR_DEVICEOPENED = 0x0100,
            /// <summary>
            /// 通用错误码:打开设备错误
            /// </summary>
            ERR_DEVICEOPEN = 0x0200,
            /// <summary>
            /// 通用错误码:设备没有打开
            /// </summary>
            ERR_DEVICENOTOPEN = 0x0400,
            /// <summary>
            /// 通用错误码:缓冲区溢出
            /// </summary>
            ERR_BUFFEROVERFLOW = 0x0800,
            /// <summary>
            /// 通用错误码:此设备不存在
            /// </summary>
            ERR_DEVICENOTEXIST = 0x1000,
            /// <summary>
            /// 通用错误码:装载动态库失败
            /// </summary>
            ERR_LOADKERNELDLL = 0x2000,
            /// <summary>
            /// 通用错误码:执行命令失败错误码
            /// </summary>
            ERR_CMDFAILED = 0x4000,
            /// <summary>
            /// 通用错误码:内存不足
            /// </summary>
            ERR_BUFFERCREATE = 0x8000
        }

        /// <summary>
        /// ZLGCAN系列接口卡信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct VCI_BOARD_INFO
        {
            public ushort hw_Version;
            public ushort fw_Version;
            public ushort dr_Version;
            public ushort in_Version;
            public ushort irq_Num;
            public byte can_Num;
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 20)]
            public string str_Serial_Num;
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 40)]
            public string str_hw_Type;
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = System.Runtime.InteropServices.UnmanagedType.U2)]
            public ushort[] Reserved;
        }

        /// <summary>
        /// CAN信息帧
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct VCI_CAN_OBJ
        {
            public uint ID;
            public uint TimeStamp;
            public byte TimeFlag;
            public byte SendType;
            public byte RemoteFlag;//是否是远程帧
            public byte ExternFlag;//是否是扩展帧
            public byte DataLen;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8, ArraySubType = UnmanagedType.I1)]
            public byte[] Data;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
            public byte[] Reserved;
        }

        /// <summary>
        /// CAN控制器状态
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct VCI_CAN_STATUS
        {
            public byte ErrInterrupt;
            public byte regMode;
            public byte regStatus;
            public byte regALCapture;
            public byte regECCapture;
            public byte regEWLimit;
            public byte regRECounter;
            public byte regTECounter;
            public uint Reserved;
        }

        /// <summary>
        /// 错误信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct VCI_ERR_INFO
        {
            public uint ErrCode;

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3, ArraySubType = UnmanagedType.I1)]
            public byte[] Passive_ErrData;

            public byte ArLost_ErrData;
        }

        /// <summary>
        /// 初始化CAN的配置信息
        /// </summary>
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct VCI_INIT_CONFIG
        {
            /// <summary>
            /// 验收码
            /// </summary>
            public uint AccCode;
            /// <summary>
            /// 屏蔽码
            /// </summary>
            public uint AccMask;
            /// <summary>
            /// 预留，填0
            /// </summary>
            public uint Reserved;
            /// <summary>
            /// 滤波方式
            /// </summary>
            public byte Filter;
            public byte Timing0;
            public byte Timing1;
            /// <summary>
            /// 模式：0:正常|1:只听
            /// </summary>
            public byte Mode;
        }
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        public struct CHGDESIPANDPORT
        {
            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 10)]
            public string szpwd;

            [System.Runtime.InteropServices.MarshalAsAttribute(System.Runtime.InteropServices.UnmanagedType.ByValTStr, SizeConst = 20)]
            public string szdesip;

            public int desport;
        }
        #endregion

        #region API函数

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_OpenDevice", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_OpenDevice(uint DeviceType, uint DeviceInd, uint Reserved);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_CloseDevice", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_CloseDevice(uint DeviceType, uint DeviceInd);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_ResetCAN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_ResetCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_InitCAN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_InitCAN(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_INIT_CONFIG pInitConfig);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_Transmit", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_Transmit(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_OBJ pSend, uint Len);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_Receive", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_Receive(uint DeviceType, uint DeviceInd, uint CANInd, IntPtr pReceive, uint Len, int WaitTime);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_GetReceiveNum", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_GetReceiveNum(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_ClearBuffer", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_ClearBuffer(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_ReadErrInfo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_ReadErrInfo(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_ERR_INFO pErrInfo);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_StartCAN", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_StartCAN(uint DeviceType, uint DeviceInd, uint CANInd);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_SetReference", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_SetReference(uint DeviceType, uint DeviceInd, uint CANInd, uint RefType, object pData);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_GetReference", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_GetReference(uint DeviceType, uint DeviceInd, uint CANInd, uint RefType, object pData);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_ReadCANStatus", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_ReadCANStatus(uint DeviceType, uint DeviceInd, uint CANInd, ref VCI_CAN_STATUS pCANStatus);

        [DllImport("ControlCAN.dll", SetLastError = true, EntryPoint = "VCI_ReadBoardInfo", CharSet = CharSet.Ansi, CallingConvention = CallingConvention.StdCall)]
        public static extern uint VCI_ReadBoardInfo(uint DeviceType, uint DeviceInd, ref VCI_BOARD_INFO pInfo);

        #endregion
    }
}
