using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using TipBot_BL.POCO;
using TipBot_BL.QT;

namespace TipBot_BL {
    public class DiscordClient {
        public static DiscordSocketClient _client;
        public readonly string ApiKey;
        public ulong ChannelId;

        private const short MaxRainUsers = 10;
        private const short MinRainUsers = 2;
        private const short MinimumUsers = 2;
        private const decimal MinimumTxnValue = (decimal)0.0001;
        private const string BaseCurrency = "GRS";

        public DiscordClient(string APIKey) {
            ApiKey = APIKey;
            _client = new DiscordSocketClient();
            _client.Log += LogAsync;
            _client.Ready += ReadyAsync;
            _client.MessageReceived += MessageReceivedAsync;

        }

        public async void Start() {
            // Tokens should be considered secret data, and never hard-coded.
            await _client.LoginAsync(TokenType.Bot, ApiKey);
            await _client.StartAsync();
        }

        private async Task LogAsync(LogMessage log) {
            Console.WriteLine(log.ToString());
        }

        // The Ready event indicates that the client has opened a
        // connection and it is now safe to access the cache.
        private async Task ReadyAsync() {
            Console.WriteLine($"{_client.CurrentUser} is connected!");
        }


        private async Task MessageReceivedAsync(SocketMessage message) {
            if (message.Author.Id == _client.CurrentUser.Id)
                return;

            var msg = message.Content.ToLower();
            var splitMsg = message.Content.Split(' ');

            if (msg.StartsWith("-address")) {
                var result = QTCommands.GetAddress(message.Author.Id);
                await message.Channel.SendMessageAsync($"Your {BaseCurrency} deposit address is: {result}");
            }

            if (msg.StartsWith("-balance")) {
               
            }

            if (msg.StartsWith("-withdraw")) {

                if (splitMsg.Length == 2) {
                    var resp = QTCommands.Withdraw(message.Author.Id, splitMsg[1]);
                    if (string.IsNullOrEmpty(resp.error)) {
                        await message.Channel.SendMessageAsync($"Withdrawn successfully! Transaction ID: {resp.result}");
                    }
                }
            }
            else if (msg.StartsWith("-tip")) {
                var user = message.MentionedUsers.FirstOrDefault();
                if (splitMsg.Length == 1 || user == null) {
                    await message.Channel.SendMessageAsync("Please specify a user");
                    return;
                }
                if (splitMsg.Length < 3) {
                    await message.Channel.SendMessageAsync("Please specify an amount to tip");
                    return;
                }


                decimal tipAmount;

                if (decimal.TryParse(splitMsg[2], out tipAmount)) {
                    if (QTCommands.CheckBalance(message.Author.Id, tipAmount)) {
                        QTCommands.SendTip(message.Author.Id, user.Id, tipAmount);
                        await message.Channel.SendMessageAsync($"{message.Author.Mention} tipped {_client.GetUser(user.Id).Username} {tipAmount} {BaseCurrency}");
                    }
                    else {
                        await message.Channel.SendMessageAsync($"You do not have enough balance to tip that much!");
                    }
                }
                else {
                    await message.Channel.SendMessageAsync($"{splitMsg[2]} is an invalid amount.");
                }
            }
            if (msg.StartsWith("-optin")) {
                await message.Channel.SendMessageAsync(DiscordUsers.OptIn(message.Author.Id));
            }
            if (msg.StartsWith("-optout")) {
                await message.Channel.SendMessageAsync(DiscordUsers.OptOut(message.Author.Id));
            }

            //     var result = DiscordUsers.Rain(message.Author.Id);

        }


    }

}
