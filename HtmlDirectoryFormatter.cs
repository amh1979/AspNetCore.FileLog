/* ===============================================
* 功能描述：AspNetCore.Logging.HtmlDirectoryFormatter
* 创 建 者：WeiGe
* 创建日期：1/2/2019 10:23:53 PM
* ===============================================*/

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
namespace AspNetCore.FileLog
{
    internal class HtmlDirectoryFormatter : IDirectoryFormatter
    {
        private const string TextHtmlUtf8 = "text/html; charset=utf-8";

        private HtmlEncoder _htmlEncoder;
        System.Globalization.CultureInfo CurrentCulture { get; }
        public HtmlDirectoryFormatter()
            : this(HtmlEncoder.Default)
        { }
        public HtmlDirectoryFormatter(HtmlEncoder encoder)
        {
               _htmlEncoder = encoder?? throw new ArgumentNullException("encoder");
            CurrentCulture= System.Globalization.CultureInfo.CurrentCulture;
        }

        /// <summary>
        /// Generates an HTML view for a directory.
        /// </summary>
        public virtual Task GenerateContentAsync(HttpContext context, IEnumerable<IFileInfo> contents)
        {
            if (context == null)
            {
                throw new ArgumentNullException("context");
            }
            if (contents == null)
            {
                throw new ArgumentNullException("contents");
            }
            context.Response.ContentType = "text/html; charset=utf-8";
            if (HttpMethods.IsHead(context.Request.Method))
            {
                return Task.CompletedTask;
            }
            PathString pathString = context.Request.PathBase + context.Request.Path;
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat("<!DOCTYPE html>\r\n<html lang=\"{0}\">", CurrentCulture.TwoLetterISOLanguageName);
            stringBuilder.AppendFormat("\r\n<head>\r\n  <title>{0} {1}</title>", HtmlEncode("Index of"), HtmlEncode(pathString.Value));
            stringBuilder.Append("\r\n  <style>\r\n    body {\r\n        font-family: \"Segoe UI\", \"Segoe WP\", \"Helvetica Neue\", 'RobotoRegular', sans-serif;\r\n        font-size: 14px;}\r\n    header h1 {\r\n        font-family: \"Segoe UI Light\", \"Helvetica Neue\", 'RobotoLight', \"Segoe UI\", \"Segoe WP\", sans-serif;\r\n        font-size: 28px;\r\n        font-weight: 100;\r\n        margin-top: 5px;\r\n        margin-bottom: 0px;}\r\n    #index {\r\n        border-collapse: separate; \r\n        border-spacing: 0; \r\n        margin: 0 0 20px; }\r\n    #index th {\r\n        vertical-align: bottom;\r\n        padding: 10px 5px 5px 5px;\r\n        font-weight: 400;\r\n        color: #a0a0a0;\r\n        text-align: center; }\r\n    #index td { padding: 3px 10px; }\r\n    #index th, #index td {\r\n        border-right: 1px #ddd solid;\r\n        border-bottom: 1px #ddd solid;\r\n        border-left: 1px transparent solid;\r\n        border-top: 1px transparent solid;\r\n        box-sizing: border-box; }\r\n    #index th:last-child, #index td:last-child {\r\n        border-right: 1px transparent solid; }\r\n    #index td.length, td.modified { text-align:right; }\r\n    a { color:#1ba1e2;text-decoration:none; }\r\n    a:hover { color:#13709e;text-decoration:underline; }\r\n  </style>\r\n</head>\r\n<body>\r\n  <section id=\"main\">");
            stringBuilder.AppendFormat("\r\n    <header><h1><a href=\"/\">Home</a>&nbsp;&nbsp;<a href='{0}'>Settings</a>&nbsp;&nbsp;", LoggerSettings.SettingsPath);
            string text = "/";
            string[] array = pathString.Value.Split(new char[1]
            {
                '/'
            }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string text2 in array)
            {
                text = text + text2 + "/";
                if (text == pathString.Value)
                {
                    stringBuilder.AppendFormat("<a href='javascript:;'>{0}</a>", HtmlEncode(text2));
                }
                else
                {
                    stringBuilder.AppendFormat("<a href=\"{0}\">{1}/</a>", HtmlEncode(text), HtmlEncode(text2));
                }
            }
            stringBuilder.AppendFormat(CurrentCulture, "</h1></header>\r\n    <table id=\"index\" summary=\"{0}\">\r\n    <thead>\r\n      <tr><th abbr=\"{1}\">{1}</th><th abbr=\"{2}\">{2}</th><th abbr=\"{3}\">{4}</th></tr>\r\n    </thead>\r\n    <tbody>",
                HtmlEncode("The list of files in the given directory.  Column headers are listed in the first row."),
                HtmlEncode("Name"),
                HtmlEncode("Size"),
                HtmlEncode("Modified"),
                HtmlEncode("Last Modified"));
            DateTimeOffset lastModified;
            foreach (IFileInfo item in from info in contents
                                       where info.IsDirectory
                                       select info)
            {
                
                StringBuilder stringBuilder2 = stringBuilder;
                string arg = HtmlEncode(item.Name);
                lastModified = item.LastModified;
                long? len=new System.IO.DirectoryInfo(item.PhysicalPath)
                    .GetFiles()
                    .Where(x=>x.Name != LoggerSettings.LogJsonFileName)
                    .Sum(x => x.Length);
                if (len <= 0)
                {
                    len = null;
                }
                stringBuilder2.AppendFormat("<tr class=\"directory\"><td class=\"name\"><a href=\"./{0}/\">{0}/</a></td><td>{1}</td><td class=\"modified\">{2}</td></tr>",
                    arg, HtmlEncode(len?.ToString("n0", CurrentCulture)), HtmlEncode(lastModified.ToString(CurrentCulture)));
            }
            foreach (IFileInfo item2 in from info in contents
                                        where !info.IsDirectory
                                        select info)
            {
                if (item2.Name == LoggerSettings.LogJsonFileName)
                {
                    continue;
                }
                StringBuilder stringBuilder3 = stringBuilder;
                string arg2 = HtmlEncode(item2.Name);
                string arg3 = HtmlEncode(item2.Length.ToString("n0", CurrentCulture));
                string arg4 = HtmlEncode(item2.PhysicalPath);
                lastModified = item2.LastModified;
                stringBuilder3.AppendFormat("\r\n      <tr class=\"file\">\r\n        <td class=\"name\"><a href=\"./{0}\" title=\"{2}\">{0}</a></td>\r\n        <td class=\"length\">{1}</td>\r\n        <td class=\"modified\">{3}</td>\r\n      </tr>",
                    arg2, arg3, arg4, HtmlEncode(lastModified.ToString(CurrentCulture)));
            }
            stringBuilder.Append("\r\n    </tbody>\r\n    </table>\r\n  </section>\r\n</body>\r\n</html>");
            string s = stringBuilder.ToString();
            byte[] bytes = Encoding.UTF8.GetBytes(s);
            context.Response.ContentLength = bytes.Length;
            return context.Response.Body.WriteAsync(bytes, 0, bytes.Length);
        }

        private string HtmlEncode(string body)
        {
            if (string.IsNullOrEmpty(body))
            {
                return string.Empty;
            }
            return _htmlEncoder.Encode(body);
        }
    }
}
