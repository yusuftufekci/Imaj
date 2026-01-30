/**
 * Customer Select Modal Component
 * Müşteri seçim modal'ı için Alpine.js component fonksiyonu
 */
function customerSelectModal() {
    return {
        showModal: false,
        targetId: null,  // Çağıran component'in ID'si

        // Filtre alanları
        filter: {
            code: '',
            name: '',
            city: '',
            areaCode: '',
            country: '',
            owner: '',
            relatedPerson: '',
            phone: '',
            fax: '',
            email: '',
            taxOffice: '',
            taxNumber: '',
            jobStatus: '',
            jobStateId: null, // Dinamik durum filtresi
            isInvalid: false,
            page: 1,
            pageSize: 5
        },

        // Sonuç verileri
        items: [],
        totalCount: 0,
        hasSearched: false,
        page: 1,

        // Dropdown verileri
        jobStatuses: [],

        async init() {
            try {
                const response = await fetch('/Customer/GetJobStates');
                if (response.ok) {
                    this.jobStatuses = await response.json();
                }
            } catch (error) {
                console.error('Durum listesi yüklenemedi:', error);
            }
        },

        /**
         * Modal'ı açar
         * @param {object} detail - Event detail (targetId içerir)
         */
        openModal(detail) {
            this.targetId = detail.targetId;
            this.showModal = true;
            this.resetFilter();
            this.hasSearched = false;
            this.items = [];
        },

        /**
         * Modal'ı kapatır
         * @param {object} customer - Seçilen müşteri
         */
        select(customer) {
            this.$dispatch('customer-selected', {
                customer: customer,
                targetId: this.targetId
            });
            this.closeModal();
        },

        closeModal() {
            this.showModal = false;
            this.targetId = null;
        },

        /**
         * Filtreyi sıfırlar
         */
        resetFilter() {
            this.filter = {
                code: '',
                name: '',
                city: '',
                areaCode: '',
                country: '',
                owner: '',
                relatedPerson: '',
                phone: '',
                fax: '',
                email: '',
                taxOffice: '',
                taxNumber: '',
                jobStatus: '',
                jobStateId: null,
                isInvalid: false,
                page: 1,
                pageSize: 5
            };
        },

        /**
         * Müşteri araması yapar
         * @param {number} page - Sayfa numarası
         */
        async search(page) {
            if (page) this.filter.page = page;

            try {
                const result = await API.post('/Customer/Search', this.filter);
                this.items = result.items || [];
                this.totalCount = result.totalCount || 0;
                this.page = result.page || 1;
                this.hasSearched = true;
            } catch (error) {
                console.error('Müşteri araması hatası:', error);
                Toast.error('Müşteri araması sırasında bir hata oluştu.');
            }
        },

        /**
         * Müşteri seçer ve event dispatch eder
         * @param {object} customer - Seçilen müşteri
         */
        select(customer) {
            this.$dispatch('customer-selected', {
                customer: customer,
                targetId: this.targetId
            });
            this.closeModal();
        }
    }
}

// Global scope'a ekle
window.customerSelectModal = customerSelectModal;
