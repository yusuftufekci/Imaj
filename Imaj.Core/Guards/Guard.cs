using System;

namespace Imaj.Core.Guards
{
    /// <summary>
    /// Guard clause helper sınıfı.
    /// Parametre doğrulama ve null kontrolleri için kullanılır.
    /// </summary>
    public static class Guard
    {
        /// <summary>
        /// Null değer kontrolü yapar.
        /// </summary>
        /// <typeparam name="T">Referans tipi</typeparam>
        /// <param name="value">Kontrol edilecek değer</param>
        /// <param name="parameterName">Parametre adı (hata mesajı için)</param>
        /// <exception cref="ArgumentNullException">Değer null ise fırlatılır</exception>
        public static void AgainstNull<T>(T? value, string parameterName) where T : class
        {
            if (value == null)
                throw new ArgumentNullException(parameterName, $"{parameterName} cannot be null.");
        }

        /// <summary>
        /// Null veya boş string kontrolü yapar.
        /// </summary>
        /// <param name="value">Kontrol edilecek string</param>
        /// <param name="parameterName">Parametre adı</param>
        /// <exception cref="ArgumentException">Değer null veya boş ise fırlatılır</exception>
        public static void AgainstNullOrEmpty(string? value, string parameterName)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException($"{parameterName} cannot be null or empty.", parameterName);
        }

        /// <summary>
        /// Negatif değer kontrolü yapar.
        /// </summary>
        /// <param name="value">Kontrol edilecek değer</param>
        /// <param name="parameterName">Parametre adı</param>
        /// <exception cref="ArgumentOutOfRangeException">Değer negatif ise fırlatılır</exception>
        public static void AgainstNegative(decimal value, string parameterName)
        {
            if (value < 0)
                throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} cannot be negative.");
        }

        /// <summary>
        /// Sıfır veya negatif değer kontrolü yapar.
        /// </summary>
        /// <param name="value">Kontrol edilecek değer</param>
        /// <param name="parameterName">Parametre adı</param>
        /// <exception cref="ArgumentOutOfRangeException">Değer sıfır veya negatif ise fırlatılır</exception>
        public static void AgainstZeroOrNegative(decimal value, string parameterName)
        {
            if (value <= 0)
                throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} must be greater than zero.");
        }

        /// <summary>
        /// Aralık dışı değer kontrolü yapar.
        /// </summary>
        /// <param name="value">Kontrol edilecek değer</param>
        /// <param name="min">Minimum değer (dahil)</param>
        /// <param name="max">Maximum değer (dahil)</param>
        /// <param name="parameterName">Parametre adı</param>
        /// <exception cref="ArgumentOutOfRangeException">Değer aralık dışında ise fırlatılır</exception>
        public static void AgainstOutOfRange(int value, int min, int max, string parameterName)
        {
            if (value < min || value > max)
                throw new ArgumentOutOfRangeException(parameterName, $"{parameterName} must be between {min} and {max}.");
        }

        /// <summary>
        /// Default değer kontrolü yapar (struct tipler için).
        /// </summary>
        /// <typeparam name="T">Value type</typeparam>
        /// <param name="value">Kontrol edilecek değer</param>
        /// <param name="parameterName">Parametre adı</param>
        /// <exception cref="ArgumentException">Değer default ise fırlatılır</exception>
        public static void AgainstDefault<T>(T value, string parameterName) where T : struct
        {
            if (value.Equals(default(T)))
                throw new ArgumentException($"{parameterName} cannot be default value.", parameterName);
        }
    }
}
