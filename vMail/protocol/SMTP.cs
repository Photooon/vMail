using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace vMail
{
    class SMTP
    {
        private Socket client;                                      // Socket
        private IPEndPoint endPoint;                                // 服务器终端
        private int status;                                         // 响应码

        public delegate void StatusUpdateHandler(int status);       // 状态更新处理函数
        public event StatusUpdateHandler StatusUpdateEvent;         // 状态更新事件

        public bool Connected                                       // 当前与服务器的连接状态
        {
            get
            {
                return client.Connected;
            }
        }

        public int Status
        {
            get
            {
                return status;
            }
        }

        public SMTP()                                               // 默认构造函数
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            status = 0;
        }

        public void UpdateStatus(string reply)                      // 更新状态码
        {
            if (reply.Length < 3)
            {
                status = 0;                                 // 没有回复时状态置为0
            }
            else
            {
                status = Convert.ToInt32(reply.Substring(0, 3));
                StatusUpdateEvent(Status);
            }
        }

        public void Init(string hostName, int hostPort)             // 初始化套接字
        {
            IPHostEntry iPHost = System.Net.Dns.GetHostEntry(hostName);        // 获取域名的IP地址

            if (iPHost.AddressList.Length > 0)
            {
                IPAddress address = iPHost.AddressList[0];
                endPoint = new IPEndPoint(address, hostPort);                  // 构建EndPoint

            }
        }

        public void Login(String mail, String key)                  // 登录邮箱
        {
            if (endPoint != null)
            {
                client.Connect(endPoint);

                SendCmd("EHLO vMail", false, 2);      // 标名用户身份
                SendCmd("AUTH LOGIN");                //认证登录
                SendCmd(mail, true);                  // 发送邮箱账号(开启Base64编码)
                SendCmd(key, true);                   // 发送密钥(开启Base64编码)  
            }
        }

        public String SendCmd(string cmd, bool forBase64 = false, int count = 1)   // 发送指定指令并接收回复。其中forBase64 = true, 则以Base64编码发送; count为默认接收次数
        {
            if (this.Connected)
            {
                if (!forBase64)
                {
                    byte[] buff = Encoding.Default.GetBytes(cmd + "\r\n");
                    client.Send(buff);
                }
                else
                {
                    byte[] buff = Encoding.Default.GetBytes(cmd);
                    string base64String = Convert.ToBase64String(buff);          // 将buff转为Base64编码的字符串
                    buff = Encoding.Default.GetBytes(base64String + "\r\n");     // 再转为byte数组
                    client.Send(buff);
                }

                /* 接受回复 */
                string reply = "";
                if (this.Connected)
                {
                    for (int i = 0; i < count; i++)
                    {
                        byte[] recvBuff = new byte[65536];
                        int recvCount = client.Receive(recvBuff);                    // 获取收到的字节数
                        reply += Encoding.Default.GetString(recvBuff, 0, recvCount);       // 获取服务器回复
                        reply += "\n";

                        if (i == 0)
                        {
                            UpdateStatus(reply);           // 更新状态
                        }
                    }
                }

                return reply;
            }

            return "";
        }

        public void SendEmail(Email email)                          // 发送邮件
        {
            if (this.Connected)
            {
                /*请求发送邮件*/
                this.SendCmd(String.Format("MAIL FROM:<{0}>", email.Head.From));
                this.SendCmd(String.Format("RCPT TO:<{0}>", email.Head.To));
                this.SendCmd("DATA");

                /*发送邮件*/
                string str = MIME.GetMIMEStr(email);
                this.SendCmd(str, false, 0);

                /*发送结束*/
                this.SendCmd(".");
            }
        }

        public void Close()                                         // 关闭与服务器的连接
        {
            if (this.Connected)
            {
                this.SendCmd("QUIT");
            }
        }
    }
}
