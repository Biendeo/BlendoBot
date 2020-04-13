namespace BlendoBot.CommandDiscovery
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Threading.Tasks;

    internal interface ICommandRouter
    {
        bool TryTranslateTerm(string term, [NotNullWhen(returnValue: true)] out Type commandType, bool includeDisabled = false);

        Task<string> RenameTerm(string termFrom, string termTo);

        Task<bool> EnableTerm(string term);

        Task<bool> DisableTerm(string term);

        ISet<string> GetDisabledTerms();

        ISet<string> GetEnabledTerms();
    }
}
