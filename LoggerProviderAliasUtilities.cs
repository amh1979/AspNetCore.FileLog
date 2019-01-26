/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerProviderAliasUtilities
* 创 建 者：WeiGe
* 创建日期：1/2/2019 11:33:31 PM
* ===============================================*/
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;


namespace AspNetCore.FileLog
{
    internal class LoggerProviderAliasUtilities
    {
        private const string AliasAttibuteTypeFullName = "Microsoft.Extensions.Logging.ProviderAliasAttribute";
        private const string AliasAttibuteAliasProperty = "Alias";

        internal static string GetAlias(Type providerType)
        {
            foreach (var attribute in providerType.GetTypeInfo().GetCustomAttributes(inherit: false))
            {
                if (attribute.GetType().FullName == AliasAttibuteTypeFullName)
                {
                    var valueProperty = attribute
                        .GetType()
                        .GetProperty(AliasAttibuteAliasProperty, BindingFlags.Public | BindingFlags.Instance);

                    if (valueProperty != null)
                    {
                        return valueProperty.GetValue(attribute) as string;
                    }
                }
            }
            return null;
        }
    }
}
