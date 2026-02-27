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
    pageSize: 10,
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
        this.showFilter = detail.showFilter || false;
        this.selectedItems = [];
        this.items = [];
        this.hasSearched = false;

        this.resetFilter();

        // Context-specific filtering (Job page function filter)
        if (detail.functionId) {
            this.filter.functionId = detail.functionId;
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
            pageSize: 10
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
                this.items = result.items || result.Items || result.data || result.Data || [];
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
        filter: { ...employeeModalConfig.defaultFilter, page: 1, pageSize: 10 },
        currentPage: 1,
        totalCount: 0,
        items: [],
        hasSearched: false,
        selectedItems: [],
        pageSize: 10,

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
            else this.selectedItems.push(emp);
        },

        isSelected(emp) {
            return this.selectedItems.some(x => x.code === emp.code);
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

        get totalPages() {
            return Math.ceil(this.totalCount / this.pageSize);
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
