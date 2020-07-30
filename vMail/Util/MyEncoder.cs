using System;
using System.Collections.Generic;
using System.Text;

namespace vMail
{
    /* 编码解码类 */
    class MyEncoder
    {
        /* 将字符串从7Bit编码解码并转为指定编码的字符串 */
        public static string DecodeWithBit7(string bit7Str, Encoding charset)
        {
            return charset.GetString(DecodeWithBit7(bit7Str));
        }

        /* 将字符串从7Bit编码解码为字节数组 */
        public static byte[] DecodeWithBit7(string bit7Str)
        {
            byte[] b = Encoding.Default.GetBytes(bit7Str);
            return b;
        }

        /* 将字符串从8Bit编码解码并转为指定编码的字符串 */
        public static string DecodeWithBit8(string bit8Str, Encoding charset)
        {
            return charset.GetString(DecodeWithBit8(bit8Str));
        }

        /* 将字符串从8Bit编码解码并转为指定编码的字符串 */
        public static byte[] DecodeWithBit8(string bit8Str)
        {
            byte[] b = Encoding.Default.GetBytes(bit8Str);
            return b;
        }

        /* 将字符串转为Base64编码 */
        public static string EncodeWithBase64(string str)
        {
            UTF8Encoding utf8 = new UTF8Encoding();                 // 默认传入的字符串是utf8编码
            byte[] b = utf8.GetBytes(str);
            string base64String = Convert.ToBase64String(b);        // 将b转为Base64编码的字符串
            return base64String;
        }

        /* 将字节数组转为Base64编码 */
        public static string EncodeWithBase64(byte[] b)
        {
            string base64String = Convert.ToBase64String(b);        // 将b转为Base64编码的字符串
            return base64String;
        }

        /* 将字符串从Base64解码并转为指定编码的字符串 */
        public static string DecodeWithBase64(string base64Str, Encoding charset)
        {
            return charset.GetString(DecodeWithBase64(base64Str));
        }

        /* 将字符串从Base64解码字节数组 */
        public static byte[] DecodeWithBase64(string base64Str)
        {
            byte[] b = Convert.FromBase64String(base64Str);     // Baes64解码
            return b;
        }

        /* 将字符串从QP编码解码并转为指定编码的字符串 */
        public static string DecodeWithQP(string qpStr, Encoding charset)
        {
            return charset.GetString(DecodeWithQP(qpStr));
        }

        /* 将字符串从QP编码解码字节数组 */
        public static byte[] DecodeWithQP(string qpStr)
        {
            char[] chars = qpStr.ToCharArray();
            List<byte> dst = new List<byte>();      // 需要一个临时的可变长byte数组

            for (int i = 0; i < chars.Length;)
            {
                if (chars[i] == '=')
                {
                    try
                    {
                        dst.Add(Convert.ToByte(chars[i + 1].ToString() + chars[i + 2].ToString(), 16));
                        i += 3;
                    }
                    catch (Exception)
                    {
                        dst.AddRange(Encoding.Default.GetBytes(chars[i + 1].ToString()));       // 强制转为byte后加入
                        dst.AddRange(Encoding.Default.GetBytes(chars[i + 2].ToString()));       // 强制转为byte后加入
                    }
                }
                else
                {
                    try                                     // 当qpStr包含非ascii码字符时，会出问题（暂不清楚为什么）
                    {
                        dst.Add(Convert.ToByte(chars[i]));
                    }
                    catch (Exception)
                    {

                    }

                    i++;
                }
            }

            return dst.ToArray();
        }

        /* 将字符串转为UTF-8编码 */
        public static string EncodeWithUTF8(string str)
        {
            UTF8Encoding utf8 = new UTF8Encoding();
            byte[] b = utf8.GetBytes(str);                      // 将字符串转为utf8的byte数组
            string utf8Str = utf8.GetString(b);                 // 将b转为utf8编码的字符串
            return utf8Str;
        }
    }
}
