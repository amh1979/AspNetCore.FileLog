/* ===============================================
* 功能描述：AspNetCore.Extensions.Internal.ApplicationBuilderFactory
* 创 建 者：WeiGe
* 创建日期：10/16/2018 6:19:08 PM
* ===============================================*/

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Builder.Internal;
using Microsoft.AspNetCore.Hosting.Builder;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Text;


namespace Microsoft.AspNetCore.Builder
{
    internal class DefaultApplicationBuilderFactory : IApplicationBuilderFactory
    {
        static private ConcurrentBag<BuilderAction> _builderActions
            = new ConcurrentBag<BuilderAction>();
        private readonly IServiceProvider _serviceProvider;
        public DefaultApplicationBuilderFactory(IServiceProvider serviceProvider)
        {            
               _serviceProvider = serviceProvider;
        }
        //Func<IServiceProvider, List<ServiceDescriptor>> GetDescriptors;
        public IApplicationBuilder CreateBuilder(IFeatureCollection serverFeatures)
        {
            var builder = new ApplicationBuilder(_serviceProvider, serverFeatures);
            while (_builderActions!=null&&!_builderActions.IsEmpty)
            {
                BuilderAction action;
                if (_builderActions.TryTake(out action))
                {
                    action.Action?.Invoke(builder);
                    action.ActionWithState?.Invoke(builder, action.State);
                }
                else {
                    throw new Exception("TryTake");
                }
            }
            _builderActions = null;
            return builder;
        }

        public static void OnCreateBuilder(Action<IApplicationBuilder> builderAction)
        {
            if (builderAction != null)
            {
                _builderActions.Add(new BuilderAction { Action = builderAction });
            }
        }
        public static void OnCreateBuilder(Action<IApplicationBuilder, object> builderAction, object state)
        {
            if (state == null)
            {
                throw new ArgumentNullException(nameof(state), "Please use OnCreateBuilder(Action<IApplicationBuilder> builderAction).");
            }
            if (builderAction != null)
            {
                _builderActions.Add(new BuilderAction { ActionWithState = builderAction, State = state });
            }
        }
        class BuilderAction
        {
            public object State { get; set; }
            public Action<IApplicationBuilder, object> ActionWithState { get; set; }
            public Action<IApplicationBuilder> Action { get; set; }
        }
    }
   
}
