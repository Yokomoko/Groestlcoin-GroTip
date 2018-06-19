using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TipBot_BL.DiscordCommands;
using TipBot_BL.POCO;
using TipBot_BL.Properties;
using TipBot_BL.QT;


namespace TipBot_BL {
    public class DiscordClientNew {
        public string ApiKey => Settings.Default.DiscordToken;
        public ulong ChannelId;

        private const string CommandPrefix = "-";
        private const string TickerPrefix = "$";
        private System.Timers.Timer timer;

        public static DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;


        public async void RunBotAsync() {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection().AddSingleton(_client).AddSingleton(_commands).BuildServiceProvider();

            _client.Log += LogAsync;

            timer = new System.Timers.Timer {
                Interval = 1000,
                AutoReset = false
            };

            await RegisterCommandsAsync();

            //event subscriptions

            await _client.LoginAsync(TokenType.Bot, ApiKey);
            await _client.StartAsync();



            await Task.Delay(-1);
        }

        private async Task LogAsync(LogMessage log) {
            Console.WriteLine(log.ToString());
            // return Task.CompletedTask;
        }

        public async Task RegisterCommandsAsync() {
            _client.MessageReceived += ClientOnMessageReceived;
            //await _commands.AddModulesAsync(Assembly.GetEntryAssembly());
            await _commands.AddModulesAsync(typeof(DiscordCommands.TipModule).Assembly);
            await _commands.AddModulesAsync(typeof(DiscordCommands.TickerModule).Assembly);
            await _commands.AddModulesAsync(typeof(TextModule).Assembly);
        }




        private async Task ClientOnMessageReceived(SocketMessage socketMessage) {
            await _client.CurrentUser.ModifyAsync(x => x.Username = "GroTip");

            var message = socketMessage as SocketUserMessage;
            if (message == null || message.Author.IsBot) {
                return;
            }

            int argPos = 0;
            if (message.HasStringPrefix(CommandPrefix, ref argPos) || message.HasMentionPrefix(_client.CurrentUser, ref argPos)) {
                if (timer.Enabled) {
                    await message.Channel.SendMessageAsync("Woah! Slow down there! Too much too hard **too fast**");
                    return;
                }
                timer.Start();
                var context = new SocketCommandContext(_client, message);

                var result = await _commands.ExecuteAsync(context, argPos, _services);

                if (!result.IsSuccess) {
                    Console.WriteLine(result.ErrorReason);
                }
            }
            else if (message.HasStringPrefix(TickerPrefix, ref argPos)){
                if (message.Channel.Id != Settings.Default.PriceCheckChannel){

                    await message.Channel.SendMessageAsync($"Please use the <#{Settings.Default.PriceCheckChannel}> channel!");
                    return;
                }

                var msg = message.ToString().Split('$').Last();
                var tModule = new TickerModule();
                var embed = await tModule.GetPriceEmbed(msg);
                await message.Channel.SendMessageAsync("", false, embed);
            }
        }
    }
}