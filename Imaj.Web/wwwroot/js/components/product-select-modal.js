/**
 * Product Select Modal Component
 * Ürün seçim modal'ı için Alpine.js component fonksiyonu
 * Single ve Multi-select desteği var
 */
function productSelectModal() {
    return {
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

        // Sonuç verileri
        items: [],
        totalCount: 0,
        page: 1,
        hasSearched: false,

        // Multi-select için seçilen öğeler
        selectedItems: [],

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
            this.filter.page = page;
            this.page = page;

            try {
                const result = await API.post('/Product/Search', this.filter);
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
        }
    }
}

window.productSelectModal = productSelectModal;
