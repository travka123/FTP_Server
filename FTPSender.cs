using System.Net.Sockets;
using System.Text;

namespace FTP_Server
{
    public class FTPSender
    {
        private Socket handler;

        public FTPSender(Socket handler)
        {
            this.handler = handler;
        }

        public void Send(int code, string message)
        {
            Logger.Log("SERVER TO " + handler.RemoteEndPoint + ": " + code + " " + message);
            handler.Send(Encoding.UTF8.GetBytes(code + message + "\r\n"));
        }

        public void SendRaw(string message)
        {
            Logger.Log("SERVER TO " + handler.RemoteEndPoint + ": " + message);
            handler.Send(Encoding.UTF8.GetBytes(message + "\r\n"));
        }
    }
}
