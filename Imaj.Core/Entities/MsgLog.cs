namespace Imaj.Core.Entities
{
    public class MsgLog : BaseEntity
    {
        public DateTime LogDT { get; set; } // smalldatetime
        public decimal LogActionID { get; set; } // User.LogActionID scriptte yok ama LogAction'a referans mantıklı
        // Scriptte MsgLog kolonları: ID, LogDT, SenderID, ReceiverID, Message, ReadFlag...
        // Script'e göre gidelim:
        
        public decimal SenderID { get; set; }
        public decimal ReceiverID { get; set; }
        public string Message { get; set; } = string.Empty;
        public bool ReadFlag { get; set; }
        public short Stamp { get; set; }
        
        public virtual User? Sender { get; set; }
        public virtual User? Receiver { get; set; }
    }
}
