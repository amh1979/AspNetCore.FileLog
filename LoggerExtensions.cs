/* ===============================================
* 功能描述：AspNetCore.Logging.LoggingExtensions
* 创 建 者：WeiGe
* 创建日期：1/2/2019 10:22:06 PM
* ===============================================*/
using AspNetCore.FileLog;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Internal;
using StaticFiles=Microsoft.AspNetCore.StaticFiles;
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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Server.Kestrel.Core.Internal.Http;
using Microsoft.AspNetCore.Http.Features;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Hosting.Builder;


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
        /// <param name="logAction">logAction</param>
        /// <para>e.g. .Logs or wwwroor/logs or C:/wwwroot/logs</para>
        public static IServiceCollection AddFileLog(this IServiceCollection services, Action<LogOptions> logAction = null)
        {
            if (!services.Any(x => x.ImplementationType == typeof(LOG.LoggerFactory)))
            {
                LogOptions logOptions = new LogOptions();
                logAction?.Invoke(logOptions);                
                services.Replace(ServiceDescriptor.Transient<IApplicationBuilderFactory, DefaultApplicationBuilderFactory>());
                //LoggerSettings.Format = logOptions.Format;
                LOG.LoggerFactory.ServiceCollection = services;
                if (string.IsNullOrEmpty(logOptions.LogDirectory))
                {
                    logOptions.LogDirectory = ".Logs";
                }
                services.AddHttpContextAccessor();
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
                        else {
                            environment.WebRootPath = _contentPath;
                        }
                    }
                    else
                    {
                        environment.WebRootPath = System.IO.Path.Combine(_config["ContentRoot"], "wwwroot");
                    }
                }

                if (Path.IsPathRooted(logOptions.LogDirectory))
                {
                    LoggerSettings.LogDirectory = logOptions.LogDirectory;
                }
                else
                {
                    LoggerSettings.LogDirectory = Path.Combine(_config["ContentRoot"], logOptions.LogDirectory);
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
                if (logOptions.LogAdapters.Count==0)
                {
                    logOptions.UseText();
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
                services.TryAddEnumerable(logOptions.LogAdapters);
                logOptions.LogAdapters.Clear();
                services.Replace(ServiceDescriptor.Singleton<ILoggerFactory, LOG.LoggerFactory>());
                services.Replace(ServiceDescriptor.Singleton<DiagnosticSource>(new DefaultDiagnosticListener()));
                if (services.IsHttpRequest())
                {
                    MarkdownFileMiddleware.SaveResourceFiles(environment.WebRootPath);
                }
                DefaultApplicationBuilderFactory.OnCreateBuilder(UseFileLog,logOptions);
                
            }
            return services;
            
        }
        static void UseFileLog(IApplicationBuilder app, object state)
        {
            var logOptions = state as LogOptions;
            LOG.LoggerFactory.ServiceProvider = app.ApplicationServices;
            if (app.ApplicationServices.GetService<ILoggerFactory>().GetType() != typeof(LOG.LoggerFactory))
            {
                throw new NotImplementedException($"Please use IServiceCollection.AddFileLog first.");
            }
            LoggerSettings.LogRequestPath = logOptions.LogRequestPath;
            LoggerSettings.SettingsPath = logOptions.SettingsPath;
            if (string.IsNullOrEmpty(LoggerSettings.LogRequestPath))
            {
                LoggerSettings.LogRequestPath = "/_Logs_/";
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

            fileOption.StaticFileOptions.OnPrepareResponse = PrepareResponse;
            fileOption.DirectoryBrowserOptions.Formatter = new HtmlDirectoryFormatter();
            app.UseFileServer(fileOption);
            app.UseWhen(context =>
            {
                return context.Request.Path.StartsWithSegments(LoggerSettings.SettingsPath);
            }, builder => builder.UseMiddleware<LoggerSettings>());
            app.UseWhen(context =>
            {
                return Regex.IsMatch(context.Request.Path, LoggerSettings.LogRequestPath + ".+\\.md$");
            }, builder => builder.UseMiddleware<MarkdownFileMiddleware>(LoggerSettings.LogDirectory));

            //app.UseWhen(context =>
            //{
            //    return context.Request.Path.StartsWithSegments( LoggerSettings.LogRequestPath);
            //}, builder => builder.UseFileServer(fileOption));
        }
        static bool IsHttpRequest(this IServiceCollection services)
        {
            return services.Any(x => x.ServiceType == typeof(Microsoft.AspNetCore.Hosting.Server.IServer));
        }

        static void PrepareResponse(StaticFiles.StaticFileResponseContext context)
        {
            if (ContentTypes.Contains(context.Context.Response.ContentType))
            {
                context.Context.Response.ContentType += "; charset=utf-8";
            }
        }
    }
}
