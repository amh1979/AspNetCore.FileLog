/* ===============================================
* 功能描述：AspNetCore.FileLog.LogOptions
* 创 建 者：WeiGe
* 创建日期：1/16/2019 12:18:31 AM
* ===============================================*/
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

namespace AspNetCore.FileLog
{
    /// <summary>
    /// log options
    /// </summary>
    public class LogOptions
    {
        /// <summary>
        /// request url
        /// <para>
        /// default: "/_Logs_"
        /// </para>
        /// </summary>
        public string LogRequestPath { get; set; } = "/_Logs_";
        /// <summary>
        /// settings url
        /// <para>
        /// default: "/_Settings_"
        /// </para>
        /// </summary>
        public string SettingsPath { get; set; } = "/_Settings_";
        /// <summary>
        /// Log file physical directory
        /// <para>
        /// default: ".Logs" of application path
        /// </para>
        /// </summary>
        public string LogDirectory { get; set; }
        /// <summary>
        /// 
        /// </summary>
        internal ICollection<ServiceDescriptor> LogAdapters { get; } = new List<ServiceDescriptor>();
        /// <summary>
        /// use text log adapter
        /// </summary>
        public void UseText()
        {
            if (!LogAdapters.Any(x => x.ImplementationType == typeof(TextLogAdapter) || x.ImplementationInstance is TextLogAdapter))
            {
                LogAdapters.Add(ServiceDescriptor.Singleton<LogAdapter, TextLogAdapter>());
            }
        }
        /// <summary>
        /// use markdown log adapter
        /// </summary>
        public void UseMarkdown()
        {
            if (!LogAdapters.Any(x => x.ImplementationType ==typeof( MarkdownLogAdapter)||x.ImplementationInstance is MarkdownLogAdapter))
            {
                LogAdapters.Add(ServiceDescriptor.Singleton<LogAdapter, MarkdownLogAdapter>());
            }
        }
        /// <summary>
        /// use log adapter
        /// </summary>
        /// <typeparam name="TAdapter"></typeparam>
        public void UseLogAdapter<TAdapter>()
            where TAdapter : LogAdapter
        {
            if (!LogAdapters.Any(x => x.ImplementationType == typeof(TAdapter) || x.ImplementationInstance is TAdapter))
            {
                LogAdapters.Add(ServiceDescriptor.Singleton<LogAdapter, TAdapter>());
            }
        }
    }
}
