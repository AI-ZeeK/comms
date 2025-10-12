using System;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class HubMethodNameAttribute : Attribute
{
    public string Name { get; }

    public HubMethodNameAttribute(string name)
    {
        Name = name;
    }
}
