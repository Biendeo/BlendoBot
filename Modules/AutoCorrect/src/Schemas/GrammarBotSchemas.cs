using System.Collections.Generic;

namespace AutoCorrect.Schemas
{
#pragma warning disable 0649
#pragma warning disable CS8618

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

#pragma warning restore CS8618
#pragma warning restore 0648

}