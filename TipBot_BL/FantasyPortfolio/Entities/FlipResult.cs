using System.Collections.Generic;
using System.Linq;

namespace TipBot_BL.FantasyPortfolio {
    public partial class FlipResults {

        public static List<FlipLeaderboard> GetLeaderboardBySpend() {
            using (var context = new FantasyPortfolio_DBEntities()) {
                return context.FlipLeaderboard.OrderByDescending(u => u.TotalBet.Value).Take(10).ToList();
            }
        }

        public static List<FlipLeaderboard> GetLeaderboardByWins() {
            using (var context = new FantasyPortfolio_DBEntities()) {
                return context.FlipLeaderboard.OrderByDescending(u => u.TotalWins.Value).Take(10).ToList();
            }
        }

        public static FlipResultStatistics GetStatistics() {
            using (var context = new FantasyPortfolio_DBEntities()) {
                return context.FlipResultStatistics.FirstOrDefault();
            }
        }
    }
}
