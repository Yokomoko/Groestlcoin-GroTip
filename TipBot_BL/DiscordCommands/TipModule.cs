using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using QRCoder;
using TipBot_BL.FantasyPortfolio;
using TipBot_BL.QT;
using Color = System.Drawing.Color;

namespace TipBot_BL.DiscordCommands {
    public class TipModule : ModuleBase<SocketCommandContext> {
        //private const decimal MinimumTxnValue = (decimal)0.0001;
        private const decimal MinimumTipValue = (decimal)0.00000001;
        private const decimal MinBetAmount = (decimal)(0.01);
        public static decimal BetWin = (decimal)1.97;

        public int OptInCount {
            get {
                var role = Context.Guild.Roles.FirstOrDefault(d => d.Name.ToLower() == "rains");
                return Context.Guild.Users.Count(d => d.Roles.Contains(role));
            }
        }

        public bool IsOptedIn {
            get {
                var role = Context.Guild.Roles.FirstOrDefault(d => d.Name.ToLower() == "rains");
                var user = Context.Guild.GetUser(Context.User.Id);
                return user.Roles.Contains(role);
            }
        }

        // The random number provider.
        private readonly RNGCryptoServiceProvider Rand = new RNGCryptoServiceProvider();

        // Return a random integer between a min and max value.
        private int RandomInteger(int min, int max) {
            uint scale = uint.MaxValue;
            while (scale == uint.MaxValue) {
                // Get four random bytes.
                byte[] four_bytes = new byte[4];
                Rand.GetBytes(four_bytes);

                // Convert that into an uint.
                scale = BitConverter.ToUInt32(four_bytes, 0);
            }

            // Add min to the scaled difference between max and min.
            return (int)(min + (max - min) *
                         (scale / (double)uint.MaxValue));
        }

        private bool CanRunTipCommands => (Context.Channel.Id == Preferences.TipBotChannel || Context.Channel.Id == Preferences.FantasyChannel) || Context.Guild.GetUser(Context.User.Id).Roles.Any(d => d.Permissions.Administrator);
        public enum CoinSide {
            heads = 0,
            tails = 1
        }

        public enum RockPaperScissors {
            rock = 0,
            paper = 1,
            scissors = 2
        }

        public enum Outcome {
            win,
            lose,
            draw
        }

        public static string FirstCharToUpper(string input) {
            if (string.IsNullOrEmpty(input)) {
                Console.WriteLine("Empty String");
            }
            return input?.First().ToString().ToUpper() + input?.Substring(1);
        }

        [Command("showoptedin")]
        public async Task ShowOptedIn() {
            var userOptedIn = 0;
            if (IsOptedIn) {
                userOptedIn = 1;
            }
            await ReplyAsync($"There are currently {OptInCount} opted in for rains. Therefore you can rain to {OptInCount - userOptedIn} people");
        }

        [Command("optallin")]
        public async Task OptAllIn() {
            if (!Context.Guild.GetUser(Context.User.Id).Roles.Any(d => d.Permissions.Administrator)) {
                await ReplyAsync("Only administrators can run this command");
                return;
            }
            var ra = Context.Guild.Users.Where(d => d.Status != UserStatus.Offline && !d.IsBot);

            var optInCount = 0;

            foreach (var socketGuildUser in ra) {
                try {
                    if (!socketGuildUser.Roles.Contains(Context.Guild.Roles.FirstOrDefault(d => d.Name == "Rains"))) {
                        Context.Guild.Users.FirstOrDefault(d => d.Id == socketGuildUser.Id)?.AddRoleAsync(Context.Guild.Roles.FirstOrDefault(d => d.Name == "Rains"));
                        //await socketGuildUser.SendMessageAsync("hello");
                        await socketGuildUser.GetOrCreateDMChannelAsync(RequestOptions.Default).Result.SendMessageAsync($"You've been automatically opted in to receive free {Preferences.BaseCurrency}! To opt out, write `-optout` in the {Preferences.TipBotChannel} channel");
                        Console.WriteLine($"Opted in {socketGuildUser.Username}");
                        optInCount++;
                    }
                }
                catch {
                    Console.WriteLine("Unable to send");
                }
            }
            if (optInCount > 0) {
                await ReplyAsync($"Opted {optInCount} users in");
            }
            else {
                await ReplyAsync("No more users to opt in.");
            }
        }

        [Command("invites")]
        public async Task Invites(SocketUser user = null) {
            if (user == null) {
                user = Context.Guild.GetUser(Context.User.Id);
            }
            var invites = Context.Guild.GetInvitesAsync().Result.Where(d => d.Inviter.Id == user.Id);

            int invCount = 0;
            var restInviteMetadatas = invites.ToList();
            if (restInviteMetadatas.Any()) {
                foreach (var list in restInviteMetadatas) {
                    invCount += list.Uses;
                }
            }
            await ReplyAsync($"{user.Username} has invited {invCount} {(invCount > 1 ? "people" : "person")}");
        }

        [Command("optin")]
        public async Task OptIn() {
            var role = Context.Guild.Roles.FirstOrDefault(d => d.Name == "Rains");

            if (role == null) {
                await ReplyAsync($"Unable to find role called 'Rains'. Please ensure the role exists");
                return;
            }
            var addRoleComm = Context.Guild.Users.FirstOrDefault(d => d == Context.User)?.AddRoleAsync(role);

            var waitCount = 0;
            while (!addRoleComm.IsCompleted && waitCount < 3) {
                Thread.Sleep(500);
                waitCount++;
            }
            if (addRoleComm.Status != TaskStatus.RanToCompletion) {
                await ReplyAsync($"Unable to opt you in, I couldn't change the roles. Please check my permissions and role position.");
            }
            else {
                await ReplyAsync($"You have opted in to rains! You now have a chance to receive free {Preferences.BaseCurrency}");
            }
        }

        [Command("flipstats")]
        public async Task FlipStats() {
            var statistics = FlipResults.GetStatistics();
            var embed = new EmbedBuilder();
            embed.WithTitle("Flip Statistics");

            if (statistics == null) {
                embed.WithDescription("No coin flip statistics to show yet.");
            }
            else {
                embed.AddInlineField("Total Flips", statistics.TotalFlips);
                embed.AddInlineField("Wins", statistics.Wins);
                embed.AddInlineField("Losses", statistics.Losses);

                embed.AddInlineField("Total Flipped", $"{Math.Round((decimal)statistics.TotalFlipped, 2)} {Preferences.BaseCurrency}");
                embed.AddInlineField("User Winnings", $"{Math.Round((decimal)statistics.PaidOut, 2)} {Preferences.BaseCurrency}");
                embed.AddInlineField("Bot Winnings", $"{Math.Round((decimal)statistics.PaidIn, 2)} {Preferences.BaseCurrency}");

                embed.AddInlineField("Win Percentage", Math.Round((decimal)statistics.WinPercentage * 100, 2) + "%");
                embed.AddInlineField("Head Flips", statistics.HeadFlips);
                embed.AddInlineField("Tail Flips", statistics.TailFlips);
            }
            await ReplyAsync("", false, embed);
        }

        [Command("optout")]
        public async Task OptOut() {
            if (CanRunTipCommands) {
                var role = Context.Guild.Roles.FirstOrDefault(d => d.Name == "Rains");

                if (role == null) {
                    await ReplyAsync($"Unable to find role called 'Rains'. Please ensure the role exists");
                    return;
                }
                var removeRoleComm = Context.Guild.Users.FirstOrDefault(d => d == Context.User)?.RemoveRoleAsync(role);

                var waitCount = 0;
                while (removeRoleComm != null && (!removeRoleComm.IsCompleted && waitCount < 3)) {
                    Thread.Sleep(500);
                    waitCount++;
                }
                if (removeRoleComm.Status != TaskStatus.RanToCompletion) {
                    await ReplyAsync($"Unable to opt you out, I couldn't change the roles. Please check my permissions and role position.");
                }
                else {
                    await ReplyAsync($"You have opted out of rains :(");
                }

            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }

        }




        [Command("address")]
        public async Task GetAddressAsync(string addr = "") {

            if (CanRunTipCommands) {
                var result = QTCommands.GetAddress(Context.User.Id);
                if (addr == "qr") {
                    QRCodeGenerator qrgen = new QRCodeGenerator();

                    QRCodeData data = qrgen.CreateQrCode($"{result}", QRCodeGenerator.ECCLevel.Q);
                    var qrcode = new Base64QRCode(data);

                    byte[] bytes = Convert.FromBase64String(qrcode.GetGraphic(4, Color.White, Color.FromArgb(54, 57, 62)));
                    MemoryStream ms = new MemoryStream(bytes);
                    await Context.Channel.SendFileAsync(ms, "qraddr.png", $"`Your deposit address is {result}`");
                }
                else {
                    var b = new EmbedBuilder();
                    b.WithDescription($"Your deposit address is: **{result}**");
                    b.WithFooter(Preferences.FooterText);
                    await ReplyAsync("", false, b);

                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        [Command("balance")]
        public async Task GetBalanceAsync() {
            if (CanRunTipCommands) {
                var result = QTCommands.GetBalance(Context.User.Id);
                await ReplyAsync($"Your balance is {result.Result} {Preferences.BaseCurrency}");
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        [Command("withdraw")]
        public async Task WithdrawAsync(string address) {
            if (CanRunTipCommands) {
                if (address.StartsWith("F") || address.StartsWith("3") || address.StartsWith("grs1")) {
                    var resp = QTCommands.Withdraw(Context.User.Id, address);
                    if (string.IsNullOrEmpty(resp.Error)) {
                        await ReplyAsync($"Withdrawn successfully! Transaction: {Preferences.ExplorerPrefix}{resp.Result}{Preferences.ExplorerSuffix}");
                    }
                    else {
                        await ReplyAsync(resp.Error);
                    }
                }
                else {
                    await ReplyAsync("Invalid Address");
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        [Command("withdraw")]
        public async Task WithdrawAsync(string address, decimal amount) {
            if (CanRunTipCommands) {
                if (address.StartsWith("F") || address.StartsWith("3") || address.StartsWith("grs1")) {
                    if (QTCommands.CheckBalance(Context.User.Id, amount)) {
                        if (amount <= QTCommands.MinimumWithdraw) {
                            await ReplyAsync($"Minimum Withdrawal: {QTCommands.MinimumWithdraw:N8}");
                            return;
                        }
                        var resp = QTCommands.Withdraw(Context.User.Id, address, amount);
                        if (string.IsNullOrEmpty(resp.Error)) {
                            await ReplyAsync($"Withdrawn successfully! Transaction: {Preferences.ExplorerPrefix}{resp.Result}{Preferences.ExplorerSuffix}");
                        }
                        else {
                            await ReplyAsync(resp.Error);
                        }
                    }
                    else {
                        await ReplyAsync("Not enough balance!");
                    }
                }
                else {
                    await ReplyAsync("Invalid Address");
                }


            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        [Command("donate")]
        public async Task DonateAsync(decimal amount) {
            if (QTCommands.CheckBalance(Context.User.Id, amount)) {
                var resp = QTCommands.Withdraw(Context.User.Id, "FWN1qdiRrymSR6jbpbanLYqZpjkEaZouHN", amount);
                if (string.IsNullOrEmpty(resp.Error)) {
                    await ReplyAsync($"Donation successful! The {Preferences.BaseCurrency} Team thanks you! Transaction: {Preferences.ExplorerPrefix}{resp.Result}{Preferences.ExplorerSuffix}");
                }
            }
            else {
                await ReplyAsync("Not enough balance!");
            }
        }

        [Command("donate")]
        public async Task DonateAsync(decimal amount, string pot) {
            if (QTCommands.CheckBalance(Context.User.Id, amount)) {
                if (pot.Contains("dev")) {
                    var resp = QTCommands.Withdraw(Context.User.Id, "FWN1qdiRrymSR6jbpbanLYqZpjkEaZouHN", amount);
                    if (string.IsNullOrEmpty(resp.Error)) {
                        await ReplyAsync($"Donation successful! The {Preferences.BaseCurrency} Team thanks you! Transaction: {Preferences.ExplorerPrefix}{resp.Result}{Preferences.ExplorerSuffix}");
                    }
                }
                else if (pot.Contains("market")) {
                    var resp = QTCommands.Withdraw(Context.User.Id, "FqkPKgvb2jFv6GdVphgyVY2iFPcHHi3dx7", amount);
                    if (string.IsNullOrEmpty(resp.Error)) {
                        await ReplyAsync($"Donation successful! The {Preferences.BaseCurrency} Team thanks you! Transaction: {Preferences.ExplorerPrefix}{resp.Result}{Preferences.ExplorerSuffix}");
                    }
                }
                else {
                    await ReplyAsync($"No Donation Fund found for {pot}! Options: `dev`, `marketing`");
                }
            }
            else {
                await ReplyAsync("Not enough balance!");
            }
        }

        [Command("tip")]
        public async Task Tip(SocketUser user, string amount) {
            if (CanRunTipCommands) {
                if (decimal.TryParse(amount, out var decAmount)) {

                    if (decAmount < MinimumTipValue) {
                        await ReplyAsync($"Minimum tip amount is {MinimumTipValue}");
                        return;
                    }

                    if (QTCommands.CheckBalance(Context.User.Id, decAmount)) {
                        QTCommands.SendTip(Context.User.Id, user.Id, decAmount);
                        await ReplyAsync($"{Context.User.Mention} tipped {user.Username} {amount} {Preferences.BaseCurrency}");
                    }
                    else {
                        await ReplyAsync("You do not have enough balance to tip that much!");
                    }
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        [Command("tip")]
        public async Task Tip(string amount, SocketUser user) {
            if (CanRunTipCommands) {
                if (decimal.TryParse(amount, out var decAmount)) {

                    if (decAmount < MinimumTipValue) {
                        await ReplyAsync($"Minimum tip amount is {MinimumTipValue}");
                        return;
                    }

                    if (QTCommands.CheckBalance(Context.User.Id, decAmount)) {
                        QTCommands.SendTip(Context.User.Id, user.Id, decAmount);
                        await ReplyAsync($"{Context.User.Mention} tipped {user.Username} {amount} {Preferences.BaseCurrency}");
                    }
                    else {
                        await ReplyAsync("You do not have enough balance to tip that much!");
                    }
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        [Command("join")]
        public async Task Join() {
            if (FantasyPortfolioModule.EntryFee == 0 || QTCommands.CheckBalance(Context.User.Id, FantasyPortfolioModule.EntryFee)) {
                if (FantasyPortfolioModule.EntryFee > 0) QTCommands.SendTip(Context.User.Id, DiscordClientNew._client.CurrentUser.Id, FantasyPortfolioModule.EntryFee);
                if (Portfolio.Join(Context.User.Id.ToString())) {
                    await ReplyAsync($"You have joined round {Round.CurrentRound}. An entry fee of {FantasyPortfolioModule.EntryFee} {Preferences.BaseCurrency} has been taken.");
                }
                else {
                    await ReplyAsync("You have already joined this round!");
                }
            }
            else {
                await ReplyAsync($"You do not have the required entry fee of {FantasyPortfolioModule.EntryFee} {Preferences.BaseCurrency}. Please send some {Preferences.BaseCurrency} to your tipping account and try again.");
            }
        }

        [Command("inactive")]
        public async Task ShowInactiveFunds() {
            if (Context.User.Id == 354270799684829185) {
                var accounts = QTCommands.ListAccounts();

                decimal inactiveTotal = 0;

                foreach (var r in accounts) {
                    if (!string.IsNullOrEmpty(r.Key)) {
                        if (ulong.TryParse(r.Key, out ulong userId)) {
                            if (Context.Guild.Users.All(d => d.Id != userId)) {
                                inactiveTotal += r.Value;
                            }
                        }
                    }
                }
                await ReplyAsync($"Total held by left members: {inactiveTotal:N8} {Preferences.BaseCurrency}");
            }
        }

        [Command("listinactive")]
        public async Task ListInactive() {
            if (Context.User.Id == 354270799684829185) {
                var accounts = QTCommands.ListAccounts();

                var sb = new StringBuilder();
                sb.Append("");
                foreach (var r in accounts) {
                    if (!string.IsNullOrEmpty(r.Key)) {
                        if (ulong.TryParse(r.Key, out ulong userId)) {
                            if (Context.Guild.Users.All(d => d.Id != userId) && r.Value > 0) {
                                sb.AppendLine($"{r.Key} - {r.Value}");
                            }
                        }
                    }
                }
                await ReplyAsync(sb.ToString());
            }
        }

        [Command("rain")]
        public async Task Rain(string amount, string numberOfPeople) {
            if (CanRunTipCommands) {
                if (int.TryParse(numberOfPeople, out var people)) {
                    var role = Context.Guild.Roles.FirstOrDefault(d => d.Name == "Rains");
                    var ra = Context.Guild.Users.Where(d => d.Roles.Contains(role));

                    var selectedUsers = ra.OrderBy(arg => Guid.NewGuid()).Where(d => d.Id != Context.User.Id).Take(people).ToList();
                    if (selectedUsers.Count >= people) {
                        if (decimal.TryParse(amount, out var rainAmount)) {
                            await ReplyAsync(SendRain(Context.User, selectedUsers, rainAmount));
                        }
                        else {
                            await ReplyAsync($"{amount} is not a valid amount to rain.");
                        }
                    }
                    else {
                        await ReplyAsync($"Not enough people to rain ({selectedUsers.Count}/{people})");
                    }
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }

        }

        [Command("rain")]
        public async Task Rain(string amount) {
            if (CanRunTipCommands) {
                var role = Context.Guild.Roles.FirstOrDefault(d => d.Name == "Rains");
                var ra = Context.Guild.Users.Where(d => d.Roles.Contains(role));

                int people;
                var selectedUsers = ra.OrderBy(arg => Guid.NewGuid()).Where(d => d.Id != Context.User.Id).ToList();

                decimal tipPeople = selectedUsers.Count / (decimal)10;

                if (tipPeople > 1) {
                    people = (int)Math.Round(tipPeople, 0);
                }
                else {
                    people = new Random().Next(1, 5);
                }
                selectedUsers = selectedUsers.Take(people).ToList();

                if (selectedUsers.Count >= people) {
                    if (decimal.TryParse(amount, out var rainAmount)) {
                        await ReplyAsync(SendRain(Context.User, selectedUsers, rainAmount));
                    }
                    else {
                        await ReplyAsync($"{amount} is not a valid amount to rain.");
                    }
                }
                else {
                    await ReplyAsync($"Not enough people to rain ({selectedUsers.Count}/{people})");
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        [Command("house")]
        public async Task GetHouseBalance() {
            if (CanRunTipCommands) {
                var balanceString = QTCommands.GetBalance(DiscordClientNew._client.CurrentUser.Id).Result;

                if (decimal.TryParse(balanceString, NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint, CultureInfo.CreateSpecificCulture("en-GB"), out var balance)) {
                    balance = balance - FantasyPortfolioModule.PrizePool;
                    await ReplyAsync($"The house balance is at {balance} {Preferences.BaseCurrency}");
                }
                else {
                    await ReplyAsync("Error getting house balance");
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        [Command("houseaddress")]
        public async Task GetHouseAddress(string qr = "") {
            if (CanRunTipCommands) {
                var houseAddr = QTCommands.GetAccountAddress(Context.Guild.CurrentUser.Id);

                if (qr.ToLower() == "qr") {
                    QRCodeGenerator qrgen = new QRCodeGenerator();
                    QRCodeData data = qrgen.CreateQrCode($"{houseAddr}", QRCodeGenerator.ECCLevel.Q);
                    var qrcode = new Base64QRCode(data);

                    byte[] bytes = Convert.FromBase64String(qrcode.GetGraphic(4, Color.White, Color.FromArgb(54, 57, 62), null));

                    MemoryStream ms = new MemoryStream(bytes);
                    await Context.Channel.SendFileAsync(ms, "qraddr.png", $"`The house deposit address is {houseAddr}`");
                }
                else {
                    EmbedBuilder b = new EmbedBuilder();
                    b.WithTitle(Context.Guild.CurrentUser.Username);
                    b.AddField("Address", houseAddr);
                    b.AddField("Developer", "Yokomoko");
                    //b.AddField("Developer Donations", "MVT2AJDK7CYTtWo5fX9u48eQT5ynWPyFFd");
                    b.WithColor(Discord.Color.Blue);
                    b.WithFooter("Payments to these addresses will help fund hosting, and support giveaway events.");
                    await ReplyAsync("", false, b);
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }


        [Command("flip")]
        public async Task Flip(string side, string amount) {
            if (CanRunTipCommands) {
                if (Enum.TryParse(side.ToLower(), out CoinSide coinSide)) {
                    if (decimal.TryParse(amount, out var betAmount)) {
                        if (QTCommands.CheckBalance(Context.User.Id, betAmount)) {
                            if (betAmount < MinBetAmount) {
                                await ReplyAsync($"Minimum bet {MinBetAmount} {Preferences.BaseCurrency}");
                                return;
                            }

                            decimal balance = 0;
                            var houseBalanceCall = QTCommands.GetBalance(Context.Client.CurrentUser.Id).Result;
                            decimal.TryParse(houseBalanceCall, out balance);

                            decimal maxBet = balance / 10;

                            if (betAmount > maxBet) {
                                await ReplyAsync($"Maximum bet exceeded. Max: {maxBet} {Preferences.BaseCurrency}");
                                return;
                            }

                            var rewardValue = betAmount * (decimal)BetWin;
                            if (QTCommands.CheckBalance(Context.Client.CurrentUser.Id, rewardValue + FantasyPortfolioModule.PrizePool)) {
                                QTCommands.SendTip(Context.User.Id, Context.Client.CurrentUser.Id, betAmount);
                                try {

                                    var coin = (CoinSide)RandomInteger(0, 2);
                                    //var coin = (CoinSide)Generator.Next(0, 2);

                                    var embed = new EmbedBuilder();

                                    string message;

                                    if (coin == coinSide) {
                                        QTCommands.SendTip(DiscordClientNew._client.CurrentUser.Id, Context.User.Id, rewardValue);
                                        embed.AddInlineField("Flipped", FirstCharToUpper(coin.ToString()));
                                        embed.AddInlineField("Prize", $"{rewardValue} {Preferences.BaseCurrency}");
                                        embed.AddInlineField("Profit", $"{(rewardValue - betAmount)} {Preferences.BaseCurrency}");
                                        embed.WithColor(Discord.Color.Green);
                                        message = $"You won! Congratulations {Context.User.Mention}!";
                                    }
                                    else {
                                        embed.AddInlineField("Flipped", FirstCharToUpper(coin.ToString()));
                                        embed.AddInlineField("Lost", $"{betAmount} {Preferences.BaseCurrency}");
                                        embed.WithColor(Discord.Color.Red);
                                        message = $"Unlucky {Context.User.Mention}, you lost :(";
                                    }
                                    embed.WithFooter(Preferences.FooterText);
                                    await ReplyAsync(message, false, embed);

                                    using (var context = new FantasyPortfolio_DBEntities()) {
                                        var result = new FlipResults();
                                        result.DateTime = DateTime.Now;
                                        result.UserId = Context.User.Id.ToString();
                                        result.FlipResult = (byte)coin;
                                        result.UserFlip = (byte)coinSide;
                                        result.FlipValue = betAmount;
                                        context.FlipResults.Add(result);
                                        context.SaveChanges();
                                    }

                                    Console.WriteLine($"{Context.User.Id} ({Context.User.Username}) bet on {side} and flipped {coin}");
                                }
                                catch (Exception e) {
                                    Console.WriteLine(e.Message);
                                    QTCommands.SendTip(DiscordClientNew._client.CurrentUser.Id, Context.User.Id, betAmount);
                                    await ReplyAsync("Sorry something went wrong. You have been refunded your bet.");
                                }

                            }
                            else {
                                await ReplyAsync("Sorry, the bot is too poor to reward you if you won :(");
                            }
                        }
                        else {
                            await ReplyAsync("You do not have enough balance to perform this action");
                        }
                    }
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }


        [Command("rps")]
        public async Task RockPaperScissorsGame(string rps, string amount) {
            if (CanRunTipCommands) {
                if (Enum.TryParse(rps.ToLower(), out RockPaperScissors userDecision)) {
                    if (decimal.TryParse(amount, out var betAmount)) {
                        if (QTCommands.CheckBalance(Context.User.Id, betAmount)) {
                            if (betAmount < MinBetAmount) {
                                await ReplyAsync($"Minimum bet {MinBetAmount} {Preferences.BaseCurrency}");
                                return;
                            }

                            decimal balance = 0;
                            var houseBalanceCall = QTCommands.GetBalance(Context.Client.CurrentUser.Id).Result;
                            decimal.TryParse(houseBalanceCall, out balance);

                            decimal maxBet = balance / 10;

                            if (betAmount > maxBet) {
                                await ReplyAsync($"Maximum bet exceeded. Max: {maxBet} {Preferences.BaseCurrency}");
                                return;
                            }

                            var rewardValue = betAmount * (decimal)BetWin;
                            if (QTCommands.CheckBalance(DiscordClientNew._client.CurrentUser.Id, rewardValue + FantasyPortfolioModule.PrizePool)) {
                                QTCommands.SendTip(Context.User.Id, DiscordClientNew._client.CurrentUser.Id, betAmount);
                                try {

                                    var botDecision = (RockPaperScissors)RandomInteger(0, 3);

                                    var embed = new EmbedBuilder();

                                    string message = "";

                                    var userOutcome = new Outcome();

                                    switch (botDecision) {
                                        case RockPaperScissors.rock: {
                                                switch (userDecision) {
                                                    case RockPaperScissors.rock:
                                                        userOutcome = Outcome.draw;
                                                        break;
                                                    case RockPaperScissors.paper:
                                                        userOutcome = Outcome.win;
                                                        break;
                                                    case RockPaperScissors.scissors:
                                                        userOutcome = Outcome.lose;
                                                        break;
                                                }
                                                break;
                                            }
                                        case RockPaperScissors.paper: {
                                                switch (userDecision) {
                                                    case RockPaperScissors.rock:
                                                        userOutcome = Outcome.lose;
                                                        break;
                                                    case RockPaperScissors.paper:
                                                        userOutcome = Outcome.draw;
                                                        break;
                                                    case RockPaperScissors.scissors:
                                                        userOutcome = Outcome.win;
                                                        break;
                                                }
                                                break;
                                            }
                                        case RockPaperScissors.scissors: {
                                                switch (userDecision) {
                                                    case RockPaperScissors.rock:
                                                        userOutcome = Outcome.win;
                                                        break;
                                                    case RockPaperScissors.paper:
                                                        userOutcome = Outcome.lose;
                                                        break;
                                                    case RockPaperScissors.scissors:
                                                        userOutcome = Outcome.draw;
                                                        break;
                                                }
                                                break;
                                            }
                                    }

                                    switch (userOutcome) {
                                        case Outcome.win:
                                            QTCommands.SendTip(DiscordClientNew._client.CurrentUser.Id, Context.User.Id, rewardValue);
                                            embed.AddInlineField("Outcome", $"Bot opted for {botDecision.ToString()} whereas you opted for {userDecision.ToString()}");
                                            embed.AddInlineField("Prize", $"{rewardValue} {Preferences.BaseCurrency}");
                                            embed.AddInlineField("Profit", $"{(rewardValue - betAmount)} {Preferences.BaseCurrency}");
                                            embed.WithColor(Discord.Color.Green);
                                            message = $"You won! Congratulations {Context.User.Mention}!";
                                            break;
                                        case Outcome.draw:
                                            QTCommands.SendTip(DiscordClientNew._client.CurrentUser.Id, Context.User.Id, betAmount);
                                            embed.AddInlineField("Outcome", $"Bot opted for {botDecision.ToString()} whereas you opted for {userDecision.ToString()}");
                                            embed.WithColor(Discord.Color.Gold);
                                            message = "It was a draw! Bet refunded";
                                            break;
                                        case Outcome.lose:
                                            QTCommands.SendTip(DiscordClientNew._client.CurrentUser.Id, Context.User.Id, betAmount);
                                            embed.AddInlineField("Outcome", $"Bot opted for {botDecision.ToString()} whereas you opted for {userDecision.ToString()}");
                                            embed.AddInlineField("Prize", $"{rewardValue} {Preferences.BaseCurrency}");
                                            embed.AddInlineField("Profit", $"{(rewardValue - betAmount)} {Preferences.BaseCurrency}");
                                            embed.WithColor(Discord.Color.Red);
                                            message = "Unlucky! You lose! Bot won";
                                            break;
                                    }
                                    embed.WithFooter(Preferences.FooterText);
                                    await ReplyAsync(message, false, embed);

                                    Console.WriteLine($"{Context.User.Id} ({Context.User.Username}) opted for {userDecision} and bot opted for {botDecision}");
                                }
                                catch (Exception e) {
                                    Console.WriteLine(e.Message);
                                    QTCommands.SendTip(DiscordClientNew._client.CurrentUser.Id, Context.User.Id, betAmount);
                                    await ReplyAsync("Sorry something went wrong. You have been refunded your bet.");
                                }

                            }
                            else {
                                await ReplyAsync("Sorry, the bot is too poor to reward you if you won :(");
                            }
                        }
                        else {
                            await ReplyAsync("You do not have enough balance to perform this action");
                        }
                    }
                }
            }
            else {
                await ReplyAsync($"Please use the <#{Preferences.TipBotChannel}> channel");
            }
        }

        private string SendRain(SocketUser fromUser, List<SocketGuildUser> tipUsers, decimal amount) {
            if (QTCommands.CheckBalance(fromUser.Id, amount)) {
                if (amount / tipUsers.Count < (decimal)0.01) {
                    return $"Rain amount must be at least 0.01 {Preferences.BaseCurrency} per person";
                }
                foreach (var person in tipUsers) {
                    QTCommands.SendTip(Context.User.Id, person.Id, Math.Round(amount / tipUsers.Count, 7));
                }
                var mentionList = new List<string>();
                foreach (var users in tipUsers) {
                    mentionList.Add($"{DiscordClientNew._client.GetUser(users.Id).Mention}");
                }
                return $"{fromUser.Mention} made it rain :cloud_rain:! Congratulations to {(mentionList.Count == 2 ? string.Join(" and ", mentionList) : string.Join(", ", mentionList))} who {(mentionList.Count > 1 ? "have" : "has")} been awarded {Math.Round(amount / mentionList.Count, 7)} {Preferences.BaseCurrency} {(mentionList.Count > 1 ? "each" : "")}";
            }
            return "You do not have enough balance to perform this rain";
        }
    }
}