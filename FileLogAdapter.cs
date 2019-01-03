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
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// 
    /// </summary>
    internal class FileLogAdapter : ILogAdapter
    {
        static readonly Type[] IgnoreTypes = new Type[] {
            typeof(Logger),
            typeof(Microsoft.Extensions.Logging.LoggerExtensions),
            typeof(Microsoft.Extensions.Logging.Logger<>),
            typeof( Microsoft.Extensions.Logging.LoggerMessage),
            typeof(FileLogAdapter),
        };
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
        public string FileDirectory { get; set; }

        public void Log(string category, string eventName, LogLevel logLevel, LogType logType,
            string message, Exception exception, HttpContext context)
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
            sb.AppendLine($"{MessageTitle[0]}{NewLineRegex.Replace(message.TrimStart('\n', '\r'), "$1\t    ")}");

            if (logType.HasFlag(LogType.HttpContext))
            {
                //HttpContext context = _httpContextAccessor?.HttpContext;

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
            }
            if (exception != null)
            {
                sb.AppendLine($"{MessageTitle[9]}{exception.GetString()}");
            }
            else if (logType.HasFlag(LogType.TraceStack))
            {
                var strack = new StackTrace(exception != null);
                sb.AppendLine($"{MessageTitle[10]}{strack.GetString((x, y) => IgnoreTypes.Contains(y.DeclaringType))}");
            }
            new LoggerContent(path, sb.ToString());
            sb.Clear();
            sb = null;
        }
    }
}
