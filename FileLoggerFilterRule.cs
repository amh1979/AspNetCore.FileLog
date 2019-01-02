/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerFilterRule
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:54:18 PM
* ===============================================*/

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;


namespace AspNetCore.FileLog
{
    internal class FileLoggerFilterRule : Microsoft.Extensions.Logging.LoggerFilterRule
    {
        public FileLoggerFilterRule(string providerName, string categoryName, LogLevel? logLevel, Func<string, string, LogLevel, bool> filter)
            : base(providerName, categoryName, logLevel, filter)
        {
        }
        public LogType LogType { get; set; }
        public override string ToString()
        {
            return $"{nameof(ProviderName)}: '{ProviderName}', {nameof(CategoryName)}: '{CategoryName}', {nameof(LogLevel)}: '{LogLevel}', {nameof(LogType)}: '{LogType}', {nameof(Filter)}: '{Filter}'";
        }
    }
}
