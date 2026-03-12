using System.Collections;
using System.Globalization;
using System.Net;
using System.Resources;
using System.Text;

namespace Imaj.Web.Services.Localization
{
    public class UiMessageLocalizer : IUiMessageLocalizer
    {
        private static readonly Lazy<IReadOnlyDictionary<string, string>> TrToEnMap = new(BuildTrToEnMap);
        private static readonly Lazy<IReadOnlyDictionary<string, string>> NormalizedTrToEnMap = new(BuildNormalizedTrToEnMap);

        public string Localize(string? message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                return string.Empty;
            }

            var decodedMessage = WebUtility.HtmlDecode(message).Trim();

            if (!CultureInfo.CurrentUICulture.Name.StartsWith("en", StringComparison.OrdinalIgnoreCase))
            {
                return decodedMessage;
            }

            if (TrToEnMap.Value.TryGetValue(decodedMessage, out var localizedMessage) &&
                !string.IsNullOrWhiteSpace(localizedMessage))
            {
                return localizedMessage;
            }

            var normalizedMessage = NormalizeForLookup(decodedMessage);
            if (!string.IsNullOrWhiteSpace(normalizedMessage) &&
                NormalizedTrToEnMap.Value.TryGetValue(normalizedMessage, out localizedMessage) &&
                !string.IsNullOrWhiteSpace(localizedMessage))
            {
                return localizedMessage;
            }

            return decodedMessage;
        }

        private static IReadOnlyDictionary<string, string> BuildTrToEnMap()
        {
            var resourceManager = new ResourceManager("Imaj.Web.Resources.SharedResource", typeof(SharedResource).Assembly);
            var trSet = resourceManager.GetResourceSet(new CultureInfo("tr-TR"), true, true);
            var enSet = resourceManager.GetResourceSet(new CultureInfo("en-US"), true, true);
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            if (trSet == null || enSet == null)
            {
                return map;
            }

            var enValues = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (DictionaryEntry entry in enSet)
            {
                if (entry.Key is string key && entry.Value is string value)
                {
                    enValues[key] = value;
                }
            }

            foreach (DictionaryEntry entry in trSet)
            {
                if (entry.Key is not string key || entry.Value is not string trValue)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(trValue))
                {
                    continue;
                }

                if (enValues.TryGetValue(key, out var enValue) && !string.IsNullOrWhiteSpace(enValue))
                {
                    map[trValue] = enValue;
                }
            }

            return map;
        }

        private static IReadOnlyDictionary<string, string> BuildNormalizedTrToEnMap()
        {
            var exactMap = BuildTrToEnMap();
            var map = new Dictionary<string, string>(StringComparer.Ordinal);

            foreach (var entry in exactMap)
            {
                var normalizedKey = NormalizeForLookup(entry.Key);
                if (string.IsNullOrWhiteSpace(normalizedKey) || string.IsNullOrWhiteSpace(entry.Value))
                {
                    continue;
                }

                map[normalizedKey] = entry.Value;
            }

            return map;
        }

        private static string NormalizeForLookup(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return string.Empty;
            }

            var normalizedInput = value
                .Replace('ı', 'i')
                .Replace('İ', 'I')
                .Replace('ğ', 'g')
                .Replace('Ğ', 'G')
                .Replace('ü', 'u')
                .Replace('Ü', 'U')
                .Replace('ş', 's')
                .Replace('Ş', 'S')
                .Replace('ö', 'o')
                .Replace('Ö', 'O')
                .Replace('ç', 'c')
                .Replace('Ç', 'C');

            var formD = normalizedInput.Normalize(NormalizationForm.FormD);
            var builder = new StringBuilder(formD.Length);

            foreach (var character in formD)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
                {
                    builder.Append(character);
                }
            }

            return string.Join(" ", builder
                    .ToString()
                    .Normalize(NormalizationForm.FormC)
                    .Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
                .Trim()
                .ToLowerInvariant();
        }
    }
}
