using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TipBot_BL.FantasyPortfolio {
    public partial class Round {
        public static int CurrentRound {
            get {
                using (var context = new FantasyPortfolio_DBEntities()) {
                    if (!context.Rounds.Any()) {
                        context.Rounds.Add(new Round { RoundEnds = DateTime.Now.AddDays(RoundDurationDays) });
                        context.SaveChanges();
                    }
                    var lastRound = context.Rounds.OrderByDescending(d => d.Id).FirstOrDefault();
                    return lastRound.Id;
                }
            }
        }

        public static int RoundDurationDays = 1;

        public static DateTime CurrentRoundEnd {
            get {
                using (var context = new FantasyPortfolio_DBEntities()) {
                    if (!context.Rounds.Any()) {
                        context.Rounds.Add(new Round { RoundEnds = DateTime.Now.AddDays(RoundDurationDays) });
                        context.SaveChanges();
                    }
                    var lastRound = context.Rounds.OrderByDescending(d => d.Id).FirstOrDefault();
                    return lastRound.RoundEnds;
                }
            }
        }

    }
}
