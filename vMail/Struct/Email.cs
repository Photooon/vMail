using System;
using System.Collections.Generic;
using System.Text;

namespace vMail
{
    /* 内容类型 */
    enum Content_Type
    {
        Text_HTML,          // text/html
        Text_Plain,         // text/plain
        Multi_Alter,        // multipart/alternative
        Multi_Mixed,        // multipart/mixed
        Multi_Related,      // multipart/related
        Appli_Pdf,          // application/pdf
        Image_png,          // image/png
        Undefined           // 不支持的类型
    }

    /* 编码类型 */
    enum Transfer_Encoding
    {
        Base64,             // Base-64编码
        Quoted_Printable,   // 可打印字符编码
        Bit8,               // 8-Bit编码
        Bit7,               // 7-Bit编码
        Undefined,          // 不支持的类型
    }

    /* 附件展示方式 */
    enum Content_Disposition
    {
        Attachment,         // 附在尾部
        Inline,             // 在邮件内显示
    }

    /* 信头 */
    class Heading
    {
        private const int MaxFromStrLength = 18;
        private const int MaxSubjectStrLength = 30;

        public int Id { get; set; }
        public String Subject { get; set; }
        public DateTime Date { get; set; }
        public String From { get; set; }
        public String To { get; set; }
        public bool IsSelected { get; set; }

        public String DateStr
        {
            get
            {
                return Date.ToString("MM-dd");
            }
        }

        public Heading()
        {
            Id = -1;
            Subject = "";
            Date = DateTime.Today;
            From = "";
            To = "";
            IsSelected = false;
        }

        /* 修剪过长字段 */
        public Heading Trim()
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
        public Content_Type ContentType { get; set; }
        public Transfer_Encoding EncodeType { get; set; }

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

        public Body()
        {
            ContentType = Content_Type.Text_HTML;
            EncodeType = Transfer_Encoding.Base64;
            Boundary = "--vMail";
            Charset = Encoding.UTF8;
            Name = "";
            Disposition = Content_Disposition.Attachment;
            Data = new byte[1];
            SubBodies = new List<Body>();
        }

        public string DataStr
        {
            get
            {
                string str = "";

                if (IsMulti)
                {
                    foreach (Body body in SubBodies)
                    {
                        str += body.DataStr;
                    }
                }
                else
                {
                    str += Charset.GetString(Data);
                }

                return str;
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
