/* ===============================================
* 功能描述：AspNetCore.Logging.Logger
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:38:57 PM
* ===============================================*/

using AspNetCore.FileLog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Internal;
using Microsoft.AspNetCore.WebUtilities;
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
    public partial class Logger : ILogger
    {
        private readonly LOG.LoggerFactory _loggerFactory;

        private LoggerInformation[] _loggers;
        ILogAdapter _logAdapter;
        private int _scopeCount;
        IHttpContextAccessor _httpContextAccessor;
        internal Logger(LOG.LoggerFactory loggerFactory, IHttpContextAccessor httpContextAccessor, ILogAdapter logAdapter)
        {
            _loggerFactory = loggerFactory;
            _httpContextAccessor = httpContextAccessor;
            _logAdapter = logAdapter;
        }

        internal LoggerInformation[] Loggers
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
                        _logAdapter.Log(loggerInfo.Category, eventId.Name, logLevel, loggerInfo.LogType, 
                            formatter(state, exception), exception, _httpContextAccessor?.HttpContext);
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
