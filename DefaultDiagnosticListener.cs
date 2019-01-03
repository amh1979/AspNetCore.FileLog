/* ===============================================
* 功能描述：AspNetCore.FileLog.DefaultDiagnosticListener
* 创 建 者：WeiGe
* 创建日期：1/3/2019 2:43:00 PM
* ===============================================*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;


namespace AspNetCore.FileLog
{
    internal class DefaultDiagnosticListener : DiagnosticListener
    {
        const string AfterActionResult = "Microsoft.AspNetCore.Mvc.AfterActionResult";
        public DefaultDiagnosticListener() : base("Microsoft.AspNetCore.Mvc")
        {

        }
        public DefaultDiagnosticListener(string name) : base(name)
        {

        }
        public override bool IsEnabled(string name)
        {
            switch (name)
            {
                case AfterActionResult:
                    return true;
            }
            var enabled = base.IsEnabled(name);
            return enabled;
        }
        public override void Write(string name, object value)
        {
            base.Write(name, value);

            switch (name)
            {
                case AfterActionResult:
                    var actionContext = (ActionContext)value?.Value("actionContext");
                    var result = value?.Value("result");
                    if (actionContext != null && result != null)
                    {
                        object obj = null;
                        if (result is ContentResult contentResult)
                        {
                            obj = new
                            {
                                ActionType = result.GetType().Name,
                                contentResult.Content,
                                contentResult.ContentType,
                                contentResult.StatusCode
                            };
                        }
                        else if (result is ObjectResult objectResult)
                        {
                            obj = new
                            {
                                ActionType = result.GetType().Name,
                                objectResult.Value
                            };
                        }
                        else if (result is PageResult pageResult)
                        {
                            obj = new
                            {
                                ActionType = result.GetType().Name,
                                pageResult.Page.Path,
                                pageResult.Page.Layout
                            };
                        }
                        else if (result is ViewResult viewResult)
                        {
                            obj = new
                            {
                                ActionType = result.GetType().Name,
                                viewResult.ViewName,
                                viewResult.Model
                            };
                        }
                        else if (result is JsonResult jsonResult)
                        {
                            obj = new
                            {
                                ActionType = result.GetType().Name,
                                jsonResult.Value
                            };
                        }
                        else
                        {
                            obj = new
                            {
                                ActionType = result.GetType().Name
                            };
                        }
                        if (obj != null)
                        {
                            Logger.Debug<ActionResult>(Newtonsoft.Json.JsonConvert.SerializeObject(obj));
                        }
                    }
                    break;
            }
        }
    }
}
