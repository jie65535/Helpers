# CANHelper


## 文件结构

| 文件 | 简介 |
| ---- | ---- |
| **CAN_API.cs**    | USBCAN API声明 |
| **CANHelper.cs**  | 帮助类 |
| **kerneldll.ini** | 配置文件 |
| **usbcan.dll**    | USBCAN核心DLL |
| **ControlCAN.dll** | CAN控件DLL |

> **在使用时需要注意，`ControlCAN.dll`与`kerneldlls`必须输出到应用程序目录中**

## CANHelper说明

### 类成员

#### 公开属性
| 名称 | 简介 |
| ---- | ---- |
| `IsOpen` | CAN是否打开 |

#### 公开方法

| 名称 | 简介 |
| ---- | ---- |
| `Initialize` | 初始化并打开CAN设备 |
| `CloseDevice` | 关闭CAN设备 |
| `ReadErrorMessage` | 读取错误信息 |
| `SendData` | 向CAN发送数据帧 |

#### 公开事件与委托


| 类型 | 名称 | 简介 |
| --- | ---- | ---- |
| delegate | ConsumptionFrameEventHandler | 消费帧事件委托 |
| event | ConsumptionFrameEvent | 消费帧事件 |

### 使用示例

#### 简单说明
```C#
// 启动之前，监听消费帧事件，以处理数据
USBCAN.CANHelper.Instance.ConsumptionFrameEvent += frame =>
{
    Console.WriteLine("ID:{0:X}\tTimeStamp:{1}\tTimeFlag:{2}\tSendType:{3}\tRemoteFlag:{4}\tExternFlag:{5}\tDataLen:{6}\tData:{7}\tReserved:{8}\n",
        frame.ID, frame.TimeStamp, frame.TimeFlag, frame.SendType, frame.RemoteFlag, frame.ExternFlag, frame.DataLen, str, string.Join(",", frame.Reserved);
};

// 初始化设备并启动
USBCAN.CANHelper.Instance.Initialize();

// 发生数据到CAN设备
USBCAN.CANHelper.Instance.SendData(0x200, new byte[8] { 0x1, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0, 0x0 });

// 关闭CAN设备
USBCAN.CANHelper.Instance.CloseDevice();
```


#### 较完整示例
```C#
public MainWindow()
{
    InitializeComponent();

    // 监听消费帧事件
    USBCAN.CANHelper.Instance.ConsumptionFrameEvent += Instance_ConsumptionFrameEvent;
}

private void Instance_ConsumptionFrameEvent(USBCAN.CAN_API.VCI_CAN_OBJ frame)
{
    Dispatcher.Invoke(new Action(() =>
    {
        //string.Join(",", frame.Data);
        StringBuilder sb = new StringBuilder(32);
        foreach (var item in frame.Data)
            sb.AppendFormat("{0:X} ", item);

        // 输出帧数据
        txtOutput.AppendText(string.Format("ID:{0:X}\tTimeStamp:{1}\tTimeFlag:{2}\tSendType:{3}\tRemoteFlag:{4}\tExternFlag:{5}\tDataLen:{6}\tData:{7}\tReserved:{8}\n",
            frame.ID, frame.TimeStamp, frame.TimeFlag, frame.SendType, frame.RemoteFlag, frame.ExternFlag, frame.DataLen, sb.ToString(), string.Join(",", frame.Reserved)));
        txtOutput.ScrollToEnd();
    }));
}

private void CekStart_Checked(object sender, RoutedEventArgs e)
{
    try
    {
        // 初始化设备
        USBCAN.CANHelper.Instance.Initialize();
    }
    catch (Exception ex)
    {
        // 启动发生异常，输出异常信息与CAN错误信息
        MessageBox.Show(ex.Message + USBCAN.CANHelper.Instance.ReadErrorMessage());
    }
}

private void CekStart_Unchecked(object sender, RoutedEventArgs e)
{
    // 关闭设备
    USBCAN.CANHelper.Instance.CloseDevice();
}
```

### 注意
* CANHelper内部使用生产者消费者模型，可以解决高速通信与缓速处理导致阻塞的问题，即读取数据与处理数据为不同线程执行
* 初始化与关闭可以安全的反复调用，若初始化失败，抛出的异常中含有错误的详细信息
* 可以通过`ReadErrorMessage`方法读取CAN的错误信息
* 发生数据`SendData`时参数`data`必须是长度为`8`的`byte[]`
* 代码中有详细的注释