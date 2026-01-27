/**
 * Customer Index Page
 * Müşteri ana sayfa için Alpine.js component fonksiyonu
 */
function customerIndex() {
    return {
        newCustomerCode: '',

        /**
         * Yeni müşteri oluşturur
         */
        async createCustomer() {
            if (!this.newCustomerCode) {
                Toast.error('Lütfen bir müşteri kodu giriniz.');
                return;
            }

            try {
                const result = await API.post('/Customer/Create', {
                    code: this.newCustomerCode
                });

                if (result.isSuccess) {
                    Toast.success(result.message || 'Müşteri başarıyla oluşturuldu.');
                    this.newCustomerCode = '';
                } else {
                    Toast.error(result.message || 'Bir hata oluştu.');
                }
            } catch (error) {
                console.error('Müşteri oluşturma hatası:', error);
                Toast.error('Müşteri oluşturulurken bir hata oluştu.');
            }
        },

        /**
         * Rapor basar
         */
        printReport() {
            Toast.success('Rapor basma işlemi başlatıldı.');
            // TODO: Implement actual print functionality
        }
    }
}

window.customerIndex = customerIndex;
