namespace BlendoBotLib.Interfaces
{
    public interface IMessageListenerRepository
    {
        void Add(IMessageListener listener);

        void Remove(IMessageListener listener);
    }
}
