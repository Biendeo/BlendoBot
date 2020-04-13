# BlendoBot

## A Discord bot + modular framework written in C# for .NET Core 3.1

BlendoBot is a Discord bot intended for fun uses. The project is split into two major outputs; the BlendoBotLib DLL, which exposes interfaces for modules, and the BlendoBot executable that connects to Discord and operates with the modules.

### Download and run BlendoBot

You will require the ability to compile and run .NET Core 3.1 programs. Please follow [the instructions for your operating system](https://dotnet.microsoft.com/download/dotnet-core/3.1) on how to set up .NET Core for your command line. Alternatively, Windows and Mac OS users may choose to open the `BlendoBot.sln` file in Visual Studio 2019 and simply run the program from there.

If you can run `dotnet` from the commandline, simply:

- Clone the repository using:

```sh
git clone https://github.com/Biendeo/BlendoBot.git
```

- Change to the cloned directory with:

```sh
cd BlendoBot
```

- Create a file called `appsettings.json` and set the fields (more detail in [#Config](###Config)).
- Finally, simply run:

```sh
dotnet run --project BlendoBot/ -c Release
```

BlendoBot will now begin running. If everything is successful, you should see a message such as:

```log
info: BlendoBot.Bot[0]
      BlendoBot (2.0) is connected to Discord!
```

When BlendoBot connects to a Discord server (guild), you will also see a log with a list of the available commands as below:

```log
info: BlendoBot.CommandDiscovery.CommandRouterFactory[0]
      Creating command router for guild 000000000000000000. Supported command types: [About,AdminV3,Help]
```

### Configuration

In order to run BlendoBot, you will need an `appsettings.json` file in your root folder (i.e. with `BlendoBot.sln`). As a minimum, the file should contain the following section:

```json
{
    "BlendoBot": {
        "name": "BlendoBot",
        "version": "2.0",
        "description": "Smelling the roses",
        "author": "Biendeo",
        "activityType": "Watching",
        "activityName": "clouds",
        "token": "<insert Discord API token>"
    }
}
```

The fields are as follows:

- `name` - Determines the name of the bot. This is not the name used on Discord, just the internal program name that displays in `?about`.
- `version` - A version number that can be specified, to help identify what version the bot is using. This appears in `?about`.
- `description` - A brief description to help describe the version. This appears in `?about`.
- `author` - An author for the bot itself. This appears in `?about`.
- `activityType` - The activity that the bot will supposedly be doing. These are the few terms that can be used by a client to indicate that they are doing something on the side. Use whichever one is the most appropriate for your case. Valid terms are: `Playing`, `Streaming`, `ListeningTo`, and `Watching`.
- `activityName` - The name of the activity that is used write after the type.
- `token` - Your Discord bot token. This should never be disclosed or anyone can use anything to log in to Discord with your bot!

Note that other commands may require additional configuration sections in `appsettings.json`.

### Internal uses

BlendoBot comes with a few commands with the bot core. These commands are simply baked into the BlendoBot executable and are available to all running instances of BlendoBot.

#### `?about`

The about command simply tells you information about the current BlendoBot instance and individual command modules. Using it without any arguments tells you about the running client (set in `config.json`), and using it with the name of a command afterwards (e.g. `?about help`) tells you information about that module.

#### `?help`

The help command tells you how a command can be used. Using it without any arguments tells you a list of available commands. Supply a name of a command afterwards (e.g. `?help about`) and it'll tell you the help information about that command.

#### `?admin`

The admin command lets you as a guild administrator add other users to become BlendoBot admins, which also have access to this admin command, and any other command which requires an admin to operate. Currently, the admin command lets you control which commands are available on your guild.

### Developing your own modules

First, make a new C# project targetting *.NET Core 3.1* and output as a *Class Library*. These can be set in your project properties. You will also need to add a reference to `BlendoBotLib` to your project. This will now allow your program to compile properly and utilise the BlendoBot library to help it interact.

Modules may have as many commands as necessary; each command must implement the `ICommand` interface from `BlendoBotLib`. To make your commands available to BlendoBot, you must register them with the `CommandRegistryBuilder` in `Program.cs`, choosing one of three command persistence options:

- transient instantiation using `RegisterTransient` (a new instance of your command class per incoming call)
- guild-scoped instantiation using `RegisterGuildScoped` (one instance of your command class per guild)
- singleton instantiation using `RegisterSingleton` (a single global instance of your command class)

BlendoBot relies on .NET Generic Host's dependency injection framework to construct instances of commands. Some useful dependencies such as `IDiscordClient` and `ILogger` are pre-registered with the DI container; if your command requires additional services, register them in a `ConfigureServices` method called from the main program during host set-up.

### Contributing to the current source code

For the moment, just drop a pull request and I'll evaluate it.
