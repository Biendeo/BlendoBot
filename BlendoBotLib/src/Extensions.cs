namespace BlendoBotLib
{
    using System.Diagnostics.CodeAnalysis;
    using DSharpPlus.EventArgs;

    public static class Extensions
    {
        public static bool TryGetTerm(this MessageCreateEventArgs e, [NotNullWhen(returnValue: true)] out string term)
        {
            var msg = e.Message.Content;
            if (string.IsNullOrWhiteSpace(msg))
            {
                term = null!;
                return false;
            }

            term = e.Message.Content.Split(' ', 2)[0];
            return true;
        }
    }
}
