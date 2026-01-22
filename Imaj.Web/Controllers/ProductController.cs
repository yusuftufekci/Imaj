using Imaj.Web.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Imaj.Web.Controllers
{
    public class ProductController : Controller
    {
        [HttpPost]
        public IActionResult Search([FromBody] ProductFilterModel? filter)
        {
            var products = GenerateMockProducts();

            var f = filter ?? new ProductFilterModel();

            if (!string.IsNullOrWhiteSpace(f.Code))
                 products = products.Where(p => p.Code != null && p.Code.Contains(f.Code, StringComparison.OrdinalIgnoreCase)).ToList();

            // Add other filters logic if needed for more realistic mock behavior
            if (f.IsInvalid)
            {
                // Filter logic for invalid...
            }

            var totalCount = products.Count;
            var items = products
                .Skip((f.Page - 1) * f.PageSize)
                .Take(f.PageSize)
                .ToList();

            return Json(new { items, totalCount, page = f.Page, pageSize = f.PageSize });
        }

        private List<ProductSearchResult> GenerateMockProducts()
        {
            var list = new List<ProductSearchResult>();
            list.Add(new ProductSearchResult { Code = "PRD001", Name = "Kurgu Hizmeti", Category = "Post-Prodüksiyon", ProductGroup = "Video Kurgu" });
            list.Add(new ProductSearchResult { Code = "PRD002", Name = "Ses Miksaj", Category = "Ses", ProductGroup = "Dublaj & Miks" });
            list.Add(new ProductSearchResult { Code = "PRD003", Name = "Renk Düzenleme", Category = "Post-Prodüksiyon", ProductGroup = "Color Grading" });
            list.Add(new ProductSearchResult { Code = "PRD004", Name = "VFX Kompoziting", Category = "VFX", ProductGroup = "Görsel Efekt" });
            list.Add(new ProductSearchResult { Code = "PRD005", Name = "Altyazı Çeviri", Category = "Lokalizasyon", ProductGroup = "Çeviri" });

            for (int i = 6; i <= 35; i++)
            {
                list.Add(new ProductSearchResult { Code = $"PRD{i:000}", Name = $"Hizmet {i}", Category = "Genel", ProductGroup = "Standart İşlem" });
            }

            return list;
        }
    }
}
