/**
 * Employee Select Modal (v6 - Using BaseSelectModal)
 * BaseSelectModal sınıfını kullanarak refactor edilmiş versiyon.
 */

// Store konfigürasyonu
const employeeModalConfig = {
    storeName: 'employeeModal',
    searchEndpoint: '/api/Employee/search',
    selectEventName: 'employee-selected',
    openEventName: 'employee-select-open',
    itemIdentifier: 'code',
    itemName: 'employee',
    itemsArrayName: 'employees',
    pageSize: 20,
    searchMethod: 'get',
    searchOnOpen: true,
    defaultFilter: {
        code: '',
        name: '',
        functionId: '',
        status: '1'
    }
};

// Employee-specific ek metodlar
const employeeCustomMethods = {
    // Fonksiyon dropdown verisi
    functions: [],

    /**
     * Dropdown verilerini yükler
     */
    async loadDropdowns() {
        await this.getFunctions();
    },

    /**
     * Fonksiyon listesini API'den çeker
     */
    async getFunctions() {
        try {
            if (typeof API === 'undefined') return;
            const result = await API.get('/api/Employee/functions');
            this.functions = result || [];
        } catch (error) {
            console.error('Functions load error:', error);
        }
    },

    /**
     * Modal açılırken context-specific filter uygular
     * @param {Object} detail - Event detail
     */
    open(detail) {
        console.log('Opening employeeModal with detail:', detail);

        this.isOpen = true;
        this.targetId = detail.targetId || '';
        this.isMultiSelect = detail.isMultiSelect || false;
        this.allowQuantity = detail.allowQuantity === true;
        this.showFilter = detail.showFilter || false;
        const parsedDefaultQuantity = Number.parseInt(detail.defaultQuantity, 10);
        this.defaultSelectionQuantity = Number.isFinite(parsedDefaultQuantity) && parsedDefaultQuantity > 0
            ? parsedDefaultQuantity
            : 1;
        this.selectedItems = [];
        this.items = [];
        this.hasSearched = false;

        this.resetFilter();

        // Context-specific filtering (Job page function filter)
        if (detail.functionId) {
            this.filter.functionId = detail.functionId;
        }

        const requestedPageSize = Number.parseInt(detail.pageSize, 10);
        if (Number.isFinite(requestedPageSize) && requestedPageSize > 0) {
            this.filter.pageSize = requestedPageSize;
        }

        // Dropdown yükle (ilk açılışta)
        if (!this.functions || this.functions.length === 0) {
            this.getFunctions();
        }

        this.search(1);
    },

    /**
     * Filtreyi sıfırlar
     */
    resetFilter() {
        this.filter = {
            code: '',
            name: '',
            functionId: '',
            status: '1',
            page: 1,
            pageSize: 20
        };
    },

    /**
     * Arama override - parametre isimlerini API'ye uygun şekilde map eder
     */
    async search(page = 1) {
        console.log('Employee search, page:', page);
        this.currentPage = page;
        this.filter.page = page;

        try {
            if (typeof API === 'undefined') return;

            // API parametre formatına çevir
            const params = {
                Code: this.filter.code,
                Name: this.filter.name,
                FunctionID: this.filter.functionId || null,
                Status: this.filter.status,
                Page: this.filter.page,
                PageSize: this.filter.pageSize
            };

            const result = await API.get('/api/Employee/search', params);

            if (result) {
                const rawItems = result.items || result.Items || result.data || result.Data || [];
                this.items = (Array.isArray(rawItems) ? rawItems : []).map(item => {
                    const selectedItem = this.selectedItems.find(x => x.code === item.code);
                    return {
                        ...item,
                        quantity: selectedItem
                            ? selectedItem.quantity
                            : this.normalizeQuantity(item.quantity)
                    };
                });
                this.totalCount = result.totalCount || result.TotalCount || 0;
            } else {
                this.items = [];
                this.totalCount = 0;
            }

            this.hasSearched = true;
        } catch (error) {
            console.error('Employee search error:', error);
            if (typeof Toast !== 'undefined') {
                Toast.error((window.imajTexts && window.imajTexts.genericError) || 'An error occurred.');
            }
            this.items = [];
            this.totalCount = 0;
            this.hasSearched = true;
        }
    }
};

// Store oluşturma - Alpine.js başlatılınca
document.addEventListener('alpine:init', () => {
    // Base store özelliklerini al
    const baseProps = {
        isOpen: false,
        targetId: '',
        isMultiSelect: false,
        showFilter: false,
        allowQuantity: false,
        defaultSelectionQuantity: 1,
        filter: { ...employeeModalConfig.defaultFilter, page: 1, pageSize: 20 },
        currentPage: 1,
        totalCount: 0,
        items: [],
        hasSearched: false,
        selectedItems: [],
        pageSize: 20,

        // Base metodlar
        close() {
            this.isOpen = false;
        },

        select(emp) {
            window.dispatchEvent(new CustomEvent('employee-selected', {
                detail: {
                    employee: emp,
                    targetId: this.targetId,
                    employees: [emp]
                }
            }));
            this.close();
        },

        toggleSelection(emp) {
            if (!this.isMultiSelect) return;
            const index = this.selectedItems.findIndex(x => x.code === emp.code);
            if (index > -1) this.selectedItems.splice(index, 1);
            else this.selectedItems.push({
                ...emp,
                quantity: this.normalizeQuantity(emp.quantity)
            });
        },

        isSelected(emp) {
            return this.selectedItems.some(x => x.code === emp.code);
        },

        normalizeQuantity(value) {
            const parsed = Number.parseInt(value, 10);
            return Number.isFinite(parsed) && parsed > 0 ? parsed : this.defaultSelectionQuantity;
        },

        getQuantity(emp) {
            const selectedItem = this.selectedItems.find(x => x.code === emp.code);
            if (selectedItem) {
                return selectedItem.quantity;
            }

            return this.normalizeQuantity(emp.quantity);
        },

        updateQuantity(emp, value) {
            const quantity = this.normalizeQuantity(value);
            emp.quantity = quantity;

            const selectedItem = this.selectedItems.find(x => x.code === emp.code);
            if (selectedItem) {
                selectedItem.quantity = quantity;
            }
        },

        submitMultiSelection() {
            window.dispatchEvent(new CustomEvent('employee-selected', {
                detail: {
                    employees: this.selectedItems,
                    targetId: this.targetId
                }
            }));
            this.close();
        },

        totalPages() {
            return Math.ceil(this.totalCount / this.pageSize);
        },

        displayColumnCount() {
            if (!Array.isArray(this.items) || this.items.length === 0) {
                return 1;
            }

            if (this.items.length >= 13) {
                return 3;
            }

            if (this.items.length >= 7) {
                return 2;
            }

            return 1;
        },

        columnGroups() {
            if (!Array.isArray(this.items) || this.items.length === 0) {
                return [];
            }

            const columnCount = this.displayColumnCount();
            const chunkSize = Math.ceil(this.items.length / columnCount);

            return Array.from({ length: columnCount }, (_, index) =>
                this.items.slice(index * chunkSize, (index + 1) * chunkSize)
            ).filter(group => group.length > 0);
        }
    };

    // Custom metodları birleştir
    const store = { ...baseProps, ...employeeCustomMethods };

    // Init metodu
    store.init = function () {
        console.log('Employee Modal Store Initialized (v6)');

        // Global event listener
        window.addEventListener('employee-select-open', (event) => {
            console.log('Event caught: employee-select-open', event.detail);
            this.open(event.detail);
        });

        // Dropdown'ları yükle
        this.loadDropdowns();
    };

    Alpine.store('employeeModal', store);
});
