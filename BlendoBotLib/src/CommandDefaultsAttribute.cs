namespace BlendoBotLib
{
    using System;

    [AttributeUsage(System.AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public sealed class CommandDefaultsAttribute : Attribute
    {
        public CommandDefaultsAttribute(
            string defaultTerm,
            bool enabled = false)
        {
            this.DefaultTerm = defaultTerm;
            this.EnabledByDefault = enabled;
        }

        public string DefaultTerm { get; }
        public bool EnabledByDefault { get; }
    }
}
