using System;
using TipBot_BL.Interfaces;

namespace TipBot_BL.POCO {
    public class TelegramUsers : Users {
        public int Id { get; set; }
        public ulong UserId { get; set; }
        public DateTime? LastReceived { get; set; }
        public DateTime? LastSent { get; set; }
        public decimal Balance { get; set; }
        public string Address { get; set; }
        public bool? RainOptIn { get; set; } = false;
        public void GetBalance(string userId){
            throw new NotImplementedException();
        }

        public void GetAddress(string userId){
            throw new NotImplementedException();
        }
    }
}
