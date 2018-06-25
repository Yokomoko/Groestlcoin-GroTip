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
using TipBot_BL.FantasyPortfolio;
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
        private System.Timers.Timer fantasyTimer;
        private System.Timers.Timer fantasyTickerTimer;

        public static DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;


        public async void RunBotAsync() {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection().AddSingleton(_client).AddSingleton(_commands).BuildServiceProvider();

            _client.Log += LogAsync;

            fantasyTickerTimer = new System.Timers.Timer {
                Interval = 900000,
                AutoReset = true,
                Enabled = true
            };

            timer = new System.Timers.Timer {
                Interval = 250,
                AutoReset = false
            };

            fantasyTimer = new System.Timers.Timer {
                Interval = 10000,
                AutoReset = true,
                Enabled = true
            };

            fantasyTimer.Elapsed += FantasyTimerOnElapsed;
            fantasyTickerTimer.Elapsed += FantasyTickerTimerOnElapsed;

            await RegisterCommandsAsync();

            //event subscriptions

            await _client.LoginAsync(TokenType.Bot, ApiKey);
            await _client.StartAsync();



            await Task.Delay(-1);
        }

        private void FantasyTickerTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs) {
            Coin.UpdateCoinValues();
        }

        private void FantasyTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs) {
            var embed = FantasyPortfolioModule.GetLeaderboardEmbed();
            var winner = FantasyPortfolioModule.GetWinner();
            var additionalText = "";

            TimeSpan span = (Round.CurrentRoundEnd - DateTime.Now);

            if (Round.CurrentRoundEnd <= DateTime.Now) {
                if (FantasyPortfolioModule.GetPlayers(new FantasyPortfolio_DBEntities()).Any()) {
                    if (FantasyPortfolioModule.PrizePool > 0) {
                        additionalText = $"Congratulations <@{winner.UserId}>! You have won the fantasy portfolio and won {FantasyPortfolioModule.PrizePool} {Preferences.BaseCurrency}";
                        QTCommands.SendTip(_client.CurrentUser.Id, ulong.Parse(winner.UserId), FantasyPortfolioModule.PrizePool);
                    }
                    else {
                        additionalText = $"Congratulations <@{winner.UserId}>! You have won the fantasy portfolio! There was no prize.";
                    }
                }
                else {
                    embed.WithDescription("Round has finished! There were no participants in this round, so nobody wins!");
                }
                using (var context = new FantasyPortfolio_DBEntities()) {
                    Round round = new Round { RoundEnds = DateTime.Now.AddDays(Round.RoundDurationDays) };
                    context.Rounds.Add(round);
                    context.SaveChanges();
                }
                fantasyTimer.Interval = 3600000;
            }
            else if (span.TotalMilliseconds < fantasyTickerTimer.Interval) {
                //Set next interval to 500ms after the round ends
                fantasyTimer.Interval = span.TotalMilliseconds + 500;
            }
            else {
                // Set the next interval to 1 hour
                fantasyTimer.Interval = 3600000;
            }
            _client.GetGuild(Settings.Default.GuildId).GetTextChannel(Settings.Default.FantasyChannel).SendMessageAsync(additionalText, false, embed);
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
            await _commands.AddModulesAsync(typeof(FantasyPortfolioModule).Assembly);
        }




        private async Task ClientOnMessageReceived(SocketMessage socketMessage) {
            //Debug


            await _client.CurrentUser.ModifyAsync(x => x.Username = "GroTip");



            var message = socketMessage as SocketUserMessage;


#if DEBUG
            if (message.Channel.Id != 456084191927468033) {
                //Do Nothing;            
                return;
            }
#endif
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
            else if (message.HasStringPrefix(TickerPrefix, ref argPos)) {
                if (message.Channel.Id != Settings.Default.PriceCheckChannel) {

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