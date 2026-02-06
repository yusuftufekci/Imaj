/**
 * Employee Select Modal Store (v5 - Restored & Fixed)
 * Alpine.js Global Store definition
 */

document.addEventListener('alpine:init', () => {
    // Global Store Tanımlaması
    Alpine.store('employeeModal', {
        isOpen: false,
        targetId: '',
        isMultiSelect: false,
        showFilter: false, // Control flag for filter UI

        // Arama ve sayfalama
        filter: {
            code: '',
            name: '',
            functionId: '',
            status: '1',
            page: 1,
            pageSize: 10
        },

        currentPage: 1,
        totalCount: 0,
        items: [],
        functions: [],
        selectedItems: [],

        init() {
            console.log('Employee Modal STORE Initialized (Restored)');

            // Global Event Listener
            // Store seviyesinde dinleme yaparak component instance sorunlarını aşıyoruz.
            window.addEventListener('employee-select-open', (event) => {
                console.log('Event Caught in STORE:', event.detail);
                this.open(event.detail);
            });
        },

        open(detail) {
            console.log('Opening Modal from STORE', detail);
            this.isOpen = true;
            this.targetId = detail.targetId;
            this.isMultiSelect = detail.isMultiSelect || false;
            this.showFilter = detail.showFilter || false;
            this.selectedItems = [];

            this.clearFilter();

            // Context-specific filtering
            if (detail.functionId) {
                this.filter.functionId = detail.functionId;
            }

            if (!this.functions || this.functions.length === 0) {
                this.getFunctions();
            }

            this.search(1);
        },

        close() {
            this.isOpen = false;
        },

        clearFilter() {
            this.filter.code = '';
            this.filter.name = '';
            this.filter.functionId = '';
            this.filter.status = '1';
            this.filter.page = 1;
            this.filter.pageSize = 10;
        },

        async getFunctions() {
            try {
                if (typeof API === 'undefined') return;
                const result = await API.get('/api/Employee/functions');
                this.functions = result || [];
            } catch (error) {
                console.error('Func error:', error);
            }
        },

        async search(page) {
            console.log('Store Search:', page);
            this.currentPage = page;
            this.filter.page = page;

            try {
                if (typeof API === 'undefined') return;

                // site.js içinde null check eklendiği için direkt gönderebiliriz
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
            } catch (error) {
                console.error('Search error:', error);
                if (typeof Toast !== 'undefined') {
                    Toast.error('Hata: ' + (error.message || error));
                }
            }
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
        }
    });
});
