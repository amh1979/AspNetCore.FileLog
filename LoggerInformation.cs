/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerInformation
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:42:44 PM
* ===============================================*/

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;


namespace AspNetCore.FileLog
{
    [System.Diagnostics.DebuggerDisplay("{Category}")]
    internal struct LoggerInformation
    {
        public ILogger Logger { get; set; }
        public string Category { get; set; }
        public Type ProviderType { get; set; }
        public LogLevel? MinLevel { get; set; }
        public LogType LogType { get; set; }
        public Func<string, string, LogLevel, bool> Filter { get; set; }
        public bool ExternalScope { get; set; }
        public bool IsEnabled(LogLevel level)
        {
            if (MinLevel != null && level < MinLevel)
            {
                return false;
            }

            if (Filter != null)
            {
                return Filter(ProviderType.FullName, Category, level);
            }

            return true;
        }
    }
}
