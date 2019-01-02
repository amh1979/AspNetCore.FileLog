/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerFilterConfigureOptions
* 创 建 者：WeiGe
* 创建日期：1/2/2019 10:00:17 PM
* ===============================================*/

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace AspNetCore.FileLog
{
    internal class FileLoggerFilterConfigureOptions : IConfigureOptions<LoggerFilterOptions>
    { 

        public FileLoggerFilterConfigureOptions(IConfiguration configuration)
        {            
            FileLoggerSettings.JsonConfiguration=(configuration as ConfigurationRoot).Providers.FirstOrDefault()
                as Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider;
        }

        public void Configure(LoggerFilterOptions options)
        {
            if (FileLoggerSettings.JsonConfiguration == null)
            {
                return;
            }
            var source = FileLoggerSettings.JsonConfiguration?.Source;
            var file = source?.FileProvider.GetFileInfo(source.Path);
            if (file != null && !file.IsDirectory && file.Exists)
            {
                using (var reader = new System.IO.StreamReader(file.CreateReadStream()))
                {
                    var content = reader.ReadToEnd();
                    var jToken = JsonConvert.DeserializeObject<JToken>(content);
                    if (jToken != null)
                    {
                        try
                        {
                            var rules = jToken.SelectToken(FileLoggerSettings.RulesKey, false)
                                .ToObject<List<FileLoggerFilterRule>>();

                            foreach (var _rule in rules)
                            {
                                options.Rules.Add(_rule);
                            }
                            return;
                        }
                        catch
                        {

                        }
                    }

                }
                System.IO.File.WriteAllText(file.PhysicalPath, FileLoggerSettings.LoggingJsonContent);
                var rule = new FileLoggerFilterRule(string.Empty, "Default", LogLevel.Information, null);
                rule.LogType = LogType.All;
                options.Rules.Add(rule);
            }
            else
            {
                var rule = new FileLoggerFilterRule(FileLoggerSettings.DefaultProviderName, FileLoggerSettings.DefaultName, LogLevel.Information, null);
                rule.LogType = LogType.All;
                options.Rules.Add(rule);
            } 
        }
    }
}
