/* ===============================================
* 功能描述：AspNetCore.Logging.Logger_1
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:39:57 PM
* ===============================================*/

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using LOG = AspNetCore.FileLog;
namespace System
{
    /// <summary>
    /// Logger
    /// </summary>
    public partial class Logger
    {
        static Type[] IgnoreTypes = new Type[] {
            typeof(Logger),
            typeof(Microsoft.Extensions.Logging.LoggerExtensions)
        };
        static readonly Regex NewLineRegex = new Regex("([\\n\\r]+)");
        static readonly string[] MessageTitle = new string[]
        {
            "Message:    ",
            "Path:       ",
            "Method:     ",
            "From IP:    ",
            "UserAgent:  ",
            "User:       ",
            "Query:      ",
            "Form:       ",
            "Body:       ",
            "Error:      ",
            "StackTrace: "
        };
        private static void WriteLog(string categoryName, EventId eventId, LogLevel level, string message, Exception exception)
        {
            //new StackTrace(true)
            if (string.IsNullOrEmpty(categoryName))
            {
                throw new ArgumentNullException(nameof(categoryName));
            }
            ILoggerFactory factory;
            if (LOG.LoggerFactory.ServiceProvider == null)
            {
                var services = LOG.LoggerFactory.ServiceCollection?? new ServiceCollection();
                services.AddFileLog();
                factory = services.BuildServiceProvider().GetService<ILoggerFactory>();
            }
            else
            {
                factory = LOG.LoggerFactory.ServiceProvider.GetService<ILoggerFactory>();
            }
            var logger = factory.CreateLogger(categoryName);
            logger.Log(level, eventId, exception, message, Array.Empty<object>());
        }
        /// <summary>
        /// writes a trace log message.
        /// <para>Level: 1</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>
        public static void Trace<T>(string message = null, Exception exception = null, EventId eventId = default(EventId))
            where T : class
        {
            WriteLog(typeof(T).FullName, eventId, LogLevel.Trace, message, exception);
        }
        /// <summary>
        /// writes a trace log message.
        /// <para>Level: 1</para>
        /// </summary>
        /// <param name="categoryName">log category name</param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>

        public static void Trace(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            WriteLog(categoryName, eventId, LogLevel.Trace, message, exception);
        }

        /// <summary>
        /// Formats and writes a debug log message.
        /// <para>Level: 1</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>
        public static void Debug<T>(string message = null, Exception exception = null, EventId eventId = default(EventId))
            where T : class
        {
            WriteLog(typeof(T).FullName, eventId, LogLevel.Debug, message, exception);
        }
        /// <summary>
        /// Formats and writes a debug log message.
        /// <para>Level: 1</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>
        public static void Debug(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            WriteLog(categoryName, eventId, LogLevel.Debug, message, exception);
        }
        /// <summary>
        /// writes an informational log message.
        /// <para>Level: 2</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>

        public static void Information<T>(string message = null, Exception exception = null, EventId eventId = default(EventId))
            where T : class
        {
            WriteLog(typeof(T).FullName, eventId, LogLevel.Information, message, exception);
        }
        /// <summary>
        /// writes an informational log message.
        /// <para>Level: 2</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>

        public static void Information(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            WriteLog(categoryName, eventId, LogLevel.Information, message, exception);
        }
        /// <summary>
        /// writes a warning log message.
        /// <para>Level: 3</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>
        public static void Warning<T>(string message = null, Exception exception = null, EventId eventId = default(EventId))
            where T : class
        {
            WriteLog(typeof(T).FullName, eventId, LogLevel.Warning, message, exception);
        }
        /// <summary>
        /// writes a warning log message.
        /// <para>Level: 3</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>

        public static void Warning(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            WriteLog(categoryName, eventId, LogLevel.Warning, message, exception);
        }
        /// <summary>
        /// writes an error log message.
        /// <para>Level: 4</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>
        public static void Error<T>(string message = null, Exception exception = null, EventId eventId = default(EventId))
            where T : class
        {
            WriteLog(typeof(T).FullName, eventId, LogLevel.Error, message, exception);
        }
        /// <summary>
        /// writes an error log message.
        /// <para>Level: 4</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>

        public static void Error(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            WriteLog(categoryName, eventId, LogLevel.Error, message, exception);
        }
        /// <summary>
        /// writes a critical log message.
        /// <para>Level: 5</para>
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>

        public static void Critical<T>(string message = null, Exception exception = null, EventId eventId = default(EventId))
              where T : class
        {
            WriteLog(typeof(T).FullName, eventId, LogLevel.Critical, message, exception);
        }
        /// <summary>
        /// writes a critical log message.
        /// <para>Level: 5</para>
        /// </summary>
        /// <param name="categoryName"></param>
        /// <param name="message">message</param>
        /// <param name="exception"><see cref="Exception"/></param>
        /// <param name="eventId"><see cref="EventId"/></param>
        public static void Critical(string categoryName, string message = null, Exception exception = null, EventId eventId = default(EventId))
        {
            WriteLog(categoryName, eventId, LogLevel.Critical, message, exception);
        }
    }
}
