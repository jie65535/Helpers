using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;

namespace USBCAN
{
    /// <summary>
    /// CAN帮助类
    /// </summary>
    /// <example>
    /// 使用方法：
    /// <code>
    /// CANHelper.Instance.Initialize(); // 在首次使用时需要初始化，初始化失败时会抛出异常
    /// CANHelper.Instance.ConsumptionFrameEvent += (CAN_API.VCI_CAN_OBJ frame){ /* TODO:在这里消费掉数据帧，注意，这里是异步调用  */ }
    /// CANHelper.Instance.SendData(0x1111, data); // 向CAN发送数据，具体方法参阅注释
    /// </code>
    /// </example>
    public sealed class CANHelper : IDisposable
    {
        /// <summary>
        /// CAN实例
        /// </summary>
        public static readonly CANHelper Instance = new CANHelper();

        #region 公开属性
        /// <summary>
        /// CAN是否打开
        /// </summary>
        public bool IsOpen { private set; get; } = false;
        #endregion

        #region 构造
        private CANHelper()
        {
        }
        #endregion

        #region 释放
        /// <summary>
        /// Flag: 标识Disposed是否已经被调用
        /// </summary>
        private bool _IsDisposed = false;

        /// <summary>
        /// 公开的Dispose方法
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// 实际Dispose方法
        /// </summary>
        /// <param name="disposing">是否由用户调用</param>
        private void Dispose(bool disposing)
        {
            if (_IsDisposed)
                return;

            if (disposing)
            {
                // 释放托管成员
            }

            // 释放非托管成员

            // 关闭CAN设备
            CloseDevice();

            // 标识为已执行Dispose
            _IsDisposed = true;
        }

        /// <summary>
        /// 析构
        /// </summary>
        ~CANHelper()
        {
            // 释放非托管内存
            Dispose(false);
        }
        #endregion

        #region 打开与关闭

        /// <summary>
        /// 初始化并打开CAN设备
        /// </summary>
        public void Initialize(CAN_API.CAN_BaudRate baudRate)
        {
            // 如果已经打开，直接返回
            if (IsOpen)
                return;

            // 打开设备
            if (CAN_API.VCI_OpenDevice(_DeviceType, _DeviceInd, _CANInd) == CAN_API.STATUS_ERR)
                throw new Exception("CAN设备打开失败，错误信息：" + ReadErrorMessage());

            // 构造CAN配置信息
            CAN_API.VCI_INIT_CONFIG pInitConfig = new CAN_API.VCI_INIT_CONFIG
            {
                AccCode     = 0x00000000,   // 表示全部接收（全部接收: AccCode:0x00000000)
                AccMask     = 0xFFFFFFFF,   //            (         AccMask:0xFFFFFFFF）
                Reserved    = 0x00,         // 保留，填0
                Filter      = 0x01,         // 滤波方式 01
                Timing0     = CAN_API.VCI_INIT_CONFIG_Timing0[(int)baudRate],    // ( 波特率查表 )
                Timing1     = CAN_API.VCI_INIT_CONFIG_Timing1[(int)baudRate],    // ( 波特率查表 )
                Mode        = 0x00          // 正常模式； 0:正常模式，可以IO。  1：表示只听模式（只接收，不影响总线）
            };
            // 初始化CAN
            if (CAN_API.VCI_InitCAN(_DeviceType, _DeviceInd, _CANInd, ref pInitConfig) == CAN_API.STATUS_ERR)
                throw new Exception("CAN初始化失败，错误信息：" + ReadErrorMessage());

            // 启动CAN
            if (CAN_API.VCI_StartCAN(_DeviceType, _DeviceInd, _CANInd) == CAN_API.STATUS_ERR)
                throw new Exception("CAN启动失败，错误信息：" + ReadErrorMessage());

            // 若未出错，标识打开
            IsOpen = true;

            // 初始化消息帧缓冲区，上限为128帧报文，若满了还未消费则阻塞
            _FrameBuffer = new BlockingCollection<CAN_API.VCI_CAN_OBJ>(128);

            // 生产者消费者开始工作
            StartWork();
        }

        /// <summary>
        /// 关闭CAN设备
        /// </summary>
        public void CloseDevice()
        {
            if (IsOpen)
            {
                // 关闭设备
                if (CAN_API.VCI_CloseDevice(_DeviceType, _DeviceInd) == CAN_API.STATUS_ERR)
                    throw new Exception("CAN设备关闭失败，错误信息：" + ReadErrorMessage());

                IsOpen = false;
            }
        }
        #endregion

        #region 公开方法
        /// <summary>
        /// 读取错误信息
        /// </summary>
        /// <returns></returns>
        public string ReadErrorMessage()
        {
            CAN_API.VCI_ERR_INFO errInfo = new CAN_API.VCI_ERR_INFO();
            try
            {
                // 尝试读取错误信息
                if (CAN_API.VCI_ReadErrInfo(_DeviceType, _DeviceInd, _CANInd, ref errInfo) == CAN_API.STATUS_ERR)
                    return "读取错误信息失败";
            }
            catch (Exception ex)
            {
                return string.Format("读取错误信息时发生异常（{0}）", ex.Message);
            }

            if (errInfo.ErrCode == 0x00)
            {
                // 若无错误信息，则返回‘无错误信息’
                return "无错误信息";
            }
            
            // 由于可能同时出现多种错误，使用按位与的方式读取错误信息
            List<string> errMsgList = new List<string>();
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_CAN_OVERFLOW) != 0)
                errMsgList.Add("CAN控制器内部FIFO溢出");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_CAN_ERRALARM) != 0)
                errMsgList.Add("CAN控制器错误报警");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_CAN_PASSIVE) != 0)
                errMsgList.Add("CAN控制器消极错误");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_CAN_LOSE) != 0)
                errMsgList.Add("CAN控制器仲裁丢失");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_CAN_BUSERR) != 0)
                errMsgList.Add("CAN控制器总线错误");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_DEVICEOPENED) != 0)
                errMsgList.Add("设备已经打开");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_DEVICEOPEN) != 0)
                errMsgList.Add("打开设备错误");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_DEVICENOTOPEN) != 0)
                errMsgList.Add("设备没有打开");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_BUFFEROVERFLOW) != 0)
                errMsgList.Add("缓冲区溢出");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_DEVICENOTEXIST) != 0)
                errMsgList.Add("此设备不存在");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_LOADKERNELDLL) != 0)
                errMsgList.Add("装载动态库失败");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_CMDFAILED) != 0)
                errMsgList.Add("执行命令失败");
            if ((errInfo.ErrCode & (uint)CAN_API.ErrorType.ERR_BUFFERCREATE) != 0)
                errMsgList.Add("内存不足");

            if (errMsgList.Count == 0)
            {
                // 若未检测到错误信息，则返回‘未知错误’
                return "未知错误";
            }
            else if (errMsgList.Count == 1)
            {
                return errMsgList[0];
            }
            else
            {
                // 否则将错误信息以'|'拼接返回
                return string.Join("|", errMsgList);
            }
        }

        /// <summary>
        /// 向CAN发送数据帧
        /// </summary>
        /// <param name="frameID">发送帧ID</param>
        /// <param name="data">数据数组（数组长度必须为8）</param>
        public void SendData(uint frameID, byte[] data)
        {
            CAN_API.VCI_CAN_OBJ frameInfo = new CAN_API.VCI_CAN_OBJ
            {
                ID          = frameID,      // 帧ID
                SendType    = 0,            // 正常发送
                RemoteFlag  = 0,            // 非远程帧
                ExternFlag  = 0,            // 非扩展帧
                DataLen     = 8,            // 数据长度
                Data        = data,         // 数据
                Reserved    = new byte[3]   // 预留
            };
            SendData(frameInfo);
        }

        /// <summary>
        /// 向CAN发送数据帧
        /// </summary>
        /// <param name="frameInfo">帧信息</param>
        public void SendData(CAN_API.VCI_CAN_OBJ frameInfo)
        {
            // 发送一帧数据
            if (CAN_API.VCI_Transmit(_DeviceType, _DeviceInd, _CANInd, ref frameInfo, 1) == CAN_API.STATUS_ERR)
                throw new Exception("数据发送失败，错误信息：" + ReadErrorMessage());
        }

        #endregion

        #region 公开事件与委托
        /// <summary>
        /// 消费帧事件委托
        /// </summary>
        /// <param name="frame">报文帧</param>
        public delegate void ConsumptionFrameEventHandler(CAN_API.VCI_CAN_OBJ frame);
        /// <summary>
        /// 消费帧事件  每读取一帧数据发生一次消费帧事件
        /// </summary>
        public event ConsumptionFrameEventHandler ConsumptionFrameEvent;
        #endregion

        #region 生产者消费者模式 - 生产数据帧，发出事件消费数据帧
        /// <summary>
        /// 帧缓冲区（生产者消费者队列）
        /// </summary>
        private BlockingCollection<CAN_API.VCI_CAN_OBJ> _FrameBuffer;
        /// <summary>
        /// 生产者线程
        /// </summary>
        private Thread _ProducerThread;
        /// <summary>
        /// 消费者线程
        /// </summary>
        private Thread _ConsumerThread;
        /// <summary>
        /// 开始工作线程
        /// </summary>
        private void StartWork()
        {
            // 启动生产者与消费者线程
            // 优先级设置为高

            _ProducerThread = new Thread(new ThreadStart(Producer))
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            _ConsumerThread = new Thread(new ThreadStart(Consumer))
            {
                IsBackground = true,
                Priority = ThreadPriority.Highest
            };
            _ProducerThread.Start();
            _ConsumerThread.Start();
        }

        /// <summary>
        /// 生产者
        /// </summary>
        private void Producer()
        {
            // 分配一个缓冲区，最大能同时容纳100帧数据
            IntPtr readBuffer = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(CAN_API.VCI_CAN_OBJ)) * 100); ;
            while (IsOpen)
            {
                // var n = CAN_API.VCI_GetReceiveNum(_DeviceType, _DeviceInd, _CANInd);
                // 接收数据，超时时间100ms，最大接收100个
                uint len = CAN_API.VCI_Receive(_DeviceType, _DeviceInd, _CANInd, readBuffer, 100, 100);

                if (len == 0)
                {
                    // 如果未读到数据，读取错误信息
                    ReadErrorMessage();
                }
                else
                {
                    // 将读取到的每一帧数据构造帧对象VCI_CAN_OBJ，装入帧缓冲区中（生产产品到库存）
                    for (int i = 0; i < len; i++)
                    {
                        // 实例化帧对象，装入帧缓冲区
                        //_FrameBuffer.Add((CAN_API.VCI_CAN_OBJ)Marshal.PtrToStructure((IntPtr)((uint)readBuffer + i * Marshal.SizeOf(typeof(CAN_API.VCI_CAN_OBJ))), typeof(CAN_API.VCI_CAN_OBJ)));
                        _FrameBuffer.Add(Marshal.PtrToStructure<CAN_API.VCI_CAN_OBJ>((IntPtr)((uint)readBuffer + i * Marshal.SizeOf(typeof(CAN_API.VCI_CAN_OBJ)))));
                    }
                }
            }
            Marshal.FreeHGlobal(readBuffer);
        }

        /// <summary>
        /// 消费者
        /// </summary>
        private void Consumer()
        {
            while (IsOpen)
            {
                // 从队列中获取帧（该方法会线程安全的阻塞，当有数据装入时立刻返回）
                var frame = _FrameBuffer.Take();
                // 消费帧
                if (ConsumptionFrameEvent != null)
                    ConsumptionFrameEvent(frame);
            }
        }
        #endregion

        #region 私有成员
        /// <summary>
        /// 设备类型
        /// </summary>
        private const uint _DeviceType = (uint)CAN_API.PCIDeviceType.VCI_USBCAN1;
        /// <summary>
        /// 设备ID
        /// </summary>
        private const uint _DeviceInd = 0;
        /// <summary>
        /// 第几路CAN
        /// </summary>
        private const uint _CANInd = 0;
        #endregion
    }
}
