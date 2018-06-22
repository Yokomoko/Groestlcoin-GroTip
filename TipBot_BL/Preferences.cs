using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TipBot_BL.Properties;

namespace TipBot_BL {
    public class Preferences {
        public static string Database => Settings.Default.Database;
        public static string DiscordToken => Settings.Default.DiscordToken;


        #region Channels
        public static ulong PriceCheckChannel {
            get { return Settings.Default.PriceCheckChannel; }
            set {
                Settings.Default.PriceCheckChannel = value;
                Settings.Default.Save();
            }
        }

        public static ulong TipBotChannel {
            get { return Settings.Default.TipBotChannel; }
            set {
                Settings.Default.TipBotChannel = value;
                Settings.Default.Save();
            }
        }

        public static ulong FantasyChannel {
            get { return Settings.Default.FantasyChannel; }
            set {
                Settings.Default.FantasyChannel = value;
                Settings.Default.Save();
            }
        }
        public static ulong GuildId => Settings.Default.GuildId;
        public static string QT_IP => Settings.Default.QT_IP;
        public static string QT_Username => Settings.Default.QT_Username;
        public static string QT_Password => Settings.Default.QT_Password;
        public static string BaseCurrency => Settings.Default.BaseCurrency;
        public static string ExplorerPrefix => Settings.Default.ExplorerPrefix;
        public static string ExplorerSuffix => Settings.Default.ExplorerSuffix;

        #endregion


        public static DateTime NextRelease {
            get { return Settings.Default.NextRelease; }
            set {
                Settings.Default.NextRelease = value;
                Settings.Default.Save();
            }
        }




    }
}
