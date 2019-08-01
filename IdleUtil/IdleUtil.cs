using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Utils
{
    /// <summary>
    /// 空闲监控工具
    /// 提供定时器操作方法，当用户超过指定时间未操作时，触发Timeout事件
    /// </summary>
    public class IdleUtil
    {
        #region Public

        /// <summary>
        /// 重置定时器为新时间
        /// </summary>
        /// <param name="dueTime">超过多少时间未操作发出事件（毫秒）</param>
        public void Reset(int dueTime)
        {
            Start(dueTime);
        }

        /// <summary>
        /// 重置定时器
        /// </summary>
        public void Reset()
        {
            Start(this.DueTime);
        }

        /// <summary>
        /// 开始计时
        /// </summary>
        /// <param name="dueTime">超过多少时间未操作发出事件（毫秒）</param>
        public void Start(int dueTime)
        {
            DueTime = dueTime;
            if (!IsRunning)
            {
                IsRunning = true;
                MonitorThread = new Thread(Moniter) { IsBackground = true };
                MonitorThread.Start();
            }
            Timer.Change(dueTime, Timeout.Infinite);
        }

        /// <summary>
        /// 停止计时
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            Timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #endregion Public

        #region Event

        /// <summary>
        /// 超过指定事件未操作事件
        /// </summary>
        public event EventHandler TimeoutEvent;

        #endregion Event

        #region Ctor

        public IdleUtil()
        {
            Timer = new Timer(OnTimeout);
        }

        #endregion Ctor

        #region Private

        /// <summary>
        /// 定时器
        /// </summary>
        private readonly Timer  Timer;

        /// <summary>
        /// 用户要求定时时间
        /// </summary>
        private          int    DueTime;

        /// <summary>
        /// 线程是否运行
        /// </summary>
        private          bool   IsRunning;

        /// <summary>
        /// 监控线程
        /// </summary>
        private          Thread MonitorThread;
        /// <summary>
        /// 监控线程方法
        /// </summary>
        private void Moniter()
        {
            while (IsRunning)
            {
                if (GetLastInputTime() == 0)
                    Reset();
                Thread.Sleep(1000);
            }
        }

        #endregion Private

        #region Trigger

        private void OnTimeout(object _)
        {
            if (TimeoutEvent != null)
                TimeoutEvent(this, EventArgs.Empty);
            // 只触发一次
            Timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #endregion Trigger

        #region WindowsAPI

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// 获取用户最后一次输入的时间
        /// </summary>
        /// <returns>最后一次输入的时间</returns>
        private static uint GetLastInputTime()
        {
            uint idleTime = 0;
            LASTINPUTINFO lastInputInfo = new LASTINPUTINFO();
            lastInputInfo.cbSize = (uint)Marshal.SizeOf(lastInputInfo);
            lastInputInfo.dwTime = 0;

            uint envTicks = (uint)Environment.TickCount;

            if (GetLastInputInfo(ref lastInputInfo))
            {
                uint lastInputTick = lastInputInfo.dwTime;

                idleTime = envTicks - lastInputTick;
            }

            return ((idleTime > 0) ? (idleTime / 1000) : 0);
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct LASTINPUTINFO
        {
            public static readonly int SizeOf = Marshal.SizeOf(typeof(LASTINPUTINFO));

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 cbSize;

            [MarshalAs(UnmanagedType.U4)]
            public UInt32 dwTime;
        }

        #endregion WindowsAPI
    }
}