using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Discord.WebSocket;
using QRCoder;
using TipBot_BL.POCO;
using System.Drawing;
using System.Drawing.Printing;
using CryptoCompare;
using TipBot_BL.QT;
using Color = System.Drawing.Color;

namespace TipBot_BL.DiscordCommands {
    public class TickerModule : ModuleBase<SocketCommandContext> {
        private CryptoCompareClient priceClient = new CryptoCompareClient();
        private CoinMarketCap.CoinMarketCapClient priceClientNew = new CoinMarketCap.CoinMarketCapClient();
        private static readonly IEnumerable<string> convertToTickers = new List<string> { "BTC", "ETH", "USD" };


        private decimal CalculatePercentDifference(double basePrice, double changePrice) {
            return (decimal)(100 * changePrice / basePrice - 100);
        }

        [Command("price")]
        public async Task GetPrice(string ticker) {
            var exchanges = await priceClient.Exchanges.ListAsync();

            ticker = ticker.ToUpper().Trim();

            var prices = await priceClient.Prices.SingleAsync(ticker, convertToTickers);

            //Get Coin Details
            var details = await priceClient.Coins.ListAsync();
            var coinDetails = details.Coins.FirstOrDefault(d => d.Value.Name == ticker);

            var emb = new EmbedBuilder();
            emb.Title = $"Price of {coinDetails.Value.FullName}";

            foreach (var price in prices){
                emb.AddField(price.Key, $"{price.Value}");
            }
            emb.AddField("Changes", "");

            emb.Color = Discord.Color.DarkBlue;
            emb.WithThumbnailUrl("https://www.cryptocompare.com" + coinDetails.Value.ImageUrl);
            emb.Url = "https://www.cryptocompare.com" + coinDetails.Value.Url;
            emb.WithFooter($"{Context.Guild.CurrentUser.Nickname} - Developed by Yokomoko");

            await ReplyAsync("", false, emb);
        }

    }
}