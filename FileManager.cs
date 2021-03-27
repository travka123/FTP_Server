using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace FTP_Server
{
    class FileManager
    {
        private Socket listner;
        private Socket handler = null;

        public bool IsBinary { get; set; }

        public FileManager(IPEndPoint endPoint)
        {
            listner = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            listner.Bind(endPoint);
            listner.Listen(1);
        }

        public void WaitForConnect()
        {
            handler = listner.Accept();
        }

        public void SendDIR(string serverDir, string clientDir)
        {
            string[] files = Directory.GetFiles(serverDir + clientDir);
            string[] directories = Directory.GetDirectories(serverDir + clientDir);

            foreach (string dir in directories)
            {
                DirectoryInfo di = new DirectoryInfo(dir);
                handler.Send(Encoding.ASCII.GetBytes(di.LastWriteTime.ToShortDateString() + " " + di.LastWriteTime.ToShortTimeString() +
                    "    <DIR> " + di.Name + "\r\n"));
            }

            foreach (string file in files)
            {
                FileInfo fi = new FileInfo(file);
                handler.Send(Encoding.ASCII.GetBytes(fi.LastWriteTime.ToShortDateString() + " " + fi.LastWriteTime.ToShortTimeString() +
                    " " +fi.Length + " " + fi.Name + "\r\n"));
            }
            //handler.Send(Encoding.ASCII.GetBytes("19.02.2021  13:44    <DIR>          Links\r\n"))

            handler.Shutdown(SocketShutdown.Send);
            handler.Close();
            handler = null;
        }

        public bool IsConnected()
        {
            return handler != null;
        }

        public void SendFile(string path)
        {
            if (IsBinary)
            {
                using (BinaryReader reader = new BinaryReader(File.Open(path, FileMode.Open)))
                {
                    byte[] buff;
                    do
                    {
                        buff = reader.ReadBytes(256);
                        if (buff.Length > 0)
                        {
                            handler.Send(buff);
                        }
                    } while (buff.Length == 256);
                }
            }
            else
            {
                using (StreamReader sr = new StreamReader(path))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        handler.Send(Encoding.ASCII.GetBytes(line));
                    }
                }
            }
            handler.Shutdown(SocketShutdown.Send);
            handler.Close();
            handler = null;
        }

        public void ReciveFile(string path)
        {
            byte[] buff = new byte[256];
            int bytes;
            if (IsBinary)
            {
                using (BinaryWriter writer = new BinaryWriter(File.Open(path, FileMode.OpenOrCreate)))
                {
                    do
                    {
                        bytes = handler.Receive(buff);
                        writer.Write(buff, 0, bytes);
                    } while (bytes != 0);
                }
            }
            else
            {
                using (StreamWriter sw = new StreamWriter(path, false, System.Text.Encoding.ASCII))
                {
                    do
                    {
                        bytes = handler.Receive(buff);
                        sw.Write(Encoding.ASCII.GetString(buff, 0, bytes));
                    } while (bytes != 0);
                }
            }
            handler.Shutdown(SocketShutdown.Send);
            handler.Close();
            handler = null;
        }
    }
}
