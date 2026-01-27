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
        searchTerm: '',
        pageSize: 10,
        currentPage: 1,
        totalCount: 0,
        items: [],

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
            this.search(1);
        },

        /**
         * Modal'ı kapatır
         */
        closeModal() {
            this.isOpen = false;
        },

        /**
         * Çalışan araması yapar
         */
        async search(page) {
            this.currentPage = page;
            try {
                const result = await API.get('/OvertimeReport/SearchEmployees', {
                    term: this.searchTerm,
                    page: page,
                    pageSize: this.pageSize
                });
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
