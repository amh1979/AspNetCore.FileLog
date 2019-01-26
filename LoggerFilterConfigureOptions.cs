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
    internal class LoggerFilterConfigureOptions : IConfigureOptions<Microsoft.Extensions.Logging.LoggerFilterOptions>
    { 

        public LoggerFilterConfigureOptions(IConfiguration configuration)
        {            
            LoggerSettings.JsonConfiguration=(configuration as ConfigurationRoot).Providers.FirstOrDefault()
                as Microsoft.Extensions.Configuration.Json.JsonConfigurationProvider;
        }

        public void Configure(Microsoft.Extensions.Logging.LoggerFilterOptions options)
        {
            if (LoggerSettings.JsonConfiguration == null)
            {
                return;
            }
            var source = LoggerSettings.JsonConfiguration?.Source;
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
                            var rules = jToken.SelectToken(LoggerSettings.RulesKey, false)
                                .ToObject<List<LoggerFilterRule>>();

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
                System.IO.File.WriteAllText(file.PhysicalPath, LoggerSettings.LoggingJsonContent);
            }
            var rule = LoggerFilterRule.Default;
            rule.LogScope = LogScope.All;            
            options.Rules.Add(rule);
        }
    }
}
