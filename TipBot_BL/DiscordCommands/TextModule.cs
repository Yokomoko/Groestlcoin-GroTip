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
    public class TextModule : ModuleBase<SocketCommandContext> {

        public enum Ranks {
            General_of_the_Army,
            General,
            Lieutenant_General,
            Major_General,
            Brigadier_General,
            Colonel,
            Lieutenant_Colonel,
            Major,
            Captain,
            First_Lieutenant,
            Second_Lieutenant,
            Chief_Warrant_Officer_5,
            Chief_Warrant_Officer_4,
            Chief_Warrant_Officer_3,
            Chief_Warrant_Officer_2,
            Warrant_Officer_1,
            Sergeant_Major_of_the_Army,
            Command_Sergeant_Major,
            Sergeant_Major,
            First_Sergeant,
            Master_Sergeant,
            Sergeant_First_Class,
            Staff_Sergeant,
            Sergeant,
            Corporal,
            Specialist,
            Private_First_Class,
            Private_Second_Class,
            Private
        }

        public static Color ChangeColorBrightness(Color color, float correctionFactor) {
            float red = (float)color.R;
            float green = (float)color.G;
            float blue = (float)color.B;

            if (correctionFactor < 0) {
                correctionFactor = 1 + correctionFactor;
                red *= correctionFactor;
                green *= correctionFactor;
                blue *= correctionFactor;
            }
            else {
                red = (255 - red) * correctionFactor + red;
                green = (255 - green) * correctionFactor + green;
                blue = (255 - blue) * correctionFactor + blue;
            }

            return Color.FromArgb(color.A, (int)red, (int)green, (int)blue);
        }


        //[Command("createranks")]
        //public async Task CreateRanks() {
        //    var guild = Context.Guild;
        //    var roles = guild.Roles;

        //    Color color = Color.FromArgb(0, 28, 59, 59);

        //    foreach (var rank in Enum.GetNames(typeof(Ranks))) {
        //        if (roles.All(d => d.Name != rank.Replace("_", " "))) {
        //            var discordColor = new Discord.Color(color.R, color.G, color.B);
        //            await guild.CreateRoleAsync(rank.Replace("_", " "), null, discordColor);
        //            color = Color.FromArgb(0, color.R + 7, color.G + 7, color.B + 7);
        //        }
        //    }
        //}

        //[Command("removeranks")]
        //public async Task RemoveRanks() {
        //    var guild = Context.Guild;
        //    var roles = guild.Roles;

        //    foreach (var rank in Enum.GetNames(typeof(Ranks))) {
        //        var role = roles.FirstOrDefault(d => d.Name == rank.Replace("_", " "));
        //        role?.DeleteAsync();
        //    }
        //}

        [Command("ping")]
        public async Task PingAsync() {
            await ReplyAsync("Pong!");
        }

        [Command("commands")]
        public async Task Commands() {
            var embed = new EmbedBuilder();
            embed.WithTitle("Commands for GroTip");

            var tipSb = new StringBuilder();
            tipSb.AppendLine("**-rain:** Rains some of your funds to other users that have opted in");
            tipSb.AppendLine("**-optin:** Opts you in to receive random Groestlcoin rains from the tip bot");
            tipSb.AppendLine("**-optout:** Opts you outof receiving random Groestlcoin rains from the tip bot :(");
            tipSb.AppendLine("**-address:** Gets your deposit address to use the tipping functionality - Optional: add 'qr' to the end to get a QR code (-address qr)");
            tipSb.AppendLine($"**-balance:** Gets your {Preferences.BaseCurrency} balance from the bot");
            tipSb.AppendLine("**-withdraw:** Withdraws your balance. Syntax: -withdraw [address]");
            tipSb.AppendLine("**-tip:** Tip another user. Syntax: -tip [@user] [amount] OR -tip [amount] [@user]");
            tipSb.AppendLine("**-giftrandom:** Tip random people. Syntax: -giftrandom [amount] [number of people]");
            tipSb.AppendLine("**-house:** Gets the balance of the house (i.e. the balance of the bot)");
            tipSb.AppendLine("**-houseaddress:** Gets the address of the bot. Optional: add 'qr' to the end to get a QR code (-houseaddress qr)");
            tipSb.AppendLine(Environment.NewLine);
            tipSb.AppendLine("**-flip:** Simple coin flip game! Bet your Groestlcoin on either Heads or Tails for a chance to win more! 2% house edge Syntax: -flip [heads/tails] [amount]");
            tipSb.AppendLine(Environment.NewLine);
            tipSb.AppendLine($"**$**: Price check a coin! Syntax: $[ticker], for example: ${Preferences.BaseCurrency} (Alt: -price [ticker])");
            tipSb.AppendLine("**-nextrelease**: Gives a countdown to the next release");
            embed.WithDescription(tipSb.ToString());
            embed.WithFooter("Developed by Yokomoko (FYoKoGrSXGpTavNFVbvW18UYxo6JVbUDDa)");

            await ReplyAsync("", false, embed);
        }

        [Command("setreleasedate")]
        public async Task SetReleaseDate(string date, string time) {
            if (DiscordClientNew._client != null) {
                var id = DiscordClientNew._client.Guilds.FirstOrDefault(d => Context.Guild != null && d.Id == Context.Guild.Id)?.Users.FirstOrDefault(d => d.Id == Context.User.Id);

                if (id != null) {
                    if (!id.GuildPermissions.Administrator) {
                        return;
                    }
                }
            }

            var datetime = $"{date} {time}";

            DateTime oDate = Convert.ToDateTime(datetime);

            Preferences.NextRelease = oDate;
            await ReplyAsync($"Release Date Set");
        }

        [Command("nextrelease")]
        public async Task NextRelease() {
            var dt = Preferences.NextRelease;

            TimeSpan span = (dt - DateTime.Now);

            var format = $"{span.Days} days, {span.Hours} hours, {span.Minutes} minutes, {span.Seconds} seconds";


            await ReplyAsync($"The next release is in {format}");
        }




    }
}