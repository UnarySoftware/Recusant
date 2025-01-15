using System;

public abstract class Singleton<T> where T : new()
{
    protected Singleton() { }

    private static T _instance;

    public static T Instance
    {
        get
        {
            _instance ??= new();
            return _instance;
        }
    }
}