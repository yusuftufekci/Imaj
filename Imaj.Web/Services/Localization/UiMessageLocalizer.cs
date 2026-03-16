using System.Collections;
using System.Globalization;
using System.Net;
using System.Resources;
using System.Text;
using System.Text.RegularExpressions;

namespace Imaj.Web.Services.Localization
{
    public class UiMessageLocalizer : IUiMessageLocalizer
    {
        private static readonly Lazy<IReadOnlyDictionary<string, string>> TrToEnMap = new(BuildTrToEnMap);
        private static readonly Lazy<IReadOnlyDictionary<string, string>> NormalizedTrToEnMap = new(BuildNormalizedTrToEnMap);
        private static readonly Lazy<IReadOnlyList<LocalizedTemplate>> TrToEnTemplates = new(BuildTrToEnTemplates);

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

            if (TryLocalizeFromTemplate(decodedMessage, out localizedMessage))
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

            AddSupplementalTranslations(map);
            return map;
        }

        private static IReadOnlyList<LocalizedTemplate> BuildTrToEnTemplates()
        {
            var resourceManager = new ResourceManager("Imaj.Web.Resources.SharedResource", typeof(SharedResource).Assembly);
            var trSet = resourceManager.GetResourceSet(new CultureInfo("tr-TR"), true, true);
            var enSet = resourceManager.GetResourceSet(new CultureInfo("en-US"), true, true);
            var templates = new List<LocalizedTemplate>();

            if (trSet == null || enSet == null)
            {
                return templates;
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

                if (string.IsNullOrWhiteSpace(trValue) ||
                    string.IsNullOrWhiteSpace(enValues.GetValueOrDefault(key)) ||
                    !ContainsPlaceholder(trValue))
                {
                    continue;
                }

                if (!TryBuildTemplate(trValue, enValues[key], out var template))
                {
                    continue;
                }

                templates.Add(template);
            }

            return templates;
        }

        private static void AddSupplementalTranslations(IDictionary<string, string> map)
        {
            void Add(string tr, string en) => map[NormalizeForLookup(tr)] = en;

            Add("Urun bilgisi bos olamaz.", "Product information cannot be empty.");
            Add("Urun kaydi icin yetki kapsami bulunamadi.", "Permission scope for the product record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin urun olusturulamadi.", "The product could not be created because company scope information is unavailable.");
            Add("Urun kodu zorunludur.", "Product code is required.");
            Add("Ayni kod ile baska bir urun kaydi mevcut.", "Another product record with the same code already exists.");
            Add("Urun ID zorunludur.", "Product ID is required.");
            Add("Urun guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the product could not be found.");
            Add("En az bir gecerli dil secilmelidir.", "At least one valid language must be selected.");
            Add("Secilen dillerden en az biri gecersiz.", "At least one of the selected languages is invalid.");
            Add("Secilen fonksiyonlardan en az biri gecersiz veya kapsam disi.", "At least one of the selected functions is invalid or out of scope.");
            Add("Secilen urun kategorisi gecersiz veya kapsam disi.", "The selected product category is invalid or out of scope.");
            Add("Secilen urun grubu gecersiz veya kapsam disi.", "The selected product group is invalid or out of scope.");

            Add("Kullanici bilgisi bos olamaz.", "User information cannot be empty.");
            Add("Kullanici kaydi icin yetki kapsami bulunamadi.", "Permission scope for the user record could not be found.");
            Add("Kullanici kodu zorunludur.", "User code is required.");
            Add("Kullanici adi zorunludur.", "User name is required.");
            Add("Kullanici kodu en fazla 16 karakter olabilir.", "User code can be at most 16 characters.");
            Add("Kullanici adi en fazla 48 karakter olabilir.", "User name can be at most 48 characters.");
            Add("Sifre bos olamaz.", "Password cannot be empty.");
            Add("Sifre en fazla 32 karakter olabilir.", "Password can be at most 32 characters.");
            Add("En az bir rol secilmelidir.", "At least one role must be selected.");
            Add("Dil secimi zorunludur.", "Language selection is required.");
            Add("Company-bound kapsamda sirket bilgisi zorunludur.", "Company information is required for company-bound scope.");
            Add("Secilen dil kaydi bulunamadi.", "The selected language record could not be found.");
            Add("Secilen sirket kaydi bulunamadi.", "The selected company record could not be found.");
            Add("Secili rollerden en az biri gecersiz.", "At least one of the selected roles is invalid.");
            Add("CompanyID NULL kullanici icin en az bir sistem rolu (Role.Global=0) zorunludur.", "At least one system role (Role.Global=0) is required for a user with CompanyID NULL.");
            Add("Kullanici kaydedilirken hata olustu.", "An error occurred while saving the user.");
            Add("Guncellenecek kullanici bulunamadi.", "The user to update could not be found.");
            Add("Kullanici guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the user could not be found.");
            Add("Kullanici guncellenirken hata olustu.", "An error occurred while updating the user.");
            Add("Sifre degistirme bilgisi bos olamaz.", "Password change information cannot be empty.");
            Add("Kullanici oturumu bulunamadi.", "The user session could not be found.");
            Add("Mevcut sifre zorunludur.", "Current password is required.");
            Add("Yeni sifre zorunludur.", "New password is required.");
            Add("Mevcut sifre hatali.", "The current password is incorrect.");
            Add("Yeni sifre mevcut sifre ile ayni olamaz.", "The new password cannot be the same as the current password.");
            Add("Sifre degistirilirken hata olustu.", "An error occurred while changing the password.");

            Add("Kaynak kategorisi bilgisi bos olamaz.", "Resource category information cannot be empty.");
            Add("Kaynak kategorisi kaydi icin yetki kapsami bulunamadi.", "Permission scope for the resource category record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin kaynak kategorisi olusturulamadi.", "The resource category could not be created because company scope information is unavailable.");
            Add("Kaynak kategorisi ID zorunludur.", "Resource category ID is required.");
            Add("Kaynak kategorisi guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the resource category could not be found.");

            Add("Fonksiyon bilgisi bos olamaz.", "Function information cannot be empty.");
            Add("Fonksiyon kaydi icin yetki kapsami bulunamadi.", "Permission scope for the function record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin fonksiyon olusturulamadi.", "The function could not be created because company scope information is unavailable.");
            Add("Fonksiyon guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the function could not be found.");
            Add("Secilen rezervasyon araligi bulunamadi.", "The selected reservation range could not be found.");
            Add("Secilen urunlerden en az biri gecersiz veya kapsam disi.", "At least one of the selected products is invalid or out of scope.");
            Add("Secilen kaynak kategorilerinden en az biri gecersiz veya kapsam disi.", "At least one of the selected resource categories is invalid or out of scope.");
            Add("Rezervasyon bilgileri secilmeden kural eklenemez.", "Rules cannot be added without reservation information.");
            Add("Kural adlari bos olamaz.", "Rule names cannot be empty.");
            Add("Kural adi en fazla 32 karakter olabilir.", "Rule name can be at most 32 characters.");
            Add("Kural minimum degeri maksimum degerden buyuk olamaz.", "The rule minimum value cannot be greater than the maximum value.");
            Add("Her kural icin en az bir kaynak kategorisi secilmelidir.", "At least one resource category must be selected for each rule.");

            Add("Urun kategorisi bilgisi bos olamaz.", "Product category information cannot be empty.");
            Add("Urun kategorisi kaydi icin yetki kapsami bulunamadi.", "Permission scope for the product category record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin urun kategorisi olusturulamadi.", "The product category could not be created because company scope information is unavailable.");
            Add("Secilen vergi tipi gecersiz veya kapsam disi.", "The selected tax type is invalid or out of scope.");
            Add("Urun kategorisi ID zorunludur.", "Product category ID is required.");
            Add("Urun kategorisi guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the product category could not be found.");

            Add("İş durumu güncellendi.", "Job status updated.");

            Add("Gerekce bilgisi bos olamaz.", "Reason information cannot be empty.");
            Add("Gerekce kaydi icin yetki kapsami bulunamadi.", "Permission scope for the reason record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin gerekce olusturulamadi.", "The reason could not be created because company scope information is unavailable.");
            Add("Ayni kod ile baska bir gerekce kaydi mevcut.", "Another reason record with the same code already exists.");
            Add("Gerekce ID zorunludur.", "Reason ID is required.");
            Add("Gerekce guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the reason could not be found.");
            Add("Secilen gerekce kategorisi gecersiz.", "The selected reason category is invalid.");

            Add("Urun grubu bilgisi bos olamaz.", "Product group information cannot be empty.");
            Add("Urun grubu kaydi icin yetki kapsami bulunamadi.", "Permission scope for the product group record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin urun grubu olusturulamadi.", "The product group could not be created because company scope information is unavailable.");
            Add("Urun grubu ID zorunludur.", "Product group ID is required.");
            Add("Urun grubu guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the product group could not be found.");

            Add("Mazeret durumu güncellendi.", "Absence status updated.");
            Add("Mazeret tarihi güncellendi.", "Absence date updated.");
            Add("Mazeret bilgisi bos olamaz.", "Absence information cannot be empty.");
            Add("Mazeret kaydi icin yetki kapsami bulunamadi.", "Permission scope for the absence record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin mazeret olusturulamadi.", "The absence could not be created because company scope information is unavailable.");
            Add("Ilgili alani en fazla 32 karakter olabilir.", "The related field can be at most 32 characters.");
            Add("Secilen fonksiyon gecersiz veya kapsam disi.", "The selected function is invalid or out of scope.");
            Add("Secilen gerekce gecersiz veya kapsam disi.", "The selected reason is invalid or out of scope.");
            Add("Acilis durumu tanimli degil.", "The open state is not defined.");
            Add("Secilen kaynaklardan en az biri gecersiz veya kapsam disi.", "At least one of the selected resources is invalid or out of scope.");

            Add("Fatura durumu güncellendi.", "Invoice status updated.");

            Add("Kaynak bilgisi bos olamaz.", "Resource information cannot be empty.");
            Add("Kaynak kaydi icin yetki kapsami bulunamadi.", "Permission scope for the resource record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin kaynak olusturulamadi.", "The resource could not be created because company scope information is unavailable.");
            Add("Ayni kod ile baska bir kaynak kaydi mevcut.", "Another resource record with the same code already exists.");
            Add("Kaynak ID zorunludur.", "Resource ID is required.");
            Add("Kaynak guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the resource could not be found.");
            Add("Secilen kaynak kategorisi gecersiz veya kapsam disi.", "The selected resource category is invalid or out of scope.");

            Add("Mesai tipi bilgisi bos olamaz.", "Overtime type information cannot be empty.");
            Add("Mesai tipi kaydi icin yetki kapsami bulunamadi.", "Permission scope for the overtime type record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin mesai tipi olusturulamadi.", "The overtime type could not be created because company scope information is unavailable.");
            Add("Mesai tipi ID zorunludur.", "Overtime type ID is required.");
            Add("Mesai tipi guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the overtime type could not be found.");

            Add("Vergi tipi bilgisi bos olamaz.", "Tax type information cannot be empty.");
            Add("Vergi tipi kaydi icin yetki kapsami bulunamadi.", "Permission scope for the tax type record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin vergi tipi olusturulamadi.", "The tax type could not be created because company scope information is unavailable.");
            Add("Ayni kod ile baska bir vergi tipi kaydi mevcut.", "Another tax type record with the same code already exists.");
            Add("Vergi tipi ID zorunludur.", "Tax type ID is required.");
            Add("Vergi tipi guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the tax type could not be found.");

            Add("Gorev tipi bilgisi bos olamaz.", "Work type information cannot be empty.");
            Add("Gorev tipi kaydi icin yetki kapsami bulunamadi.", "Permission scope for the work type record could not be found.");
            Add("Company kapsam bilgisi olmadigi icin gorev tipi olusturulamadi.", "The work type could not be created because company scope information is unavailable.");
            Add("Gorev tipi ID zorunludur.", "Work type ID is required.");
            Add("Gorev tipi guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the work type could not be found.");

            Add("Müşteri başarıyla eklendi.", "Customer added successfully.");
            Add("Müşteri eklenirken bir hata oluştu. Lütfen bilgileri kontrol edip tekrar deneyin.", "An error occurred while adding the customer. Please check the information and try again.");
            Add("Müşteri güncellenirken bir hata oluştu. Lütfen tekrar deneyin.", "An error occurred while updating the customer. Please try again.");

            Add("Calisan bilgisi bos olamaz.", "Employee information cannot be empty.");
            Add("Calisan olusturma icin yetki kapsami bulunamadi.", "Permission scope for creating the employee could not be found.");
            Add("Company kapsam bilgisi olmadigi icin calisan olusturulamadi.", "The employee could not be created because company scope information is unavailable.");
            Add("Ayni kod ile baska bir calisan kaydi mevcut.", "Another employee record with the same code already exists.");
            Add("Calisan ID zorunludur.", "Employee ID is required.");
            Add("Calisan guncelleme icin yetki kapsami bulunamadi.", "Permission scope for updating the employee could not be found.");
            Add("Secilen fonksiyonlardan en az biri yetki kapsaminda degil.", "At least one of the selected functions is outside the permission scope.");
            Add("Secilen gorev tiplerinden en az biri company kapsaminda degil.", "At least one of the selected work types is outside the company scope.");
            Add("Secilen mesai tiplerinden en az biri company kapsaminda degil.", "At least one of the selected overtime types is outside the company scope.");
        }

        private static bool TryLocalizeFromTemplate(string message, out string localizedMessage)
        {
            foreach (var template in TrToEnTemplates.Value)
            {
                var match = template.Pattern.Match(message);
                if (!match.Success)
                {
                    continue;
                }

                localizedMessage = template.TargetTemplate;
                for (var index = 0; index < template.PlaceholderIndexes.Length; index++)
                {
                    var placeholderIndex = template.PlaceholderIndexes[index];
                    localizedMessage = localizedMessage.Replace(
                        "{" + placeholderIndex + "}",
                        match.Groups[index + 1].Value,
                        StringComparison.Ordinal);
                }

                return true;
            }

            localizedMessage = string.Empty;
            return false;
        }

        private static bool ContainsPlaceholder(string value)
        {
            return Regex.IsMatch(value, @"\{\d+\}");
        }

        private static bool TryBuildTemplate(string sourceTemplate, string targetTemplate, out LocalizedTemplate template)
        {
            var matches = Regex.Matches(sourceTemplate, @"\{(\d+)\}");
            if (matches.Count == 0)
            {
                template = default!;
                return false;
            }

            var builder = new StringBuilder("^");
            var placeholderIndexes = new List<int>();
            var currentIndex = 0;

            foreach (Match match in matches)
            {
                builder.Append(Regex.Escape(sourceTemplate.Substring(currentIndex, match.Index - currentIndex)));
                builder.Append("(.+?)");
                placeholderIndexes.Add(int.Parse(match.Groups[1].Value, CultureInfo.InvariantCulture));
                currentIndex = match.Index + match.Length;
            }

            builder.Append(Regex.Escape(sourceTemplate.Substring(currentIndex)));
            builder.Append("$");

            template = new LocalizedTemplate(
                new Regex(builder.ToString(), RegexOptions.CultureInvariant | RegexOptions.Singleline),
                targetTemplate,
                placeholderIndexes.ToArray());
            return true;
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

        private sealed class LocalizedTemplate
        {
            public LocalizedTemplate(Regex pattern, string targetTemplate, int[] placeholderIndexes)
            {
                Pattern = pattern;
                TargetTemplate = targetTemplate;
                PlaceholderIndexes = placeholderIndexes;
            }

            public Regex Pattern { get; }
            public string TargetTemplate { get; }
            public int[] PlaceholderIndexes { get; }
        }
    }
}
