using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
}
