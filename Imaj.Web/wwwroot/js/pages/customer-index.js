/**
 * Customer Index Page
 * Müşteri ana sayfa için Alpine.js component fonksiyonu
 */
const customerText = (key, fallback) => (window.imajTexts && window.imajTexts[key]) || fallback;

function customerIndex() {
    return {
        newCustomerCode: '',

        /**
         * Yeni müşteri oluşturur
         */
        async createCustomer() {
            if (!this.newCustomerCode) {
                Toast.error(customerText('pleaseEnterCustomerCode', 'Please enter a customer code.'));
                return;
            }

            try {
                const result = await API.post('/Customer/Create', {
                    code: this.newCustomerCode
                });

                if (result.isSuccess) {
                    Toast.success(result.message || customerText('customerCreatedSuccess', 'Customer created successfully.'));
                    this.newCustomerCode = '';
                } else {
                    Toast.error(result.message || customerText('genericError', 'An error occurred.'));
                }
            } catch (error) {
                console.error('Müşteri oluşturma hatası:', error);
                Toast.error(customerText('customerCreateFailed', 'An error occurred while creating the customer.'));
            }
        },

        /**
         * Rapor basar
         */
        printReport() {
            Toast.success(customerText('printReportStarted', 'Report print process started.'));
            // TODO: Implement actual print functionality
        }
    }
}

window.customerIndex = customerIndex;
