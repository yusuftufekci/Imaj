using System.Globalization;
using System.Text;

namespace Imaj.Web.Models
{
    public static class JobProductDisplayOrder
    {
        public static int GetRank(JobProductItem product)
        {
            var text = Normalize(string.Join(' ', product.Code, product.Name, product.CategoryName));

            if (text.Contains("CAFE", StringComparison.Ordinal) ||
                text.Contains("KAFE", StringComparison.Ordinal) ||
                text.Contains("KAFETERYA", StringComparison.Ordinal))
            {
                return 92;
            }

            if ((text.Contains("FAZLA", StringComparison.Ordinal) &&
                 text.Contains("MESAI", StringComparison.Ordinal)) ||
                text.Contains("OVERTIME", StringComparison.Ordinal))
            {
                return 91;
            }

            if (text.Contains("OPERATOR", StringComparison.Ordinal))
            {
                return 90;
            }

            return text.Contains("SUIT", StringComparison.Ordinal) ? 0 : 10;
        }

        private static string Normalize(string value)
        {
            var normalized = value.Normalize(NormalizationForm.FormD);
            var chars = normalized
                .Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                .ToArray();

            return new string(chars).Normalize(NormalizationForm.FormC).ToUpperInvariant();
        }
    }
}
