namespace Imaj.Service.Results
{
    public class ServiceResult<T>
    {
        public T? Data { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public static ServiceResult<T> Success(T data, string? message = null)
        {
            return new ServiceResult<T>
            {
                Data = data,
                IsSuccess = true,
                Message = message
            };
        }

        public static ServiceResult<T> Fail(string message)
        {
            return new ServiceResult<T>
            {
                IsSuccess = false,
                Message = message
            };
        }
    }
}
