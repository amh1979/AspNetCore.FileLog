/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerFilterOptions
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:53:15 PM
* ===============================================*/
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;


namespace AspNetCore.FileLog
{
    internal class LoggerFilterOptions: Microsoft.Extensions.Logging.LoggerFilterOptions
    {
        public LoggerFilterOptions(Microsoft.Extensions.Logging.LoggerFilterOptions options)
        {
            this.MinLevel = LogLevel.Information;
            this.MiniScope = LogScope.None;
            this.LogType = LogType.Text;
            foreach (LoggerFilterRule rule in options.Rules)
            {
                if (rule.CategoryName.Equals(LoggerSettings.DefaultName))
                {
                    this.MiniScope = rule.LogScope;
                    this.MinLevel = rule.LogLevel ?? LogLevel.Information;
                    this.TraceCount = rule.TraceCount;
                    this.LogType = rule.LogType;
                }
                this.Rules.Add(rule);
            }
        }
        public LogType LogType { get; set; }
        public LogScope MiniScope { get; set; }
        public int TraceCount { get; set; }
    }
}
