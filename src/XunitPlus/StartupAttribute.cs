namespace XunitPlus;

[AttributeUsage(AttributeTargets.Class)]
public sealed class StartupAttribute : Attribute
{
    public StartupAttribute(Type startupType)
    {
        StartupType = startupType;
    }

    public Type StartupType { get; }

    /// <summary>Default is false. If false, a isolated Startup will be created for the test class.</summary>
    public bool Shared { get; set; }
}