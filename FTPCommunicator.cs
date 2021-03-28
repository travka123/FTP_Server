using System;
using System.IO;
using System.Net;
using System.Net.Sockets;

namespace FTP_Server
{
    public class FTPCommunicator
    {
        private static int FileSenderCounts = 0;

        private string username = null;
        private string password = null;
        private bool isAuthorized = false;
        private FTPReciver reciver;
        private FTPSender sender;
        private string serverDir;
        private string clientDir;
        private FileManager fileManager = null;
        private int port;

        public FTPCommunicator(Socket handler, string path)
        {
            reciver = new FTPReciver(handler);
            sender = new FTPSender(handler);

            bool dataConnectionSet = false;
            while (!dataConnectionSet)
            {
                try
                {
                    port = ++FileSenderCounts;
                    fileManager = new FileManager(new IPEndPoint(IPAddress.Parse(FTPServer.ipAddress), 10240 + port));
                }
                catch
                {
                    continue;
                }
                dataConnectionSet = true;
            }

            serverDir = path;
            clientDir = @"\";
            sender.Send(220, " hello client!");
        }

        public void Communicate()
        {
            ClientRequest clientRequest = new ClientRequest();
            while (clientRequest.type != ClientCommands.CONNECTION_CLOSED)
            {
                clientRequest = reciver.GetNext();
                if (isAuthorized)
                {
                    switch (clientRequest.type)
                    {
                        case ClientCommands.SYST:
                            SYST();
                            break;

                        case ClientCommands.FEAT:
                            FEAT();
                            break;

                        case ClientCommands.PWD:
                            PWD();
                            break;

                        case ClientCommands.TYPE:
                            TYPE(clientRequest.param);
                            break;

                        case ClientCommands.PASV:
                            PASV();
                            break;

                        case ClientCommands.LIST:
                            LIST();
                            break;

                        case ClientCommands.CWD:
                            CWD(clientRequest.param);
                            break;

                        case ClientCommands.RETR:
                            RETR(clientRequest.param);
                            break;

                        case ClientCommands.STOR:
                            STOR(clientRequest.param);
                            break;

                        case ClientCommands.CDUP:
                            CDUP();
                            break;

                        case ClientCommands.MKD:
                            MKD(clientRequest.param);
                            break;

                        case ClientCommands.OPTS:
                            OPTS(clientRequest.param);
                            break;

                        case ClientCommands.UNKNOWN:
                            sender.Send(500, " unrecognized command");
                            break;

                        default:
                            if (clientRequest.type != ClientCommands.CONNECTION_CLOSED)
                                sender.Send(500, " unexpected command");
                            break;
                    }
                }
                else
                {
                    switch (clientRequest.type)
                    {
                        case ClientCommands.USER:
                            USER(clientRequest.param);
                            break;

                        case ClientCommands.PASS:
                            sender.Send(500, " firstly use USER");
                            break;

                        default:
                            sender.Send(530, " not logged in");
                            break;
                    }
                }
            }
        }

        private void USER(string username)
        {
            this.username = username;
            isAuthorized = false;
            sender.Send(331, " user name okay, need password");

            ClientRequest clientRequest = reciver.GetNext();
            switch (clientRequest.type)
            {
                case ClientCommands.PASS:
                    PASS(clientRequest.param);
                    break;

                default:
                    sender.Send(400, " PASS should follow after USER");
                    break;
            }

        }

        private void PASS(string password)
        {
            if (username != null)
            {
                this.password = password;
                if (AccountManager.IsSutable(username, password))
                {
                    isAuthorized = true;
                    sender.Send(230, " logged in " + username);
                }
                else
                {
                    sender.Send(430, " invalid username or password");
                }
            }
            else
            {
                throw new Exception();
            }
        }

        private void SYST()
        {
            sender.Send(215, " windows 10");
        }

        private void FEAT()
        {
            sender.Send(211, "-Extensions supported:");

            //Отослать поддерживаемые команды
            sender.SendRaw("UTF8");

            sender.Send(211, " END");
        }

        private void PWD()
        {
            sender.Send(257, " \"" + clientDir.Replace('\\', '/') + "\" is current directory");
        }

        private void TYPE(string param)
        {
            if (param == "I")
            {
                fileManager.IsBinary = true;
                sender.Send(200, " type set to I");
            }
            else if (param == "A")
            {
                fileManager.IsBinary = false;
                sender.Send(200, " type set to A");
            }
            else
            {
                sender.Send(500, " mode not recognized use I / A");
            }
        }

        private void PASV()
        {
            sender.Send(227, " (" + FTPServer.ipAddress.Replace('.', ',') + ",40," + port + ")");
            fileManager.WaitForConnect();
        }

        private void LIST()
        {
            if (fileManager.IsConnected())
            {
                sender.Send(150, " here comes the directory listing");
                fileManager.SendDIR(serverDir, clientDir);
                sender.Send(226, " directory send OK");
            }
            else
            {
                sender.Send(500, " use PASV first");
            }
        }

        private void CWD(string path)
        {
            string tempClientDir = path.Replace('/', '\\');
            if (tempClientDir.Contains(".."))
            {
                sender.Send(500, " .. forbidden");
                return;
            }
            if ((tempClientDir.Length > 0) && (tempClientDir[0] != '\\'))
            {
                tempClientDir = clientDir + tempClientDir;
            }
            DirectoryInfo di = new DirectoryInfo(serverDir + tempClientDir);
            if (di.Exists)
            {
                if (tempClientDir[tempClientDir.Length - 1] != '\\')
                {
                    tempClientDir += '\\';
                }
                clientDir = tempClientDir;
                sender.Send(250, " CWD command successful");
                return;
            }
            else
            {
                sender.Send(500, " wrong path");
            }

        }

        private void RETR(string filename)
        {
            sender.Send(150, " sending file");
            fileManager.SendFile(serverDir + clientDir + filename);
            sender.Send(226, " file send OK");
        }

        private void STOR(string filename)
        {
            sender.Send(150, " ready to recive file");
            fileManager.ReciveFile(serverDir + clientDir + filename);
            sender.Send(226, " file recived OK");
        }

        private void CDUP()
        {
            if (clientDir == "\\")
            {
                sender.Send(550, " you are in the root directory");
            }
            else
            {
                clientDir = clientDir.Substring(0, clientDir.Length - 1 - Path.GetFileName(clientDir.Substring(0, clientDir.Length - 1)).Length);
                sender.Send(250, " CDUP command successful");
            }
        }

        private void MKD(string directoryName)
        {
            directoryName.Replace('/', '\\');
            if (directoryName.Length != 0)
            {
                if (directoryName[0] == '\\')
                {
                    if (Directory.Exists(serverDir + directoryName))
                    {
                        sender.Send(550, " directory already exists");
                    }
                    else
                    {
                        Directory.CreateDirectory(serverDir + directoryName);
                        sender.Send(200, " directory created");
                    }
                }
                else
                {
                    if (Directory.Exists(serverDir + clientDir + directoryName))
                    {
                        sender.Send(550, " directory already exists");
                    }
                    else
                    {
                        Directory.CreateDirectory(serverDir + clientDir + directoryName);
                        sender.Send(200, " directory created");
                    }
                }
            }
            else
            {
                sender.Send(550, " need name");
            }
        }

        private void OPTS(string param)
        {
            if (param.Contains("UTF8 ON"))
            {
                sender.Send(200, "  UTF8 ON");
            }
            else if (param.Contains("UTF8 OFF"))
            {
                sender.Send(200, "  UTF8 OFF");
            }
            else
            {
                sender.Send(500, " args not recognized");
            }
        }
    }
}
