# NrcTcpLibrary

## ABOUT

用于向 Nrc 系列机器人控制器收发数据的.NET 库。

.NET CORE 3.1 / C# / DotNetty

## 编译

生成解决方案-生成 NrcTcpLibrary

## 使用

```csharp
using NrcTcpLibrary

namespace YourProject
{
    class Client
    {
        private readonly static object lockObj = new object();
        private static Client instance = null;
        public static Client GetInstance()
        {
            if (instance == null)
            {
                lock (lockObj)
                {
                    if (instance == null)
                    {
                        instance = new Client();
                    }
                }
            }
            return instance;
        }
        public ClientBase clientBase = ClientBase.GetInstance(new HandleReceiveMessage());
        public void Connect(string ip,int port)
        {
            Task.Run(() => clientBase.RunClientAsync(ip, port));
        }
        public void SendMessage(int command,string data)
        {
            clientBase.SendMessage(command, data);
        }
        public void SendMessage(int command,byte[] data)
        {
            clientBase.SendMessage(command, data);
        }
    }

    public class HandleReceiveMessage:MessageHandler
    {
        public HandleReceiveMessage()
        {
        }
        public override void Handler(Message message)
        {
            int command = message.command;
            string data = message.data;
            doSomethine(command,data)
        }
    }

    public partial class MainWindow
    {
        Tcp.Client client = Tcp.Client.GetInstance();
        public MainWindow()
        {
            InitializeComponent();
            DataContext = Model.Versions.GetInstance;
            System.Diagnostics.Trace.WriteLine("软件打开");
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            client.Connect("ip地址", 6000);
        }

        private void Button1_Click_1(object sender, RoutedEventArgs e)
        {
            client.SendMessage(0x5565,"sth");
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            if (client.clientBase.clientChannel != null && client.clientBase.clientChannel.Active)
            {
                client.clientBase.clientChannel.CloseAsync();
            }
            base.OnClosing(e);
        }
    }
}
```
