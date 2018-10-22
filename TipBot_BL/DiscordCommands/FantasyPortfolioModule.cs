using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using TipBot_BL.FantasyPortfolio;

namespace TipBot_BL.DiscordCommands {
    public class FantasyPortfolioModule : ModuleBase<SocketCommandContext> {
        public static decimal EntryFee => (decimal)5;

        public static decimal PrizePool {
            get {
                using (var context = new FantasyPortfolio_DBEntities()) {
                    return (context.Leaderboards.Count(d => d.RoundId == Round.CurrentRound) * EntryFee) * (decimal)0.98;
                }
            }
        }

        [Command("ticker")]
        public async Task FantasyTicker(string ticker) {
            var tickerFormatted = ticker.ToUpper();
            var coins = await Coin.GetTickers(tickerFormatted);

            var embed = new EmbedBuilder();

            if (coins.Count > 1) {
                embed.WithTitle("Multiple Coins Found");
                foreach (var coin in coins) {
                    embed.AddInlineField(coin.TickerName, $"${decimal.Parse(Math.Round(coin.PriceUSD, 8).ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint)}");
                }
                embed.WithDescription("Multiple coins found with the same ticker, please use the name below.");
            }
            else if (coins.Count == 0) {
                embed.WithTitle("No coin found with this ticker");
                embed.WithDescription("No coin with this ticker has been found.");
            }
            else {
                var coin = coins.FirstOrDefault();
                embed.WithTitle($"Fantasy Price of {coin?.TickerName}");
                embed.WithDescription($"${decimal.Parse(Math.Round(coin.PriceUSD, 8).ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint)}");
                TimeSpan span = DateTime.Now - coin.LastUpdated;

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

                var endStr = string.IsNullOrEmpty(timeRemainingStr) ? "Just now" : $"{timeRemainingStr} ago";

                embed.WithFooter($"Last Updated: {endStr}");
            }
            await ReplyAsync("", false, embed);
        }

        [Command("help")]
        public async Task FantasyHelp() {
            var embed = new EmbedBuilder();
            embed.WithTitle("GroestlTip Fantasy Portfolio");

            embed.AddField("How to Play", "GroestlTip Fantasy Portfolio is a new way to trade cryptocurrencies. Newbies can enjoy the thrill of trading without risking the capital, and are able to learn the ropes easier, experienced traders are able to capitalise on their experience and trade their way to the top of the leaderboard for real prizes.");

            embed.AddField("-join", $"Join the Fantasy Portfolio Round. The current entry fee is {EntryFee} {Preferences.BaseCurrency}");
            embed.AddField("-share", "Share your current holdings with everyone else, or to yourself in a Direct Message");
            embed.AddField("-buy <amount (USD)|all> <ticker>", "Buy the currency of your choice. Any coin available on Coinmarketcap are availble to buy. The amount is in USD.");
            embed.AddField("-sell <amount|all> <ticker>", "Sell a currency that you own. The amount is in the currency you've provided.");
            embed.AddField("-ticker <ticker>", "Gets the current rate for a ticker for the fantasy portfolio.");
            embed.AddField("-leaderboard", "Show the current leaderboard standings along with the current prize pool. This is shared once an hour anyway and also when the round expires.");

            embed.WithFooter(Preferences.FooterText);
            embed.WithColor(Color.Blue);

            await ReplyAsync("", false, embed);

        }

        [Command("share")]
        public async Task Share() {
            var sb = new StringBuilder();
            var embed = new EmbedBuilder();


            using (var context = new FantasyPortfolio_DBEntities()) {
                var userId = Context.User.Id.ToString();

                var ports = context.LeaderboardTickers.Where(d => d.RoundId == Round.CurrentRound && d.UserId == userId);
                if (!ports.Any()) {
                    sb.AppendLine($"You are not signed up to this round. Please ensure you have the entry fee of {EntryFee} {Preferences.BaseCurrency} and type `-join`");
                    embed.WithTitle("Not Signed Up");
                }
                else {
                    string usdValue = "";
                    decimal totalAmount = 0;

                    foreach (var p in ports) {
                        totalAmount += p.DollarValue.GetValueOrDefault(0);
                        if (p.TickerName == "USD") {
                            if (!p.DollarValue.HasValue || p.DollarValue == 0) {
                                usdValue = "0.00";
                            }
                            else {
                                usdValue = p.DollarValue?.ToString("N") ?? "0.00";
                            }
                        }
                        else {
                            if (p.DollarValue == 0) continue;
                            sb.AppendLine($"**{p.TickerName}** - ${Math.Round(p.DollarValue.GetValueOrDefault(0), 2):N} ({p.CoinCount} {p.TickerName})");
                        }
                    }

                    sb.AppendLine("**USD**: $" + usdValue);
                    sb.AppendLine(Environment.NewLine + $"Total Value: ${totalAmount:N}");
                    embed.Title = "Your Portfolio";
                }
            }
            embed.Description = sb.ToString();
            await ReplyAsync("", false, embed);
        }

        [Command("sell")]
        public async Task Sell(string amount, string ticker) {
            var userId = Context.User.Id.ToString();
            if (amount != "all") {
                if (!CheckBalance(amount, ticker, out var reason)) {
                    await ReplyAsync($"Error Selling {ticker.ToUpper()} - {reason}");
                    return;
                }
            }
            using (var context = new FantasyPortfolio_DBEntities()) {
                var coins = await Coin.GetTickers(ticker);

                var embed = new EmbedBuilder();

                if (coins.Count > 1) {
                    embed.WithTitle("Multiple Coins Found");
                    foreach (var coin in coins) {
                        embed.AddInlineField(coin.TickerName, $"${decimal.Parse(Math.Round(coin.PriceUSD, 8).ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint)}");
                    }
                    embed.WithDescription("Multiple coins found with the same ticker, please use the name below.");
                    await ReplyAsync("", false, embed);
                    return;
                }
                else if (coins.Count == 0) {
                    embed.WithTitle("No coin found with this ticker");
                    embed.WithDescription("No coin with this ticker has been found.");
                    await ReplyAsync("", false, embed);
                    return;
                }
                else {
                    var coin = coins.FirstOrDefault();

                    var portCoin = context.Portfolios.FirstOrDefault(d => d.RoundId == Round.CurrentRound && d.UserId == userId && d.TickerId == coin.TickerId);

                    if (amount == "all") {
                        amount = portCoin?.CoinAmount.ToString();
                    }

                    if (decimal.TryParse(amount, out var amountDec)) {
                        amountDec = Math.Round(amountDec, 8);

                        var usdAmount = context.Portfolios.FirstOrDefault(d => d.RoundId == Round.CurrentRound && d.UserId == userId && d.TickerId == -1);

                        var roundedAmount = Math.Round(amountDec * coin.PriceUSD, 8) * (decimal)0.995;

                        if (portCoin == null) {
                            portCoin = new Portfolio {
                                UserId = Context.User.Id.ToString(),
                                TickerId = coin.TickerId,
                                RoundId = Round.CurrentRound,
                                CoinAmount = 0
                            };
                            context.Portfolios.Add(portCoin);
                        }
                        else {
                            context.Portfolios.Attach(portCoin);
                        }
                        portCoin.CoinAmount -= amountDec;
                        Debug.Assert(usdAmount != null, nameof(usdAmount) + " != null");
                        usdAmount.CoinAmount = usdAmount.CoinAmount + roundedAmount;
                        try {
                            context.SaveChanges();
                        }
                        catch (Exception e) {
                            await ReplyAsync(e.Message + Environment.NewLine + Environment.NewLine + e.InnerException?.Message);
                            return;
                        }
                        await ReplyAsync($"Successfully sold {amountDec} {ticker.ToUpper()}");
                        return;
                    }
                }
            }
            await ReplyAsync("Error connecting to database... Please try again");
        }


        [Command("buy")]
        public async Task Buy(string amount, string ticker) {
            var userId = Context.User.Id.ToString();

            if (amount != "all") {
                if (!CheckBalance(amount, "USD", out var reason)) {
                    await ReplyAsync($"Error Buying {ticker.ToUpper()} - {reason}");
                    return;
                }
            }

            using (var context = new FantasyPortfolio_DBEntities()) {
                var coins = await Coin.GetTickers(ticker);

                var embed = new EmbedBuilder();

                if (coins.Count > 1) {
                    embed.WithTitle("Multiple Coins Found");
                    foreach (var coin in coins) {
                        embed.AddInlineField(coin.TickerName, $"${decimal.Parse(Math.Round(coin.PriceUSD, 8).ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint)}");
                    }

                    embed.WithDescription("Multiple coins found with the same ticker, please use the name below.");
                    await ReplyAsync("", false, embed);
                    return;
                }
                else if (coins.Count == 0) {
                    embed.WithTitle("No coin found with this ticker");
                    embed.WithDescription("No coin with this ticker has been found.");
                    await ReplyAsync("", false, embed);
                    return;
                }
                else {
                    var coin = coins.FirstOrDefault();

                    if (coin.Volume24 != null && coin.Volume24 < 50000) {
                        embed.WithTitle("Volume is too low for this ticker");
                        embed.WithDescription($"Volume is too low for this ticker. Minimum volume: $50,000 (Current: ${coin.Volume24:N})");
                        await ReplyAsync("", false, embed);
                        return;
                    }

                    if (amount == "all") {
                        amount = context.Portfolios.FirstOrDefault(d => d.UserId == userId && d.TickerId == -1 && Round.CurrentRound == d.RoundId)?.CoinAmount.ToString();
                    }

                    if (decimal.TryParse(amount, out var amountDec)) {
                        amountDec = Math.Round(amountDec, 8);

                        var feeAmount = amountDec * (decimal)0.005;
                        amountDec = amountDec * (decimal)0.995;

                        var usdAmount = context.Portfolios.FirstOrDefault(d => d.RoundId == Round.CurrentRound && d.UserId == userId && d.TickerId == -1);

                        var usdAmount2 = usdAmount.CoinAmount;

                        var portCoin = context.Portfolios.FirstOrDefault(d => d.RoundId == Round.CurrentRound && d.UserId == userId && d.TickerId == coin.TickerId);

                        var roundedAmount = Math.Round(amountDec / coin.PriceUSD, 8);

                        if (roundedAmount == 0) {
                            await ReplyAsync("Buy failed: Amount bought would be 0.");
                            return;
                        }

                        if (portCoin == null) {
                            portCoin = new Portfolio {
                                UserId = Context.User.Id.ToString(),
                                TickerId = coin.TickerId,
                                RoundId = Round.CurrentRound,
                                CoinAmount = 0
                            };
                            context.Portfolios.Add(portCoin);
                        }
                        else {
                            context.Portfolios.Attach(portCoin);
                        }

                        portCoin.CoinAmount += roundedAmount;
                        if (usdAmount != null) usdAmount.CoinAmount = usdAmount.CoinAmount - amountDec - feeAmount;
                        try {
                            context.SaveChanges();
                        }
                        catch (Exception e) {
                            await ReplyAsync(e.Message + Environment.NewLine + Environment.NewLine + e.InnerException?.Message);
                            return;
                        }

                        await ReplyAsync($"Successfully bought {roundedAmount} {ticker.ToUpper()}");
                        return;
                    }
                }
            }
            await ReplyAsync("Error... Please try again.");
        }

        [Command("leaderboard")]
        public async Task Leaderboard() {
            var embed = GetLeaderboardEmbed();
            await ReplyAsync("", false, embed);
        }

        public static Leaderboard GetWinner() {
            using (var context = new FantasyPortfolio_DBEntities()) {
                return context.Leaderboards.FirstOrDefault(d => d.RoundId == Round.CurrentRound);
            }
        }

        public static EmbedBuilder GetLeaderboardEmbed() {
            using (var context = new FantasyPortfolio_DBEntities()) {
                var leaderboards = context.Leaderboards.Where(d => d.RoundId == Round.CurrentRound).OrderByDescending(d => d.totalamount);

                var position = 1;
                var sb = new StringBuilder();
                if (leaderboards.Any()) {
                    foreach (var player in leaderboards){
                        sb.AppendLine($"{position} - {DiscordClientNew._client.GetUser(ulong.Parse(player.UserId))?.Username ?? player.UserId} - ${player.totalamount:N}");
                        position++;
                    }
                }
                else {
                    sb.AppendLine("No players currently participating in this round");
                }

                sb.AppendLine(Environment.NewLine);

                var embed = new EmbedBuilder {
                    Title = $"{Preferences.BaseCurrency} Discord Leaderboard - Round {Round.CurrentRound}",
                    Description = sb.ToString()
                };

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

                var endStr = string.IsNullOrEmpty(timeRemainingStr) ? "soon" : $"in {timeRemainingStr}";

                embed.WithFooter($"Round Ends {endStr} - Grand Prize: {PrizePool} {Preferences.BaseCurrency}");
                return embed;
            }
        }

        public bool CheckBalance(string amount, string ticker, out string reason) {
            reason = "";
            var userId = Context.User.Id.ToString();

            if (decimal.TryParse(amount, out var amountDec)) {
                using (var context = new FantasyPortfolio_DBEntities()) {
                    int tickerId;

                    if (ticker.ToUpper() == "USD") {
                        tickerId = -1;
                    }
                    else {
                        var coins = Coin.GetTickers(ticker);
                        if (coins.Result.Count > 1) {
                            var nameStrings = new List<string>();
                            foreach (var coin in coins.Result) {
                                nameStrings.Add($"`{coin.TickerName}`");
                            }
                            reason = $"Multiple coins in database with this ticker exist, please use the coin name instead: {string.Join(", ", nameStrings)})";
                            return false;
                        }
                        else if (coins.Result.Count == 0) {
                            reason = "No coin found with this ticker or name";
                            return false;
                        }
                        else {
                            // ReSharper disable once PossibleNullReferenceException - Not null as above
                            tickerId = coins.Result.FirstOrDefault().TickerId;
                        }
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