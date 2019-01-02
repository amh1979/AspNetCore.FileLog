/* ===============================================
* 功能描述：AspNetCore.Logging.Logger
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:38:57 PM
* ===============================================*/

using AspNetCore.FileLog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using LOG = AspNetCore.FileLog;

namespace System
{
    /// <summary>
    /// Logger
    /// </summary>
    public partial class FileLogger : ILogger
    {
        private readonly LOG.FileLoggerFactory _loggerFactory;

        private FileLoggerInformation[] _loggers;

        private int _scopeCount;
        IHttpContextAccessor _httpContextAccessor;
        internal FileLogger(LOG.FileLoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor)
        {
            _loggerFactory = loggerFactory;
            _httpContextAccessor = httpContextAccessor;
        }

        internal FileLoggerInformation[] Loggers
        {
            get { return _loggers; }
            set
            {
                var scopeSize = 0;
                foreach (var loggerInformation in value)
                {
                    if (!loggerInformation.ExternalScope)
                    {
                        scopeSize++;
                    }
                }
                _scopeCount = scopeSize;
                _loggers = value;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="logLevel"></param>
        /// <param name="eventId"></param>
        /// <param name="state"></param>
        /// <param name="exception"></param>
        /// <param name="formatter"></param>
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state,
            Exception exception, Func<TState, Exception, string> formatter)
        {
            var loggers = Loggers;
            if (loggers == null)
            {
                return;
            }

            List<Exception> exceptions = null;
            bool loged = false;
            foreach (var loggerInfo in loggers)
            {
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }
                try
                {
                    loggerInfo.Logger.Log(logLevel, eventId, state, exception, formatter);
                    if (!loged)
                    {
                        loged = true;
                        LogToFile(loggerInfo, logLevel, eventId, state, exception, formatter);
                    }
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }
        }
        private void LogToFile<TState>(FileLoggerInformation loggerInfo, LogLevel logLevel,
            EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            string path = string.Empty;
            if (!string.IsNullOrEmpty(eventId.Name))
            {
                var index = eventId.Name.LastIndexOf('.');
                string name = eventId.Name;
                if (index > 0)
                {
                    name = eventId.Name.Remove(0, eventId.Name.LastIndexOf('.') + 1);
                }
                path = Path.Combine(FileLoggerSettings.LogDirectory, (exception != null ? LogLevel.Error : logLevel).ToString(),
                    loggerInfo.Category, name, $"{DateTime.Now.ToString("yyyyMMdd")}.txt");
            }
            else
            {
                path = Path.Combine(FileLoggerSettings.LogDirectory, (exception != null ? LogLevel.Error : logLevel).ToString(),
                  loggerInfo.Category, $"{DateTime.Now.ToString("yyyyMMdd")}.txt");
            }

            StringBuilder message = new StringBuilder();
            message.AppendLine($"#################### {DateTime.Now.ToString("MM/dd/yyyy HH:mm:ss.fffffff")} ###############");
            message.AppendLine($"{MessageTitle[0]}{NewLineRegex.Replace(formatter(state, exception).TrimStart('\n', '\r'), "$1\t    ")}");

            if (loggerInfo.LogType.HasFlag(LogType.HttpContext))
            {
                HttpContext context = _httpContextAccessor?.HttpContext;

                if (context != null)
                {
                    if (context.Request.Host.HasValue)
                    {
                        message.AppendLine($"{MessageTitle[1]}{context.Request.Scheme}://{context.Request.Host}{context.Request.Path}{context.Request.QueryString}");
                    }
                    message.AppendLine($"{MessageTitle[2]}{context.Request.Method}");
                    message.AppendLine($"{MessageTitle[3]}{context.Connection.RemoteIpAddress}");
                    if (context.Request.Headers["User-Agent"].Count > 0)
                    {
                        message.AppendLine($"{MessageTitle[4]}{context.Request.Headers["User-Agent"]}");
                    }
                    if (context.User.Identity.IsAuthenticated)
                    {
                        message.AppendLine($"{MessageTitle[5]}{context.User.Identity.Name}");
                    }
                    if (!context.IsFile() && (context.Request.ContentLength > 0 || context.Request.HasFormContentType))
                    {

                        if (context.Request.Query.Count > 0)
                        {
                            message.AppendLine($"{MessageTitle[6]}{string.Join(",", context.Request.Query.Select(kv => $"{kv.Key}={kv.Value}"))}");
                        }
                        if (context.Request.HasFormContentType)
                        {
                            var forms = context.Request.ReadFormAsync().GetAwaiter().GetResult();
                            if (forms.Count > 0)
                            {
                                message.AppendLine($"{MessageTitle[7]}{string.Join(",", forms.Select(kv => $"{kv.Key}={kv.Value}"))}");
                            }
                        }
                        else
                        {

                            if (context.Request.HasFormContentType)
                            {
                                var form = context.Request.ReadFormAsync().GetAwaiter().GetResult();
                            }
                            context.Request.EnableRewind();
                            using (StreamReader sr = new System.IO.StreamReader(context.Request.Body))
                            {
                                var body = sr.ReadToEnd();
                                if (!string.IsNullOrEmpty(body))
                                {
                                    message.AppendLine($"{MessageTitle[8]}{body.TrimStart('\n', '\r', ' ')}");
                                }
                            }
                        }
                    }
                }
            }
            if (exception != null)
            {
                message.AppendLine($"{MessageTitle[9]}{exception.GetString()}");
            }
            else if (loggerInfo.LogType.HasFlag(LogType.TraceStack))
            {
                var strack = new StackTrace(exception != null);
                message.AppendLine($"{MessageTitle[10]}{strack.GetString((x, y) => IgnoreTypes.Contains(y.DeclaringType))}");
            }
            LOG.FileLoggerFactory.Contents.Add(new FileLoggerContent { Path = path, Message = message.ToString() });
            message.Clear();
            message = null;
        }
       
        /// <summary>
        /// 
        /// </summary>
        /// <param name="logLevel"></param>
        /// <returns></returns>
        public bool IsEnabled(LogLevel logLevel)
        {
            var loggers = Loggers;
            if (loggers == null)
            {
                return false;
            }

            List<Exception> exceptions = null;
            foreach (var loggerInfo in loggers)
            {
                if (!loggerInfo.IsEnabled(logLevel))
                {
                    continue;
                }

                try
                {
                    if (loggerInfo.Logger.IsEnabled(logLevel))
                    {
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).",
                    innerExceptions: exceptions);
            }

            return false;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="TState"></typeparam>
        /// <param name="state"></param>
        /// <returns></returns>
        public IDisposable BeginScope<TState>(TState state)
        {
            var loggers = Loggers;

            if (loggers == null)
            {
                return NullScope.Instance;
            }

            var scopeProvider = _loggerFactory.ScopeProvider;
            var scopeCount = _scopeCount;

            if (scopeProvider != null)
            {
                // if external scope is used for all providers
                // we can return it's IDisposable directly
                // without wrapping and saving on allocation
                if (scopeCount == 0)
                {
                    return scopeProvider.Push(state);
                }
                else
                {
                    scopeCount++;
                }

            }

            var scope = new Scope(scopeCount);
            List<Exception> exceptions = null;
            for (var index = 0; index < loggers.Length; index++)
            {
                var loggerInformation = loggers[index];
                if (loggerInformation.ExternalScope)
                {
                    continue;
                }

                try
                {
                    scopeCount--;
                    // _loggers and _scopeCount are not updated atomically
                    // there might be a situation when count was updated with
                    // lower value then we have loggers
                    // This is small race that happens only on configuraiton reload
                    // and we are protecting from it by checkig that there is enough space
                    // in Scope
                    if (scopeCount >= 0)
                    {
                        var disposable = loggerInformation.Logger.BeginScope(state);
                        scope.SetDisposable(scopeCount, disposable);
                    }
                }
                catch (Exception ex)
                {
                    if (exceptions == null)
                    {
                        exceptions = new List<Exception>();
                    }

                    exceptions.Add(ex);
                }
            }

            if (scopeProvider != null)
            {
                scope.SetDisposable(0, scopeProvider.Push(state));
            }

            if (exceptions != null && exceptions.Count > 0)
            {
                throw new AggregateException(
                    message: "An error occurred while writing to logger(s).", innerExceptions: exceptions);
            }

            return scope;
        }

        private class Scope : IDisposable
        {
            private bool _isDisposed;

            private IDisposable _disposable0;
            private IDisposable _disposable1;
            private readonly IDisposable[] _disposable;

            public Scope(int count)
            {
                if (count > 2)
                {
                    _disposable = new IDisposable[count - 2];
                }
            }

            public void SetDisposable(int index, IDisposable disposable)
            {
                switch (index)
                {
                    case 0:
                        _disposable0 = disposable;
                        break;
                    case 1:
                        _disposable1 = disposable;
                        break;
                    default:
                        _disposable[index - 2] = disposable;
                        break;
                }
            }

            public void Dispose()
            {
                if (!_isDisposed)
                {
                    _disposable0?.Dispose();
                    _disposable1?.Dispose();

                    if (_disposable != null)
                    {
                        var count = _disposable.Length;
                        for (var index = 0; index != count; ++index)
                        {
                            if (_disposable[index] != null)
                            {
                                _disposable[index].Dispose();
                            }
                        }
                    }

                    _isDisposed = true;
                }
            }
        }

    }
}
