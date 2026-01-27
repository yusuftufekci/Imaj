namespace Imaj.Core.Constants
{
    /// <summary>
    /// Uygulama genelinde kullanılan hata mesajları.
    /// Magic string'leri önler ve tutarlılık sağlar.
    /// </summary>
    public static class ErrorMessages
    {
        // Entity bulunamadı hataları
        public const string CustomerNotFound = "Müşteri bulunamadı.";
        public const string JobNotFound = "İş bulunamadı.";
        public const string InvoiceNotFound = "Fatura bulunamadı.";
        public const string UserNotFound = "Kullanıcı bulunamadı.";
        public const string RecordNotFound = "Kayıt bulunamadı.";

        // Validation hataları
        public const string RequiredField = "{0} alanı zorunludur.";
        public const string InvalidFormat = "{0} formatı geçersiz.";
        public const string MaxLengthExceeded = "{0} alanı en fazla {1} karakter olabilir.";
        public const string MinLengthRequired = "{0} alanı en az {1} karakter olmalıdır.";

        // İşlem hataları
        public const string OperationFailed = "İşlem başarısız oldu.";
        public const string SaveFailed = "Kayıt sırasında bir hata oluştu.";
        public const string UpdateFailed = "Güncelleme sırasında bir hata oluştu.";
        public const string DeleteFailed = "Silme sırasında bir hata oluştu.";
        public const string DuplicateRecord = "Bu kayıt zaten mevcut.";

        // Yetkilendirme hataları
        public const string Unauthorized = "Bu işlem için yetkiniz bulunmamaktadır.";
        public const string InvalidCredentials = "Kullanıcı adı veya şifre hatalı.";
        public const string SessionExpired = "Oturumunuz sona erdi. Lütfen tekrar giriş yapın.";

        // Genel hatalar
        public const string ServerError = "Sunucu hatası oluştu. Lütfen daha sonra tekrar deneyin.";
        public const string InvalidRequest = "Geçersiz istek.";
    }
}
