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
                Task.WaitAll(WriteToFile(Contents.ToList()));
            };
            AppDomain.CurrentDomain.UnhandledException += (object sender, UnhandledExceptionEventArgs e) =>
            {
                Logger.Error<AppDomain>($"{sender}; {e.ExceptionObject.ToJson()}");
            };
            System.Timers.Timer timer = new System.Timers.Timer(1000 * 0.8);
            timer.Elapsed += async (sender, e) =>
            {
                if (!Contents.IsEmpty)
                {
                    var list = new List<LoggerContent>();
                    while (!Contents.IsEmpty)
                    {
                        if (Contents.TryTake(out LoggerContent content))
                        {
                            list.Add(content);
                        }
                    }
                    await WriteToFile(list);
                    list = null;
                }
            };
            timer.Start();
        }
        static async Task WriteToFile(List<LoggerContent> contents)
        {
            if (contents.Count > 0)
            {
                var list = contents.GroupBy(t => t.PathHash).Select(t => new
                {
                    Path = t.Min(x => x.Path),
                    List = t.ToList()
                });

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
                        bool retreid = false;
                    RETRY:
                        try
                        {
                            using (var write = file.AppendText())
                            {
                                foreach (var f in content.List.OrderBy(t => t.Ticks))
                                {
                                    using (f)
                                    {
                                        write.WriteLine(f.Message);
                                    }
                                }
                                write.Flush();
                            }
                        }
                        catch
                        {
                            if (!retreid)
                            {
                                file = new FileInfo(System.IO.Path.Combine(file.DirectoryName, $"{System.IO.Path.GetFileNameWithoutExtension(file.Name)}_fail{file.Extension}"));
                                retreid = true;
                                goto RETRY;
                            }
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
            this.Message = message.Trim();
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
