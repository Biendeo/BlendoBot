using BlendoBot.CommandDiscovery;
using BlendoBot.Commands;
using BlendoBot.Commands.Admin;
using BlendoBot.ConfigSchemas;
using BlendoBotLib.Interfaces;
using DSharpPlus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace BlendoBot
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            await CreateHostBuilder(args).Build().RunAsync();
        }

        private static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureAppConfiguration((hostContext, config) =>
                {
                    // NOTE: by default this will load appconfig.json from the PWD
                })
                .ConfigureLogging(logging =>
                {
                    logging.AddConsole();
                    logging.AddFilter("OverwatchLeague", Microsoft.Extensions.Logging.LogLevel.Trace);
                })
                .UseDefaultServiceProvider((hostContext, options) =>
                {
                    options.ValidateOnBuild = true;
                })
                .ConfigureServices((hostContext, services) =>
                {
                    // Bind config sections
                    var config = hostContext.Configuration;
                    var blendoBotConfig = config.GetSection("BlendoBot").Get<BlendoBotConfig>();

                    // Configure external services to be injected
                    AdminV3.ConfigureServices(hostContext, services);
                    AutoCorrect.AutoCorrectCommand.ConfigureServices(hostContext, services);
                    CurrencyConverter.CurrencyConverter.ConfigureServices(hostContext, services);
                    MrPing.MrPing.ConfigureServices(hostContext, services);
                    RemindMe.RemindMe.ConfigureServices(hostContext, services);
                    UserTimeZone.UserTimeZone.ConfigureServices(hostContext, services);
                    Weather.Weather.ConfigureServices(hostContext, services);
                    WheelOfFortune.WheelOfFortune.ConfigureServices(hostContext, services);

                    // Command registry and commands
                    var commandRegistryBuilder = new CommandRegistryBuilder(services)
                        .RegisterGuildScoped<About>()
                        .RegisterGuildScoped<AdminV3>(InstantiationBehaviour.Eager)
                        .RegisterTransient<AutoCorrect.AutoCorrectCommand>()
                        .RegisterTransient<CurrencyConverter.CurrencyConverter>()
                        .RegisterTransient<DecimalSpiral.DecimalSpiral>()
						.RegisterGuildScoped<Help>()
                        .RegisterGuildScoped<MrPing.MrPing>()
                        .RegisterSingleton<OverwatchLeague.OverwatchLeague>(InstantiationBehaviour.Eager)
                        .RegisterTransient<Regional.Regional>()
                        .RegisterGuildScoped<RemindMe.RemindMe>(InstantiationBehaviour.Eager)
						.RegisterSingleton<Roll.Roll>()
                        .RegisterTransient<UserTimeZone.UserTimeZone>()
                        .RegisterTransient<Weather.Weather>()
                        .RegisterGuildScoped<WheelOfFortune.WheelOfFortune>();
                    services.AddSingleton<ICommandRegistryBuilder>(commandRegistryBuilder);

                    // Command router factory and manager
                    CommandRouterFactory.ConfigureServices(hostContext, services);
                    services.AddSingleton<ICommandRouterFactory, CommandRouterFactory>();
                    services.AddSingleton<ICommandRouterManager, CommandRouterManager>();

                    // Dynamic message listeners
                    services.AddSingleton<MessageListenerRepository>();
                    services.AddTransient<IMessageListenerRepository>(sp => sp.GetRequiredService<MessageListenerRepository>());
                    services.AddTransient<IMessageListenerEnumerable>(sp => sp.GetRequiredService<MessageListenerRepository>());

                    // Discord client service
                    var discordClient = new DiscordClient(new DiscordConfiguration
                    {
                        Token = blendoBotConfig.Token,
                        TokenType = TokenType.Bot
                    });
                    services.AddSingleton<DiscordClient>(discordClient);
                    services.AddSingleton<IDiscordClient, DiscordClientService>();

                    // Main bot service
                    services.AddSingleton<BlendoBotConfig>(blendoBotConfig);
                    services.AddHostedService<Bot>();
                })
                .UseConsoleLifetime();
    }
}
