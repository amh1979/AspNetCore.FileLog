/* ===============================================
* 功能描述：AspNetCore.Logging.SystemExtionsions
* 创 建 者：WeiGe
* 创建日期：1/2/2019 11:46:25 PM
* ===============================================*/

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Security;
using System.Text;
using System.Text.RegularExpressions;

namespace System
{
    internal static class SystemExtionsions
    {
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
        internal static StringBuilder GetString(this Exception exception, bool showDetails = true)
        {
            StringBuilder _message = new StringBuilder();
            while (exception != null)
            {
                _message.Insert(0, $"{exception.GetType().Name}: {exception.Message}{Environment.NewLine}{(showDetails ? $"{exception.StackTrace}{Environment.NewLine}" : "")}{Environment.NewLine}");
                exception = exception.InnerException;
            }
            return _message;
        }
        const string word_At = "at";
        const string inFileLineNum = "in {0}:line {1}";
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

    }

    /// <summary>
    /// 
    /// </summary>
    public static class ExceptionExtionsions
    {
        /// <summary>
        /// 
        /// </summary>
        /// <param name="exception"></param>
        /// <param name="message"></param>
        /// <returns></returns>
        public static Exception Log(this Exception exception, string message = null)
        {
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
            var type = exception.GetType();
            Logger.Error<T>(message, exception);
            return exception;
        }
    }
}
