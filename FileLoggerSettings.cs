/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerSettings
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:55:36 PM
* ===============================================*/

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Configuration.Json;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using LOG = AspNetCore.FileLog;

namespace AspNetCore.FileLog
{
    internal class FileLoggerSettings
    {
        internal static string LogRequestPath { get; set; } = "/_Logs_";
        internal static string SettingsPath { get; set; } = "/_Settings_";
        internal static string LogDirectory { get; set; }

        const string ResourceName = "AspNetCore.FileLog.settingsPage.html";
        public const string LogJsonFileName = "_logging.json";
        public const string RulesKey = "Rules";
        public const string DefaultName = "Default";
        internal const string DefaultProviderName = "Logging";
  
        string SavePath = "/_Settings_/Save";
        private readonly RequestDelegate _next;
        internal static JsonConfigurationProvider JsonConfiguration { get; set; }
        static readonly string html;
        static FileLoggerSettings()
        {
            if (string.IsNullOrEmpty(html))
            {
                using (StreamReader sr = new StreamReader(
                        System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)))
                {
                    html = sr.ReadToEnd();
                }
            }
        }
        public FileLoggerSettings(RequestDelegate next)
        {
            _next = next;
            SavePath = $"{SettingsPath}/Save";
            SavePath = SavePath.Replace("//", "/");
        }
        internal const string LoggingJsonContent = "{\"Rules\":[{\"CategoryName\":\"Default\",\"LogLevel\":\"Information\",\"LogType\":\"All\"}]}";
        public async Task Invoke(HttpContext context)
        {
            FileLoggerFilterOptions _filterOption = FileLoggerFactory._filterOptions;
            if (context.Request.Path.StartsWithSegments(SavePath))
            {
                //var _logLevel = json.ToModel<Newtonsoft.Json.Linq.JObject>();               
                string logFilePath = null;
                if (JsonConfiguration != null)
                {
                    var fi = JsonConfiguration.Source.FileProvider.GetFileInfo(JsonConfiguration.Source.Path);
                    if (!fi.IsDirectory)
                    {
                        logFilePath = fi.PhysicalPath;
                    }
                    var fileInfo = new FileInfo(logFilePath);
                    if (!fileInfo.Directory.Exists)
                    {
                        fileInfo.Directory.Create();
                    }
                }
                if (string.IsNullOrEmpty(logFilePath))
                {
                    logFilePath = Path.Combine(LogDirectory, LogJsonFileName);
                }
                string json = string.Empty;
                using (StreamReader sr = new StreamReader(context.Request.Body))
                {
                    json = sr.ReadToEnd();
                }
                File.WriteAllText(logFilePath, json);
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync("{message:'ok',status:200}");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                var levels = Enum.GetNames(typeof(LogLevel));
                var types = Enum.GetNames(typeof(LogType));

                sb.AppendLine("<header><h1><a href=\"" + LogRequestPath +
                    "\">Logs</a></h1><br>Set Default : <select id='defaltLevel' onchange='setDefaultLevel(this)'><option>--Log Level--</option>");
                sb.Append(string.Join("", levels.Select(t => $"<option>{t}</option>")));
                sb.Append("</select> &nbsp;&nbsp; Default Type: <select id='defaltType' onchange='setDefaultType(this)'><option>--Log Type--</option>");
                sb.Append(string.Join("", types.Select(t => $"<option>{t}</option>")));
                sb.Append("</select> &nbsp;<button type='buttone' onclick='_save()'>Save</button></header>");
                sb.AppendLine("<table id='index'><thead><tr><th abbr='Name'>Name</th><th abbr='Level'>Level</th><th>Type</th><th abbr='Operation'>Operation</th></tr></thead>");
                SortedDictionary<string, LogLevel> levelValues = new SortedDictionary<string, LogLevel>();
                SortedDictionary<string, LogType> typeValues = new SortedDictionary<string, LogType>();
                foreach (FileLoggerFilterRule rule in _filterOption.Rules)
                {
                    levelValues[rule.CategoryName ?? "Default"] = rule.LogLevel ?? _filterOption.MinLevel;
                    typeValues[rule.CategoryName ?? "Default"] = rule.LogType;
                }
                foreach (var log in FileLoggerFactory._loggers.OrderBy(t => t.Key))
                {
                    if (log.Value.Loggers.Length > 0)
                    {
                        levelValues[log.Key] = log.Value.Loggers[0].MinLevel ?? _filterOption.MinLevel;
                        typeValues[log.Key] = log.Value.Loggers[0].LogType;
                    }
                }
                sb.AppendLine("<tbody>");
                foreach (var log in levelValues)
                {
                    sb.Append($"<tr><td>{log.Key}</td><td>{log.Value}</td><td>{typeValues[log.Key]}</td><td>");
                    sb.Append("<select name='_level'>");
                    foreach (var n in levels)
                    {
                        if (n == log.Value.ToString())
                        {
                            sb.Append($"<option selected>{n}</option>");
                        }
                        else
                        {
                            sb.Append($"<option>{n}</option>");
                        }
                    }
                    sb.Append("</select>&nbsp;&nbsp;");
                    sb.Append("<select name='_type'>");
                    var s = typeValues[log.Key];
                    foreach (var n in types)
                    {
                        if (n == s.ToString())
                        {
                            sb.Append($"<option selected>{n}</option>");
                        }
                        else
                        {
                            sb.Append($"<option>{n}</option>");
                        }
                    }
                    sb.Append("</select>");
                    sb.Append("</td></tr>");
                    sb.AppendLine();
                }
                sb.AppendLine("<tbody></table>");
                await context.Response.WriteAsync(html.Replace("{{url}}", SavePath).Replace("{{body}}", sb.ToString()));
            }
        }
    }
}
