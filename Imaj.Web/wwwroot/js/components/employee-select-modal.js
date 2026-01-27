/**
 * Employee Select Modal Component
 * Çalışan seçim modal'ı için Alpine.js component fonksiyonu
 * Single ve Multi-select desteği var
 */
function employeeSelectModal() {
    return {
        isOpen: false,
        targetId: '',
        isMultiSelect: false,

        // Arama ve sayfalama
        filter: {
            code: '',
            name: '',
            functionId: '',
            status: '1', // Default: Sadece geçerli
            page: 1,
            pageSize: 10
        },

        currentPage: 1,
        totalCount: 0,
        items: [],
        functions: [], // Fonksiyon listesi

        // Multi-select için seçilen öğeler
        selectedItems: [],

        init() {
            this.getFunctions();
        },

        /**
         * Modal'ı açar
         */
        openModal(detail) {
            this.isOpen = true;
            this.targetId = detail.targetId;
            this.isMultiSelect = detail.isMultiSelect || false;
            this.selectedItems = [];

            // Filtreyi sıfırla
            this.clearFilter();

            // Eğer fonksiyonlar yüklenmediyse yükle
            if (this.functions.length === 0) {
                this.getFunctions();
            }

            this.search(1);
        },

        /**
         * Modal'ı kapatır
         */
        closeModal() {
            this.isOpen = false;
        },

        /**
         * Filtreleri temizler
         */
        clearFilter() {
            this.filter.code = '';
            this.filter.name = '';
            this.filter.functionId = '';
            this.filter.status = '1';
            this.filter.page = 1;
            this.filter.pageSize = 10;
        },

        /**
         * Fonksiyon listesini çeker
         */
        async getFunctions() {
            try {
                const result = await API.get('/api/Employee/functions');
                this.functions = result || [];
            } catch (error) {
                console.error('Fonksiyon listesi hatası:', error);
            }
        },

        /**
         * Çalışan araması yapar
         */
        async search(page) {
            this.currentPage = page;
            this.filter.page = page;

            try {
                // filter objesini query string'e çevir
                const params = {
                    code: this.filter.code,
                    name: this.filter.name,
                    functionID: this.filter.functionId,
                    status: this.filter.status,
                    page: this.filter.page,
                    pageSize: this.filter.pageSize
                };

                const result = await API.get('/api/Employee/search', params);
                this.items = result.items || [];
                this.totalCount = result.totalCount || 0;
            } catch (error) {
                console.error('Çalışan araması hatası:', error);
                Toast.error('Çalışan araması sırasında bir hata oluştu.');
            }
        },

        /**
         * Tek seçim yapar
         */
        select(emp) {
            this.$dispatch('employee-selected', {
                employee: emp,
                targetId: this.targetId
            });
            this.closeModal();
        },

        /**
         * Çoklu seçimde öğeyi toggle eder
         */
        toggleSelection(emp) {
            if (!this.isMultiSelect) return;

            const index = this.selectedItems.findIndex(x => x.code === emp.code);
            if (index > -1) {
                this.selectedItems.splice(index, 1);
            } else {
                this.selectedItems.push(emp);
            }
        },

        /**
         * Öğenin seçili olup olmadığını kontrol eder
         */
        isSelected(emp) {
            return this.selectedItems.some(x => x.code === emp.code);
        },

        /**
         * Çoklu seçimi gönderir
         */
        submitMultiSelection() {
            this.$dispatch('employee-selected', {
                employees: this.selectedItems,
                targetId: this.targetId
            });
            this.closeModal();
        }
    }
}

window.employeeSelectModal = employeeSelectModal;
