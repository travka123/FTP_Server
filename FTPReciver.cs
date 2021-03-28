using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;

namespace FTP_Server
{
    public enum ClientCommands { UNKNOWN, CONNECTION_CLOSED, USER, PASS, SYST, FEAT, PWD, TYPE, PASV, LIST, CWD, RETR, STOR, CDUP, MKD, OPTS }

    public struct ClientRequest
    {
        public ClientCommands type;
        public string param;
    }

    public class FTPReciver
    {
        private StringBuilder commandBuffer = new StringBuilder();
        private Socket handler;

        public FTPReciver(Socket handler)
        {
            this.handler = handler;
        }

        public ClientRequest GetNext()
        {
            ClientRequest clientRequest = new ClientRequest();
            string request = GetNextCommandString();
            Logger.Log(handler.RemoteEndPoint + " TO SERVER: " + request);
            if (request == null)
            {
                clientRequest.type = ClientCommands.CONNECTION_CLOSED;
            }
            else
            {
                request = request.Trim();

                if (Regex.IsMatch(request, @"^USER .*"))
                {
                    clientRequest.type = ClientCommands.USER;
                    clientRequest.param = request.Substring(5);
                }
                else if (Regex.IsMatch(request, @"^PASS .*"))
                {
                    clientRequest.type = ClientCommands.PASS;
                    clientRequest.param = request.Substring(5);
                }
                else if (Regex.IsMatch(request, @"^SYST"))
                {
                    clientRequest.type = ClientCommands.SYST;
                }
                else if (Regex.IsMatch(request, @"^FEAT"))
                {
                    clientRequest.type = ClientCommands.FEAT;
                }
                else if (Regex.IsMatch(request, @"^PWD"))
                {
                    clientRequest.type = ClientCommands.PWD;
                }
                else if (Regex.IsMatch(request, @"^TYPE .*"))
                {
                    clientRequest.type = ClientCommands.TYPE;
                    clientRequest.param = request.Substring(5);
                }
                else if (Regex.IsMatch(request, @"^PASV"))
                {
                    clientRequest.type = ClientCommands.PASV;
                }
                else if (Regex.IsMatch(request, @"^LIST"))
                {
                    clientRequest.type = ClientCommands.LIST;
                }
                else if (Regex.IsMatch(request, @"^CWD .*"))
                {
                    clientRequest.type = ClientCommands.CWD;
                    clientRequest.param = request.Substring(4);
                }
                else if (Regex.IsMatch(request, @"^RETR .*"))
                {
                    clientRequest.type = ClientCommands.RETR;
                    clientRequest.param = request.Substring(5);
                }
                else if (Regex.IsMatch(request, @"^STOR .*"))
                {
                    clientRequest.type = ClientCommands.STOR;
                    clientRequest.param = request.Substring(5);
                }
                else if (Regex.IsMatch(request, @"^CDUP"))
                {
                    clientRequest.type = ClientCommands.CDUP;
                }
                else if (Regex.IsMatch(request, @"^MKD .*"))
                {
                    clientRequest.type = ClientCommands.MKD;
                    clientRequest.param = request.Substring(4);
                }
                else if (Regex.IsMatch(request, @"^OPTS .*"))
                {
                    clientRequest.type = ClientCommands.OPTS;
                    clientRequest.param = request.Substring(5);
                }
                else
                {
                    clientRequest.type = ClientCommands.UNKNOWN;
                }
            }
            return clientRequest;
        }

        private string GetNextCommandString()
        {
            while (true)
            {
                string commandBufferString = commandBuffer.ToString();
                Match command = Regex.Match(commandBufferString, @".*\r\n");
                if (command.Success)
                {
                    commandBuffer.Remove(0, command.Value.Length);
                    return command.Value.Substring(0, command.Value.Length - 2);
                }

                byte[] buffer = new byte[256];
                int bytes = handler.Receive(buffer);
                if (bytes == 0)
                {
                    return null;
                }
                string text = Encoding.UTF8.GetString(buffer);
                text = text.Substring(0, text.IndexOf('\0'));
                commandBuffer.Append(text);
            }
        }
    }
}
