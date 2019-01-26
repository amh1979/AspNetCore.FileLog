/* ===============================================
* 功能描述：AspNetCore.FileLog.ILoggerWriter
* 创 建 者：WeiGe
* 创建日期：1/3/2019 2:58:09 PM
* ===============================================*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// 
    /// </summary>
    public abstract class LogAdapter
    {
        /// <summary>
        /// 
        /// </summary>
        protected abstract LogType LogType { get; }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="eventName"></param>
        /// <param name="logLevel"></param>
        /// <param name="type"></param>
        /// <param name="stackFrames"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        public abstract void Log(string category, string eventName, LogLevel logLevel, LogType type,
              string message, StackFrame[] stackFrames, Exception exception, HttpContext context);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="current"></param>
        /// <param name="rule"></param>
        /// <returns></returns>
        public virtual bool IsEnabled(LogType current, LogType rule)
        {
            return this.LogType.HasFlag(current) && (rule == LogType.All || rule.HasFlag(current));
        }
    }
}
