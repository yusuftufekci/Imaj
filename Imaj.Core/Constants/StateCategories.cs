namespace Imaj.Core.Constants
{
    /// <summary>
    /// State tablosundaki Category değerleri için sabit değerler.
    /// Magic string kullanımını önlemek için bu sabitleri kullanın.
    /// </summary>
    public static class StateCategories
    {
        /// <summary>
        /// İş (Job) kayıtları için durum kategorisi
        /// </summary>
        public const string Job = "Job";

        /// <summary>
        /// Fatura kayıtları için durum kategorisi
        /// </summary>
        public const string Invoice = "Invoice";

        /// <summary>
        /// Müşteri kayıtları için durum kategorisi
        /// </summary>
        public const string Customer = "Customer";

        /// <summary>
        /// Ürün kayıtları için durum kategorisi
        /// </summary>
        public const string Product = "Product";
    }
}
