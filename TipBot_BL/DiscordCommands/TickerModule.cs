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
using CoinMarketCap.Models;
using CryptoCompare;
using TipBot_BL.Properties;
using TipBot_BL.QT;
using Color = System.Drawing.Color;

namespace TipBot_BL.DiscordCommands {
    public class TickerModule : ModuleBase<SocketCommandContext> {
        public static CoinMarketCap.CoinMarketCapClient priceClientNew = new CoinMarketCap.CoinMarketCapClient();

        [Command("price")]
        public async Task GetPrice(string ticker) {
            if (Context.Channel.Id != Preferences.PriceCheckChannel) {
                await ReplyAsync($"Please use the <#{Preferences.PriceCheckChannel}> channel!");
                return;
            }
            var embed = await GetPriceEmbed(ticker);
            await ReplyAsync("", false, embed);
        }

        public async Task<Embed> GetPriceEmbed(string ticker) {
            var tickerFormatted = ticker.ToUpper().Trim();

            var listings = await priceClientNew.GetListingsAsync();
            var listingSingle = listings.Data.FirstOrDefault(d => d.Symbol == tickerFormatted);

            if (listingSingle != null) {
                var tickerResponse = await priceClientNew.GetTickerAsync((int)listingSingle.Id, "BTC");
                var emb = new EmbedBuilder();
                emb.WithTitle($"Price of {listingSingle.Name} [{tickerFormatted}]");
                var sb = new StringBuilder();
                sb.AppendLine($"**Rank:** {tickerResponse.Data.Rank}");
                sb.Append(Environment.NewLine);

                foreach (var quote in tickerResponse.Data.Quotes) {
                    if (quote.Key == "USD") {
                        sb.AppendLine("**Price " + quote.Key + ":** " + "$" + Math.Round(quote.Value.Price.GetValueOrDefault(0), 2));
                    }
                    else {
                        sb.AppendLine("**Price " + quote.Key + ":** " + decimal.Parse(Math.Round(quote.Value.Price.GetValueOrDefault(0), 8).ToString(), NumberStyles.AllowDecimalPoint | NumberStyles.AllowExponent) + " " + quote.Key);
                    }
                }

                sb.Append(Environment.NewLine);
                sb.AppendLine($"**Market Cap: **${tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.MarketCap:n}");
                sb.AppendLine($"**24h volume: **${tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Volume24H:n}");
                sb.AppendLine($"**Supply: **{tickerResponse.Data.TotalSupply:n}");
                sb.Append(Environment.NewLine);
                sb.AppendLine($"**Change 1h: **{tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.PercentChange1H:n}%");
                sb.AppendLine($"**Change 24h: **{tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.PercentChange24H:n}%");
                sb.AppendLine($"**Change 7 days: **{tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.PercentChange7D:n}%");

                emb.WithDescription(sb.ToString());
                emb.WithUrl($"https://coinmarketcap.com/currencies/{tickerResponse.Data.Name}/");
                emb.ThumbnailUrl = $"https://s2.coinmarketcap.com/static/img/coins/32x32/{listingSingle.Id}.png";

                emb.WithFooter("GroTip Price Checker Bot | By Yokomoko");
                return emb;
            }
            return null;
        }



    }
}