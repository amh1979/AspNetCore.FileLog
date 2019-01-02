/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerContent
* 创 建 者：WeiGe
* 创建日期：1/2/2019 11:30:07 PM
* ===============================================*/

using System;
using System.Collections.Generic;
using System.Text;


namespace AspNetCore.FileLog
{
    internal class FileLoggerContent : IDisposable
    {
        public long Ticks { get; } = DateTime.Now.Ticks;
        public string Path { get; set; }
        public string Message { get; set; }
        public void Dispose()
        {
            this.Path = null;
            this.Message = null;
        }
    }
}
