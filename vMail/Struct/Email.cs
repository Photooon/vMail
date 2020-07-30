using System;
using System.Collections.Generic;
using System.Text;

namespace vMail
{
    /* 信头 */
    class Heading
    {
        private const int MaxFromStrLength = 18;            // From字段的最大显示长度
        private const int MaxSubjectStrLength = 30;         // Subject字段的最大显示长度

        public int Id { get; set; }                         // 邮件的ID
        public String Subject { get; set; }                 // 邮件的Subject
        public DateTime Date { get; set; }                  // 邮件的Date
        public String From { get; set; }                    // 邮件的From
        public String To { get; set; }                      // 邮件的To
        public bool IsSelected { get; set; }                // 邮件是否被选中

        public String DateStr                               // 字符串格式的日期
        {
            get
            {
                return Date.ToString("MM-dd");
            }
        }

        public Heading()                                    // 默认构造函数
        {
            Id = -1;
            Subject = "";
            Date = DateTime.Today;
            From = "";
            To = "";
            IsSelected = false;
        }

        public Heading Trim()                               // 对字段进行裁剪，以便显示
        {
            /* 修减From */
            string[] tmp = From.Split('\t');
            if (tmp.Length == 2 && tmp[0].Length > MaxFromStrLength)
            {
                tmp[0] = tmp[0].Substring(0, 15);
                tmp[0] += "...";
                From = tmp[0] + "   " + tmp[1];
            }

            /* 修剪Subject */
            if (Subject.Length > MaxSubjectStrLength)
            {
                Subject = Subject.Substring(0, 30);
                Subject += "...";
            }

            return this;
        }
    }

    /* 信体 */
    class Body
    {
        /* 固有的属性 */
        public Content_Type ContentType { get; set; }       // Content-Type字段类型
        public Transfer_Encoding EncodeType { get; set; }   // Content-Transfer-Encoding字段类型

        /* 一些可能会有的属性，依据ContentType而定 */
        public String Boundary { get; set; }
        public Encoding Charset { get; set; }
        public String Name { get; set; }
        public Content_Disposition Disposition { get; set; }
        
        /* 固有的属性 */
        public byte[] Data { get; set; }                    // 所有格式的Data都统一用字节数组表示
        public List<Body> SubBodies { get; set; }           // 子信体

        public bool IsMulti                                 // 判断是否为集合类型
        {
            get
            {
                if (ContentType == Content_Type.Multi_Alter || ContentType == Content_Type.Multi_Mixed 
                    || ContentType == Content_Type.Multi_Related)
                {
                    return true;
                }
                else
                {
                    return false;
                }
            }
        }

        public bool IsAttachment                            // 是否为附件类型
        { 
            get
            {
                if (ContentType == Content_Type.Appli_Pdf)
                {
                    return true;
                }

                return false;
            }
        }

        public Body()                                       // 默认构造函数
        {
            ContentType = Content_Type.Text_HTML;
            EncodeType = Transfer_Encoding.Base64;
            Boundary = "----=_Part_908473_394990143.1594909720513";
            Charset = Encoding.UTF8;
            Name = "";
            Disposition = Content_Disposition.Attachment;
            Data = new byte[1];
            SubBodies = new List<Body>();
        }

        public string Text                                  // 获取所有文本
        {
            get
            {
                string str = "";

                if (IsMulti)
                {
                    foreach (Body body in SubBodies)
                    {
                        str += body.Text;
                    }
                }
                else if (ContentType == Content_Type.Text_HTML || ContentType == Content_Type.Text_Plain)    // 文本类型
                {
                    str += Charset.GetString(Data);
                }

                return str;
            }
        }

        public List<Attachment> Attachments                 // 获取所有附件
        {
            get
            {
                List<Attachment> attachments = new List<Attachment>();

                if (IsMulti)
                {
                    foreach (Body body in SubBodies)
                    {
                        attachments.AddRange(body.Attachments);
                    }
                }
                else if (ContentType == Content_Type.Appli_Pdf || ContentType == Content_Type.Image_png)    // 附件类型
                {
                    Attachment attachment = new Attachment();
                    attachment.Name = Name;
                    attachment.Data = Data;
                    attachments.Add(attachment);
                }

                return attachments;
            }
        }
    }

    /* 附件 */
    class Attachment
    {
        public String Name { get; set; }        // 附件名
        public byte[] Data { get; set; }        // 附件体

        public String ImageName                 // 用户界面显示的小图标图片资源名
        {
            get
            {
                string type = Name.Split('.')[1].ToLower();
                if (type == "pdf")
                {
                    return "assets/pdf.png";
                }
                else if (type == "png")
                {
                    return "assets/image.png";
                }
                else
                {
                    return "assets/logo.png";
                }
            }
        }

        public Content_Type ContentType         // 附件的类型
        {
            get
            {
                string type = Name.Split('.')[1].ToLower();
                if (type == "pdf")
                {
                    return Content_Type.Appli_Pdf;
                }
                else
                {
                    return Content_Type.Appli_Pdf;
                }
            }
        }
    }

    /* 信件 */
    class Email
    {
        public Heading Head { get; set; }
        public Body Content { get; set; }

        public Email()
        {
            this.Head = new Heading();
            this.Content = new Body();
        }
    }
}
