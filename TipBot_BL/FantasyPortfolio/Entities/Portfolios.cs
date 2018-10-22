using System.Collections.Generic;
using System.Linq;
using Discord.Commands;

namespace TipBot_BL.FantasyPortfolio {
    public partial class Portfolio : ModuleBase<SocketCommandContext> {

        public decimal UsdValue => decimal.Round(CoinAmount * Coin.GetTicker(TickerId).Result.PriceUSD);

        public static bool Join(string userId) {
            using (var context = new FantasyPortfolio_DBEntities()) {
                var users = context.Portfolios.Where(d => d.RoundId == Round.CurrentRound).Select(m => m.UserId).Distinct();
                if (users.Contains(userId)) {
                    return false;
                }

                var user = new Portfolio {
                    RoundId = Round.CurrentRound,
                    CoinAmount = 10000,
                    TickerId = -1,
                    UserId = userId
                };
                context.Portfolios.Add(user);
                context.SaveChanges();
            }

            return true;
        }

        public static List<Portfolio> Share(string userId) {
            using (var context = new FantasyPortfolio_DBEntities()) {
                var portfolios = context.Portfolios.Where(d => d.UserId == userId && d.RoundId == Round.CurrentRound);
                return portfolios.ToList();
            }
        }
    }
}
