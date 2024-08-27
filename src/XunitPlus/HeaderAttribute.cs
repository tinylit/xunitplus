namespace XunitPlus;

/// <summary>
/// 请求头。
/// </summary>
[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public class HeaderAttribute : Attribute
{
    /// <summary>
    /// 请求头键。
    /// </summary>
    public string Key { get; }
    
    /// <summary>
    /// 请求头值。
    /// </summary>
    public string StringValues { get; }

    /// <summary>
    /// 设置请求头。
    /// </summary>
    /// <param name="key">请求头键。</param>
    /// <param name="stringValues">请求头值。</param>
    /// <exception cref="ArgumentException">参数'<paramref name="key"/>'为<see langword="null"/>或空字符！</exception>
    public HeaderAttribute(string key, string stringValues)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException(nameof(key));
        }

        Key = key;
        StringValues = stringValues;
    }
}