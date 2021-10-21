using System;
using System.Collections.Generic;
using System.Text;
using DotNetty.Buffers;
using DotNetty.Handlers.Timeout;
using DotNetty.Transport.Channels;

namespace NrcTcpLibrary
{
    public class ClientHandler : ChannelHandlerAdapter
    {
        private List<byte> recivedBytes = new List<byte>();
        private MessageHandler messageHandler = null;

        public ClientHandler(MessageHandler msh)
        {
            this.messageHandler = msh;
        }

        public override void ChannelActive(IChannelHandlerContext context)
        {
            base.ChannelActive(context);
            this.messageHandler.ConnectState(true);
        }

        public override void ChannelInactive(IChannelHandlerContext context)
        {
            base.ChannelInactive(context);
            this.messageHandler.ConnectState(false);
        }

        public override void ChannelRead(IChannelHandlerContext context, object message)
        {
            var byteBuffer = message as IByteBuffer;
            if (byteBuffer != null)
            {
                byte[] byteArray = new byte[byteBuffer.ReadableBytes];
                byteBuffer.GetBytes(byteBuffer.ReaderIndex, byteArray);
                recivedBytes.AddRange(byteArray);
            }
        }

        public override void ChannelReadComplete(IChannelHandlerContext context)
        {
            Handle();
        }

        public override void UserEventTriggered(IChannelHandlerContext context, object evt)
        {
            base.UserEventTriggered(context, evt);
            if (evt is IdleStateEvent)
            {
                var e = evt as IdleStateEvent;
                switch (e.State)
                {
                    case IdleState.ReaderIdle:
                        {
                            if (!context.Channel.Active)
                            {
                                return;
                            }
                        }
                        break;

                    case IdleState.WriterIdle:
                        {
                            TimeSpan ts = DateTime.Now - new DateTime(1970, 1, 1, 0, 0, 0, 0);
                            string tsString = Convert.ToInt64(ts.TotalMilliseconds).ToString();
                            string jsonString = "{\"time\":" + tsString + "}";
                            IByteBuffer buffer = ClientBase.EncodeMessage(0x7266, jsonString);
                            context.WriteAndFlushAsync(buffer);
                        }
                        break;

                    default:
                        break;
                }
            }
        }

        public override void ExceptionCaught(IChannelHandlerContext context, Exception exception)
        {
            Console.WriteLine("Exception: " + exception);
            this.messageHandler.ConnectState(false);
            context.CloseAsync();
        }

        private void Handle()
        {
            if (recivedBytes.Count < 10) { return; }
            int fIndex = recivedBytes.FindIndex(v => v == 0x4E);
            if (fIndex == -1) { recivedBytes.Clear(); return; }
            if (recivedBytes[fIndex + 1] != 0x66) { return; }
            int dataLength = recivedBytes[fIndex + 2] * 256 + recivedBytes[fIndex + 3];
            if (recivedBytes.Count < 10 + dataLength) { return; }
            int commandInt = recivedBytes[fIndex + 4] * 256 + recivedBytes[fIndex + 5];
            byte[] dataBytes = new byte[dataLength];
            recivedBytes.CopyTo(fIndex + 6, dataBytes, 0, dataLength);
            Message message = new Message();
            message.data = Encoding.UTF8.GetString(dataBytes);
            message.command = commandInt;
            this.messageHandler.Handler(message);
            recivedBytes.RemoveRange(0, fIndex + 10 + dataLength);
            Handle();
        }
    }
}