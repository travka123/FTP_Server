using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FTP_Server
{
    public static class FTPServer
    {
        private static Socket listener;
        private static int commandPort = 21;
        private static ManualResetEvent ConnectionRecived = new ManualResetEvent(false);
        private static string serverDir;

        public static bool IsWorking { get; set; }

        static FTPServer()
        {
            IsWorking = false;
            IPEndPoint ipPoint = new IPEndPoint(IPAddress.Loopback, commandPort);
            listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listener.Bind(ipPoint);
        }

        public static void Start()
        {
            listener.Listen(10);
            Logger.Log("SERVER: Hello world!");
            Task.Run(BeginListen);
            IsWorking = true;
            serverDir = Directory.GetCurrentDirectory();
            if (!Directory.Exists(serverDir + @"\Server_Files"))
            {
                Directory.CreateDirectory(serverDir + @"\Server_Files");
            }
            serverDir = serverDir + @"\Server_Files";
        }

        public static void ExecuteServerCommand(string command)
        {
            if (Regex.IsMatch(command, @"^stop"))
            {
                Stop();
            }
            else if (Regex.IsMatch(command, @"^add username=\S+ pass=\S+$"))
            {
                ParseUserAddReques(command);
                Logger.Log("SERVER: User added");
            }
            else
            {
                Logger.Log("SERVER: Unknown command: " + command);
            }
        }

        public static void Stop()
        {
            IsWorking = false;
            Logger.Log("SERVER: Closing...");
            Environment.Exit(0);
        }

        public static void ParseUserAddReques(string command)
        {
            AccountManager.Add(Regex.Match(command, @"username=\S+").ToString().Substring(9),
                Regex.Match(command, @"pass=\S+").ToString().Substring(5));
        }

        private static void BeginListen()
        {
            Logger.Log("SERVER: Waiting for conections...");
            while (true)
            {
                ConnectionRecived.Reset();
                listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);
                ConnectionRecived.WaitOne();
            }
        }

        private static void AcceptCallback(IAsyncResult ar)
        {
            listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);
            ConnectionRecived.Set();
            Logger.Log("SERVER: Connected: " + handler.RemoteEndPoint);

            FTPCommunicator communicator = new FTPCommunicator(handler, serverDir);
            communicator.Communicate();

            handler.Shutdown(SocketShutdown.Both);
            Logger.Log("SERVER: Disconnected: " + handler.RemoteEndPoint);
            handler.Close();
        }
    }
}
