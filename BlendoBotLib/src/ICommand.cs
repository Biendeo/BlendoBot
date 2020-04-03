namespace BlendoBotLib
{
    using System.Threading.Tasks;
    using DSharpPlus.EventArgs;

    public interface ICommand
    {
        string Name { get; }

        string Description { get; }

        string Usage { get; }

        string Author { get; }

        string Version { get; }

        Task OnMessage(MessageCreateEventArgs e);
    }
}
