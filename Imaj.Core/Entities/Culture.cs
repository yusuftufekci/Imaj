namespace Imaj.Core.Entities
{
    public class Culture : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string DateSep { get; set; } = string.Empty;
        public string TimeSep { get; set; } = string.Empty;
        public string DigitSep { get; set; } = string.Empty;
        public string DecimalSep { get; set; } = string.Empty;
        public bool FormattedNumeric { get; set; }
        public bool PadWithZero { get; set; }
        public byte MoneyWidth { get; set; }
        public byte? MoneyDecimals { get; set; }
    }
}
