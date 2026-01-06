using System;
using System.Security.Cryptography;
using System.Text;

namespace Imaj.Service.Helpers
{
    public static class PasswordHelper
    {
        // Example Legacy MD5 Hash - Replace with actual legacy logic
        public static string HashPassword(string password)
        {
            using (var md5 = MD5.Create())
            {
                var inputBytes = Encoding.ASCII.GetBytes(password);
                var hashBytes = md5.ComputeHash(inputBytes);
                
                var sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
                return sb.ToString();
            }
        }
    }
}
