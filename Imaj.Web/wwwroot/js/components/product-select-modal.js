/**
 * Product Select Modal Component (v2 - Using BaseSelectModal patterns)
 * Ürün seçim modal'ı için Alpine.js component fonksiyonu
 * Single ve Multi-select desteği var
 */

function productSelectModal() {
    return {
        // Modal State
        isOpen: false,
        targetId: '',
        isMultiSelect: false,

        // Filtre alanları
        filter: {
            code: '',
            category: '',
            productGroup: '',
            function: '',
            isInvalid: false,
            page: 1,
            pageSize: 10
        },

        // Dropdown verileri
        categories: [],
        productGroups: [],
        functions: [],

        // Sonuç verileri
        items: [],
        totalCount: 0,
        page: 1,
        hasSearched: false,

        // Multi-select için seçilen öğeler
        selectedItems: [],

        /**
         * Component başlatma - Dropdown verilerini yükler
         */
        async init() {
            await this.loadDropdowns();
        },

        /**
         * Tüm dropdown verilerini paralel yükler
         */
        async loadDropdowns() {
            await Promise.all([
                this.loadCategories(),
                this.loadProductGroups(),
                this.loadFunctions()
            ]);
        },

        async loadCategories() {
            try {
                const response = await fetch('/Product/GetCategories');
                if (response.ok) {
                    this.categories = await response.json();
                }
            } catch (e) {
                console.error('Kategoriler yüklenirken hata:', e);
            }
        },

        async loadProductGroups() {
            try {
                const response = await fetch('/Product/GetProductGroups');
                if (response.ok) {
                    this.productGroups = await response.json();
                }
            } catch (e) {
                console.error('Ürün grupları yüklenirken hata:', e);
            }
        },

        async loadFunctions() {
            try {
                const response = await fetch('/Product/GetFunctions');
                if (response.ok) {
                    this.functions = await response.json();
                }
            } catch (e) {
                console.error('Fonksiyonlar yüklenirken hata:', e);
            }
        },

        /**
         * Modal'ı açar
         */
        openModal(detail) {
            this.isOpen = true;
            this.targetId = detail.targetId;
            this.isMultiSelect = detail.isMultiSelect || false;
            this.selectedItems = [];
            this.resetFilter();
            this.items = [];
            this.hasSearched = false;
        },

        /**
         * Modal'ı kapatır
         */
        closeModal() {
            this.isOpen = false;
        },

        /**
         * Filtreyi sıfırlar
         */
        resetFilter() {
            this.filter = {
                code: '',
                category: '',
                productGroup: '',
                function: '',
                isInvalid: false,
                page: 1,
                pageSize: 10
            };
        },

        /**
         * Ürün araması yapar
         */
        async search(page) {
            this.items = [];
            this.hasSearched = false;

            this.filter.page = page;
            this.page = page;

            // Filter temizleme
            const payload = { ...this.filter };
            if (this.filter.isInvalid === "") {
                payload.isInvalid = null;
            } else {
                payload.isInvalid = String(this.filter.isInvalid) === 'true';
            }

            try {
                const result = await API.post('/Product/Search', payload);
                this.items = result.items || [];
                this.totalCount = result.totalCount || 0;
                this.hasSearched = true;
            } catch (error) {
                console.error('Ürün araması hatası:', error);
                Toast.error('Ürün araması sırasında bir hata oluştu.');
            }
        },

        /**
         * Tek seçim yapar
         */
        select(prod) {
            this.$dispatch('product-selected', {
                product: prod,
                targetId: this.targetId
            });
            this.closeModal();
        },

        /**
         * Çoklu seçimde öğeyi toggle eder
         */
        toggleSelection(prod) {
            if (!this.isMultiSelect) return;

            const index = this.selectedItems.findIndex(x => x.code === prod.code);
            if (index > -1) {
                this.selectedItems.splice(index, 1);
            } else {
                this.selectedItems.push(prod);
            }
        },

        /**
         * Öğenin seçili olup olmadığını kontrol eder
         */
        isSelected(prod) {
            return this.selectedItems.some(x => x.code === prod.code);
        },

        /**
         * Çoklu seçimi gönderir
         */
        submitMultiSelection() {
            this.$dispatch('product-selected', {
                products: this.selectedItems,
                targetId: this.targetId
            });
            this.closeModal();
        },

        /**
         * Toplam sayfa sayısını hesaplar
         */
        get totalPages() {
            return Math.ceil(this.totalCount / this.filter.pageSize);
        }
    }
}

// Global scope'a ekle
window.productSelectModal = productSelectModal;
