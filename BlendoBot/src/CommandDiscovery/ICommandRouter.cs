namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Diagnostics.CodeAnalysis;

    public interface ICommandRouter
    {
        bool TryTranslateTerm(string term, [NotNullWhen(returnValue: true)] out Type commandType);
    }
}
