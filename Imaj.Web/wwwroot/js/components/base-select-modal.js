/**
 * Base Select Modal Class (v1)
 * Tüm select modal'ları için ortak fonksiyonellik sağlar.
 * Employee, Customer, Product modal'ları bu yapıyı kullanır.
 * 
 * Kullanım:
 * const config = {
 *     storeName: 'employeeModal',
 *     searchEndpoint: '/api/Employee/search',
 *     selectEventName: 'employee-selected',
 *     openEventName: 'employee-select-open',
 *     itemIdentifier: 'code' // Seçim karşılaştırması için kullanılacak alan
 * };
 * BaseSelectModal.createStore(config, customMethods);
 */

class BaseSelectModal {
    /**
     * Alpine.js store oluşturur ve kaydeder
     * @param {Object} config - Modal konfigürasyonu
     * @param {Object} customMethods - Modal'a özel metodlar
     */
    static createStore(config, customMethods = {}) {
        const storeName = config.storeName;
        const searchEndpoint = config.searchEndpoint;
        const selectEventName = config.selectEventName;
        const openEventName = config.openEventName;
        const itemIdentifier = config.itemIdentifier || 'code';

        // Varsayılan store özellikleri
        const baseStore = {
            // Modal State
            isOpen: false,
            targetId: '',
            isMultiSelect: false,
            showFilter: false,

            // Arama ve Sayfalama
            filter: {},
            defaultFilter: config.defaultFilter || {},

            currentPage: 1,
            pageSize: config.pageSize || 10,
            totalCount: 0,
            items: [],
            hasSearched: false,

            // Multi-select
            selectedItems: [],

            // Dropdown verileri (custom metodlarla doldurulur)
            dropdowns: {},

            /**
             * Store başlatma
             */
            init() {
                console.log(`${storeName} Store Initialized`);

                // Global event listener
                if (openEventName) {
                    window.addEventListener(openEventName, (event) => {
                        console.log(`Event caught: ${openEventName}`, event.detail);
                        this.open(event.detail);
                    });
                }

                // Dropdown verilerini yükle (varsa)
                if (typeof this.loadDropdowns === 'function') {
                    this.loadDropdowns();
                }

                // Filter'ı default değerlerle başlat
                this.resetFilter();
            },

            /**
             * Modal'ı açar
             * @param {Object} detail - Event detail
             */
            open(detail) {
                console.log(`Opening ${storeName}`, detail);

                this.isOpen = true;
                this.targetId = detail.targetId || '';
                this.isMultiSelect = detail.isMultiSelect || false;
                this.showFilter = detail.showFilter || false;
                this.selectedItems = [];
                this.items = [];
                this.hasSearched = false;

                this.resetFilter();

                // Context-specific filter override
                if (detail.filters) {
                    Object.assign(this.filter, detail.filters);
                }

                // Modal açılınca otomatik arama (customizable)
                if (config.searchOnOpen !== false) {
                    this.search(1);
                }
            },

            /**
             * Modal'ı kapatır
             */
            close() {
                this.isOpen = false;
            },

            /**
             * Filtreyi varsayılan değerlere sıfırlar
             */
            resetFilter() {
                this.filter = {
                    ...this.defaultFilter,
                    page: 1,
                    pageSize: this.pageSize
                };
            },

            /**
             * Arama yapar
             * @param {number} page - Sayfa numarası
             */
            async search(page = 1) {
                this.currentPage = page;
                this.filter.page = page;

                try {
                    if (typeof API === 'undefined') {
                        console.error('API is not defined');
                        return;
                    }

                    // Null/empty değerleri temizle
                    const cleanFilter = {};
                    for (const [key, value] of Object.entries(this.filter)) {
                        if (value !== '' && value !== null && value !== undefined) {
                            cleanFilter[key] = value;
                        }
                    }

                    const method = config.searchMethod || 'get';
                    let result;

                    if (method === 'post') {
                        result = await API.post(searchEndpoint, cleanFilter);
                    } else {
                        result = await API.get(searchEndpoint, cleanFilter);
                    }

                    if (result) {
                        // Farklı API response formatlarını destekle
                        this.items = result.items || result.Items || result.data || result.Data || [];
                        this.totalCount = result.totalCount || result.TotalCount || 0;
                    } else {
                        this.items = [];
                        this.totalCount = 0;
                    }

                    this.hasSearched = true;
                } catch (error) {
                    console.error(`${storeName} search error:`, error);
                    if (typeof Toast !== 'undefined') {
                        Toast.error((window.imajTexts && window.imajTexts.genericError) || 'An error occurred.');
                    }
                    this.items = [];
                    this.totalCount = 0;
                    this.hasSearched = true;
                }
            },

            /**
             * Tek öğe seçer
             * @param {Object} item - Seçilen öğe
             */
            select(item) {
                const eventDetail = {
                    [config.itemName || 'item']: item,
                    targetId: this.targetId
                };

                // Ek array property (backward compatibility)
                eventDetail[config.itemsArrayName || 'items'] = [item];

                window.dispatchEvent(new CustomEvent(selectEventName, { detail: eventDetail }));
                this.close();
            },

            /**
             * Multi-select modunda öğeyi toggle eder
             * @param {Object} item - Toggle edilecek öğe
             */
            toggleSelection(item) {
                if (!this.isMultiSelect) return;

                const index = this.selectedItems.findIndex(x => x[itemIdentifier] === item[itemIdentifier]);
                if (index > -1) {
                    this.selectedItems.splice(index, 1);
                } else {
                    this.selectedItems.push(item);
                }
            },

            /**
             * Öğenin seçili olup olmadığını kontrol eder
             * @param {Object} item - Kontrol edilecek öğe
             * @returns {boolean}
             */
            isSelected(item) {
                return this.selectedItems.some(x => x[itemIdentifier] === item[itemIdentifier]);
            },

            /**
             * Çoklu seçimi gönderir
             */
            submitMultiSelection() {
                const eventDetail = {
                    [config.itemsArrayName || 'items']: this.selectedItems,
                    targetId: this.targetId
                };

                window.dispatchEvent(new CustomEvent(selectEventName, { detail: eventDetail }));
                this.close();
            },

            /**
             * Toplam sayfa sayısını hesaplar
             * @returns {number}
             */
            get totalPages() {
                return Math.ceil(this.totalCount / this.pageSize);
            },

            /**
             * Önceki sayfaya git
             */
            prevPage() {
                if (this.currentPage > 1) {
                    this.search(this.currentPage - 1);
                }
            },

            /**
             * Sonraki sayfaya git
             */
            nextPage() {
                if (this.currentPage < this.totalPages) {
                    this.search(this.currentPage + 1);
                }
            }
        };

        // Custom metodları birleştir
        const finalStore = { ...baseStore, ...customMethods };

        // Alpine.js store olarak kaydet
        document.addEventListener('alpine:init', () => {
            Alpine.store(storeName, finalStore);
        });

        return finalStore;
    }

    /**
     * Alpine.js component fonksiyonu oluşturur (Store yerine Component pattern)
     * @param {Object} config - Modal konfigürasyonu
     * @param {Object} customMethods - Modal'a özel metodlar
     */
    static createComponent(config, customMethods = {}) {
        const searchEndpoint = config.searchEndpoint;
        const selectEventName = config.selectEventName;
        const itemIdentifier = config.itemIdentifier || 'code';

        return function () {
            return {
                // Modal State
                isOpen: false,
                showModal: false, // Alias for backward compatibility
                targetId: '',
                isMultiSelect: false,

                // Arama ve Sayfalama
                filter: { ...config.defaultFilter, page: 1, pageSize: config.pageSize || 10 },
                currentPage: 1,
                totalCount: 0,
                items: [],
                hasSearched: false,

                // Multi-select
                selectedItems: [],

                async init() {
                    if (typeof customMethods.init === 'function') {
                        await customMethods.init.call(this);
                    }
                },

                openModal(detail) {
                    this.isOpen = true;
                    this.showModal = true;
                    this.targetId = detail.targetId || '';
                    this.isMultiSelect = detail.isMultiSelect || false;
                    this.selectedItems = [];
                    this.items = [];
                    this.hasSearched = false;
                    this.resetFilter();
                },

                closeModal() {
                    this.isOpen = false;
                    this.showModal = false;
                },

                resetFilter() {
                    this.filter = {
                        ...config.defaultFilter,
                        page: 1,
                        pageSize: config.pageSize || 10
                    };
                },

                async search(page = 1) {
                    this.filter.page = page;
                    this.currentPage = page;

                    const cleanFilter = {};
                    for (const [key, value] of Object.entries(this.filter)) {
                        if (value !== '' && value !== null && value !== undefined) {
                            cleanFilter[key] = value;
                        }
                    }

                    try {
                        const method = config.searchMethod || 'post';
                        const result = method === 'post'
                            ? await API.post(searchEndpoint, cleanFilter)
                            : await API.get(searchEndpoint, cleanFilter);

                        this.items = result.items || result.Items || [];
                        this.totalCount = result.totalCount || result.TotalCount || 0;
                        this.hasSearched = true;
                    } catch (error) {
                        console.error('Search error:', error);
                        Toast.error((window.imajTexts && window.imajTexts.genericError) || 'An error occurred.');
                    }
                },

                select(item) {
                    this.$dispatch(selectEventName, {
                        [config.itemName || 'item']: item,
                        targetId: this.targetId
                    });
                    this.closeModal();
                },

                toggleSelection(item) {
                    if (!this.isMultiSelect) return;
                    const idx = this.selectedItems.findIndex(x => x[itemIdentifier] === item[itemIdentifier]);
                    if (idx > -1) this.selectedItems.splice(idx, 1);
                    else this.selectedItems.push(item);
                },

                isSelected(item) {
                    return this.selectedItems.some(x => x[itemIdentifier] === item[itemIdentifier]);
                },

                submitMultiSelection() {
                    this.$dispatch(selectEventName, {
                        [config.itemsArrayName || 'items']: this.selectedItems,
                        targetId: this.targetId
                    });
                    this.closeModal();
                },

                // Merge custom methods
                ...customMethods
            };
        };
    }
}

// Global scope'a ekle
window.BaseSelectModal = BaseSelectModal;
