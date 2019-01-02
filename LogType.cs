/* ===============================================
* 功能描述：AspNetCore.Logging.LogType
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:43:26 PM
* ===============================================*/

using System;
using System.Collections.Generic;
using System.Text;


namespace AspNetCore.FileLog
{
    internal enum LogType
    {
        None = 0,
        HttpContext = 1,
        TraceStack = 2,
        All = 3,
    }
}
