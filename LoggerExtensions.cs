/* ===============================================
* 功能描述：AspNetCore.Logging.LoggingExtensions
* 创 建 者：WeiGe
* 创建日期：1/2/2019 10:22:06 PM
* ===============================================*/

using AspNetCore.FileLog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using LOG = AspNetCore.FileLog;
using Logging=Microsoft.Extensions.Logging;
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// 
    /// </summary>
    public static class LoggerExtensions
    {
        internal static readonly List<string> ContentTypes = new List<string> {
            "text/plain",
            "text/css",
            "text/javascript",
            "text/json",
            "text/html"
        };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="services"></param>
        /// <param name="logDirectory">log file path
        /// <para>e.g. .Logs or wwwroor/logs or C:/wwwroot/logs</para>
        /// </param>
        public static void AddFileLog(this IServiceCollection services, string logDirectory = ".Logs")
        {
            if (!services.Any(x => x.ImplementationType == typeof(LOG.LoggerFactory)))
            {
                LOG.LoggerFactory.ServiceCollection = services;
                if (string.IsNullOrEmpty(logDirectory))
                {
                    logDirectory = ".Logs";
                }
                services.AddHttpContextAccessor();
                services.AddSingleton<ILogAdapter, FileLogAdapter>();
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                Console.OutputEncoding = System.Text.Encoding.UTF8;
                var _config = services.FirstOrDefault(x => x.ServiceType == typeof(IConfiguration))?.ImplementationInstance as IConfiguration;
                if (_config == null)
                {
                    _config = new ConfigurationBuilder()
                        .AddEnvironmentVariables("ASPNETCORE_")
                        .SetBasePath(AppContext.BaseDirectory)
                        .Build();
                    _config[WebHostDefaults.ContentRootKey] = AppContext.BaseDirectory;
                    services.AddSingleton(_config);
                }
                if (string.IsNullOrEmpty(_config[WebHostDefaults.ContentRootKey]))
                {
                    _config[WebHostDefaults.ContentRootKey] = AppContext.BaseDirectory;
                }

                IHostingEnvironment environment = (IHostingEnvironment)services.FirstOrDefault(x => x.ServiceType == typeof(IHostingEnvironment))
                    ?.ImplementationInstance;
                if (environment == null)
                {
                    WebHostOptions options = new WebHostOptions(_config, Assembly.GetEntryAssembly().GetName().Name);
                    environment = new HostingEnvironment();
                    environment.Initialize(AppContext.BaseDirectory, options);
                    services.TryAddSingleton<IHostingEnvironment>(environment);
                }
                if (string.IsNullOrEmpty(environment.WebRootPath))
                {
                    var _contentPath = _config[WebHostDefaults.ContentRootKey];
                    var binIndex = _contentPath.LastIndexOf("\\bin\\");
                    if (binIndex > -1)
                    {
                        var contentPath = _contentPath.Substring(0, binIndex);
                        if (contentPath.IndexOf(environment.ApplicationName) > -1)
                        {
                            _config[WebHostDefaults.ContentRootKey] = contentPath;
                            environment.ContentRootPath = contentPath;
                            environment.WebRootPath = System.IO.Path.Combine(contentPath, "wwwroot");
                        }
                    }
                    else
                    {
                        environment.WebRootPath = System.IO.Path.Combine(_config["ContentRoot"], "wwwroot");
                    }
                }
                if (Path.IsPathRooted(logDirectory))
                {
                   LoggerSettings. LogDirectory = logDirectory;
                }
                else
                {
                    LoggerSettings.LogDirectory = Path.Combine(_config["ContentRoot"], logDirectory);
                }
                if (!Directory.Exists(LoggerSettings.LogDirectory))
                {
                    Directory.CreateDirectory(LoggerSettings.LogDirectory);
                }
                var path = Path.Combine(LoggerSettings.LogDirectory, LoggerSettings.LogJsonFileName);
                if (!File.Exists(path))
                {
                    File.AppendAllText(path, LoggerSettings.LoggingJsonContent);
                }
                ConfigurationBuilder configurationBuilder = new ConfigurationBuilder();
                configurationBuilder
                    .SetBasePath(environment.ContentRootPath)
                    .AddJsonFile(path, true, true);
                var configuration = configurationBuilder.Build();
                services.RemoveAll<ILoggerProviderConfigurationFactory>();
                services.RemoveAll(typeof(ILoggerProviderConfiguration<>));


                var type = typeof(ILoggerProviderConfigurationFactory).Assembly.DefinedTypes
                    .SingleOrDefault(t => t.Name == "LoggingConfiguration");
                services.RemoveAll(type);

                services.AddLogging(x =>
                {
                    x.AddConfiguration(configuration);
                    if (!x.Services.Any(t => t.ServiceType == typeof(ILoggerProvider)))
                    {
                        x.AddConsole();
                    }
                    x.Services.RemoveAll<IConfigureOptions<Logging.LoggerFilterOptions>>();
                    x.Services.AddSingleton<IConfigureOptions<Logging.LoggerFilterOptions>>(new LOG.LoggerFilterConfigureOptions(configuration));
                });
                services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, LOG.LoggerFactory>());
                services.Replace(ServiceDescriptor.Singleton<DiagnosticSource>(new DefaultDiagnosticListener()));
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="app"></param>
        /// <param name="browsePath"></param>
        /// <param name="settingsPath"></param>
        public static void UseFileLog(this IApplicationBuilder app,string browsePath= "/_Logs_",string settingsPath = "/_Settings_")
        {
            LOG.LoggerFactory.ServiceProvider = app.ApplicationServices;
            if (app.ApplicationServices.GetService<ILoggerFactory>().GetType() != typeof(LOG.LoggerFactory))
            {
                throw new NotImplementedException($"Please use IServiceCollection.AddFileLog first.");
            }
            LoggerSettings.LogRequestPath = browsePath;
            LoggerSettings.SettingsPath = settingsPath;
            if (string.IsNullOrEmpty(LoggerSettings.LogRequestPath))
            {
                LoggerSettings.LogRequestPath = "/_Logs_";
            }
            if (string.IsNullOrEmpty(LoggerSettings.SettingsPath))
            {
                LoggerSettings.SettingsPath = "/_Settings_";
            }
            var fileOption = new FileServerOptions
            {
                EnableDirectoryBrowsing = true,
                RequestPath = LoggerSettings.LogRequestPath,
                FileProvider = new PhysicalFileProvider(LoggerSettings.LogDirectory),
            };
            fileOption.StaticFileOptions.OnPrepareResponse =
                (context) =>
                {
                    if (ContentTypes.Contains(context.Context.Response.ContentType))
                    {
                        context.Context.Response.ContentType += "; charset=utf-8";
                    }
                };
            fileOption.DirectoryBrowserOptions.Formatter = new HtmlDirectoryFormatter();
            app.UseFileServer(fileOption);
            app.UseWhen(context => context.Request.Path.StartsWithSegments(LoggerSettings.SettingsPath),
                    builder => builder.UseMiddleware<LoggerSettings>());
        }
    }
}
