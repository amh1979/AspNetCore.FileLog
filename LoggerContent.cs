/* ===============================================
* 功能描述：AspNetCore.Logging.LoggerContent
* 创 建 者：WeiGe
* 创建日期：1/2/2019 11:30:07 PM
* ===============================================*/

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AspNetCore.FileLog
{
    internal class LoggerContent : IDisposable
    {
        #region static write information to file
        internal static readonly ConcurrentBag<LoggerContent> Contents = new ConcurrentBag<LoggerContent>();
        static LoggerContent()
        {
            AppDomain.CurrentDomain.ProcessExit += (object sender, EventArgs e) =>
            {
                WriteToFile(Contents.ToList()).GetAwaiter().GetResult();
            };
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                Logger.Error<AppDomain>($"{sender}; {Newtonsoft.Json.JsonConvert.SerializeObject(e.ExceptionObject)}");
            };
            Task.Run(async () =>
            {
                while (true)
                {
                    System.Threading.Thread.Sleep(1000 * 2);
                    if (!Contents.IsEmpty)
                    {
                        var list = new List<LoggerContent>();
                        while (!Contents.IsEmpty)
                        {
                            if (Contents.TryTake(out LoggerContent content))
                            {
                                list.Add(content);
                            }
                            else
                            {
                                throw new Exception("ConcurrentBag.TryTake error");
                            }
                        }
                        await WriteToFile(list);
                        list = null;
                    }
                }
            });
        }
        static async Task WriteToFile(List<LoggerContent> contents)
        {
            if (contents.Count > 0)
            {
                var list = contents.GroupBy(t => t.PathHash).Select(t => new
                {
                    Path = t.Min(x => x.Path),
                    List = t.ToList()
                }).ToList();

                foreach (var content in list)
                {
                    await Task.Run(async () =>
                    {
                        await Task.Yield();
                        var file = new FileInfo(content.Path);
                        if (!file.Directory.Exists)
                        {
                            file.Directory.Create();
                        }
                        using (var write = file.AppendText())
                        {
                            foreach (var f in content.List.OrderBy(t => t.Ticks))
                            {
                                using (f)
                                {
                                    write.Write(f.Message);
                                }
                            }
                            write.Flush();
                        }
                    });
                }
            }
        }
        #endregion
        public LoggerContent(string path, string message)
        {
            if (string.IsNullOrEmpty(path) || string.IsNullOrEmpty(message))
            {
                return;
            }
            this.Path = path;
            this.Message = message;
            this.PathHash = this.Path.GetHashCode();
            Contents.Add(this);
        }
        public long Ticks { get; } = DateTime.Now.Ticks;
        public string Path { get; private set; }
        public int PathHash { get; }
        public string Message { get; private set; }
        public void Dispose()
        {
            this.Path = null;
            this.Message = null;
        }
    }
}
