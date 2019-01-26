/* ===============================================
* 功能描述：AspNetCore.FileLog.FileLogAdapter
* 创 建 者：WeiGe
* 创建日期：1/3/2019 3:09:42 PM
* ===============================================*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// 
    /// </summary>
    public class FileLogAdapter : ILogAdapter
    {
        static readonly Regex HtmlReg = new Regex("([\\n\\r\\t]|[\\s]{2})");
        internal const string MarkdownHead = "|时间|消息|请求|错误|跟踪|\r\n|--|--|--|--|--|\r\n";
        HtmlEncoder htmlEncoder;
        /// <summary>
        /// 
        /// </summary>
        public FileLogAdapter()
        {
            FileDirectory = LoggerSettings.LogDirectory;
            htmlEncoder = HtmlEncoder.Default;
        }
        
        const string NullMessage = "[null]";
        static readonly Regex NewLineRegex = new Regex("([\\n\\r]+)");
        static readonly string[] MessageTitle = new string[]
        {
            "Message:    ",
            "Path:       ",
            "Method:     ",
            "From IP:    ",
            "UserAgent:  ",
            "User:       ",
            "Query:      ",
            "Form:       ",
            "Body:       ",
            "Error:      ",
            "StackTrace: "
        };
        /// <summary>
        /// 
        /// </summary>
        public string FileDirectory { get; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="eventName"></param>
        /// <param name="logLevel"></param>
        /// <param name="stackFrames"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        /// <param name="format"></param>
        public virtual void Log(string category, string eventName,
            LogLevel logLevel, Format format,
            string message, StackFrame[] stackFrames, Exception exception, HttpContext context)
        {
            switch (format)
            {
                case Format.Txt:
                    LogTxt(category, eventName, logLevel,  message, stackFrames, exception, context);
                    break;
                case Format.Markdown:
                    LogMarkdown(category, eventName, logLevel,  message, stackFrames, exception, context);
                    break;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="eventName"></param>
        /// <param name="logLevel"></param>
        /// <param name="stackFrames"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        protected virtual void LogTxt(string category, string eventName,
            LogLevel logLevel, string message,
            StackFrame[] stackFrames, Exception exception, HttpContext context)
        {
            
            //await Task.Yield();
            
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="eventName"></param>
        /// <param name="logLevel"></param>
        /// <param name="stackFrames"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        protected virtual void LogMarkdown(string category, string eventName,
            LogLevel logLevel, string message,
            StackFrame[] stackFrames, Exception exception, HttpContext context)
        {
            //await Task.Yield();
            
        }

    }
}
