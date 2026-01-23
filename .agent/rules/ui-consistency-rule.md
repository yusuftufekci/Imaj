---
trigger: always_on
---

UI CONSISTENCY RULE (GLOBAL)

Bu projede geliştirilen veya modernize edilen her sayfa, mevcut “UI Pattern Catalog” ve ortak “PageShell” iskeletine (Header/Breadcrumb/Title/Actions/Content) uygun olmak zorundadır. 
Tüm spacing, tipografi, renk ve etkileşim davranışları yalnızca ortak component library ve design token’lar üzerinden uygulanır. 
Bir sayfa için yeni bir görsel pattern/yerleşim gerekiyorsa, önce shared pattern/component olarak tasarlanır ve katalog güncellenmeden sayfa içinde tekil/özel çözüm üretilmez.
Her yeni sayfa çıktısında, ilgili sayfanın hangi mevcut pattern’e oturduğu (List/Form/Detail/Wizard/Dashboard) açıkça belirtilir ve bu pattern dışına çıkılıyorsa gerekçe yazılır.