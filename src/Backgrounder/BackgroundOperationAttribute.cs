namespace Backgrounder;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class BackgroundOperationAttribute : Attribute
{
    public string? ExtensionName { get; set; }

    public Type? ServiceType { get; set; }
}

#if NET7_0_OR_GREATER
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class BackgroundOperationAttribute<TService> : BackgroundOperationAttribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BackgroundOperationAttribute"/> class.
    /// </summary>
    public BackgroundOperationAttribute()
    {
        ServiceType = typeof(TService);
    }
}
#endif
