namespace Imaj.Service.Results
{
    /// <summary>
    /// Tüm service result sınıfları için base class.
    /// Ortak property ve metodları içerir.
    /// </summary>
    public abstract class ServiceResultBase
    {
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }
        
        /// <summary>
        /// Birden fazla hata mesajı için (örn: validation hataları)
        /// </summary>
        public List<string> Errors { get; set; } = new();
        
        /// <summary>
        /// HTTP status code (Controller'da kullanılabilir)
        /// </summary>
        public int StatusCode { get; set; } = 200;
    }

    /// <summary>
    /// Generic service result wrapper.
    /// Tüm service method'ları bu yapı ile dönüş yapar.
    /// </summary>
    public class ServiceResult<T> : ServiceResultBase
    {
        public T? Data { get; set; }

        /// <summary>
        /// Başarılı sonuç döner
        /// </summary>
        public static ServiceResult<T> Success(T data, string? message = null)
        {
            return new ServiceResult<T>
            {
                Data = data,
                IsSuccess = true,
                Message = message,
                StatusCode = 200
            };
        }

        /// <summary>
        /// Hata sonucu döner (400 Bad Request)
        /// </summary>
        public static ServiceResult<T> Fail(string message)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = message,
                StatusCode = 400
            };
        }

        /// <summary>
        /// Kayıt bulunamadı sonucu döner (404 Not Found)
        /// </summary>
        public static ServiceResult<T> NotFound(string message = "Kayıt bulunamadı.")
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = message,
                StatusCode = 404
            };
        }

        /// <summary>
        /// Validation hatası sonucu döner (400 Bad Request)
        /// </summary>
        public static ServiceResult<T> ValidationError(List<string> errors)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = "Doğrulama hatası.",
                Errors = errors,
                StatusCode = 400
            };
        }

        /// <summary>
        /// Sunucu hatası sonucu döner (500 Internal Server Error)
        /// </summary>
        public static ServiceResult<T> ServerError(string message = "Sunucu hatası oluştu.")
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = message,
                StatusCode = 500
            };
        }
    }

    /// <summary>
    /// Data içermeyen service result.
    /// </summary>
    public class ServiceResult : ServiceResultBase
    {
        public static ServiceResult Success(string? message = null)
        {
            return new ServiceResult
            {
                IsSuccess = true,
                Message = message,
                StatusCode = 200
            };
        }

        public static ServiceResult Fail(string message)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = message,
                StatusCode = 400
            };
        }

        public static ServiceResult NotFound(string message = "Kayıt bulunamadı.")
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = message,
                StatusCode = 404
            };
        }

        public static ServiceResult ValidationError(List<string> errors)
        {
            return new ServiceResult
            {
                IsSuccess = false,
                Message = "Doğrulama hatası.",
                Errors = errors,
                StatusCode = 400
            };
        }
    }
}
