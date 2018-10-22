using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using Microsoft.Extensions.DependencyInjection;
using TipBot_BL.DiscordCommands;
using TipBot_BL.FantasyPortfolio;
using TipBot_BL.Properties;
using TipBot_BL.QT;
using Timer = System.Timers.Timer;


namespace TipBot_BL {
    public class DiscordClientNew {
        public string ApiKey => Settings.Default.DiscordToken;

        private const string CommandPrefix = "-";
        private const string TickerPrefix = "$";
        private Timer timer;
        private Timer fantasyTimer;
        private Timer fantasyTickerTimer;

        public static DiscordSocketClient _client;
        private CommandService _commands;
        private IServiceProvider _services;

        public async void RunBotAsync() {
            _client = new DiscordSocketClient();
            _commands = new CommandService();
            _services = new ServiceCollection().AddSingleton(_client).AddSingleton(_commands).BuildServiceProvider();

            _client.Log += LogAsync;

            fantasyTickerTimer = new Timer {
                Interval = 900000,
                AutoReset = true,
                Enabled = true
            };

            PriceUpdateController cont = new PriceUpdateController();
            Thread thread = new Thread(cont.Thread_ContinuousChecker) {
                IsBackground = true,
                Name = "Fantasy Price Updater"
            };
            thread.Start();


            timer = new Timer {
                Interval = 250,
                AutoReset = false
            };

            fantasyTickerTimer.Elapsed += FantasyTimerOnElapsed;
            await RegisterCommandsAsync();

            //event subscriptions

            await _client.LoginAsync(TokenType.Bot, ApiKey);
            await _client.StartAsync();

            fantasyTimer = new Timer {
                Interval = 60000,
                AutoReset = true,
                Enabled = true
            };
            Console.WriteLine("Timer Started");

            try {
                await _client.CurrentUser.ModifyAsync(x => x.Username = "GroestlTip");
                Console.WriteLine("Name Changed");
            }
            catch {

            }

            await Task.Delay(-1);
        }


        private void FantasyTimerOnElapsed(object sender, ElapsedEventArgs elapsedEventArgs) {
            var embed = FantasyPortfolioModule.GetLeaderboardEmbed();
            var winner = FantasyPortfolioModule.GetWinner();
            var additionalText = "";

            TimeSpan span = (Round.CurrentRoundEnd - DateTime.Now);

            if (Round.CurrentRoundEnd <= DateTime.Now) {
                Console.WriteLine($"Round {Round.CurrentRoundEnd} Finished. Winner: {winner.UserId}");
                using (var context = new FantasyPortfolio_DBEntities()) {
                    if (context.Leaderboards.Any(d => d.RoundId == Round.CurrentRound)) {
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
                    Round round = new Round { RoundEnds = DateTime.Now.AddDays(Round.RoundDurationDays) };
                    context.Rounds.Add(round);
                    context.SaveChanges();
                }
            }
            else if (span.TotalMilliseconds < fantasyTickerTimer.Interval) {
                //Set next interval to 5000ms after the round ends
                fantasyTimer.Interval = span.TotalMilliseconds + 5000;
                Console.WriteLine("Next fantasy interval set to 5 seconds");
            }
            else {
                // Set the next interval to 2 hours
                fantasyTickerTimer.Interval = 7200000;
                Console.WriteLine("Next fantasy interval set to 2 hours");
            }
            _client.GetGuild(Settings.Default.GuildId).GetTextChannel(Settings.Default.FantasyChannel).SendMessageAsync(additionalText, false, embed);


        }



        private async Task LogAsync(LogMessage log) {
            Console.WriteLine(log.ToString());
        }

        public async Task RegisterCommandsAsync() {
            _client.MessageReceived += ClientOnMessageReceived;
            await _commands.AddModulesAsync(typeof(TipModule).Assembly);
            await _commands.AddModulesAsync(typeof(TickerModule).Assembly);
            await _commands.AddModulesAsync(typeof(TextModule).Assembly);
            await _commands.AddModulesAsync(typeof(FantasyPortfolioModule).Assembly);
            await _commands.AddModulesAsync(typeof(BlockchainModule).Assembly);
        }


        private async Task ClientOnMessageReceived(SocketMessage socketMessage) {
            //Debug
            if (!(socketMessage is SocketUserMessage message) || message.Author.IsBot) {
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
                var embed = await TickerModule.GetPriceEmbed(msg);
                await message.Channel.SendMessageAsync("", false, embed);
            }
        }
    }

    public class PriceUpdateController {
        public async void Thread_ContinuousChecker() {
            while (true) {
                var count = 0;
                using (var context = new FantasyPortfolio_DBEntities()) {
                    if (context.Coins.Any()) {
                        foreach (var coin in context.Coins) {
                            if (coin.LastUpdated <= DateTime.Now.AddMinutes(-20)) {
                                try {
                                    await Coin.UpdateCoinValue(coin.Id);
                                }
                                catch (Exception ex) {
                                    Console.WriteLine($"Failed to Update {coin.TickerName} - {ex.Message}");
                                }
                                count++;
                            }
                            if (count == 25) {
                                Console.WriteLine($"{DateTime.Now} - Limit reached: Sleeping for a minute");
                                count = 0;
                                Thread.Sleep(60000);
                                Console.WriteLine($"{DateTime.Now} - Continuing");
                            }
                        }
                    }
                    Thread.Sleep(120000);
                }
            }
        }
    }
}