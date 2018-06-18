using System;

namespace TipBot_BL.Interfaces {
    public interface Users {
        int Id { get; set; }
        ulong UserId { get; set; }
        DateTime? LastReceived { get; set; }
        DateTime? LastSent { get; set; }
        bool? RainOptIn { get; set; }
    }
}
