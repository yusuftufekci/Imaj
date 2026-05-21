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

            productId: '',
            productCode: '',
            productName: '',

            page: 1,
        },

        customerName: '',
        pageSize: 10,
        createFunction: '',
        createFunctionStorageKey: 'imaj.job.createFunctionId',

        /**
         * Sayfa başlatma
         */
        init() {
            // Başlangıç tarihlerini URL'den veya default değerlerden al
            const rememberedFunction = window.sessionStorage?.getItem(this.createFunctionStorageKey);
            this.createFunction = rememberedFunction || this.filter.function || '';
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
                const product = detail.product || (detail.products && detail.products[0]);
                if (!product) {
                    return;
                }

                this.filter = {
                    ...this.filter,
                    productName: product.name ?? product.Name ?? '',
                    productCode: product.code ?? product.Code ?? '',
                    productId: product.id ?? product.Id ?? ''
                };
            }
        },

        clearProductSelection() {
            this.filter = {
                ...this.filter,
                productId: '',
                productName: '',
                productCode: ''
            };
        },

        /**
         * Çalışan seçimi handler
         */
        handleEmployeeSelection(detail) {
            if (detail.targetId === 'jobEmployee') {
                const emp = detail.employee || (detail.employees && detail.employees[0]);
                if (emp) {
                    this.filter = {
                        ...this.filter,
                        employeeName: emp.name ?? emp.Name ?? '',
                        employeeCode: emp.code ?? emp.Code ?? ''
                    };
                } else {
                    this.clearEmployeeSelection();
                }
            }
        },

        clearEmployeeSelection() {
            this.filter = {
                ...this.filter,
                employeeName: '',
                employeeCode: ''
            };
        },

        rememberCreateFunction() {
            if (this.createFunction) {
                window.sessionStorage?.setItem(this.createFunctionStorageKey, this.createFunction);
            }
        },

        buildCreateJobUrl() {
            this.rememberCreateFunction();
            return this.createFunction
                ? '/Job/Create?functionId=' + encodeURIComponent(this.createFunction)
                : '/Job/Create';
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
                productId: '',
                productCode: '',
                productName: '',
                page: 1,
            };
            this.customerName = '';
        }
    }
}

window.jobPage = jobPage;
