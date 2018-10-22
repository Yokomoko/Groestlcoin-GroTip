using System;
using System.Diagnostics;
using System.Linq;

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
                    Debug.Assert(lastRound != null, nameof(lastRound) + " != null");
                    return lastRound.Id;
                }
            }
        }

        public static int RoundDurationDays = 7;

        public static DateTime CurrentRoundEnd {
            get {
                using (var context = new FantasyPortfolio_DBEntities()) {
                    if (!context.Rounds.Any()) {
                        context.Rounds.Add(new Round { RoundEnds = DateTime.Now.AddDays(RoundDurationDays) });
                        context.SaveChanges();
                    }
                    var lastRound = context.Rounds.OrderByDescending(d => d.Id).FirstOrDefault();
                    Debug.Assert(lastRound != null, nameof(lastRound) + " != null");
                    return lastRound.RoundEnds;
                }
            }
        }

    }
}
