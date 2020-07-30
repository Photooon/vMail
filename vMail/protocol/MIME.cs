using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace vMail
{
    class MIME
    {
        private static string EncodeField(string str)                   // 将字段值编码(含非ASCII码时)
        {
            string utf8Str = MyEncoder.EncodeWithUTF8(str);
            string base64Str = MyEncoder.EncodeWithBase64(utf8Str);

            string encodedFieldStr = String.Format("=?UTF-8?B?{0}?=", base64Str);
            return encodedFieldStr;
        }

        private static string DecodeField(string str)                   // 将字段值解码(含非ASCII码时)
        {
            string decodedField = "";

            string[] fields = str.Split('?');       // 会分成5段，第2段为字符编码，第3段为字节编码，第4段为内容

            if (fields.Length == 5)
            {
                string encodeType1 = fields[1];     // 字符编码
                string encodeType2 = fields[2];

                if (encodeType2 == "B")             // 如果是Base64编码
                {
                    decodedField += MyEncoder.DecodeWithBase64(fields[3], Encoding.GetEncoding(encodeType1));
                    // 先转为字节数组，再以GBK解码
                }
                else if (encodeType2 == "Q")        // 如果是Quoted-printable编码
                {
                    decodedField += MyEncoder.DecodeWithQP(fields[3], Encoding.GetEncoding(encodeType1));
                }
                else                                // 暂时不处理其他类型的编码
                {
                    decodedField += (fields[3] + " ");
                }
            }
            else                                    // 不带编码，纯英文
            {
                decodedField = str + " ";
            }

            return decodedField;
        }

        private static string GetContentTypeStr(Content_Type type)      // 将Content-Type转为字符串
        {
            switch (type)
            {
                case Content_Type.Text_HTML:
                    return "text/html";
                case Content_Type.Text_Plain:
                    return "text/plain";
                case Content_Type.Multi_Alter:
                    return "multipart/alternative";
                case Content_Type.Multi_Mixed:
                    return "multipart/mixed";
                case Content_Type.Multi_Related:
                    return "multipart/related";
                case Content_Type.Appli_Pdf:
                    return "application/pdf";
                case Content_Type.Image_png:
                    return "image/png";
                default:
                    return "application/octet-stream";
            }
        }

        private static Content_Type GetContentType(string input)        // 将字符串转为Content-Type
        {
            input = input.ToLower();
            if (input == "text/plain")
            {
                return Content_Type.Text_Plain;
            }
            else if (input == "text/html")
            {
                return Content_Type.Text_HTML;
            }
            else if (input == "multipart/mixed")
            {
                return Content_Type.Multi_Mixed;
            }
            else if (input == "multipart/related")
            {
                return Content_Type.Multi_Related;
            }
            else if (input == "multipart/alternative")
            {
                return Content_Type.Multi_Alter;
            }
            else if (input == "application/pdf")
            {
                return Content_Type.Appli_Pdf;
            }
            else if (input == "image/png")
            {
                return Content_Type.Image_png;
            }
            else
            {
                return Content_Type.Undefined;
            }
        }

        private static Transfer_Encoding GetEncodingType(string input)  // 将字符串转为Encode-Type
        {
            if (input.ToUpper() == "BASE64")
            {
                return Transfer_Encoding.Base64;
            }
            else if (input.ToUpper() == "7BIT")
            {
                return Transfer_Encoding.Bit7;
            }
            else if (input.ToUpper() == "8BIT")
            {
                return Transfer_Encoding.Bit8;
            }
            else if (input.ToUpper() == "QUOTED-PRINTABLE")
            {
                return Transfer_Encoding.Quoted_Printable;
            }
            else
            {
                return Transfer_Encoding.Undefined;
            }
        }

        private static string GetBoundary(string input)                 // 捕获Boundary属性
        {
            string boundaryPattern = "boundary=[ ]*([^;]*)[ ]*(;|\r)";
            Match bmatch = Regex.Match(input, boundaryPattern);
            if (bmatch.Success)
            {
                return bmatch.Groups[1].Value.Trim().Trim('"');
            }
            else
            {
                return "--vMail";
            }
        }

        private static Encoding GetCharset(string input)                // 捕获Charset属性
        {
            string charsetPattern = "charset=[ ]*(.*)[ ]*(\r)";
            Match cmatch = Regex.Match(input, charsetPattern);
            if (cmatch.Success)
            {
                return Encoding.GetEncoding(cmatch.Groups[1].Value.Trim().Trim('"'));
            }
            else
            {
                return Encoding.UTF8;
            }
        }

        private static string GetName(string input)                     // 捕获Name属性
        {
            string charsetPattern = "name=[ ]*(.*)[ ]*(\r)";
            Match cmatch = Regex.Match(input, charsetPattern);
            if (cmatch.Success)
            {
                return cmatch.Groups[1].Value.Trim().Trim('"');
            }
            else
            {
                return "";
            }
        }

        private static Content_Disposition GetDisposition(string input) // 捕获Disposition属性
        {
            string disPattern = "(\r\n|\n)Content-Disposition:[ ]*(.*)[ ]*;.*(\r)";
            Match dismatch = Regex.Match(input, disPattern);
            if (dismatch.Success)
            {
                if (dismatch.Groups[2].Value.Trim().Trim('"').ToLower() == "attachment")
                {
                    return Content_Disposition.Attachment;
                }
                else
                {
                    return Content_Disposition.Inline;
                }
            }
            else
            {
                return Content_Disposition.Attachment;
            }
        }

        private static string HeadingToStr(Heading heading)             // 将信头转为字符串
        {
            string headStr = "";

            /* 构建邮件头 */
            // From
            headStr += String.Format("From: <{0}>\r\n", heading.From);

            // To
            headStr += String.Format("To: {0}\r\n", heading.To);

            // Subject
            headStr += String.Format("Subject: {0}\r\n", EncodeField(heading.Subject));

            // Date
            headStr += String.Format("Date: {0}\r\n", heading.Date.ToString("r"));

            // MIME-Version
            headStr += String.Format("Mime-Version: 1.0\r\n");

            return headStr;
        }

        private static string BodyToStr(Body body)                      // 将信体转为字符串
        {
            string bodyStr = "";

            // Content-Type
            if (body.IsMulti)
            {
                bodyStr += String.Format("Content-Type: {0};\r\n\tboundary=\"{1}\"\r\n", GetContentTypeStr(body.ContentType), body.Boundary);
            }
            else if (body.IsAttachment)
            {
                bodyStr += String.Format("Content-Type: {0};\r\n\tname=\"{1}\"\r\n", GetContentTypeStr(body.ContentType), body.Name);
            }
            else
            {
                bodyStr += String.Format("Content-Type: {0};\r\n\tcharset=\"{1}\"\r\n", GetContentTypeStr(body.ContentType), "UTF-8");
            }

            // Content-Transfer-Encoding
            if (!body.IsMulti)
            {
                bodyStr += String.Format("Content-Transfer-Encoding: base64\r\n");
            }

            // Other Attributes
            if (body.IsAttachment)
            {
                bodyStr += String.Format("Content-Disposition: attachment; filename=\"{0}\"\r\n", body.Name);
            }

            /* 构建邮件体 */
            if (body.IsMulti)
            {
                foreach (Body subBody in body.SubBodies)
                {
                    bodyStr += String.Format("\r\n--{0}\r\n", body.Boundary);       // 添加起头
                    bodyStr += BodyToStr(subBody);
                }
                bodyStr += String.Format("\r\n--{0}--\r\n", body.Boundary);         // 添加结尾
            }
            else
            {
                string base64Data = MyEncoder.EncodeWithBase64(body.Data);          // 编码为base64
                string dstStr = "";
                dstStr += "\r\n";
                for (int i = 0; i < base64Data.Length; i += 75)                     // 保证每行不超过80字符
                {
                    if (i + 75 < base64Data.Length)
                    {
                        dstStr += base64Data.Substring(i, 75) + "\r\n";
                    }
                    else
                    {
                        dstStr += base64Data.Substring(i, base64Data.Length - i) + "\r\n";
                    }
                }
                dstStr += "\r\n";
                bodyStr += dstStr;
            }

            return bodyStr;
        }

        /* 将字符串转为信头结构 */
        public static Heading GetHeading(String rawStr, int index)
        {
            string raw = rawStr.Replace("\r\n ", " ").Replace("\r\n\t", " ");          // 展开折叠的字段

            Heading heading = new Heading();

            /* From处理*/
            Match match = Regex.Match(raw, "(\r\n|\n)?From:(.*)(<.*@.*>)(\r\n|\n)");            //子表达式2是发件人名称，子表达式3是发件人邮箱，From可能是起始行，所以\r\n加了?
            if (match.Success)                                                                  // 附上了发件人时
            {
                heading.From = DecodeField(match.Groups[2].Value.Trim().Trim('"')) + "   ";         // 发件人
                heading.From += DecodeField(match.Groups[3].Value.Trim(new char[] { '<', '>' }));  // 发件邮箱
            }
            else                                                                                // 没有附上发件人时
            {
                match = Regex.Match(raw, "(\r\n|\n)?From:(.*@.*)(\r\n|\n)");

                if (match.Success)
                {
                    heading.From = DecodeField(match.Groups[2].Value.Trim());
                }
            }

            /* To处理*/
            match = Regex.Match(raw, "(\r\n|\n)To:(.*)(<.*@.*>)(\r\n|\n)");    // 同上
            if (match.Success)                                                                  // 附上了收件人时
            {
                heading.To = DecodeField(match.Groups[2].Value.Trim().Trim('"') + "   ");         // 收件人
                heading.To += DecodeField(match.Groups[3].Value.Trim(new char[] { '<', '>' }));   // 收件人邮箱
            }
            else                                                                                // 没有附收件人时
            {
                match = Regex.Match(raw, "(\r\n|\n)To:(.*@.*)(\r\n|\n)");

                if (match.Success)
                {
                    heading.To = DecodeField(match.Groups[2].Value.Trim());
                }
            }

            /* 主题处理 */
            match = Regex.Match(raw, "(\r\n|\n)Subject:(.*)(\r\n|\n)");
            if (match.Success)
            {
                string[] fields = match.Groups[2].Value.Split(' ');       // 将可能分成了多段的字段切割开，逐个处理后合并

                heading.Subject = "";
                for (int i = 0; i < fields.Length; i++)
                {
                    string tmp = fields[i].Trim('"');
                    heading.Subject += DecodeField(tmp);
                }
            }

            /* 日期处理 */
            match = Regex.Match(raw, "(\r\n|\n)Date:(.*)(\r\n|\n)");
            if (match.Success)
            {
                try                                 // 从字符串转为日期可能会出问题，做一个异常捕获
                {
                    string dateStr = match.Groups[2].Value.Trim().Trim('"');
                    DateTime date = Convert.ToDateTime(dateStr);
                    heading.Date = date;
                }
                catch (Exception)
                {
                    // 处理错误
                }
            }
            else
            {
                // 邮件没有给出日期
            }



            heading.Id = index;
            heading.IsSelected = false;

            return heading;
        }

        /* 将字符串转为信体结构 */
        public static Body GetBody(string rawStr)
        {
            Body body = new Body();

            string unfoledRawStr = rawStr.Replace("\r\n ", " ").Replace("\r\n\t", " ");       // 展开字段


            /* 处理Content-Type和Transfer-Encoding-Type */
            string contentTypePattern = "(\r\n|\n)Content-Type:([^;]*);(.*)(\r\n|\n)";           // Content-Type匹配模式
            string encodingTypePattern = "(\r\n|\n)Content-Transfer-Encoding:[ ]*(.*)[ ]*(\r\n|\n)";    // Transfer-Encoding-Type匹配模式
            Match conmatch = Regex.Match(unfoledRawStr, contentTypePattern);
            Match enmatch = Regex.Match(unfoledRawStr, encodingTypePattern);

            if (!conmatch.Success || !enmatch.Success)     // 两个必有属性必须匹配成功
                return body;

            string contentTypeStr = conmatch.Groups[2].Value.Trim().Trim('"').ToLower();        // 主属性
            string otherAttributes = conmatch.Groups[3].Value;              // 其他属性

            body.ContentType = GetContentType(contentTypeStr);                                  // 添加Content-Type
            body.EncodeType = GetEncodingType(enmatch.Groups[2].Value.Trim().Trim('"'));        // 添加Transfer-Encoding-Type


            /* 依据不同的内容类型分别处理 */
            if (contentTypeStr.Contains("multipart"))                       // 混合类型
            {
                body.Boundary = GetBoundary(otherAttributes);
                body.Charset = GetCharset(otherAttributes);

                string boundary = "--" + body.Boundary;                     // 分割字符串
                string endBoundary = "--" + body.Boundary + "--";           // 结尾字符串
                string[] parts = Regex.Split(rawStr.Replace(endBoundary, boundary), boundary);   // 按边界切割

                /* 处理每个子部分 */
                for (int i = 1; i < parts.Length; i++)                      // 忽略第一部分，因为是信头
                {
                    if (parts[i] == "\r\n" || parts[i] == "\r\n\r\n" || parts[i] == "")         // 跳过无意义的部分
                        continue;
                    body.SubBodies.Add(GetBody(parts[i]));                  // 递归处理子信体
                }
            }
            else if (contentTypeStr.Contains("text"))                       // 文本类型
            {
                body.Charset = GetCharset(otherAttributes);

                string[] strs = Regex.Split(rawStr, "\r\n\r\n");

                string data = "";
                for (int i = 1; i < strs.Length; i++)                       // 跳过信头
                {
                    if (strs[i] == "\r\n" || strs[i] == "\r\n\r\n" || strs[i] == "")            // 跳过无意义的部分
                        continue;

                    string contentStr;
                    switch (body.EncodeType)                                        // 依据不同编码进行解码
                    {
                        case Transfer_Encoding.Base64:
                            contentStr = strs[i].Replace("\r\n", "");               // 去除所有换行标记
                            data += (MyEncoder.DecodeWithBase64(contentStr, body.Charset) + "\n");
                            break;
                        case Transfer_Encoding.Quoted_Printable:
                            strs[i] += "\r\n";                                      // 每段结尾补一个\r\n，以避免QP编码换行标志被破坏
                            contentStr = strs[i].Replace("=\r\n", "").Replace("\r\n", "");        // 去除所有换行标记
                            data += (MyEncoder.DecodeWithQP(contentStr, body.Charset) + "\n");
                            break;
                        case Transfer_Encoding.Bit8:
                            contentStr = strs[i].Replace("\r\n", "");               // 去除所有换行标记
                            data += (MyEncoder.DecodeWithBit8(contentStr, body.Charset) + "\n");
                            break;
                        case Transfer_Encoding.Bit7:
                            contentStr = strs[i].Replace("\r\n", "");               // 去除所有换行标记
                            data += (contentStr + "\n");
                            break;
                        default:
                            contentStr = strs[i].Replace("\r\n", "");               // 去除所有换行标记
                            data += (contentStr + "\n");
                            break;
                    }
                }
                body.Data = body.Charset.GetBytes(data);
            }
            else if (contentTypeStr.Contains("application"))                // 应用类型
            {
                body.Name = GetName(otherAttributes);
                body.Disposition = GetDisposition(rawStr);

                string[] paras = Regex.Split(rawStr, "\r\n\r\n");
                byte[] data;

                switch (body.EncodeType)                                    // 依据不同编码进行解码
                {
                    case Transfer_Encoding.Base64:
                        data = MyEncoder.DecodeWithBase64(paras[1].Replace("\r\n", ""));
                        break;
                    case Transfer_Encoding.Quoted_Printable:
                        data = MyEncoder.DecodeWithQP(paras[1].Replace("=\r\n", "").Replace("\r\n", ""));
                        break;
                    case Transfer_Encoding.Bit8:
                        data = MyEncoder.DecodeWithBit8(paras[1].Replace("\r\n", ""));
                        break;
                    case Transfer_Encoding.Bit7:
                        data = Encoding.Default.GetBytes(paras[1].Replace("\r\n", ""));
                        break;
                    default:
                        data = Encoding.Default.GetBytes(paras[1].Replace("\r\n", ""));
                        break;
                }
                body.Data = data;
            }
            else if (contentTypeStr.Contains("image"))                      // 图片类型
            {
                body.Name = GetName(otherAttributes);

                string[] paras = Regex.Split(rawStr, "\r\n\r\n");
                byte[] data;

                switch (body.EncodeType)                                    // 依据不同编码进行解码
                {
                    case Transfer_Encoding.Base64:
                        data = MyEncoder.DecodeWithBase64(paras[1].Replace("\r\n", ""));
                        break;
                    case Transfer_Encoding.Quoted_Printable:
                        data = MyEncoder.DecodeWithQP(paras[1].Replace("=\r\n", "").Replace("\r\n", ""));
                        break;
                    case Transfer_Encoding.Bit8:
                        data = MyEncoder.DecodeWithBit8(paras[1].Replace("\r\n", ""));
                        break;
                    case Transfer_Encoding.Bit7:
                        data = Encoding.Default.GetBytes(paras[1].Replace("\r\n", ""));
                        break;
                    default:
                        data = Encoding.Default.GetBytes(paras[1].Replace("\r\n", ""));
                        break;
                }
                body.Data = data;
            }
            else
            {

            }

            return body;
        }

        /* 将字符串转为信件 */
        public static Email GetEmail(String rawEmailStr, int index)
        {
            Email email = new Email();

            /* 信头 */
            Heading heading = GetHeading(rawEmailStr, index);   // 获取信头
            email.Head = heading;

            /* 信体 */
            Body body = GetBody(rawEmailStr);
            email.Content = body;

            return email;
        }

        /* 将Email转换为可发送字符串 */
        public static string GetMIMEStr(Email email)
        {
            string mailStr = "";

            mailStr += HeadingToStr(email.Head);
            mailStr += BodyToStr(email.Content);
            mailStr += ".\r\n";                              // 添加发送结束标志

            return mailStr;
        }
    }
}

