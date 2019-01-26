/* ===============================================
* 功能描述：AspNetCore.Logging.SystemExtionsions
* 创 建 者：WeiGe
* 创建日期：1/2/2019 11:46:25 PM
* ===============================================*/

using AspNetCore.FileLog;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace AspNetCore.FileLog
{

    internal static class SystemExtensions
    {/// <summary>
     /// Use Reflect get value of current <paramref name="value"/> by <paramref name="name"/>
     /// </summary>
     /// <param name="value">current value</param>
     /// <param name="name">field or property
     /// <para>e.g.: 'a.b.c'</para>
     /// </param>
     /// <returns></returns>
        public static object Value(this object value, string name)
        {
            if (value == null || value == DBNull.Value || string.IsNullOrEmpty(name))
            {
                return null;
            }
            var names = name.Split(new char[] { '.' }, StringSplitOptions.RemoveEmptyEntries);
            object _value = value;
            foreach (string n in names)
            {
                var @delegate = FastExpressions.CreateDelegate(_value);
                if (@delegate != null)
                {
                    _value = @delegate(_value, n, false, null);
                }
                if (_value == null)
                {
                    break;
                }
            }
            return _value;
        }
        static Regex FileRegex = new Regex("\\.[^\\.]+$", RegexOptions.IgnoreCase);
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static bool IsFile(this HttpContext context)
        {
            if (context != null && context.Request.Path.HasValue)
            {
                if (FileRegex.IsMatch(context.Request.Path.ToString()))
                {
                    return true;
                }
            }
            return false;
        }

        const string word_At = "at";
        const string inFileLineNum = "in {0}:line {1}";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="stackTrace"></param>
        /// <param name="maxCount"></param>
        /// <param name="needSkip"></param>
        /// <returns></returns>
        public static StackFrame[] GetFrames(this StackTrace stackTrace, int maxCount, Func<StackFrame, MethodBase, bool> needSkip = null)
        {
            List<StackFrame> frames;
            if (maxCount > 0)
            {
                frames = new List<StackFrame>(maxCount);
            }
            else
            {
                frames = new List<StackFrame>(stackTrace.FrameCount);
            }
            var count = 0;
            for (int iFrameIndex = 0; iFrameIndex < stackTrace.FrameCount; iFrameIndex++)
            {
                StackFrame sf = stackTrace.GetFrame(iFrameIndex);
                MethodBase mb = sf.GetMethod();
                if (mb != null)
                {
                    if (needSkip != null && needSkip(sf, mb))
                    {
                        continue;
                    }
                    if (maxCount > 0 && count >= maxCount)
                    {
                        break;
                    }
                    count++;
                    frames.Add(sf);
                }
            }
            return frames.ToArray();
        }
        public static StringBuilder GetString(this StackFrame[] stackFrames)
        {
            if (stackFrames == null || stackFrames.Length <= 0)
            {
                return default;
            }
            StringBuilder sb = new StringBuilder(255);
            bool displayFilenames = true;   // we'll try, but demand may fail
            bool fFirstFrame = true;            
            for (int iFrameIndex = 0; iFrameIndex < stackFrames.Length; iFrameIndex++)
            {
                StackFrame sf = stackFrames[iFrameIndex];
                MethodBase mb = sf.GetMethod();
                if (mb != null)
                {
                    // We want a newline at the end of every line except for the last
                    if (fFirstFrame)
                    {
                        fFirstFrame = false;
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", word_At);
                    }
                    else
                    {
                        sb.Append(Environment.NewLine);
                        sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", word_At);
                    }

                    Type t = mb.DeclaringType;
                    // if there is a type (non global method) print it
                    if (t != null)
                    {
                        sb.Append(t.FullName.Replace('+', '.'));
                        sb.Append(".");
                    }
                    sb.Append(mb.Name);

                    // deal with the generic portion of the method
                    if (mb is MethodInfo && ((MethodInfo)mb).IsGenericMethod)
                    {
                        Type[] typars = ((MethodInfo)mb).GetGenericArguments();
                        sb.Append("[");
                        int k = 0;
                        bool fFirstTyParam = true;
                        while (k < typars.Length)
                        {
                            if (fFirstTyParam == false)
                                sb.Append(",");
                            else
                                fFirstTyParam = false;

                            sb.Append(typars[k].Name);
                            k++;
                        }
                        sb.Append("]");
                    }

                    // arguments printing
                    sb.Append("(");
                    ParameterInfo[] pi = mb.GetParameters();
                    bool fFirstParam = true;
                    for (int j = 0; j < pi.Length; j++)
                    {
                        if (fFirstParam == false)
                            sb.Append(", ");
                        else
                            fFirstParam = false;

                        String typeName = "<UnknownType>";
                        if (pi[j].ParameterType != null)
                            typeName = pi[j].ParameterType.Name;
                        sb.Append(typeName + " " + pi[j].Name);
                    }
                    sb.Append(")");

                    // source location printing
                    if (displayFilenames && (sf.GetILOffset() != -1))
                    {
                        // If we don't have a PDB or PDB-reading is disabled for the module,
                        // then the file name will be null.
                        String fileName = null;

                        // Getting the filename from a StackFrame is a privileged operation - we won't want
                        // to disclose full path names to arbitrarily untrusted code.  Rather than just omit
                        // this we could probably trim to just the filename so it's still mostly usefull.
                        try
                        {
                            fileName = sf.GetFileName();
                        }
                        catch (SecurityException)
                        {
                            // If the demand for displaying filenames fails, then it won't
                            // succeed later in the loop.  Avoid repeated exceptions by not trying again.
                            displayFilenames = false;
                        }

                        if (fileName != null)
                        {
                            // tack on " in c:\tmp\MyFile.cs:line 5"
                            sb.Append(' ');
                            sb.AppendFormat(CultureInfo.InvariantCulture, inFileLineNum, fileName, sf.GetFileLineNumber());
                        }
                    }
                }
            }
            sb.Append(Environment.NewLine);
            return sb;
        }
        public static StringBuilder GetString(this StackTrace stackTrace, Func<StackFrame, MethodBase, bool> needSkip = null)
        {
            StringBuilder sb = new StringBuilder(255);
            if (stackTrace == null || stackTrace.FrameCount <= 0)
            {
                return default(StringBuilder);
            }
            bool displayFilenames = true;   // we'll try, but demand may fail


            bool fFirstFrame = true;
            for (int iFrameIndex = 0; iFrameIndex < stackTrace.FrameCount; iFrameIndex++)
            {
                StackFrame sf = stackTrace.GetFrame(iFrameIndex);
                MethodBase mb = sf.GetMethod();
                if (mb != null)
                {
                    if (needSkip != null && needSkip(sf, mb))
                    {
                        continue;
                    }
                    // We want a newline at the end of every line except for the last
                    if (fFirstFrame)
                    {
                        fFirstFrame = false;
                        sb.AppendFormat(CultureInfo.InvariantCulture, "{0} ", word_At);
                    }
                    else
                    {
                        sb.Append(Environment.NewLine);
                        sb.AppendFormat(CultureInfo.InvariantCulture, "   {0} ", word_At);
                    }

                    Type t = mb.DeclaringType;
                    // if there is a type (non global method) print it
                    if (t != null)
                    {
                        sb.Append(t.FullName.Replace('+', '.'));
                        sb.Append(".");
                    }
                    sb.Append(mb.Name);

                    // deal with the generic portion of the method
                    if (mb is MethodInfo && ((MethodInfo)mb).IsGenericMethod)
                    {
                        Type[] typars = ((MethodInfo)mb).GetGenericArguments();
                        sb.Append("[");
                        int k = 0;
                        bool fFirstTyParam = true;
                        while (k < typars.Length)
                        {
                            if (fFirstTyParam == false)
                                sb.Append(",");
                            else
                                fFirstTyParam = false;

                            sb.Append(typars[k].Name);
                            k++;
                        }
                        sb.Append("]");
                    }

                    // arguments printing
                    sb.Append("(");
                    ParameterInfo[] pi = mb.GetParameters();
                    bool fFirstParam = true;
                    for (int j = 0; j < pi.Length; j++)
                    {
                        if (fFirstParam == false)
                            sb.Append(", ");
                        else
                            fFirstParam = false;

                        String typeName = "<UnknownType>";
                        if (pi[j].ParameterType != null)
                            typeName = pi[j].ParameterType.Name;
                        sb.Append(typeName + " " + pi[j].Name);
                    }
                    sb.Append(")");

                    // source location printing
                    if (displayFilenames && (sf.GetILOffset() != -1))
                    {
                        // If we don't have a PDB or PDB-reading is disabled for the module,
                        // then the file name will be null.
                        String fileName = null;

                        // Getting the filename from a StackFrame is a privileged operation - we won't want
                        // to disclose full path names to arbitrarily untrusted code.  Rather than just omit
                        // this we could probably trim to just the filename so it's still mostly usefull.
                        try
                        {
                            fileName = sf.GetFileName();
                        }
                        catch (SecurityException)
                        {
                            // If the demand for displaying filenames fails, then it won't
                            // succeed later in the loop.  Avoid repeated exceptions by not trying again.
                            displayFilenames = false;
                        }

                        if (fileName != null)
                        {
                            // tack on " in c:\tmp\MyFile.cs:line 5"
                            sb.Append(' ');
                            sb.AppendFormat(CultureInfo.InvariantCulture, inFileLineNum, fileName, sf.GetFileLineNumber());
                        }
                    }
                }
            }
            sb.Append(Environment.NewLine);
            return sb;
        }
        private static JsonSerializer CreateDefault()
        {
            var serializer = JsonSerializer.CreateDefault();
            if (serializer.ContractResolver == null)
            {
                serializer.ContractResolver = new DefaultContractResolver { NamingStrategy = new CamelCaseNamingStrategy(true, true) };
            }
            serializer.DateFormatString = "yyyy/MM/dd HH:mm:ss";
            serializer.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
            //serializer.StringEscapeHandling = StringEscapeHandling.EscapeHtml;
            return serializer;
        }
        public static string ToJson(this object value)
        {
            StringWriter stringWriter = new StringWriter(new StringBuilder(256));
            using (JsonTextWriter jsonTextWriter = new JsonTextWriter(stringWriter))
            {
                CreateDefault().Serialize(jsonTextWriter, value);
            }
            return stringWriter.ToString();
        }
    }

 
}
namespace System
{    
    
    /// <summary>
     /// 
     /// </summary>
    public enum ExceptionType
    {
        /// <summary>
        /// 
        /// </summary>
        All,
        /// <summary>
        /// 
        /// </summary>
        Message,
        /// <summary>
        /// 
        /// </summary>
        Details,
    }
    /// <summary>
    /// 
    /// </summary>
    public static class ExceptionExtensions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public static StringBuilder GetString(this Exception exception, ExceptionType type = ExceptionType.All)
        {
            StringBuilder _message = new StringBuilder();
            while (exception != null)
            {
                switch (type)
                {
                    case ExceptionType.All:
                        _message.Insert(0, $"{exception.GetType().Name}=> {exception.Message}{Environment.NewLine}{exception.StackTrace}{Environment.NewLine}");
                        break;
                    case ExceptionType.Details:
                        _message.Insert(0, $"{exception.StackTrace}{Environment.NewLine}");
                        break;
                    case ExceptionType.Message:
                        _message.Insert(0, $"{exception.GetType().Name}=> {exception.Message}{Environment.NewLine}");
                        break;
                }

                exception = exception.InnerException;
            }
            return _message;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Exception Log(this Exception exception, string message = null)
        {
            if (exception == null)
            {
                return null;
            }
            Logger.Error(exception.GetType().FullName, message, exception);
            return exception;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Exception Log<T>(this Exception exception, string message = null)
            where T : class
        {
            if (exception == null)
            {
                return null;
            }
            Logger.Error<T>(message, exception);
            return exception;
        }
    }
    /// <summary>
    /// 
    /// </summary>
    public static class HttpContextExtensions
    {
        static readonly object _state = new object();
        internal const string Body = "_BODY";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public static string ReadBody(this HttpContext context)
        {
            if (context.Items[Body] != null)
            {
                return context.Items[Body] as string;
            }
            int counts = 0;
            string content=string.Empty;
            if (Interlocked.Increment(ref counts) == 1)
            {
                Stream body = context.Request.Body;
                if (!(body is FileBufferingReadStream stream))
                {
                    stream = (FileBufferingReadStream)(context.Request.Body = new FileBufferingReadStream(body, 30720, default(long?), _getTempDirectory));
                    context.Response.RegisterForDispose(stream);
                    if (context.Request.HasFormContentType)
                    {
                        var form = context.Request.ReadFormAsync().GetAwaiter().GetResult();
                    }
                }
                if (stream.IsDisposed)
                {
                    return string.Empty;
                }
                if (stream.Position > 0)
                {
                    stream.Seek(0, SeekOrigin.Begin);
                }
                StreamReader sr = new StreamReader(stream);
                content = sr.ReadToEnd();
                if (content.Length < 1024 * 1024 * 2)
                {
                    context.Items[Body] = content;
                }
                stream.Seek(0, SeekOrigin.Begin);               
            }
            Interlocked.Decrement(ref counts);
            return content;
        }
        private static readonly Func<string> _getTempDirectory = () => TempDirectory;

        private static string _tempDirectory;
        /// <summary>
        /// 
        /// </summary>
        public static string TempDirectory
        {
            get
            {
                if (_tempDirectory == null)
                {
                    string text = Environment.GetEnvironmentVariable("ASPNETCORE_TEMP") ?? Path.GetTempPath();
                    if (!Directory.Exists(text))
                    {
                        throw new DirectoryNotFoundException(text);
                    }
                    _tempDirectory = text;
                }
                return _tempDirectory;
            }
        }
    }
}