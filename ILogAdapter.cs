/* ===============================================
* 功能描述：AspNetCore.FileLog.ILoggerWriter
* 创 建 者：WeiGe
* 创建日期：1/3/2019 2:58:09 PM
* ===============================================*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;


namespace AspNetCore.FileLog
{
    /// <summary>
    /// 
    /// </summary>
    public interface ILogAdapter
    {
        /// <summary>
        /// 
        /// </summary>
        string FileDirectory { get; set; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="eventName"></param>
        /// <param name="logLevel"></param>
        /// <param name="logType"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        void Log(string category, string eventName, LogLevel logLevel, LogType logType, string message, Exception exception, HttpContext context);
    }
}
