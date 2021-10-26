using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Bootstrapping;
using DotNetty.Transport.Channels;
using DotNetty.Transport.Channels.Sockets;

namespace NrcTcpLibrary
{
    public class ClientBase
    {
        private MessageHandler messageHandler = null;
        private ManualResetEvent closingArrivedEvent = new ManualResetEvent(false);
        public IChannel clientChannel = null;

        public ClientBase(MessageHandler msh)
        {
            messageHandler = msh;
        }

        public async Task RunClientAsync(string ip, int port)
        {
            var group = new MultithreadEventLoopGroup();
            try
            {
                Bootstrap bootstrap = new Bootstrap();
                bootstrap.Group(group).Channel<TcpSocketChannel>()
                    .Option(ChannelOption.TcpNodelay, true)
                    .Option(ChannelOption.ConnectTimeout, TimeSpan.FromSeconds(3))
                    .Handler(new ActionChannelInitializer<ISocketChannel>(channel =>
                 {
                     IChannelPipeline pipeline = channel.Pipeline;
                     pipeline.AddLast("idleStateHandle", new IdleStateHandler(2, 2, 0));
                     pipeline.AddLast("client", new ClientHandler(this.messageHandler));
                 }));
                clientChannel = await bootstrap.ConnectAsync(new IPEndPoint(IPAddress.Parse(ip), port));
                closingArrivedEvent.Reset();
                closingArrivedEvent.WaitOne();
                await clientChannel.CloseAsync();
            }
            finally
            {
                await group.ShutdownGracefullyAsync(TimeSpan.FromMilliseconds(100), TimeSpan.FromSeconds(1));
            }
        }

        public static IByteBuffer EncodeMessage(int command, byte[] data)
        {
            byte[] headerByte = new byte[] { 0x4e, 0x66 };
            int command_pre = command / 256;
            int command_aft = command % 256;
            byte[] commandByte = new byte[] { (byte)command_pre, (byte)command_aft };
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] dataByte = data;
            int dataLength = dataByte.Length;
            int dataLengthByte_pre = dataLength / 256;
            int dataLengthByte_aft = dataLength % 256;
            byte[] dataLengthByte = new byte[] { (byte)dataLengthByte_pre, (byte)dataLengthByte_aft };
            byte[] msgByte = new byte[10 + dataLength];
            headerByte.CopyTo(msgByte, 0);
            dataLengthByte.CopyTo(msgByte, 2);
            commandByte.CopyTo(msgByte, 4);
            dataByte.CopyTo(msgByte, 6);
            byte[] msgToCrc32 = msgByte.Skip(2).Take(msgByte.Length - 6).ToArray();
            uint crc32Num = Crc32.CRC(msgToCrc32);
            byte[] _crc32Byte = BitConverter.GetBytes(crc32Num);
            byte[] crc32Byte = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                crc32Byte[i] = _crc32Byte[3 - i];
            }
            crc32Byte.CopyTo(msgByte, 6 + dataLength);
            IByteBuffer messageBuffer = Unpooled.Buffer(msgByte.Length);
            messageBuffer.WriteBytes(msgByte);
            return messageBuffer;
        }

        public static IByteBuffer EncodeMessage(int command, string data)
        {
            byte[] headerByte = new byte[] { 0x4e, 0x66 };
            int command_pre = command / 256;
            int command_aft = command % 256;
            byte[] commandByte = new byte[] { (byte)command_pre, (byte)command_aft };
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] dataByte = utf8.GetBytes(data);
            int dataLength = dataByte.Length;
            int dataLengthByte_pre = dataLength / 256;
            int dataLengthByte_aft = dataLength % 256;
            byte[] dataLengthByte = new byte[] { (byte)dataLengthByte_pre, (byte)dataLengthByte_aft };
            byte[] msgByte = new byte[10 + dataLength];
            headerByte.CopyTo(msgByte, 0);
            dataLengthByte.CopyTo(msgByte, 2);
            commandByte.CopyTo(msgByte, 4);
            dataByte.CopyTo(msgByte, 6);
            byte[] msgToCrc32 = msgByte.Skip(2).Take(msgByte.Length - 6).ToArray();
            uint crc32Num = Crc32.CRC(msgToCrc32);
            byte[] _crc32Byte = BitConverter.GetBytes(crc32Num);
            byte[] crc32Byte = new byte[4];
            for (int i = 0; i < 4; i++)
            {
                crc32Byte[i] = _crc32Byte[3 - i];
            }
            crc32Byte.CopyTo(msgByte, 6 + dataLength);

            IByteBuffer messageBuffer = Unpooled.Buffer(msgByte.Length);
            messageBuffer.WriteBytes(msgByte);
            return messageBuffer;
        }

        private async Task SendMessageTask(int command, byte[] data)
        {
            if (clientChannel == null || !clientChannel.Active || !clientChannel.IsWritable)
            {
                return;
            }
            IByteBuffer buffer = EncodeMessage(command, data);
            await clientChannel.WriteAndFlushAsync(buffer);
        }

        private async Task SendMessageTask(int command, string data)
        {
            if (clientChannel == null || !clientChannel.Active || !clientChannel.IsWritable)
            {
                System.Diagnostics.Debug.WriteLine("不能发送");
                System.Diagnostics.Debug.WriteLine(clientChannel);
                return;
            }
            IByteBuffer buffer = EncodeMessage(command, data);
            System.Diagnostics.Debug.WriteLine("已经发送");
            await clientChannel.WriteAndFlushAsync(buffer);
        }

        public void SendMessage(int command, string data)
        {
            System.Diagnostics.Debug.WriteLine("begin sending task{0:x4}{1:G}", command, data);
            Task.Run(() => SendMessageTask(command, data));
        }

        public void SendMessage(int command, byte[] data)
        {
            System.Diagnostics.Debug.WriteLine("begin sending task {0:x4} {1:G}", command, data.ToString());
            Task.Run(() => SendMessageTask(command, data));
        }
    }
}