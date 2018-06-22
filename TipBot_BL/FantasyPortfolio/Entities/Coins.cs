using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TipBot_BL.DiscordCommands;

namespace TipBot_BL.FantasyPortfolio {
    public partial class Coin {
        public static string GetTickerName(int tickerId) {
            using (var context = new FantasyPortfolio_DBEntities()) {
                return context.Coins.FirstOrDefault(d => d.TickerId == tickerId)?.TickerName;
            }
        }

        public static async Task<Coin> GetTicker(int tickerId) {
            using (var context = new FantasyPortfolio_DBEntities()) {
                var coin = context.Coins.FirstOrDefault(d => d.TickerId == tickerId);

                var listings = await TickerModule.priceClientNew.GetListingsAsync();
                var listingSingle = listings.Data.FirstOrDefault(d => d.Id == tickerId);
                if (listingSingle != null) {
                    var tickerResponse = await TickerModule.priceClientNew.GetTickerAsync((int)listingSingle.Id, "USD");
                    if (coin == null) {
                        coin = new Coin {
                            TickerId = (int)listingSingle.Id,
                            TickerName = listingSingle.Symbol,
                            LastUpdated = DateTime.Now
                        };
                        context.Coins.Add(coin);
                    }
                    else {
                        context.Coins.Attach(coin);
                    }
                    coin.PriceUSD = (decimal)tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Price;
                    context.SaveChanges();
                }
                return coin;
            }
        }

        public static async Task<Coin> GetTicker(string tickerName) {
            using (var context = new FantasyPortfolio_DBEntities()) {
                var coin = context.Coins.FirstOrDefault(d => d.TickerName == tickerName.ToUpper());
                var tickerFormatted = tickerName.ToUpper().Trim();

                var listings = await TickerModule.priceClientNew.GetListingsAsync();
                var listingSingle = listings.Data.FirstOrDefault(d => d.Symbol == tickerFormatted);
                if (listingSingle != null) {
                    var tickerResponse = await TickerModule.priceClientNew.GetTickerAsync((int)listingSingle.Id, "USD");
                    if (coin == null) {
                        coin = new Coin {
                            TickerId = (int)listingSingle.Id,
                            TickerName = listingSingle.Symbol,
                            LastUpdated = DateTime.Now
                        };
                        context.Coins.Add(coin);
                    }
                    else {
                        context.Coins.Attach(coin);
                    }
                    coin.PriceUSD = (decimal)tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Price;
                    context.SaveChanges();
                }
                return coin;
            }
        }

        public static async Task UpdateCoinValues() {
            using (var context = new FantasyPortfolio_DBEntities()) {
                foreach (var coin in context.Coins) {
                    if (coin.LastUpdated <= DateTime.Now.AddMinutes(-15)) {
                        var ticker = await TickerModule.priceClientNew.GetTickerAsync(coin.TickerId, "USD");
                        coin.LastUpdated = DateTime.Now;
                        coin.PriceUSD = (decimal)ticker.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Price;
                    }
                }
                context.SaveChanges();
            }
        }

    }
}
