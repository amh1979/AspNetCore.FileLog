/* ===============================================
* 功能描述：AspNetCore.FileLog.LogType
* 创 建 者：WeiGe
* 创建日期：1/23/2019 12:44:32 PM
* ===============================================*/

using System;
using System.Collections.Generic;
using System.Text;


namespace AspNetCore.FileLog
{
    /// <summary>
    /// LogType
    /// </summary>
    public enum LogType
    {  
        /// <summary>
        /// 
        /// </summary>
        All=0,
        /// <summary>
        /// Txt
        /// </summary>
        Text = 1,
        /// <summary>
        /// Markdown
        /// </summary>
        Markdown = 2,
        /// <summary>
        /// Database
        /// </summary>
        Database = 4,
        /// <summary>
        /// 
        /// </summary>
        SystemEvent,
    }
}
