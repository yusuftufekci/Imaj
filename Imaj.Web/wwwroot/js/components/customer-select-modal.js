/**
 * Customer Select Modal Component (v2 - Using BaseSelectModal patterns)
 * Alpine.js component fonksiyonu - BaseSelectModal pattern'lerini kullanır.
 */

function customerSelectModal() {
    return {
        // Modal State
        showModal: false,
        targetId: null,
        searchEndpoint: '/Customer/Search',
        jobStatesEndpoint: '/Customer/GetJobStates',

        // Filtre alanları - Customer'a özel geniş filtre
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
            isInvalid: null, // null = Tümü, false = Hayır, true = Evet
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

        /**
         * Component başlatma - Dropdown verilerini yükler
         */
        async init() {
            // Endpoints target'e göre değişebildiği için dropdownları modal açılışında yüklüyoruz.
        },

        /**
         * Dropdown verilerini yükler
         */
        async loadDropdowns() {
            try {
                const response = await fetch(this.jobStatesEndpoint);
                if (response.ok) {
                    this.jobStatuses = await response.json();
                } else {
                    this.jobStatuses = [];
                }
            } catch (error) {
                console.error('Durum listesi yüklenemedi:', error);
                this.jobStatuses = [];
            }
        },

        /**
         * Modal'ı açar
         * @param {Object} detail - Event detail (targetId içerir)
         */
        async openModal(detail) {
            this.targetId = detail?.targetId || null;
            this.searchEndpoint = detail?.searchEndpoint || '/Customer/Search';
            this.jobStatesEndpoint = detail?.jobStatesEndpoint || '/Customer/GetJobStates';
            this.showModal = true;
            this.resetFilter();
            this.hasSearched = false;
            this.items = [];
            await this.loadDropdowns();
        },

        /**
         * Modal'ı kapatır
         */
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
                isInvalid: null,
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
            this.page = this.filter.page;

            // Boş değerleri null'a çevir
            const filterToSend = { ...this.filter };
            if (filterToSend.isInvalid === "") {
                filterToSend.isInvalid = null;
            } else if (filterToSend.isInvalid === "true") {
                filterToSend.isInvalid = true;
            } else if (filterToSend.isInvalid === "false") {
                filterToSend.isInvalid = false;
            }

            if (filterToSend.jobStatus === "") {
                filterToSend.jobStatus = null;
            }

            try {
                const result = await API.post(this.searchEndpoint, filterToSend);
                this.items = result.items || [];
                this.totalCount = result.totalCount || 0;
                this.page = result.page || 1;
                this.hasSearched = true;
            } catch (error) {
                console.error('Müşteri araması hatası:', error);
                Toast.error((window.imajTexts && window.imajTexts.genericError) || 'An error occurred.');
            }
        },

        /**
         * Müşteri seçer ve event dispatch eder
         * @param {Object} customer - Seçilen müşteri
         */
        select(customer) {
            this.$dispatch('customer-selected', {
                customer: customer,
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
window.customerSelectModal = customerSelectModal;
