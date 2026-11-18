using System;

namespace TileGameCore
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class EnableIfAttribute : Attribute
    {
        public string ConditionName { get; private set; }

        public EnableIfAttribute(string conditionName) 
        { 
            ConditionName = conditionName;
        }
    }
}
