using System;
using System.Collections.Generic;
using Sandbox.ModAPI.Ingame;

namespace IngameScript
{
    internal class Program : MyGridProgram
    {
        public static Dictionary<string, object> v = new Dictionary<string, object>
        {
            { "depth-limit", new Variable<float> { value = 80, parser = s => float.Parse(s) } }
        };
    }
}

public class Variable<T>
{
    public T value;
    public Func<string, T> parser;
    public void Set(string v) { value = parser(v); }
    public void Set<T1>(T1 v) { value = (T)(object)v; }
    public T1 Get<T1>() { return (T1)(object)value; }
}