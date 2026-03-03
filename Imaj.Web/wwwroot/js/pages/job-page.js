/**
 * Job Page
 * İş sayfası için Alpine.js component fonksiyonu
 */
function jobPage() {
    return {
        filter: {
            function: '',
            customerId: '',
            customerCode: '',
            referenceStart: '',
            referenceEnd: '',
            referenceList: '',
            jobName: '',
            relatedPerson: '',
            startDateStart: '',
            startDateEnd: '',
            endDateStart: '',
            endDateEnd: '',
            status: '',
            emailSent: '',
            evaluated: '',

            employeeCode: '',
            employeeName: '',
            taskType: '',
            overtimeType: '',

            productCode: '',
            productName: '',

            page: 1,
        },

        customerName: '',
        pageSize: 10,

        /**
         * Sayfa başlatma
         */
        init() {
            // Başlangıç tarihlerini URL'den veya default değerlerden al
        },

        /**
         * Müşteri seçimi handler
         */
        handleCustomerSelection(detail) {
            if (detail.targetId === 'jobFilter') {
                this.filter.customerId = detail.customer.id || '';
                this.filter.customerCode = detail.customer.code;
                this.customerName = detail.customer.name;
            }
        },

        /**
         * Ürün seçimi handler
         */
        handleProductSelection(detail) {
            if (detail.targetId === 'jobProduct') {
                this.filter.productName = detail.product.name;
                this.filter.productCode = detail.product.code;
            }
        },

        /**
         * Çalışan seçimi handler
         */
        handleEmployeeSelection(detail) {
            if (detail.targetId === 'jobEmployee') {
                const emp = detail.employee;
                if (emp) {
                    this.filter.employeeName = emp.name;
                    this.filter.employeeCode = emp.code;
                } else {
                    this.filter.employeeName = '';
                    this.filter.employeeCode = '';
                }
            }
        },

        /**
         * Filtreyi temizler
         */
        resetFilter() {
            this.filter = {
                function: '',
                customerId: '',
                customerCode: '',
                referenceStart: '',
                referenceEnd: '',
                referenceList: '',
                jobName: '',
                relatedPerson: '',
                startDateStart: '',
                startDateEnd: '',
                endDateStart: '',
                endDateEnd: '',
                status: '',
                emailSent: '',
                evaluated: '',
                employeeCode: '',
                employeeName: '',
                taskType: '',
                overtimeType: '',
                productCode: '',
                productName: '',
                page: 1,
            };
            this.customerName = '';
        }
    }
}

window.jobPage = jobPage;
