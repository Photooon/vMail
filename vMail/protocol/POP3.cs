using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace vMail
{
    class POP3
    {
        private Socket client;                          // Socket
        private IPEndPoint endPoint;                    // 服务器终端

        public bool Status { get; set; }                // 指令发送的状态，成功为true，失败为false
        
        public bool Connected                           // 当前与服务器的连接状态
        {
            get
            {
                return client.Connected;
            }
        }

        public void Init(string hostName, int hostPort) // 构建EndPoint
        {
            IPHostEntry iPHost = System.Net.Dns.GetHostEntry(hostName);        // 获取域名的IP地址
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            if (iPHost.AddressList.Length > 0)
            {
                IPAddress address = iPHost.AddressList[0];
                endPoint = new IPEndPoint(address, hostPort);

            }
        }

        public void Login(String mail, String key)      // 登录
        {
            if (endPoint != null)
            {
                client.Connect(endPoint);
                byte[] recvBuff = new byte[65536];
                client.Receive(recvBuff);                 // 接收说明信息

                SendCmd("USER " + mail);                  // 发送邮箱账号
                SendCmd("PASS " + key);                   // 发送密钥 
            }
        }

        public String SendCmd(string cmd, bool longContent = false)    // 发送指令。其中longContent表示接收较长内容，启动自动判断结束机制
        {
            if (this.Connected)
            {
                byte[] buff = Encoding.Default.GetBytes(cmd + "\r\n");
                client.Send(buff);

                /* 接受回复 */
                string reply = "";

                if (longContent)
                {
                    while(true)
                    {
                        byte[] recvBuff = new byte[65536];

                        int recvCount = client.Receive(recvBuff);                          // 获取收到的字节数
                        reply += Encoding.Default.GetString(recvBuff, 0, recvCount);       // 获取服务器回复

                        if (reply.Contains("OK"))
                        {
                            Status = true;
                        }
                        else if (reply.Contains("ERR"))
                        {
                            Status = false;
                            reply = "";
                            break;
                        }

                        if (reply.EndsWith("\r\n.\r\n"))        // 遇到结束标记才中止接收
                        {
                            int firstIndex = reply.IndexOf("\r\n");         // 删除开头第一行
                            reply = reply.Remove(0, firstIndex + 2);
                            int lastIndex = reply.LastIndexOf(".\r\n");     // 删除结尾一行，均为标记
                            reply = reply.Remove(lastIndex, reply.Length - lastIndex);
                            break;
                        }
                    }
                }
                else
                {
                    byte[] recvBuff = new byte[65536];

                    int recvCount = client.Receive(recvBuff);                          // 获取收到的字节数
                    reply += Encoding.Default.GetString(recvBuff, 0, recvCount);       // 获取服务器回复

                    if (reply.Contains("OK"))
                    {
                        Status = true;                  // 更新状态
                    }
                    else if (reply.Contains("ERR"))
                    {
                        Status = false;
                        reply = "";
                    }
                }

                return reply;
            }

            return "";
        }

        public (int, int) Stat()                        // 获取统计信息(STAT指令)
        {
            if (Connected)
            {
                String[] replies = SendCmd("STAT").Split(' ');
                if (replies.Length == 3)
                {
                    return (Convert.ToInt32(replies[1]), Convert.ToInt32(replies[2]));
                }
            }

            return (0, 0);
        }

        public Heading GetHeading(int index)            // 获取摘要(TOP指令)
        {
            if (Connected)
            {
                String RawEmail = SendCmd("TOP " + index + " 0", true);

                if (RawEmail.Contains("deleted") || RawEmail.Contains("not exists") || RawEmail == "")       // 若邮件已经被删除，则不处理
                {
                    return null;
                }

                return MIME.GetHeading(RawEmail, index);
            }

            return null;
        }

        public Email GetEmail(int index)                // 获取某封邮件(RETR指令)
        {
            if (Connected)
            {
                String RawEmail = SendCmd("RETR " + index, true);

                if (RawEmail.Contains("deleted") || RawEmail.Contains("not exists"))       // 若邮件已经被删除，则不处理
                {
                    return null;
                }

                return MIME.GetEmail(RawEmail, index);
            }

            return null;
        }

        public void Delete(List<int> indexes)           // 删除某封邮件(DELE指令)
        {
            indexes.Sort((x, y) => -x.CompareTo(y));    // 降序排序

            foreach (var index in indexes)
            {
                SendCmd("DELE " + index);
            }
        }

        public void Close()                             // 关闭与服务器的连接
        {
            client.Close();
        }
    }
}
