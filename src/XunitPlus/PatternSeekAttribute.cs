using System;
using System.Collections.Generic;
using System.Text;

namespace XunitPlus
{
    /// <summary>
    /// 匹配查找，用于<see cref="Startup"/>中，限制查找依赖注入的范围。
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public sealed class PatternSeekAttribute : Attribute
    {
        /// <summary>
        /// 指定查找DLL过滤规则<see cref="Directory.GetFiles(string, string)"/>。
        /// </summary>
        /// <param name="pattern">DLL过滤规则。</param>
        /// <exception cref="ArgumentException">参数“<paramref name="pattern"/>”为空或空字符串！</exception>
        public PatternSeekAttribute(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
            {
                throw new ArgumentException($"“{nameof(pattern)}”不能为 null 或空。", nameof(pattern));
            }

            Pattern = pattern;
        }

        /// <summary>
        /// DLL过滤规则。
        /// </summary>
        public string Pattern { get; }
    }
}
