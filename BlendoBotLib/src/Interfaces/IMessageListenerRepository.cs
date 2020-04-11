namespace BlendoBotLib.Interfaces
{
    public interface IMessageListenerRepository
    {
        void Add(ulong guildId, IMessageListener listener);

        void Remove(ulong guildId, IMessageListener listener);
    }
}
