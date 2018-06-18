using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Discord.Commands;
using LiteDB;
using Newtonsoft.Json;
using TipBot_BL.Interfaces;
using TipBot_BL.Properties;
using TipBot_BL.QT;

namespace TipBot_BL.POCO {

    public class DiscordUsers : ModuleBase<SocketCommandContext> {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public DateTime? LastReceived { get; set; }
        public DateTime? LastSent { get; set; }
        public bool? RainOptIn { get; set; }

        public static List<DiscordUsers> UserList {
            get {
                var list = new List<DiscordUsers>();
                using (var db = new LiteDatabase(Settings.Default.Database)) {
                    var discordUsers = db.GetCollection<DiscordUsers>("discordusers");
                    var users = discordUsers.Find(Query.All(Query.Descending));
                    foreach (var user in users) {
                        list.Add(new DiscordUsers {
                            UserId = user.UserId,
                            RainOptIn = user.RainOptIn
                        });
                    }

                    return list;
                }
            }
        }

        public static string OptIn(ulong userId) {
            using (var db = new LiteDatabase(Settings.Default.Database)) {
                var discordUsers = db.GetCollection<DiscordUsers>("discordusers");
                var user = discordUsers.Find(d => d.UserId == userId).FirstOrDefault();
                if (user == null) {
                    user = new DiscordUsers();
                    user.UserId = userId;
                    discordUsers.Insert(user);
                }
                if (user.RainOptIn.GetValueOrDefault(false)) {
                    return "You are already opted in!";
                }
                user.RainOptIn = true;

                discordUsers.Update(user);

                return "You have opted in to rain events";
            }
        }



        public static string OptOut(ulong userId) {
            using (var db = new LiteDatabase(Settings.Default.Database)) {
                var discordUsers = db.GetCollection<DiscordUsers>("discordusers");
                var user = discordUsers.Find(d => d.UserId == userId).FirstOrDefault();
                if (user == null) {
                    user = new DiscordUsers();
                    user.UserId = userId;
                    discordUsers.Insert(user);
                }
                if (!user.RainOptIn.GetValueOrDefault(false)) {
                    return "You are already opted out!";
                }
                user.RainOptIn = false;
                discordUsers.Update(user);
                return "You have opted out of rain events";
            }
        }


    }
}
