using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;
using TipBot_BL.FantasyPortfolio;

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

        public static async Task<Embed> GetPriceEmbed(string ticker) {
            var tickerFormatted = ticker.ToUpper().Trim();
            long? tickerId;

            using (var context = new FantasyPortfolio_DBEntities()) {
                var coin = context.Coins.FirstOrDefault(d => d.TickerName.ToUpper().Trim() == tickerFormatted);
                if (coin == null) {
                    var listings = await priceClientNew.GetListingsAsync();
                    var listingSingle = listings.Data.FirstOrDefault(d => d.Symbol == tickerFormatted);

                    if (listingSingle != null) {
                        coin = new Coin {
                            TickerId = (int)listingSingle.Id,
                            TickerName = listingSingle.Symbol,
                            LastUpdated = new DateTime(2000, 1, 1)
                        };
                        context.Coins.Add(coin);
                        context.SaveChanges();
                    }
                }
                tickerId = coin?.TickerId;
            }
            if (tickerId != null) {
                var tickerResponse = await priceClientNew.GetTickerAsync((int)tickerId, "BTC");
                var emb = new EmbedBuilder();
                emb.WithTitle($"Price of {tickerResponse.Data.Name} [{tickerFormatted}]");
                var sb = new StringBuilder();
                sb.AppendLine($"**Rank:** {tickerResponse.Data.Rank}");
                sb.Append(Environment.NewLine);

                foreach (var quote in tickerResponse.Data.Quotes) {
                    if (quote.Key == "USD") {
                        sb.AppendLine("**Price " + quote.Key + ":** " + "$" + decimal.Parse(Math.Round(quote.Value.Price.GetValueOrDefault(0), 5).ToString(), NumberStyles.AllowExponent | NumberStyles.AllowDecimalPoint));
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
                try {
                    emb.WithUrl($"https://coinmarketcap.com/currencies/{tickerResponse.Data.WebsiteSlug}/");
                }
                catch {
                    // ignored
                }

                emb.ThumbnailUrl = $"https://s2.coinmarketcap.com/static/img/coins/32x32/{tickerId}.png";

                emb.WithFooter(Preferences.FooterText);
                return emb;
            }
            return null;
        }

        public static async Task<List<CoinGeckoSummary>> GetMarketSummary(string market = "usd") {
            using (var httpClient = new HttpClient()) {
                string url = $"https://api.coingecko.com/api/v3/coins/markets?vs_currency={market}&ids=groestlcoin";
                HttpResponseMessage response = await httpClient.GetAsync(url);
                if (response.IsSuccessStatusCode) {
                    string result = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<CoinGeckoSummary>>(result);
                }
                else {
                    return null;
                }
            }
        }

        [Command("price")]
        public async Task GetCoinGeckoSummary() {
            var summary = await GetMarketSummary();
            var embed = new EmbedBuilder();
            embed.WithTitle($"{Preferences.BaseCurrency} - CoinGecko");
            embed.WithUrl($"https://www.coingecko.com/en/coins/groestlcoin");
            embed.WithThumbnailUrl(summary[0].image);
            var sb = new StringBuilder();

            sb.AppendLine($"**Price:** ${Math.Round(summary[0].current_price, 5)}");
            sb.AppendLine($"**Market Cap:** ${Math.Round(summary[0].market_cap, 2):n}");
            sb.AppendLine($"**Total Volume (24h):** ${summary[0].total_volume:n}");
            sb.AppendLine($"**Rank:** {summary[0].market_cap_rank}");
            sb.AppendLine($"**Supply:** {decimal.Parse(summary[0].circulating_supply):n} {Preferences.BaseCurrency}{Environment.NewLine}");

            sb.AppendLine($"**All Time High:** ${Math.Round(summary[0].ath, 5)}");

            embed.WithDescription(sb.ToString());
            embed.WithFooter(Preferences.FooterText);

            await ReplyAsync("", false, embed);
        }



        [Command("getmarket")]
        public async Task GetCoinGeckoSummary(string market) {
            var summary = await GetMarketSummary(market.ToLower());
            var embed = new EmbedBuilder();
            embed.WithTitle($"{Preferences.BaseCurrency} - CoinGecko - ({market.ToUpper()})");
            embed.WithUrl($"https://www.coingecko.com/en/coins/{Preferences.BaseCurrency.ToLower()}");
            embed.WithThumbnailUrl(summary[0].image);
            var sb = new StringBuilder();

            sb.AppendLine($"**Price:** {Math.Round(summary[0].current_price, 8):n8}");
            sb.AppendLine($"**Market Cap:** {Math.Round(summary[0].market_cap, 8):n8}");
            sb.AppendLine($"**Total Volume (24h):** {summary[0].total_volume:n8}");
            sb.AppendLine($"**Rank:** {summary[0].market_cap_rank}");
            sb.AppendLine($"**Supply:** {decimal.Parse(summary[0].circulating_supply):n} {Preferences.BaseCurrency}{Environment.NewLine}");

            sb.AppendLine($"**All Time High:** {Math.Round(summary[0].ath, 8):n8}");

            embed.WithDescription(sb.ToString());
            embed.WithFooter(Preferences.FooterText);

            await ReplyAsync("", false, embed);
        }

        public class CoinGeckoSummary {
            public string id { get; set; }
            public string symbol { get; set; }
            public string name { get; set; }
            public string image { get; set; }
            public double current_price { get; set; }
            public double market_cap { get; set; }
            public int market_cap_rank { get; set; }
            public double total_volume { get; set; }
            public double high_24h { get; set; }
            public double low_24h { get; set; }
            public double price_change_24h { get; set; }
            public double price_change_percentage_24h { get; set; }
            public double market_cap_change_24h { get; set; }
            public double market_cap_change_percentage_24h { get; set; }
            public string circulating_supply { get; set; }
            public int total_supply { get; set; }
            public double ath { get; set; }
            public double ath_change_percentage { get; set; }
            public DateTime ath_date { get; set; }
            public object roi { get; set; }
            public DateTime last_updated { get; set; }
        }


    }
}