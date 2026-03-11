using System.Collections;
using System.Globalization;
using System.Net;
using System.Resources;

namespace Imaj.Web.Services.Localization
{
    public class UiMessageLocalizer : IUiMessageLocalizer
    {
        private static readonly Lazy<IReadOnlyDictionary<string, string>> TrToEnMap = new(BuildTrToEnMap);

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
    }
}
