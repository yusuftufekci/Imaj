using System;
using System.Collections.Generic;
using System.Linq;

namespace Imaj.Web.Models
{
    public class ApiResponse<T>
    {
        public bool Success { get; init; }
        public T? Data { get; init; }
        public string? Message { get; init; }
        public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

        public static ApiResponse<T> Ok(T? data, string? message = null)
        {
            return new ApiResponse<T>
            {
                Success = true,
                Data = data,
                Message = message
            };
        }

        public static ApiResponse<T> Fail(string? message, IEnumerable<string>? errors = null)
        {
            return new ApiResponse<T>
            {
                Success = false,
                Message = message,
                Errors = errors?.ToArray() ?? Array.Empty<string>()
            };
        }
    }
}
