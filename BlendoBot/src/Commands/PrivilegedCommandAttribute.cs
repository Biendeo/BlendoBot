namespace BlendoBot.Commands
{
    using System;

    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = true)]
    internal sealed class PrivilegedCommandAttribute : Attribute
    {
        public PrivilegedCommandAttribute()
        {
        }
    }
}
