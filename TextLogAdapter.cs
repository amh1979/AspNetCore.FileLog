/* ===============================================
* 功能描述：AspNetCore.FileLog.TextLogAdapter
* 创 建 者：WeiGe
* 创建日期：1/23/2019 11:20:20 AM
* ===============================================*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// 
    /// </summary>
    internal class TextLogAdapter : LogAdapter
    {
        static readonly Regex NewLineRegex = new Regex("([\\n\\r]+)");
        const string NullMessage = "[null]";
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
        protected override LogType LogType { get; } = LogType.Text;
        /// <summary>
        /// 
        /// </summary>
        public string FileDirectory { get { return LoggerSettings.LogDirectory; } }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="category"></param>
        /// <param name="eventName"></param>
        /// <param name="logLevel"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <param name="stackFrames"></param>
        /// <param name="exception"></param>
        /// <param name="context"></param>
        public override void Log(string category, string eventName, LogLevel logLevel,LogType type, string message, StackFrame[] stackFrames, Exception exception, HttpContext context)
        {
            string path = string.Empty;
            if (!string.IsNullOrEmpty(eventName))
            {
                var index = eventName.LastIndexOf('.');
                string name = eventName;
                if (index > 0)
                {
                    name = eventName.Remove(0, eventName.LastIndexOf('.') + 1);
                }
                path = Path.Combine(FileDirectory, (exception != null ? LogLevel.Error : logLevel).ToString(),
                    category, name, $"{DateTime.Now.ToString("yyyyMMdd")}.txt");
            }
            else
            {
                path = Path.Combine(FileDirectory, (exception != null ? LogLevel.Error : logLevel).ToString(),
                  category, $"{DateTime.Now.ToString("yyyyMMdd")}.txt");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($"#################### {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fffffff")} ###############");
            string excetionDetails = null;
            if (message == NullMessage && exception != null)
            {
                sb.AppendLine($"{MessageTitle[0]}{exception.GetString(ExceptionType.Message).ToString().Trim()}");
                excetionDetails = exception.GetString(ExceptionType.Details).ToString().Trim();
                exception = null;
            }
            else
            {
                sb.AppendLine($"{MessageTitle[0]}{NewLineRegex.Replace(message.TrimStart('\n', '\r'), "$1\t    ")}");
            }
            if (context != null)
            {
                if (context.Request.Host.HasValue)
                {
                    sb.AppendLine($"{MessageTitle[1]}{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
                }
                sb.AppendLine($"{MessageTitle[2]}{context.Request.Method}");
                sb.AppendLine($"{MessageTitle[3]}{context.Connection.RemoteIpAddress}");
                if (context.Request.Headers["User-Agent"].Count > 0)
                {
                    sb.AppendLine($"{MessageTitle[4]}{context.Request.Headers["User-Agent"]}");
                }
                if (context.User.Identity.IsAuthenticated)
                {
                    sb.AppendLine($"{MessageTitle[5]}{context.User.Identity.Name}");
                }
                if (!context.IsFile() && (context.Request.ContentLength > 0 || context.Request.HasFormContentType))
                {

                    if (context.Request.Query.Count > 0)
                    {
                        sb.AppendLine($"{MessageTitle[6]}{string.Join(",", context.Request.Query.Select(kv => $"{kv.Key}={kv.Value}"))}");
                    }
                    if (context.Request.HasFormContentType)
                    {
                        var forms = context.Request.ReadFormAsync().GetAwaiter().GetResult();
                        if (forms.Count > 0)
                        {
                            sb.AppendLine($"{MessageTitle[7]}{string.Join(",", forms.Select(kv => $"{kv.Key}={kv.Value}"))}");
                        }
                    }
                    else
                    {
                        var body = context.ReadBody();
                        if (!string.IsNullOrEmpty(body))
                        {
                            sb.AppendLine($"{MessageTitle[8]}{body.TrimStart('\n', '\r', ' ')}");
                        }
                    }
                }
            }
            if (exception != null)
            {
                sb.AppendLine($"{MessageTitle[9]}{exception?.GetString()}");
            }
            else if (excetionDetails != null)
            {
                sb.AppendLine($"{MessageTitle[9]}{excetionDetails}");
            }
            if (stackFrames != null && stackFrames.Length > 0)
            {
                sb.AppendLine($"{MessageTitle[10]}{stackFrames.GetString()}");
            }
            new LoggerContent(path, sb.ToString());
            sb.Clear();
            sb = null;
        }
    }
}
