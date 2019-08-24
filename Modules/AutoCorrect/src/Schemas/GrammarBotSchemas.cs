using System.Collections.Generic;

namespace AutoCorrect.Schemas
{
    // Lots of unused field warnings for schemas
    #pragma warning disable 0649

    internal class GrammarBotResponse
    {
        public object software;

        public object language;

        public object warnings;

        public List<GrammarBotMatch> matches;
    }

    internal class GrammarBotMatch
    {
        public string message;

        public string shortMessage;

        public List<GrammarBotReplacementValue> replacements;

        public int offset;

        public int length;

        public object context;

        public string sentence;

        public object type;

        public object rule;
    }

    internal class GrammarBotReplacementValue
    {
        public string value;
    }
}