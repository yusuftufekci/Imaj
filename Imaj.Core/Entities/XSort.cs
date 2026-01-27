namespace Imaj.Core.Entities
{
    public class XSort : BaseEntity
    {
        public decimal SortID { get; set; }
        public decimal LanguageID { get; set; }
        public string Name { get; set; } = string.Empty;
        public short Stamp { get; set; }

        // Sort entity henüz oluşturulmadı, sonraki batch'te yapılacak ama burada referans verebiliriz
        // Derleme hatası almamak için şimdilik Sort tipini yorum satırı yapıyoruz veya Sort entity'sini sonraki adımda hemen oluşturuyoruz.
        // Hata almamak için Sort entity'sini hemen burada oluşturmak en iyisi.
        // Ancak batch mantığında Sort Batch 6'daydı. 
        // Sorun değil, burada navigation'ı ekleyip Sort'u sonra eklesem hata verir mi? Evet.
        // O zaman Sort entity'sini de hemen bir sonraki write_to_file ile ekleyeceğim.
        
        public virtual Sort? Sort { get; set; }
        public virtual Language? Language { get; set; }
    }
}
