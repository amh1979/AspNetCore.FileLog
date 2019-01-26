/* ===============================================
* 功能描述：AspNetCore.FileLog.MarkdownFileMiddleware
* 创 建 者：WeiGe
* 创建日期：1/12/2019 8:33:38 PM
* ===============================================*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;

namespace AspNetCore.FileLog
{
    internal class MarkdownFileMiddleware
    {
        internal const string MarkdownHead = "|时间|消息|请求|错误|跟踪|\r\n|--|--|--|--|--|\r\n";
        private readonly RequestDelegate _next;
        static readonly Assembly _assembly = typeof(MarkdownFileMiddleware).Assembly;
        const string cerulean = "AspNetCore.FileLog.markdown.cerulean.min.css";
        const string font = "AspNetCore.FileLog.markdown.font.woff2";
        const string strapdownCss = "AspNetCore.FileLog.markdown.strapdown.min.css";
        const string strapdownJs = "AspNetCore.FileLog.markdown.strapdown.min.js";
        const string markdownHtml = "AspNetCore.FileLog.markdown.markdown.html";
        const string strapdown = "strapdown";
        static string markdownHtmlContent;
        readonly string _fileDirectory;
        private static readonly HtmlEncoder _htmlEncoder= HtmlEncoder.Default;
        internal static void SaveResourceFiles(string path)
        {
            if (!Directory.Exists(Path.Combine(path, strapdown)))
            {
                Directory.CreateDirectory(Path.Combine(path, strapdown));
            }
            var file = Path.Combine(path, strapdown, "cerulean.min.css");
            if (!File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(_assembly.GetManifestResourceStream(cerulean)))
                {
                    File.WriteAllText(file, sr.ReadToEnd());
                }
            }

            file = Path.Combine(path, strapdown, "font.woff2");
            if (!File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(_assembly.GetManifestResourceStream(font)))
                {
                    File.WriteAllText(file, sr.ReadToEnd());
                }
            }

            file = Path.Combine(path, strapdown, "strapdown.css");
            if (!File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(_assembly.GetManifestResourceStream(strapdownCss)))
                {
                    File.WriteAllText(file, sr.ReadToEnd());
                }
            }

            file = Path.Combine(path, strapdown, "strapdown.js");
            if (!File.Exists(file))
            {
                using (StreamReader sr = new StreamReader(_assembly.GetManifestResourceStream(strapdownJs)))
                {
                    File.WriteAllText(file, sr.ReadToEnd());
                }
            }
            if (string.IsNullOrEmpty(markdownHtmlContent))
            {
                using (StreamReader sr = new StreamReader(_assembly.GetManifestResourceStream(markdownHtml)))
                {
                    markdownHtmlContent = sr.ReadToEnd();
                }
            }
        }

        public MarkdownFileMiddleware(RequestDelegate next, string fileDirectory)
        {
            _next = next;
            _fileDirectory = fileDirectory;

        }
        public async Task Invoke(HttpContext context)
        {
            context.Response.ContentType = "text/html; charset=utf-8";
            var _path = context.Request.Path.ToString().Replace(LoggerSettings.LogRequestPath, "");
            var file = new FileInfo(Path.Combine(_fileDirectory, _path.TrimStart('\\','/')));
            context.Response.Headers.Add(HeaderNames.AcceptRanges, "bytes");
            EntityTagHeaderValue _tag = null;
            if (file.Exists)
            {
                var _lastModified = file.LastAccessTime.ToUniversalTime();
                long value = _lastModified.ToFileTime() ^ file.Length;
                _tag = EntityTagHeaderValue.Parse($"\"{ Convert.ToString(value, 16)}\"");
                if (context.Request.Headers.Keys.Contains("If-None-Match"))
                {
                    var tag = context.Request.Headers["If-None-Match"].ToString();
                    if (tag == _tag.Tag)
                    {
                        context.Response.StatusCode = 304;
                        await Task.CompletedTask;
                        return;
                    }
                }
                if (_tag != null)
                {
                    var _responseHeaders = context.Response.GetTypedHeaders();
                    _responseHeaders.LastModified = file.LastAccessTime;
                    _responseHeaders.ETag = _tag;
                    _responseHeaders.CacheControl = new CacheControlHeaderValue
                    {
                        MaxAge = TimeSpan.FromHours(2),
                        //Public = true,
                    };
                }
                string fileContent;
                using (var stream = file.OpenRead())
                {
                    using (StreamReader reader = new StreamReader(stream))
                    {
                        fileContent = reader.ReadToEnd();
                    }
                }
                StringBuilder html = new StringBuilder(markdownHtmlContent);
                var pathString = context.Request.Path;
                string text = "/";
                string[] array = pathString.Value.Split(new char[1]
                {
                '/'
                }, StringSplitOptions.RemoveEmptyEntries);
                //var _paths = array.Take(array.Length - 1).ToArray();
                StringBuilder stringBuilder = new StringBuilder($"<a href='{LoggerSettings.SettingsPath}'>Settings</a>&nbsp;&nbsp;");
                foreach (string text2 in array)
                {
                    text =string.Format("{0}{1}/", text , text2);
                    if (text.TrimEnd('/') == context.Request.Path)
                    {
                        stringBuilder.AppendFormat("<a href='javascript:;'>{0}</a>", _htmlEncoder.Encode(text2));
                    }
                    else
                    {
                        stringBuilder.AppendFormat("<a href=\"{0}\">{1}/</a>", _htmlEncoder.Encode(text), _htmlEncoder.Encode(text2));
                    }
                }
                html.Replace("{{content}}",MarkdownHead+fileContent)
                    .Replace("{{title}}", string.Join("/", array.Take(array.Length - 1)))
                    .Replace("{{link}}", stringBuilder.ToString());
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(html.ToString(), Encoding.UTF8);
            }
        }
    }
}
