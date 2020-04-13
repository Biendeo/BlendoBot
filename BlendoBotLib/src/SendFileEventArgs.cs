namespace BlendoBotLib
{
    using DSharpPlus.Entities;
    using System;

#pragma warning disable CS8618

    /// <summary>
    /// An object that contains various arguments involved with sending a file.
    /// </summary>
    public class SendFileEventArgs : EventArgs
    {
        /// <summary>
        /// The path to the file to send. This is a local path, so the bot needs to probably create the file.
        /// </summary>
        public string FilePath { get; set; }
        /// <summary>
        /// The channel to send the file to. This is often the channel that sent the command that raised this event.
        /// </summary>
        public DiscordChannel Channel { get; set; }
        /// <summary>
        /// The type of this event. This can be given a useful naem for quick debugging.
        /// </summary>
        public string LogMessage { get; set; }
    }

#pragma warning restore CS8618

}
