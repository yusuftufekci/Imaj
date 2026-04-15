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
        searchEndpoint: '/Product/Search',
        categoriesEndpoint: '/Product/GetCategories',
        productGroupsEndpoint: '/Product/GetProductGroups',
        functionsEndpoint: '/Product/GetFunctions',
        lockedFunctionId: '',
        lockedFunctionName: '',
        isFunctionLocked: false,
        showFilter: true,

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
            // Endpoints ekran bazli degisebildigi icin dropdownlari modal acilisinda yüklüyoruz.
        },

        /**
         * Tüm dropdown verilerini paralel yükler
         */
        async loadDropdowns() {
            await Promise.all([
                this.loadCategories(),
                this.loadFunctions()
            ]);

            await this.loadProductGroups();
        },

        async loadCategories() {
            try {
                const response = await fetch(this.categoriesEndpoint);
                if (response.ok) {
                    this.categories = await response.json();
                } else {
                    this.categories = [];
                }
            } catch (e) {
                console.error('Kategoriler yüklenirken hata:', e);
                this.categories = [];
            }
        },

        async loadProductGroups() {
            try {
                const url = new URL(this.productGroupsEndpoint, window.location.origin);
                if (this.filter.function) {
                    url.searchParams.set('functionId', this.filter.function);
                }

                const response = await fetch(url.toString());
                if (response.ok) {
                    this.productGroups = await response.json();
                } else {
                    this.productGroups = [];
                }
            } catch (e) {
                console.error('Ürün grupları yüklenirken hata:', e);
                this.productGroups = [];
            }
        },

        async loadFunctions() {
            try {
                const response = await fetch(this.functionsEndpoint);
                if (response.ok) {
                    this.functions = await response.json();
                } else {
                    this.functions = [];
                }
            } catch (e) {
                console.error('Fonksiyonlar yüklenirken hata:', e);
                this.functions = [];
            }

            this.ensureLockedFunction();
        },

        /**
         * Modal'ı açar
         */
        async openModal(detail) {
            this.isOpen = true;
            this.targetId = detail.targetId;
            this.isMultiSelect = detail.isMultiSelect || false;
            this.searchEndpoint = detail?.searchEndpoint || '/Product/Search';
            this.categoriesEndpoint = detail?.categoriesEndpoint || '/Product/GetCategories';
            this.productGroupsEndpoint = detail?.productGroupsEndpoint || '/Product/GetProductGroups';
            this.functionsEndpoint = detail?.functionsEndpoint || '/Product/GetFunctions';
            this.lockedFunctionId = detail?.lockFunction && detail?.functionId ? String(detail.functionId) : '';
            this.lockedFunctionName = detail?.functionName || '';
            this.isFunctionLocked = !!this.lockedFunctionId;
            this.showFilter = detail?.showFilter ?? true;
            this.selectedItems = [];
            this.resetFilter();
            if (detail?.functionId) {
                this.filter.function = String(detail.functionId);
            }
            if (detail?.productGroup) {
                this.filter.productGroup = String(detail.productGroup);
            }
            const requestedPageSize = Number.parseInt(detail?.pageSize, 10);
            if (Number.isFinite(requestedPageSize) && requestedPageSize > 0) {
                this.filter.pageSize = requestedPageSize;
            }
            this.items = [];
            this.hasSearched = false;
            await this.loadDropdowns();
            this.ensureLockedFunction();
            if (detail?.autoSearch) {
                await this.search(1);
            }
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
                function: this.isFunctionLocked ? this.lockedFunctionId : '',
                isInvalid: false,
                page: 1,
                pageSize: 10
            };
        },

        ensureLockedFunction() {
            if (!this.isFunctionLocked || !this.lockedFunctionId) {
                return;
            }

            const normalizedLockedId = String(this.lockedFunctionId);
            const hasOption = this.functions.some(func => String(func.id) === normalizedLockedId);

            if (!hasOption) {
                this.functions = [{
                    id: normalizedLockedId,
                    name: this.lockedFunctionName || normalizedLockedId
                }, ...this.functions];
            }

            this.filter.function = normalizedLockedId;
        },

        hasFunctionOption(functionId) {
            const normalizedId = String(functionId || '');
            return this.functions.some(func => String(func.id) === normalizedId);
        },

        async onFunctionChanged() {
            if (this.isFunctionLocked) {
                this.filter.function = this.lockedFunctionId;
                return;
            }

            this.filter.productGroup = '';
            await this.loadProductGroups();
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
                const result = await API.post(this.searchEndpoint, payload);
                this.items = (result.items || []).map(item => {
                    const selectedItem = this.selectedItems.find(x => x.code === item.code);
                    return {
                        ...item,
                        quantity: selectedItem ? selectedItem.quantity : 1
                    };
                });
                this.totalCount = result.totalCount || 0;
                this.hasSearched = true;
            } catch (error) {
                console.error('Ürün araması hatası:', error);
                Toast.error((window.imajTexts && window.imajTexts.genericError) || 'An error occurred.');
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
                this.selectedItems.push({
                    ...prod,
                    quantity: prod.quantity ?? 1
                });
            }
        },

        updateQuantity(prod, rawValue) {
            const parsed = Number.parseFloat(rawValue);
            const quantity = Number.isFinite(parsed) && parsed >= 0 ? parsed : 0;

            prod.quantity = quantity;

            const selectedItem = this.selectedItems.find(x => x.code === prod.code);
            if (selectedItem) {
                selectedItem.quantity = quantity;
            }
        },

        formatPrice(value) {
            const amount = Number.parseFloat(value);
            if (!Number.isFinite(amount)) {
                return '0,00';
            }

            return amount.toLocaleString('tr-TR', {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            });
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
