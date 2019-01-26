/* ===============================================
* 功能描述：AspNetCore.FileLog.MarkdownLogAdpater
* 创 建 者：WeiGe
* 创建日期：1/23/2019 11:30:42 AM
* ===============================================*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// 
    /// </summary>
    internal class MarkdownLogAdapter : LogAdapter
    {
        static readonly Regex HtmlReg = new Regex("([\\n\\r\\t]|[\\s]{2})");
        const string NullMessage = "[null]";
        HtmlEncoder htmlEncoder;
        static readonly string[] MessageTitle = new string[]
        {
            "Message:",
            "Path:",
            "Method:",
            "From IP:",
            "UserAgent:",
            "User:",
            "Query:",
            "Form:",
            "Body:",
            "Error:",
            "StackTrace: "
        };

        /// <summary>
        /// 
        /// </summary>
        public MarkdownLogAdapter()
        {
            htmlEncoder = HtmlEncoder.Default;
        }
        /// <summary>
        /// 
        /// </summary>
        public string FileDirectory { get { return LoggerSettings.LogDirectory; } }
        /// <summary>
        /// 
        /// </summary>
        protected override LogType LogType { get; } = LogType.Markdown;
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
        public override void Log(string category, string eventName, LogLevel logLevel, LogType type, string message, StackFrame[] stackFrames, Exception exception, HttpContext context)
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
                    category, name, $"{DateTime.Now.ToString("yyyyMMdd")}.md");
            }
            else
            {
                path = Path.Combine(FileDirectory, (exception != null ? LogLevel.Error : logLevel).ToString(),
                  category, $"{DateTime.Now.ToString("yyyyMMdd")}.md");
            }
            StringBuilder sb = new StringBuilder("|");
            sb.Append(DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fffffff"));
            sb.Append("|");

            string excetionDetails = null;
            if (message == NullMessage && exception != null)
            {
                sb.Append(exception.GetString(ExceptionType.Message)
                    .Replace("\n", "")
                    .Replace("\r", "")
                    .Replace("|", "&#124;")
                    .ToString().Trim());
                excetionDetails = exception.GetString(ExceptionType.Details).ToString().Trim();
                exception = null;
            }
            else
            {
                sb.Append(htmlEncoder.Encode(HtmlReg.Replace(message, " ").Replace("__", "\\__").Replace("|", "&#124;")));
            }
            sb.Append("|");
            if (context != null)
            {
                sb.Append("<ul>");
                if (context.Request.Host.HasValue)
                {
                    sb.AppendFormat("<li>{0}{1}://{2}{3}{4}</li>",
                        MessageTitle[1], context.Request.Scheme,
                        context.Request.Host, context.Request.Path, context.Request.QueryString);
                }
                sb.AppendFormat("<li>{0}{1}</li>", MessageTitle[2], context.Request.Method);
                sb.AppendFormat("<li>{0}{1}</li>", MessageTitle[3], context.Connection.RemoteIpAddress);
                if (context.Request.Headers["User-Agent"].Count > 0)
                {
                    sb.AppendFormat("<li>{0}{1}</li>", MessageTitle[4], context.Request.Headers["User-Agent"]);
                }
                if (context.User.Identity.IsAuthenticated)
                {
                    sb.AppendFormat("<li>{0}{1}</li>", MessageTitle[5], context.User.Identity.Name);
                }
                if (!context.IsFile() && (context.Request.ContentLength > 0 || context.Request.HasFormContentType))
                {
                    if (context.Request.Query.Count > 0)
                    {
                        sb.AppendFormat("<li>{0}{1}</li>",
                            MessageTitle[6], string.Join(",", context.Request.Query.Select(kv => $"{kv.Key}={kv.Value}")));
                    }
                    if (context.Request.HasFormContentType)
                    {
                        var forms = context.Request.ReadFormAsync().GetAwaiter().GetResult();
                        if (forms.Count > 0)
                        {
                            sb.AppendFormat("<li>{0}{1}</li>",
                                MessageTitle[7], string.Join(",", forms.Select(kv => $"{kv.Key}={kv.Value}")));
                        }
                    }
                    else
                    {
                        var body = context.ReadBody();

                        if (!string.IsNullOrEmpty(body))
                        {
                            StringBuilder _sb = new StringBuilder();
                            StringWriter sw = new StringWriter(_sb);
                            htmlEncoder.Encode(sw, body, 0, 1024 * 1024);
                            sb.AppendFormat("<li>{0}{1}</li>", MessageTitle[8], _sb.ToString());
                        }
                    }
                }
                sb.Append("</ul>");
            }
            else
            {
                sb.Append('-');
            }
            sb.Append("|");
            if (exception != null || excetionDetails != null)
            {
                sb.Append("<ul>");
                using (StringReader strReader = new StringReader(excetionDetails ?? exception.GetString().ToString()))
                {
                    string line;
                    while ((line = strReader.ReadLine()) != null)
                    {
                        sb.AppendFormat("<li>{0}</li>", htmlEncoder.Encode(line.Trim().Replace("__", "\\__").Replace("|", "&#124;")));
                    }
                }
                sb.Append("</ul>");
            }
            else
            {
                sb.Append('-');
            }
            sb.Append("|");

            if (stackFrames != null && stackFrames.Length > 0)
            {
                sb.Append("<ul>");
                using (StringReader strReader = new StringReader(stackFrames.GetString().ToString()))
                {
                    string line;
                    while ((line = strReader.ReadLine()) != null)
                    {
                        sb.AppendFormat("<li>{0}</li>", htmlEncoder.Encode(line.Trim().Replace("__", "\\__").Replace("|", "&#124;")));
                    }
                }
                sb.Append("</ul>|");
            }
            else
            {
                sb.Append("-|");
            }
            new LoggerContent(path,
                sb
                .Replace("`", "\\`")
                .Replace("(", "&#40;")
                .Replace("[", "&#91;")
                .Replace("{", "&#123;")
                .Replace("~~", "\\~~")
                .Replace("**", "\\**")
                .Replace("://", "&#58;//")
                //.Replace("<", "&lt;")
                //.Replace(">","&gt;")
                .ToString());
            sb.Clear();
            sb = null;
        }
    }
}
