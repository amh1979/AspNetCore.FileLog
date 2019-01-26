/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerFactory
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:49:23 PM
* ===============================================*/
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MSLOG = Microsoft.Extensions.Logging;

namespace AspNetCore.FileLog
{
    internal class LoggerFactory : ILoggerFactory
    {
        private static readonly LoggerRuleSelector RuleSelector = new LoggerRuleSelector();       
        internal static readonly Dictionary<string, Logger> _loggers = new Dictionary<string, Logger>(StringComparer.Ordinal);
        private readonly List<ProviderRegistration> _providerRegistrations = new List<ProviderRegistration>();
        private static readonly object _sync = new object();
        private volatile bool _disposed;
        private IDisposable _changeTokenRegistration;
        internal static LoggerFilterOptions _filterOptions;
        IHostingEnvironment _environment;
        IHttpContextAccessor _httpContextAccessor;
        IEnumerable<LogAdapter> _logAdapters;
        internal LoggerExternalScopeProvider ScopeProvider { get; private set; }
        static internal IServiceCollection ServiceCollection { get; set; }
        static internal IServiceProvider ServiceProvider { get; set; }
        public LoggerFactory(IHostingEnvironment environment,
            IEnumerable<ILoggerProvider> providers,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IOptionsMonitor<MSLOG.LoggerFilterOptions> filterOption,
            IEnumerable<LogAdapter> logAdapters)
        {
            ServiceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
            if (string.IsNullOrEmpty(LoggerSettings.LogDirectory))
            {
                LoggerSettings.LogDirectory = Path.Combine(_environment.ContentRootPath ?? AppContext.BaseDirectory, ".Logs");
            }
            if (!Directory.Exists(LoggerSettings.LogDirectory))
            {
                Directory.CreateDirectory(LoggerSettings.LogDirectory);
            } 
            foreach (var provider in providers)
            {
                AddProviderRegistration(provider, dispose: false);
            }
            //File.AppendAllText(Path.Combine(Logger.LogPath, $"{DateTime.Now.ToString("yyyyMMdd")}.txt"),
            //    $"Create ILoggerFactory{Environment.NewLine}");
            RefreshFilters(filterOption.CurrentValue, string.Empty);
            _changeTokenRegistration = filterOption.OnChange(RefreshFilters);
            _logAdapters = logAdapters;
            //_logAdapter.FileDirectory = ;
        }

   
        private void RefreshFilters(MSLOG.LoggerFilterOptions filterOptions, string value)
        {
            lock (_sync)
            {
                _filterOptions = new LoggerFilterOptions(filterOptions);

                foreach (var logger in _loggers)
                {
                    var loggerInformation = logger.Value.Loggers;
                    var categoryName = logger.Key;

                    ApplyRules(loggerInformation, categoryName, 0, loggerInformation.Length);
                }
            }
        }

        public ILogger CreateLogger<T>()
            where T:class
        {
            return this.CreateLogger(typeof(T).FullName);
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }
            lock (_sync)
            {
                if (!_loggers.TryGetValue(categoryName, out var logger))
                {
                    logger = new Logger(this, _httpContextAccessor, _logAdapters)
                    {
                        Loggers = CreateLoggers(categoryName)
                    };
                    _loggers[categoryName] = logger;
                }
                return logger;
            }
        }

        public void AddProvider(ILoggerProvider provider)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(LoggerFactory));
            }

            AddProviderRegistration(provider, dispose: true);

            lock (_sync)
            {

                foreach (var logger in _loggers)
                {
                    var loggerInformation = logger.Value.Loggers;
                    var categoryName = logger.Key;

                    Array.Resize(ref loggerInformation, loggerInformation.Length + 1);
                    var newLoggerIndex = loggerInformation.Length - 1;

                    SetLoggerInformation(ref loggerInformation[newLoggerIndex], provider, categoryName);
                    ApplyRules(loggerInformation, categoryName, newLoggerIndex, 1);
                    logger.Value.Loggers = loggerInformation;
                }
            }
        }

        private void AddProviderRegistration(ILoggerProvider provider, bool dispose)
        {
            _providerRegistrations.Add(new ProviderRegistration
            {
                Provider = provider,
                ShouldDispose = dispose
            });

            if (provider is ISupportExternalScope supportsExternalScope)
            {
                if (ScopeProvider == null)
                {
                    ScopeProvider = new LoggerExternalScopeProvider();
                }

                supportsExternalScope.SetScopeProvider(ScopeProvider);
            }
        }

        private void SetLoggerInformation(ref LoggerInformation loggerInformation, ILoggerProvider provider, string categoryName)
        {
            loggerInformation.Logger = provider.CreateLogger(categoryName);
            loggerInformation.ProviderType = provider.GetType();
            loggerInformation.ExternalScope = provider is ISupportExternalScope;
            //loggerInformation.Rule = LoggerFilterRule.CreateDefault();
        }

        private LoggerInformation[] CreateLoggers(string categoryName)
        {
            var loggers = new LoggerInformation[_providerRegistrations.Count];
            for (int i = 0; i < _providerRegistrations.Count; i++)
            {
                SetLoggerInformation(ref loggers[i], _providerRegistrations[i].Provider, categoryName);
            }
            ApplyRules(loggers, categoryName, 0, loggers.Length);
            return loggers;
        }

        private void ApplyRules(LoggerInformation[] loggers, string categoryName, int start, int count)
        {
            for (var index = start; index < start + count; index++)
            {
                ref var loggerInformation = ref loggers[index];

                RuleSelector.Select(_filterOptions,
                    loggerInformation.ProviderType,
                    categoryName,
                    out var logType,
                    out var logScope,
                    out var minLevel,
                    out int traceCount,
                    out var filter);
                loggerInformation.Rule = new LoggerFilterRule(LoggerSettings.DefaultProviderName, categoryName, minLevel,filter);
                loggerInformation.Rule.LogScope = logScope;
                loggerInformation.Rule.LogType = logType;
                loggerInformation.Filter = filter;
                loggerInformation.Rule.TraceCount = traceCount;
            }
        }

        /// <summary>
        /// Check if the factory has been disposed.
        /// </summary>
        /// <returns>True when <see cref="Dispose()"/> as been called</returns>
        protected virtual bool CheckDisposed() => _disposed;

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;

                _changeTokenRegistration?.Dispose();

                foreach (var registration in _providerRegistrations)
                {
                    try
                    {
                        if (registration.ShouldDispose)
                        {
                            registration.Provider.Dispose();
                        }
                    }
                    catch
                    {
                        // Swallow exceptions on dispose
                    }
                }
            }
        }

        private struct ProviderRegistration
        {
            public ILoggerProvider Provider;
            public bool ShouldDispose;
        }
    }
}
