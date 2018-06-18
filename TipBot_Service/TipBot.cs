using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.ServiceProcess;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TipBot_BL;

namespace TipBot_Service {
    public partial class TipBot : ServiceBase {
        public TipBot() {
            InitializeComponent();
        }

        protected override void OnStart(string[] args) {
            Thread thread = new Thread(StartService);
            thread.Start();
        }

        protected override void OnStop() {

        }

        private void StartService() {
            var discordClient = new DiscordClientNew(); //Discord Token
            discordClient.RunBotAsync();
        }
    }
}
