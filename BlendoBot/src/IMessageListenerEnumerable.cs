namespace BlendoBot
{
    using System.Collections.Generic;
    using BlendoBotLib.Interfaces;

    public interface IMessageListenerEnumerable
    {
        IEnumerable<IMessageListener> ForGuild(ulong guildId);
    }
}