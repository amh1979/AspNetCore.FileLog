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
    /// <summary>
    /// 
    /// </summary>
    public enum LogType
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,
        /// <summary>
        /// Contains http request information
        /// </summary>
        HttpContext = 1,
        /// <summary>
        /// Contains stack trace information
        /// </summary>
        TraceStack = 2,
        /// <summary>
        /// Contains all information
        /// </summary>
        All = 3,
    }
}
