/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerFactory
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:49:23 PM
* ===============================================*/

using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.FileLog
{
    internal class FileLoggerFactory : ILoggerFactory
    {
        private static readonly FileLoggerRuleSelector RuleSelector = new FileLoggerRuleSelector();
        internal static readonly ConcurrentBag<FileLoggerContent> Contents = new ConcurrentBag<FileLoggerContent>();
        internal static readonly Dictionary<string, FileLogger> _loggers = new Dictionary<string, FileLogger>(StringComparer.Ordinal);
        private readonly List<ProviderRegistration> _providerRegistrations = new List<ProviderRegistration>();
        private static readonly object _sync = new object();
        private volatile bool _disposed;
        private IDisposable _changeTokenRegistration;
        internal static FileLoggerFilterOptions _filterOptions;
        IHostingEnvironment _environment;
        IHttpContextAccessor _httpContextAccessor;
        internal LoggerExternalScopeProvider ScopeProvider { get; private set; }
       static internal IServiceProvider ServiceProvider { get; private set; }
        public FileLoggerFactory(IHostingEnvironment environment,
            IEnumerable<ILoggerProvider> providers,
            IHttpContextAccessor httpContextAccessor,
            IServiceProvider serviceProvider,
            IOptionsMonitor<LoggerFilterOptions> filterOption)
        {
            ServiceProvider = serviceProvider;
            _httpContextAccessor = httpContextAccessor;
            _environment = environment;
            if (string.IsNullOrEmpty(FileLoggerSettings.LogDirectory))
            {
                FileLoggerSettings.LogDirectory = Path.Combine(_environment.ContentRootPath ?? AppContext.BaseDirectory, ".Logs");
            }
            if (!Directory.Exists(FileLoggerSettings.LogDirectory))
            {
                Directory.CreateDirectory(FileLoggerSettings.LogDirectory);
            }
            RunTask();
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) =>
            {
                WriteToFile(Contents.ToList());
            };
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                FileLogger.Error<AppDomain>($"{sender}; {Newtonsoft.Json.JsonConvert.SerializeObject( e.ExceptionObject)}");
            };
            foreach (var provider in providers)
            {
                AddProviderRegistration(provider, dispose: false);
            }
            //File.AppendAllText(Path.Combine(Logger.LogPath, $"{DateTime.Now.ToString("yyyyMMdd")}.txt"),
            //    $"Create ILoggerFactory{Environment.NewLine}");
            RefreshFilters(filterOption.CurrentValue, string.Empty);
            _changeTokenRegistration = filterOption.OnChange(RefreshFilters);
        }

        #region write log
        static bool isRunning = false;
        private void RunTask()
        {
            lock (_sync)
            {
                if (!isRunning)
                {
                    isRunning = true;
                    Task.Run(() =>
                    {
                        while (true)
                        {
                            System.Threading.Thread.Sleep(1000 * 2);
                            if (!Contents.IsEmpty)
                            {
                                var list = new List<FileLoggerContent>();
                                while (!Contents.IsEmpty)
                                {
                                    if (Contents.TryTake(out FileLoggerContent content))
                                    {
                                        list.Add(content);
                                    }
                                    else
                                    {
                                        throw new Exception("ConcurrentBag.TryTake error");
                                    }
                                }
                                WriteToFile(list);
                                list = null;
                            }
                        }
                    });
                }
            }
        }
        private void WriteToFile(List<FileLoggerContent> contents)
        {
            if (contents.Count > 0)
            {
                var list = contents.GroupBy(t => t.Path).Select(t => new
                {
                    Path = t.Key,
                    List = t.ToList()
                }).ToList();

                foreach (var a in list)
                {
                    var file = new FileInfo(a.Path);
                    if (!file.Directory.Exists)
                    {
                        file.Directory.Create();
                    }
                    using (var write = file.AppendText())
                    {
                        foreach (var f in a.List.OrderBy(t => t.Ticks))
                        {
                            using (f)
                            {
                                write.Write($"{f.Message}{Environment.NewLine}");
                            }
                        }
                        write.Flush();
                    }
                }
            }
        }

        #endregion
        private void RefreshFilters(LoggerFilterOptions filterOptions, string value)
        {
            lock (_sync)
            {
                _filterOptions = new FileLoggerFilterOptions(filterOptions);

                foreach (var logger in _loggers)
                {
                    var loggerInformation = logger.Value.Loggers;
                    var categoryName = logger.Key;

                    ApplyRules(loggerInformation, categoryName, 0, loggerInformation.Length);
                }
            }
        }

        public ILogger CreateLogger(string categoryName)
        {
            if (CheckDisposed())
            {
                throw new ObjectDisposedException(nameof(FileLoggerFactory));
            }
            lock (_sync)
            {
                if (!_loggers.TryGetValue(categoryName, out var logger))
                {
                    logger = new FileLogger(this, _httpContextAccessor)
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
                throw new ObjectDisposedException(nameof(FileLoggerFactory));
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

        private void SetLoggerInformation(ref FileLoggerInformation loggerInformation, ILoggerProvider provider, string categoryName)
        {
            loggerInformation.Logger = provider.CreateLogger(categoryName);
            loggerInformation.ProviderType = provider.GetType();
            loggerInformation.ExternalScope = provider is ISupportExternalScope;
            loggerInformation.LogType = LogType.HttpContext;
        }

        private FileLoggerInformation[] CreateLoggers(string categoryName)
        {
            var loggers = new FileLoggerInformation[_providerRegistrations.Count];
            for (int i = 0; i < _providerRegistrations.Count; i++)
            {
                SetLoggerInformation(ref loggers[i], _providerRegistrations[i].Provider, categoryName);
            }
            ApplyRules(loggers, categoryName, 0, loggers.Length);
            return loggers;
        }

        private void ApplyRules(FileLoggerInformation[] loggers, string categoryName, int start, int count)
        {
            for (var index = start; index < start + count; index++)
            {
                ref var loggerInformation = ref loggers[index];

                RuleSelector.Select(_filterOptions,
                    loggerInformation.ProviderType,
                    categoryName,
                    out var logType,
                    out var minLevel,
                    out var filter);

                loggerInformation.Category = categoryName;
                loggerInformation.MinLevel = minLevel;
                loggerInformation.LogType = logType;
                loggerInformation.Filter = filter;
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
