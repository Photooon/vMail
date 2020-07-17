using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace vMail
{
    class SMTP
    {
        private Socket client;
        private IPEndPoint endPoint;
        private int status;

        public delegate void StatusUpdateHandler(int status);
        public event StatusUpdateHandler StatusUpdateEvent;         // 状态更新事件

        public bool Connected
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

        public SMTP()
        {
            client = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            status = 0;
        }

        private string EncodeWithBase64(string str)
        {
            byte[] b = Encoding.Default.GetBytes(str);
            string base64String = Convert.ToBase64String(b);     // 将b转为Base64编码的字符串
            return base64String;
        }

        private void UpdateStatus(string reply)
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

        private String SendCmd(string cmd, bool forBase64 = false, int count = 1)   // forBase64 = true, 则以Base64编码发送; count为默认接收次数
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

        public void Init(string hostName, int hostPort)                        // 构建EndPoint
        {
            IPHostEntry iPHost = System.Net.Dns.GetHostEntry(hostName);        // 获取域名的IP地址

            if (iPHost.AddressList.Length > 0)
            {
                IPAddress address = iPHost.AddressList[0];
                endPoint = new IPEndPoint(address, hostPort);
                
            }
        }

        public void Login(String mail, String key)
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

        public void SendEmail(Email email)
        {
            if (this.Connected)
            {
                /*请求发送邮件*/
                this.SendCmd(String.Format("MAIL FROM:<{0}>", email.Head.From));
                this.SendCmd(String.Format("RCPT TO:<{0}>", email.Head.To));
                this.SendCmd("DATA");

                /*发送邮件*/
                string str = MIME.ToMIMEStr(email);
                this.SendCmd(str, false, 0);

                /*发送结束*/
                this.SendCmd(".");
            }
        }

        public void Close()
        {
            if (this.Connected)
            {
                this.SendCmd("QUIT");
            }
        }

    }
}
