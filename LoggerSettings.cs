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
    internal class LoggerSettings
    {
        public const int TraceCount = 5;
        internal static string LogRequestPath { get; set; }
        internal static string SettingsPath { get; set; }
        internal static string LogDirectory { get; set; }
        internal static LogType Format { get; set; }
        const string ResourceName = "AspNetCore.FileLog.settingsPage.html";
        public const string LogJsonFileName = "_logging.json";
        public const string RulesKey = "Rules";
        public const string DefaultName = "Default";
        internal const string DefaultProviderName = "Logging";
  
        string SavePath = "/_Settings_/Save";
        private readonly RequestDelegate _next;
        internal static JsonConfigurationProvider JsonConfiguration { get; set; }
        static readonly string html;
        static LoggerSettings()
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
        public LoggerSettings(RequestDelegate next)
        {
            _next = next;
            SavePath = $"{SettingsPath}/Save";
            SavePath = SavePath.Replace("//", "/");
        }
        internal const string LoggingJsonContent = "{\"Rules\":[{\"CategoryName\":\"Default\",\"LogLevel\":\"Information\",\"LogType\":\"Text\",\"TraceCount\":5,\"LogScope\":\"All\"}]}";
        public async Task Invoke(HttpContext context)
        {
            LoggerFilterOptions _filterOption = LoggerFactory._filterOptions;
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
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync("{message:'ok',status:200}");
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                var levels = Enum.GetNames(typeof(LogLevel));
                var types = Enum.GetNames(typeof(LogScope));
                var formats = Enum.GetNames(typeof(LogType));
                sb.AppendLine("<header><h1><a href=\"" + LogRequestPath +
                    "\">Logs</a></h1><br>Default Level: <select id='defaultLevel' onchange='_setDefault(this,\"_level\")'><option>--Log Level--</option>");
                sb.Append(string.Join("", levels.Select(t => $"<option>{t}</option>")));
                sb.Append("</select> &nbsp;&nbsp; Default Scope: <select id='defaultScope' onchange='_setDefault(this,\"_scope\")'><option>--Log Scope--</option>");
                sb.Append(string.Join("", types.Select(t => $"<option>{t}</option>")));
                sb.Append("</select> &nbsp;&nbsp; Default Type: <select id='defaultType' onchange='_setDefault(this,\"_type\")'><option>--Log Type--</option>");
                sb.Append(string.Join("", formats.Select(t => $"<option>{t}</option>")));
                sb.Append("</select>");

                sb.Append($" &nbsp;&nbsp; TraceCount: <input type=text id='traceCount' value='' onchange='_setDefault(this,\"_count\")'/>");
                sb.Append(" &nbsp;<button type='button' onclick='_save()'>Save</button></header>");
   
                SortedDictionary<string, LoggerFilterRule> rules = new SortedDictionary<string, LoggerFilterRule>();
                //sb.AppendLine("<table id='index'><thead><tr><th abbr='Name'>Name</th><th abbr='Level'>Level</th><th>Type</th><th abbr='Operation'>Operation</th></tr></thead>");
                SortedDictionary<string, LogLevel> levelValues = new SortedDictionary<string, LogLevel>();
                SortedDictionary<string, LogScope> typeValues = new SortedDictionary<string, LogScope>();
                foreach (LoggerFilterRule rule in _filterOption.Rules)
                {
                    rules[rule.CategoryName ?? "Default"] = rule;
                    //levelValues[rule.CategoryName ?? "Default"] = rule.LogLevel ?? _filterOption.MinLevel;
                    //typeValues[rule.CategoryName ?? "Default"] = rule.LogType;
                }
                foreach (var log in LoggerFactory._loggers.OrderBy(t => t.Key))
                {
                    if (log.Value.Loggers.Length > 0)
                    {
                        rules[log.Key] = log.Value.Loggers[0].Rule;
                        //levelValues[log.Key] = log.Value.Loggers[0].Rule.LogLevel ?? _filterOption.MinLevel;
                        //typeValues[log.Key] = log.Value.Loggers[0].Rule.LogType;
                    }
                }
                sb.AppendLine();
                var objs = rules.Select(k => k.Value)
                    .Select(t => new
                    {
                        Name = t.CategoryName,
                        LogScope=t.LogScope.ToString(),
                        LogLevel=t.LogLevel.ToString(),
                        LogType = t.LogType.ToString(),
                        t.TraceCount,
                    });
                sb.AppendLine($"<script>var rules={objs.ToJson()};</script>");
                /*sb.AppendLine("<tbody>");
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
                    var rule = _filterOption.Rules.FirstOrDefault(x => x.CategoryName.Equals(log.Key, StringComparison.OrdinalIgnoreCase));
                    var traceCount = TraceCount;
                    if (rule is LoggerFilterRule _rule)
                    {
                        traceCount = _rule.TraceCount;
                    }
                    sb.AppendFormat("&nbsp;<input value='{0}' name='traceCount'/></td></tr>",traceCount);
                    sb.AppendLine();
                }
                sb.AppendLine("<tbody></table>");
                */
                context.Response.StatusCode = 200;
                await context.Response.WriteAsync(html.Replace("{{url}}", SavePath).Replace("{{body}}", sb.ToString()));
            }
        }
    }
}
