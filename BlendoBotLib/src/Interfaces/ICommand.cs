namespace BlendoBotLib.Interfaces
{
    using System.Threading.Tasks;
    using DSharpPlus.EventArgs;

    public interface ICommand
    {
        string Name { get; }

        string Description { get; }

        string Author { get; }

        string Version { get; }

        string GetUsage(string term);

        Task OnMessage(MessageCreateEventArgs e);
    }
}
