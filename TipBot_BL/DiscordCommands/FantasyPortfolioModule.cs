using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TipBot_BL.FantasyPortfolio;
using TipBot_BL.Properties;

namespace TipBot_BL.DiscordCommands {
    public class FantasyPortfolioModule : ModuleBase<SocketCommandContext> {
        public static decimal EntryFee => (decimal)0.00;

        private bool IsInFantasyChannel => Context.Channel.Id == Preferences.FantasyChannel;
        public static decimal PrizePool => (GetPlayers(new FantasyPortfolio_DBEntities()).Count * EntryFee) * (decimal)0.98;

        [Command("share")]
        public async Task Share() {
            var ports = Portfolio.Share(Context.User.Id.ToString());

            var sb = new StringBuilder();
            var embed = new EmbedBuilder();

            if (!ports.Any()) {
                sb.AppendLine($"You are not signed up to this round. Please ensure you have the entry fee of {EntryFee} {Preferences.BaseCurrency} and type `-join`");
                embed.WithTitle("Not Signed Up");
            }
            else {
                string usdValue = "";

                decimal totalAmount = 0;

                foreach (var p in ports) {
                    if (p.TickerId == -1) {
                        usdValue = p.CoinAmount.ToString();
                        totalAmount += p.CoinAmount;
                    }
                    else {
                        totalAmount += p.CoinAmount * Coin.GetTicker(p.TickerId).Result.PriceUSD;
                        sb.AppendLine($"**{Coin.GetTickerName(p.TickerId)}** - ${Math.Round(p.CoinAmount * Coin.GetTicker(p.TickerId).Result.PriceUSD, 2):N} ({p.CoinAmount} {Coin.GetTickerName(p.TickerId)})");
                    }
                }

                sb.AppendLine("**USD**: $" + usdValue);
                sb.AppendLine(Environment.NewLine + $"Total Value: ${totalAmount:N}");


                embed.Title = "Your Portfolio";
            }
            embed.Description = sb.ToString();

            await ReplyAsync("", false, embed);
        }

        [Command("sell")]
        public async Task Sell(string amount, string ticker) {
            var userId = Context.User.Id.ToString();
            var reason = "";
            if (!CheckBalance(amount, ticker, out reason)) {
                await ReplyAsync($"Error Selling {ticker.ToUpper()} - {reason}");
                return;
            }
            using (var context = new FantasyPortfolio_DBEntities()) {
                decimal amountDec;
                if (decimal.TryParse(amount, out amountDec)) {
                    amountDec = Math.Round(amountDec, 8);
                    var coin = await Coin.GetTicker(ticker);
                    var usdAmount = context.Portfolios.FirstOrDefault(d => d.RoundId == Round.CurrentRound && d.UserId == userId && d.TickerId == -1);

                    var portCoin = context.Portfolios.FirstOrDefault(d => d.RoundId == Round.CurrentRound && d.UserId == userId && d.TickerId == coin.TickerId);

                    var roundedAmount = Math.Round(amountDec * coin.PriceUSD, 8);

                    if (portCoin == null) {
                        portCoin = new Portfolio();
                        portCoin.UserId = Context.User.Id.ToString();
                        portCoin.TickerId = coin.TickerId;
                        portCoin.RoundId = Round.CurrentRound;
                        portCoin.CoinAmount = 0;
                        context.Portfolios.Add(portCoin);
                    }
                    else {
                        context.Portfolios.Attach(portCoin);
                    }
                    portCoin.CoinAmount -= amountDec;
                    usdAmount.CoinAmount = usdAmount.CoinAmount + roundedAmount;
                    try {
                        context.SaveChanges();
                    }
                    catch (Exception e) {
                        await ReplyAsync(e.Message + Environment.NewLine + Environment.NewLine + e.InnerException.Message);
                        return;
                    }

                    await ReplyAsync($"Successfully sold {amountDec} {ticker.ToUpper()}");
                    return;
                }
            }
            await ReplyAsync("Error");
        }


        [Command("buy")]
        public async Task Buy(string amount, string ticker) {
            var userId = Context.User.Id.ToString();
            var reason = "";
            if (!CheckBalance(amount, "USD", out reason)) {
                await ReplyAsync($"Error Buying {ticker.ToUpper()} - {reason}");
                return;
            }
            using (var context = new FantasyPortfolio_DBEntities()) {
                decimal amountDec;
                if (decimal.TryParse(amount, out amountDec)) {
                    amountDec = Math.Round(amountDec, 0);
                    var coin = await Coin.GetTicker(ticker);
                    var usdAmount = context.Portfolios.FirstOrDefault(d => d.RoundId == Round.CurrentRound && d.UserId == userId && d.TickerId == -1);

                    var portCoin = context.Portfolios.FirstOrDefault(d => d.RoundId == Round.CurrentRound && d.UserId == userId && d.TickerId == coin.TickerId);

                    var roundedAmount = Math.Round(amountDec / coin.PriceUSD, 8);

                    if (portCoin == null) {
                        portCoin = new Portfolio();
                        portCoin.UserId = Context.User.Id.ToString();
                        portCoin.TickerId = coin.TickerId;
                        portCoin.RoundId = Round.CurrentRound;
                        portCoin.CoinAmount = 0;
                        context.Portfolios.Add(portCoin);
                    }
                    else {
                        context.Portfolios.Attach(portCoin);
                    }
                    portCoin.CoinAmount += (decimal)roundedAmount;
                    usdAmount.CoinAmount = usdAmount.CoinAmount - amountDec;
                    try {
                        context.SaveChanges();
                    }
                    catch (Exception e) {
                        await ReplyAsync(e.Message + Environment.NewLine + Environment.NewLine + e.InnerException.Message);
                        return;
                    }

                    await ReplyAsync($"Successfully bought {roundedAmount} {ticker.ToUpper()}");
                    return;
                }
            }
            await ReplyAsync("Error");
        }

        [Command("leaderboard")]
        public async Task Leaderboard() {
            var embed = GetLeaderboardEmbed();
            await ReplyAsync("", false, embed);
        }

        public static Players GetWinner() {
            var players = GetPlayers(new FantasyPortfolio_DBEntities());
            return players.OrderByDescending(d => d.Balance).FirstOrDefault();
        }

        public static List<Players> GetPlayers(FantasyPortfolio_DBEntities dbContext) {
            List<Players> players = new List<Players>();
            var results = from p in dbContext.Portfolios
                          where p.RoundId == Round.CurrentRound
                          group p by p.UserId
                    into g
                          select new { UserId = g.Key, Balance = g.ToList() };
            foreach (var r in results) {
                var player = new Players { UserId = r.UserId };
                foreach (var b in r.Balance) {
                    if (b.CoinAmount > 0 && b.TickerId != -1) {
                        player.Balance += decimal.Round(b.CoinAmount * Coin.GetTicker(b.TickerId).Result.PriceUSD);
                    }
                    else {
                        player.Balance = player.Balance + b.CoinAmount;
                    }
                }
                players.Add(player);
            }
            return players;
        }

        public static EmbedBuilder GetLeaderboardEmbed() {
            using (var context = new FantasyPortfolio_DBEntities()) {
                List<Players> players = GetPlayers(context);

                var sb = new StringBuilder();
                var position = 1;

                if (players.Any()) {
                    foreach (var player in players.OrderByDescending(d => d.Balance)) {
                        sb.AppendLine($"{position} - {DiscordClientNew._client.GetUser(ulong.Parse(player.UserId)).Username} - ${player.Balance:N}");
                        position++;
                    }
                }
                else {
                    sb.AppendLine("No players currently participating in this round");
                }

                sb.AppendLine(Environment.NewLine);

                var embed = new EmbedBuilder();
                embed.Title = $"Groestlcoin Discord Leaderboard - Round {Round.CurrentRound}";
                embed.Description = sb.ToString();

                TimeSpan span = (Round.CurrentRoundEnd - DateTime.Now);

                var timeRemainingStr = "";

                if (span.Days > 0) {
                    timeRemainingStr += $"{span.Days} {(span.Days > 1 ? "Days" : "Day")} ";
                }

                if (span.Hours > 0) {
                    timeRemainingStr += $"{span.Hours} {(span.Hours > 1 ? "Hours" : "Hour")} ";
                }
                if (span.Hours < 1 && span.Minutes > 0) {
                    timeRemainingStr += $"{span.Minutes} {(span.Minutes > 1 ? "Minutes" : "Minute")}";
                }

                var endStr = "";
                endStr = string.IsNullOrEmpty(timeRemainingStr) ? "soon" : $"in {timeRemainingStr}";

                embed.WithFooter($"Round Ends {endStr} - Grand Prize: {EntryFee * context.Portfolios.Count(d => d.RoundId == Round.CurrentRound) * (decimal)0.98} {Preferences.BaseCurrency}");
                return embed;
            }
        }

        public bool CheckBalance(string amount, string ticker, out string reason) {
            reason = "";
            var userId = Context.User.Id.ToString();

            decimal amountDec;
            if (decimal.TryParse(amount, out amountDec)) {
                using (var context = new FantasyPortfolio_DBEntities()) {
                    int tickerId;

                    if (ticker.ToUpper() == "USD") {
                        tickerId = -1;
                    }
                    else {
                        var coin = Coin.GetTicker(ticker);
                        tickerId = coin.Result.TickerId;
                    }
                    var value = context.Portfolios.FirstOrDefault(d => d.UserId == userId && d.TickerId == tickerId && d.RoundId == Round.CurrentRound);
                    if (value != null && value.CoinAmount >= amountDec) {
                        return true;
                    }
                    reason = "Not enough balance";
                    return false;

                }
            }
            reason = "Invalid input";
            return false;
        }


        public class Players {
            public string UserId;
            public decimal Balance;
        }
    }
}