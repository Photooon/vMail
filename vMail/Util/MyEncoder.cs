using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace vMail
{
    /* 编码解码类 */
    class MyEncoder
    {
        /* 将字符串转为Base64编码 */
        public static string EncodeWithBase64(string str)
        {
            byte[] b = Encoding.Default.GetBytes(str);
            string base64String = Convert.ToBase64String(b);     // 将b转为Base64编码的字符串
            return base64String;
        }

        /* 将字符串转为UTF-8编码 */
        public static string EncodeWithUTF8(string str)
        {
            byte[] b = Encoding.UTF8.GetBytes(str);               // 将字符串转为utf8的byte数组
            string base64String = Encoding.UTF8.GetString(b);     // 将b转为utf8编码的字符串
            return base64String;
        }

        public static string EncodeWithUTF8(byte[] b)
        {
            string base64String = Encoding.UTF8.GetString(b);     // 将b转为utf8编码的字符串
            return base64String;
        }

        /* 将字符串从Base64解码为指定编码的字符串 */
        public static string DecodeWithBase64(string base64Str, Encoding charset)
        {
            try
            {
                byte[] b = Convert.FromBase64String(base64Str);     // Baes64解码
                string str = charset.GetString(b);             // 按目标编码解码为字符串
                return str;
            }
            catch (Exception)
            {
                return "DecodeERR";
            }
        }

        /* 将字符串从QP编码解码为指定编码的字符串 */
        public static string DecodeWithQP(string qpStr, Encoding charset)  // 先转为byte数组，再解码
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

            return charset.GetString(dst.ToArray());
        }

        /* 将字符串从8Bit编码解码为指定编码的字符串 */
        public static string DecodeWithBit8(string bit8Str, Encoding charset)
        {
            byte[] b = Encoding.Default.GetBytes(bit8Str);
            return charset.GetString(b);
        }

        /* 将字符串从7Bit编码解码为指定编码的字符串 */
        public static string DecodeWithBit7(string bit7Str, Encoding charset)
        {
            string result = string.Empty;
            byte[] b = new byte[65000];
            string temp = string.Empty;

            for (int i = 0; i < bit7Str.Length; i += 2)
            {
                b[i / 2] = (byte)Convert.ToByte((bit7Str[i].ToString() + bit7Str[i + 1].ToString()), 16);
            }

            int j = 0;          // while计数
            int tmp = 1;        // temp中二进制字符字符个数
            while (j < bit7Str.Length / 2 - 1)
            {
                string s = Convert.ToString(b[j], 2);

                while (s.Length < 8)        // 将s补满8位
                {
                    s = "0" + s;
                }

                result += (char)Convert.ToInt32(s.Substring(tmp) + temp, 2);

                temp = s.Substring(0, tmp);     // 前一位组多的部分

                if (tmp > 6)                    // 多余的部分满7位，加入一个字符
                {
                    result += (char)Convert.ToInt32(temp, 2);
                    temp = string.Empty;
                    tmp = 0;
                }

                tmp++;
                j++;

                if (j == bit7Str.Length / 2 - 1)        // 最后一个字符
                {
                    result += (char)Convert.ToInt32(Convert.ToString(b[j], 2) + temp, 2);
                }
            }

            byte[] bf = Encoding.Default.GetBytes(result);
            return charset.GetString(bf);
        }
    }
}
