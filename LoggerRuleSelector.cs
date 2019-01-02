/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerRuleSelector
* 创 建 者：WeiGe
* 创建日期：1/2/2019 9:51:28 PM
* ===============================================*/

using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;


namespace AspNetCore.FileLog
{
    internal class LoggerRuleSelector
    {
        public void Select(LoggerFilterOptions options, Type providerType, string category, out LogType logType,
            out LogLevel? minLevel, out Func<string, string, LogLevel, bool> filter)
        {
            filter = null;
            minLevel = options.MinLevel;
            logType = options.MiniType;
            // Filter rule selection:
            // 1. Select rules for current logger type, if there is none, select ones without logger type specified
            // 2. Select rules with longest matching categories
            // 3. If there nothing matched by category take all rules without category
            // 3. If there is only one rule use it's level and filter
            // 4. If there are multiple rules use last
            // 5. If there are no applicable rules use global minimal level

            var providerAlias = LoggerProviderAliasUtilities.GetAlias(providerType);
            LoggerFilterRule current = null;
            foreach (LoggerFilterRule rule in options.Rules)
            {
                if (IsBetter(rule, current, providerType.FullName, category)
                    || (!string.IsNullOrEmpty(providerAlias) && IsBetter(rule, current, providerAlias, category)))
                {
                    current = rule;
                }
            }

            if (current != null)
            {
                filter = current.Filter;
                minLevel = current.LogLevel;
                logType = current.LogType;
            }

        }


        private static bool IsBetter(LoggerFilterRule rule, LoggerFilterRule current, string logger, string category)
        {
            // Skip rules with inapplicable type or category
            if (rule.ProviderName != null && rule.ProviderName != logger)
            {
                return false;
            }

            if (rule.CategoryName != null && !category.StartsWith(rule.CategoryName, StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            if (current?.ProviderName != null)
            {
                if (rule.ProviderName == null)
                {
                    return false;
                }
            }
            else
            {
                // We want to skip category check when going from no provider to having provider
                if (rule.ProviderName != null)
                {
                    return true;
                }
            }

            if (current?.CategoryName != null)
            {
                if (rule.CategoryName == null)
                {
                    return false;
                }

                if (current.CategoryName.Length > rule.CategoryName.Length)
                {
                    return false;
                }
            }

            return true;
        }
    }
}
