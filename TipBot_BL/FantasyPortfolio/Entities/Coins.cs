using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
                var listingSingle = listings.Data.OrderByDescending(d => d.Id).FirstOrDefault(d => d.Id == tickerId);
                if (listingSingle != null) {
                    var tickerResponse = await TickerModule.priceClientNew.GetTickerAsync((int)listingSingle.Id);
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
                    var valuePrice = tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Price;
                    coin.Volume24 = (decimal?)tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Volume24H;
                    if (valuePrice != null) coin.PriceUSD = (decimal)valuePrice;
                    context.SaveChanges();
                }
                return coin;
            }
        }


        public static async Task<List<Coin>> GetTickers(string tickerName) {
            var coinList = new List<Coin>();

            using (var context = new FantasyPortfolio_DBEntities()) {
                var tickerFormatted = tickerName.ToUpper().Trim();

                var listings = await TickerModule.priceClientNew.GetListingsAsync();
                var listingSingle = listings.Data.Where(d => d.Symbol == tickerFormatted).ToList();

                if (listingSingle.Count == 0) {
                    listingSingle = listings.Data.Where(d => string.Equals(d.WebsiteSlug, tickerFormatted, StringComparison.CurrentCultureIgnoreCase)).ToList();
                }

                foreach (var listing in listingSingle) {
                    var tickerResponse = await TickerModule.priceClientNew.GetTickerAsync((int)listing.Id);

                    //If there are more than one listing of the same name, search the database using website slug, otherwise use the ticker name.
                    Coin coin;
                    if (listingSingle.Count > 1) {
                        coin = context.Coins.FirstOrDefault(d => d.TickerName == tickerResponse.Data.WebsiteSlug);
                    }
                    else {
                        coin = context.Coins.FirstOrDefault(d => d.TickerName == tickerResponse.Data.WebsiteSlug || d.TickerName == tickerResponse.Data.Symbol);
                    }
                    if (coin == null) {
                        var newCoin = new Coin {
                            TickerId = (int)listing.Id,
                            LastUpdated = DateTime.Now,
                            //If there are more than one listing of the same name, use the slug, otherwise use the symbol
                            TickerName = listingSingle.Count > 1 ? listing.WebsiteSlug : listing.Symbol
                        };
                        context.Coins.Add(newCoin);
                        if (newCoin.PriceUSD == 0) {
                            var valuePrice = tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Price;
                            if (valuePrice != null) newCoin.PriceUSD = (decimal)valuePrice;
                            newCoin.Volume24 = (decimal?)tickerResponse.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Volume24H;
                        }
                        context.SaveChanges();
                        coinList.Add(newCoin);
                    }
                    else {
                        coinList.Add(coin);
                    }
                }
                return coinList;
            }
        }

        public static async Task UpdateCoinValue(int coinId) {
            using (var context = new FantasyPortfolio_DBEntities()) {
                var coin = context.Coins.FirstOrDefault(d => d.Id == coinId);
                if (coin != null) {
                    var ticker = await TickerModule.priceClientNew.GetTickerAsync(coin.TickerId);
                    coin.LastUpdated = DateTime.Now;
                    var valuePrice = ticker.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Price;
                    if (valuePrice != null) coin.PriceUSD = (decimal)valuePrice;
                    coin.Volume24 = (decimal?)ticker.Data.Quotes.FirstOrDefault(d => d.Key == "USD").Value.Volume24H;
                    context.SaveChanges();
                    Console.WriteLine($"{DateTime.Now} - Updated {coin.TickerName} price.");
                }
            }

        }
    }
}
