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
    internal class FileLoggerFilterOptions: Microsoft.Extensions.Logging.LoggerFilterOptions
    {
        public FileLoggerFilterOptions(LoggerFilterOptions options)
        {
            this.MinLevel = LogLevel.Information;
            this.MiniType = LogType.None;
            foreach (FileLoggerFilterRule rule in options.Rules)
            {
                if (rule.CategoryName.Equals(FileLoggerSettings.DefaultName))
                {
                    this.MiniType = rule.LogType;
                    this.MinLevel = rule.LogLevel ?? LogLevel.Information;
                }
                this.Rules.Add(rule);
            }
        }
        public LogType MiniType { get; set; }
    }
}
