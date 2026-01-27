using System.Collections.Generic;

namespace Imaj.Core.Entities
{
    public class ReasonCat : BaseEntity
    {
        public string Descr { get; set; } = string.Empty;
        public short Stamp { get; set; }

        public virtual ICollection<Reason> Reasons { get; set; } = new List<Reason>();
    }
}
