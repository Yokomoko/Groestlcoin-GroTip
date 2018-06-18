using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using TipBot_BL;

namespace Tip {
    class Program {
        static void Main() {
            var discordClient = new DiscordClientNew(); //Discord Token
            discordClient.RunBotAsync();


            //discordClient.Start();
            Console.ReadLine();
        }


    }
}
