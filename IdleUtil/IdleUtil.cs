using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace Utils
{
    /// <summary>
    /// ���м�ع���
    /// �ṩ��ʱ���������������û�����ָ��ʱ��δ����ʱ������Timeout�¼�
    /// </summary>
    public class IdleUtil
    {
        #region Public

        /// <summary>
        /// ���ö�ʱ��Ϊ��ʱ��
        /// </summary>
        /// <param name="dueTime">��������ʱ��δ���������¼������룩</param>
        public void Reset(int dueTime)
        {
            Start(dueTime);
        }

        /// <summary>
        /// ���ö�ʱ��
        /// </summary>
        public void Reset()
        {
            Start(this.DueTime);
        }

        /// <summary>
        /// ��ʼ��ʱ
        /// </summary>
        /// <param name="dueTime">��������ʱ��δ���������¼������룩</param>
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
        /// ֹͣ��ʱ
        /// </summary>
        public void Stop()
        {
            IsRunning = false;
            Timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #endregion Public

        #region Event

        /// <summary>
        /// ����ָ���¼�δ�����¼�
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
        /// ��ʱ��
        /// </summary>
        private readonly Timer  Timer;

        /// <summary>
        /// �û�Ҫ��ʱʱ��
        /// </summary>
        private          int    DueTime;

        /// <summary>
        /// �߳��Ƿ�����
        /// </summary>
        private          bool   IsRunning;

        /// <summary>
        /// ����߳�
        /// </summary>
        private          Thread MonitorThread;
        /// <summary>
        /// ����̷߳���
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
            // ֻ����һ��
            Timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

        #endregion Trigger

        #region WindowsAPI

        [DllImport("user32.dll")]
        private static extern bool GetLastInputInfo(ref LASTINPUTINFO plii);

        /// <summary>
        /// ��ȡ�û����һ�������ʱ��
        /// </summary>
        /// <returns>���һ�������ʱ��</returns>
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