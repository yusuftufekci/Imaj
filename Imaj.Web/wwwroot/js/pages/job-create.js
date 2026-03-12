const jobCreateText = (key, fallback) => (window.imajTexts && window.imajTexts[key]) || fallback;

function jobCreate(config) {
    return {
        defaultWorkTypeId: config.defaultWorkTypeId || 0,
        defaultWorkTypeName: config.defaultWorkTypeName || '',
        workTypeNames: config.workTypeNames || {},
        defaultTimeTypeId: config.defaultTimeTypeId || 0,

        // Validasyon hatası mesajı
        validationError: '',
        validationErrorDetails: [],
        isSubmitting: false,

        // Model'den gelen değerlerle başlat
        form: {
            customerId: config.form.customerId || 0,
            customerCode: config.form.customerCode || '',
            customerName: config.form.customerName || '',
            name: config.form.name || '',
            relatedPerson: config.form.relatedPerson || '',
            startDate: config.form.startDate || '',
            endDate: config.form.endDate || ''
        },
        overtimes: [],
        products: (config.products || []).map(prod => ({
            id: prod.id || 0,
            code: prod.code || '',
            name: prod.name || '',
            quantity: prod.quantity || 1,
            price: prod.price || 0,
            notes: prod.notes || ''
        })),

        // Form validation - müşteri seçilmeden ve isim girilmeden gönderilmez
        async validateAndSubmit() {
            this.validationError = '';
            this.validationErrorDetails = [];

            // Müşteri kontrolü
            if (!this.form.customerId || this.form.customerId <= 0) {
                this.validationError = jobCreateText('pleaseSelectCustomer', 'Please select a customer.');
                window.scrollTo({ top: 0, behavior: 'smooth' });
                return;
            }

            // İsim kontrolü
            if (!this.form.name || this.form.name.trim() === '') {
                this.validationError = jobCreateText('pleaseEnterJobName', 'Please enter a job name.');
                window.scrollTo({ top: 0, behavior: 'smooth' });
                return;
            }

            // Tarih kontrolü
            if (!this.form.startDate) {
                this.validationError = jobCreateText('pleaseEnterStartDate', 'Please enter a start date.');
                window.scrollTo({ top: 0, behavior: 'smooth' });
                return;
            }

            // Çift tıklama engelleme
            if (this.isSubmitting) {
                return;
            }

            this.isSubmitting = true;

            // AJAX Submission
            const formElement = this.$refs.createJobForm;
            const formData = new FormData(formElement);

            try {
                const response = await fetch(formElement.action, {
                    method: 'POST',
                    body: formData,
                    headers: {
                        'X-Requested-With': 'XMLHttpRequest'
                    }
                });

                if (response.ok) {
                    const result = await response.json();
                    if (result.success) {
                        // Başarılı - Yönlendir
                        window.location.href = result.redirectUrl;
                    } else {
                        // Başarısız - Hata göster
                        this.validationError = result.message || jobCreateText('genericError', 'An error occurred.');
                        if (result.errors && result.errors.length > 0) {
                            this.validationErrorDetails = result.errors;
                        }
                        window.scrollTo({ top: 0, behavior: 'smooth' });
                    }
                } else {
                    this.validationError = jobCreateText('serverError', 'A server error occurred.');
                    window.scrollTo({ top: 0, behavior: 'smooth' });
                }
            } catch (error) {
                console.error('Submission error:', error);
                this.validationError = jobCreateText('connectionError', 'A connection error occurred.');
                window.scrollTo({ top: 0, behavior: 'smooth' });
            } finally {
                this.isSubmitting = false;
            }
        },

        handleCustomer(detail) {
            if (detail.targetId === 'createJob') {
                this.form.customerId = detail.customer.id || 0;
                this.form.customerCode = detail.customer.code;
                this.form.customerName = detail.customer.name;
            }
        },

        addEmployee(detail) {
            if (detail.targetId === 'overtimeAdd') {
                // Check for multi-select payload first
                const employees = detail.employees || (detail.employee ? [detail.employee] : []);

                employees.forEach(emp => {
                    // Check dupe if needed, but allowing for now.
                    const taskTypeId = emp.defaultWorkTypeId || emp.DefaultWorkTypeId || this.defaultWorkTypeId;
                    const taskTypeName = emp.defaultWorkTypeName
                        || emp.DefaultWorkTypeName
                        || this.resolveWorkTypeName(taskTypeId)
                        || this.defaultWorkTypeName
                        || '';

                    this.overtimes.push({
                        id: emp.id || 0,
                        code: emp.code,
                        name: emp.name,
                        taskType: taskTypeId,
                        taskTypeName: taskTypeName,
                        overtimeType: this.defaultTimeTypeId,
                        quantity: 1,
                        amount: 0,
                        notes: ''
                    });
                });
            }
        },

        removeOvertime(index) {
            this.overtimes.splice(index, 1);
        },

        resolveWorkTypeName(workTypeId) {
            if (!workTypeId) {
                return '';
            }

            return this.workTypeNames[String(workTypeId)] || '';
        },

        calculateOvertimeTotal() {
            return this.overtimes.reduce((sum, item) => sum + (parseFloat(item.amount) || 0), 0).toFixed(2);
        },

        addProduct(detail) {
            if (detail.targetId === 'productAdd') {
                const products = detail.products || (detail.product ? [detail.product] : []);

                products.forEach(prod => {
                    this.products.push({
                        id: prod.id || 0,
                        code: prod.code,
                        name: prod.name,
                        quantity: 1,
                        price: prod.price || 0,
                        notes: ''
                    });
                });
            }
        },

        removeProduct(index) {
            this.products.splice(index, 1);
        },

        calculateProductTotal() {
            return this.products.reduce((sum, item) => sum + ((parseFloat(item.quantity) || 0) * (parseFloat(item.price) || 0)), 0).toFixed(2);
        }
    }
}
