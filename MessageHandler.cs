namespace NrcTcpLibrary
{
    public class MessageHandler
    {
        public virtual void Handler(Message message)
        {
        }

        public virtual void ConnectState(bool state)
        {
        }
    }
}