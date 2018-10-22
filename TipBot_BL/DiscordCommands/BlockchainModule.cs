using System;
using System.Net.Http;
using System.Threading.Tasks;
using Discord;
using Discord.Commands;
using Newtonsoft.Json;

namespace TipBot_BL.DiscordCommands {
    public class BlockchainModule : ModuleBase<SocketCommandContext> {
        private static readonly string ChainzURL = "http://chainz.cryptoid.info/grs/api.dws";
        //https://chainz.cryptoid.info/grs/api.dws?key=797f266685db&q=getdifficulty
        [Command("getbalance")]
        public async Task GetAddressBalance(string address) {
            var balance = await GetBalance(address);

            if (!string.IsNullOrEmpty(balance)) {
                await ReplyAsync($"The balance for the address {address} is {balance}");
            }
            else {
                await ReplyAsync($"Error getting the balance for the address {address}");
            }
        }

        public static async Task<string> GetBalance(string address) {
            using (var httpClient = new HttpClient()) {
                var res = await httpClient.GetStringAsync(new Uri($"{ChainzURL}?key=797f266685db&q=getbalance&a={address}"));
                return res;
            }
        }

      
        [Command("donations")]
        public async Task GetDonationBalances() {
            var embed = new EmbedBuilder();
            embed.WithTitle("Groestlcoin Donation Funds");

            //Marketing Current
            using (var httpClient = new HttpClient()) {
                var res = await httpClient.GetStringAsync(new Uri($"{ChainzURL}?key=797f266685db&q=getbalance&a=FqkPKgvb2jFv6GdVphgyVY2iFPcHHi3dx7"));
                if (decimal.TryParse(res, out var marketingCurrent)) {
                    embed.AddField("Marketing This Month (FqkPKgvb2jFv6GdVphgyVY2iFPcHHi3dx7)", marketingCurrent.ToString("N") + " " + Preferences.BaseCurrency);
                }

            }
            //Marketing Old
            using (var httpClient = new HttpClient()) {
                var res = await httpClient.GetStringAsync(new Uri($"{ChainzURL}?key=797f266685db&q=getbalance&a=FfNCiBxkU3ZmfqoPZk1MF8Wc2EsVLRXFBY"));
                if (decimal.TryParse(res, out var marketingOld)) {
                    embed.AddField("Marketing Previous Months (FfNCiBxkU3ZmfqoPZk1MF8Wc2EsVLRXFBY)", marketingOld.ToString("N") + " " + Preferences.BaseCurrency);
                }
            }

            //Dev Current
            using (var httpClient = new HttpClient()) {
                var res = await httpClient.GetStringAsync(new Uri($"{ChainzURL}?key=797f266685db&q=getbalance&a=FWN1qdiRrymSR6jbpbanLYqZpjkEaZouHN"));
                if (decimal.TryParse(res, out var devCurrent)) {
                    embed.AddField("Development This Month (FWN1qdiRrymSR6jbpbanLYqZpjkEaZouHN)", devCurrent.ToString("N") + " " + Preferences.BaseCurrency);
                }
            }

            //Dev Old
            using (var httpClient = new HttpClient()) {
                var res = await httpClient.GetStringAsync(new Uri($"{ChainzURL}?key=797f266685db&q=getbalance&a=Fo5Xvgc58JMsMXsfwEY8TvjxX2x4Tdm5jf"));
                if (decimal.TryParse(res, out var devOld)) {
                    embed.AddField("Development Previous Months (Fo5Xvgc58JMsMXsfwEY8TvjxX2x4Tdm5jf)", devOld.ToString("N") + " " + Preferences.BaseCurrency);
                }
            }
            await ReplyAsync("", false, embed);
        }
    }
}
