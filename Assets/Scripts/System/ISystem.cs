using System;

[AttributeUsage(AttributeTargets.Method)]
public class InitDependency : Attribute
{
    public Type[] Types;

    public InitDependency(params Type[] DependantTypes)
    {
        Types = DependantTypes;
    }
}

public interface ISystem
{
    public abstract void Initialize();
    public abstract void Deinitialize();
}
