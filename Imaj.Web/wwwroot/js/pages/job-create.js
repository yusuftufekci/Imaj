const jobCreateText = (key, fallback) => (window.imajTexts && window.imajTexts[key]) || fallback;

function jobCreate(config) {
    return {
        defaultWorkTypeId: config.defaultWorkTypeId || 0,
        defaultWorkTypeName: config.defaultWorkTypeName || '',
        workTypeNames: config.workTypeNames || {},
        defaultTimeTypeId: config.defaultTimeTypeId || 0,
        productPicker: config.productPicker || {},

        validationError: '',
        validationErrorDetails: [],
        isSubmitting: false,

        form: {
            customerId: config.form.customerId || 0,
            customerCode: config.form.customerCode || '',
            customerName: config.form.customerName || '',
            name: config.form.name || '',
            relatedPerson: config.form.relatedPerson || '',
            startDate: config.form.startDate || '',
            endDate: config.form.endDate || ''
        },
        productGroups: [],
        selectedProductGroup: '',
        productPickerPageSize: config.productPicker?.defaultPageSize || 16,
        selectedProductKeys: [],
        productSequence: 0,
        autoOvertimeTemplates: (config.autoOvertimeTemplates || []).map(item => ({
            employeeId: item.employeeId || item.id || 0,
            employeeCode: item.employeeCode || item.code || '',
            employeeName: item.employeeName || item.name || '',
            workTypeId: item.workTypeId || item.taskType || 0,
            timeTypeId: item.timeTypeId || item.overtimeType || 0,
            quantity: item.quantity,
            amount: item.amount,
            notes: item.notes || ''
        })),
        overtimes: [],
        products: [],
        productCategories: [],

        async init() {
            this.overtimes = (config.overtimes || []).map(item => this.createOvertimeItem(item));
            this.products = (config.products || []).map(prod => this.createProductItem(prod));
            this.productCategories = (config.productCategories || []).map(cat => this.createCategoryItem(cat));
            this.recalculateProductCategories();
            this.syncAutoOvertimes();

            if (typeof this.$watch === 'function') {
                this.$watch('form.startDate', () => this.syncAutoOvertimes());
                this.$watch('form.endDate', () => this.syncAutoOvertimes());
            }

            await this.loadProductGroups();
        },

        parseDecimal(value, fallback = 0) {
            const parsed = Number.parseFloat(value);
            return Number.isFinite(parsed) ? parsed : fallback;
        },

        roundMoney(value) {
            return Math.round((this.parseDecimal(value) + Number.EPSILON) * 100) / 100;
        },

        clampDiscount(value) {
            const discount = this.parseDecimal(value, 0);
            return Math.min(100, Math.max(0, discount));
        },

        formatMoney(value) {
            return this.roundMoney(value).toLocaleString('tr-TR', {
                minimumFractionDigits: 2,
                maximumFractionDigits: 2
            });
        },

        formatQuantity(value) {
            const quantity = this.parseDecimal(value, 0);
            return quantity.toLocaleString('tr-TR', {
                minimumFractionDigits: 0,
                maximumFractionDigits: 2
            });
        },

        normalizeCode(value) {
            return String(value || '').trim().toUpperCase();
        },

        parseDateTimeLocal(value) {
            if (!value || typeof value !== 'string') {
                return null;
            }

            const [datePart, timePart = '00:00'] = value.split('T');
            if (!datePart || !timePart) {
                return null;
            }

            const [year, month, day] = datePart.split('-').map(Number);
            const [hour, minute] = timePart.split(':').map(Number);

            if (![year, month, day, hour, minute].every(Number.isFinite)) {
                return null;
            }

            return new Date(year, month - 1, day, hour, minute);
        },

        isAfterHoursTime(dateValue) {
            return !!dateValue && (
                dateValue.getHours() > 18 ||
                (dateValue.getHours() === 18 && dateValue.getMinutes() >= 0)
            );
        },

        shouldAddAfterHoursOvertime() {
            const startDate = this.parseDateTimeLocal(this.form.startDate);
            const endDate = this.parseDateTimeLocal(this.form.endDate);
            return this.isAfterHoursTime(startDate) || this.isAfterHoursTime(endDate);
        },

        createOvertimeItem(item) {
            const taskTypeId = item.workTypeId || item.taskType || item.defaultWorkTypeId || this.defaultWorkTypeId;
            const code = item.employeeCode || item.code || '';
            const employeeId = item.employeeId || item.id || 0;

            return {
                id: employeeId,
                code,
                name: item.employeeName || item.name || '',
                taskType: taskTypeId,
                taskTypeName: item.workTypeName || item.taskTypeName || this.resolveWorkTypeName(taskTypeId) || '',
                overtimeType: item.timeTypeId || item.overtimeType || this.defaultTimeTypeId,
                quantity: this.parseDecimal(item.quantity, 1),
                amount: this.parseDecimal(item.amount, 0),
                notes: item.notes || '',
                autoTemplateCode: this.normalizeCode(code)
            };
        },

        hasAutoOvertime(template) {
            const templateEmployeeId = template.employeeId || 0;
            const templateCode = this.normalizeCode(template.employeeCode);

            return this.overtimes.some(item =>
                (templateEmployeeId > 0 && this.parseDecimal(item.id, 0) === templateEmployeeId) ||
                (templateCode && this.normalizeCode(item.code) === templateCode)
            );
        },

        syncAutoOvertimes() {
            if (!this.shouldAddAfterHoursOvertime() || !this.autoOvertimeTemplates.length) {
                return;
            }

            this.autoOvertimeTemplates.forEach(template => {
                if (this.hasAutoOvertime(template)) {
                    return;
                }

                this.overtimes.push(this.createOvertimeItem(template));
            });
        },

        createProductItem(prod) {
            this.productSequence += 1;

            const quantity = this.parseDecimal(prod.quantity, 1);
            const price = this.parseDecimal(prod.price, 0);
            const subTotal = this.roundMoney(quantity * price);
            const netAmount = this.roundMoney(prod.netAmount ?? subTotal);

            return {
                clientKey: `prod-${this.productSequence}`,
                id: prod.id || 0,
                code: prod.code || '',
                name: prod.name || '',
                categoryId: prod.categoryId || 0,
                categoryName: prod.categoryName || prod.category || '',
                quantity,
                price,
                netAmount,
                notes: prod.notes || ''
            };
        },

        createCategoryItem(category) {
            return {
                categoryId: category.categoryId || 0,
                name: category.name || '',
                subTotal: this.roundMoney(category.subTotal || 0),
                discount: this.clampDiscount(category.discount || 0),
                discountAmount: this.roundMoney(category.discountAmount || 0),
                netTotal: this.roundMoney(category.netTotal || 0)
            };
        },

        buildCategoryKey(categoryId, categoryName) {
            return `${categoryId || 0}:${categoryName || ''}`;
        },

        calculateProductSubTotal(item) {
            return this.roundMoney(this.parseDecimal(item.quantity) * this.parseDecimal(item.price));
        },

        recalculateProductCategories() {
            const discountByCategory = new Map(
                (this.productCategories || []).map(category => [
                    this.buildCategoryKey(category.categoryId, category.name),
                    this.clampDiscount(category.discount)
                ])
            );

            const groupedCategories = new Map();
            this.products.forEach(product => {
                const categoryId = product.categoryId || 0;
                const categoryName = product.categoryName || jobCreateText('uncategorized', '-');
                const categoryKey = this.buildCategoryKey(categoryId, categoryName);

                if (!groupedCategories.has(categoryKey)) {
                    groupedCategories.set(categoryKey, {
                        categoryId,
                        name: categoryName,
                        subTotal: 0,
                        discount: discountByCategory.get(categoryKey) || 0,
                        discountAmount: 0,
                        netTotal: 0
                    });
                }

                const group = groupedCategories.get(categoryKey);
                group.subTotal = this.roundMoney(group.subTotal + this.parseDecimal(product.netAmount));
            });

            this.productCategories = Array.from(groupedCategories.values())
                .sort((left, right) => (left.name || '').localeCompare(right.name || '', 'tr'))
                .map(category => {
                    const discount = this.clampDiscount(category.discount);
                    const discountAmount = this.roundMoney(category.subTotal * discount / 100);

                    return {
                        ...category,
                        discount,
                        discountAmount,
                        netTotal: this.roundMoney(category.subTotal - discountAmount)
                    };
                });
        },

        updateProductNetAmount(item, rawValue) {
            item.netAmount = this.roundMoney(Math.max(0, this.parseDecimal(rawValue, 0)));
        },

        updateCategoryDiscount(category, rawValue) {
            category.discount = this.clampDiscount(rawValue);
        },

        calculateProductGrossTotal() {
            return this.products.reduce((sum, item) => sum + this.calculateProductSubTotal(item), 0);
        },

        calculateProductNetTotal() {
            return this.products.reduce((sum, item) => sum + this.parseDecimal(item.netAmount), 0);
        },

        calculateProductTotal() {
            return this.roundMoney(
                this.products.reduce((sum, item) => sum + this.calculateProductSubTotal(item), 0)
            ).toFixed(2);
        },

        calculateCategoryGrossTotal() {
            return this.productCategories.reduce((sum, item) => sum + this.parseDecimal(item.subTotal), 0);
        },

        calculateCategoryDiscountTotal() {
            return this.productCategories.reduce((sum, item) => sum + this.parseDecimal(item.discountAmount), 0);
        },

        calculateCategoryNetTotal() {
            return this.productCategories.reduce((sum, item) => sum + this.parseDecimal(item.netTotal), 0);
        },

        normalizeProductPickerPageSize() {
            const parsed = Number.parseInt(this.productPickerPageSize, 10);
            const fallback = this.productPicker.defaultPageSize || 16;
            this.productPickerPageSize = Number.isFinite(parsed) && parsed > 0 ? parsed : fallback;
            return this.productPickerPageSize;
        },

        getSelectedFunctionId() {
            const formFunctionId = this.$refs.createJobForm
                ? this.$refs.createJobForm.querySelector('input[name="FunctionId"]')?.value
                : '';

            return formFunctionId || this.productPicker.functionId || '';
        },

        async loadProductGroups() {
            if (!this.productPicker.productGroupsEndpoint) {
                this.productGroups = [];
                this.selectedProductGroup = '';
                return;
            }

            try {
                const url = new URL(this.productPicker.productGroupsEndpoint, window.location.origin);
                const selectedFunctionId = this.getSelectedFunctionId();
                if (selectedFunctionId) {
                    url.searchParams.set('functionId', selectedFunctionId);
                }

                const response = await fetch(url.toString());
                if (!response.ok) {
                    this.productGroups = [];
                    this.selectedProductGroup = '';
                    return;
                }

                const groups = await response.json();
                this.productGroups = Array.isArray(groups) ? groups : [];

                if (this.productGroups.length === 0) {
                    this.selectedProductGroup = '';
                    return;
                }

                const hasCurrentSelection = this.productGroups.some(group => group?.name === this.selectedProductGroup);
                if (!hasCurrentSelection) {
                    this.selectedProductGroup = this.productGroups[0]?.name || '';
                }
            } catch (error) {
                console.error('Product groups could not be loaded:', error);
                this.productGroups = [];
                this.selectedProductGroup = '';
            }
        },

        openProductPicker() {
            this.openProductPickerModal({
                productGroup: '',
                autoSearch: false,
                showFilter: true
            });
        },

        openGroupedProductPicker() {
            this.openProductPickerModal({
                productGroup: this.selectedProductGroup || '',
                autoSearch: true,
                showFilter: false
            });
        },

        openProductPickerModal({ productGroup, autoSearch, showFilter }) {
            const selectedFunctionId = this.getSelectedFunctionId();

            this.$dispatch('product-select-open', {
                targetId: 'productAdd',
                isMultiSelect: true,
                searchEndpoint: this.productPicker.searchEndpoint || '/Job/ProductSearch',
                categoriesEndpoint: this.productPicker.categoriesEndpoint || '/Job/ProductCategories',
                productGroupsEndpoint: this.productPicker.productGroupsEndpoint || '/Job/ProductGroups',
                functionsEndpoint: this.productPicker.functionsEndpoint || '/Job/ProductFunctions',
                functionId: selectedFunctionId,
                functionName: this.productPicker.functionName || '',
                lockFunction: true,
                productGroup: productGroup || '',
                autoSearch: !!autoSearch,
                showFilter: showFilter !== false,
                pageSize: this.normalizeProductPickerPageSize()
            });
        },

        async validateAndSubmit() {
            this.validationError = '';
            this.validationErrorDetails = [];
            this.recalculateProductCategories();

            if (!this.form.customerId || this.form.customerId <= 0) {
                this.validationError = jobCreateText('pleaseSelectCustomer', 'Please select a customer.');
                window.scrollTo({ top: 0, behavior: 'smooth' });
                return;
            }

            if (!this.form.name || this.form.name.trim() === '') {
                this.validationError = jobCreateText('pleaseEnterJobName', 'Please enter a job name.');
                window.scrollTo({ top: 0, behavior: 'smooth' });
                return;
            }

            if (!this.form.startDate) {
                this.validationError = jobCreateText('pleaseEnterStartDate', 'Please enter a start date.');
                window.scrollTo({ top: 0, behavior: 'smooth' });
                return;
            }

            if (this.isSubmitting) {
                return;
            }

            this.isSubmitting = true;

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
                        window.location.href = result.redirectUrl;
                    } else {
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
                const employees = detail.employees || (detail.employee ? [detail.employee] : []);

                employees.forEach(emp => {
                    const taskTypeId = emp.defaultWorkTypeId || emp.DefaultWorkTypeId || this.defaultWorkTypeId;
                    const taskTypeName = emp.defaultWorkTypeName
                        || emp.DefaultWorkTypeName
                        || this.resolveWorkTypeName(taskTypeId)
                        || this.defaultWorkTypeName
                        || '';

                    this.overtimes.push(this.createOvertimeItem({
                        employeeId: emp.id || 0,
                        employeeCode: emp.code,
                        employeeName: emp.name,
                        workTypeId: taskTypeId,
                        workTypeName: taskTypeName,
                        timeTypeId: this.defaultTimeTypeId,
                        quantity: emp.quantity,
                        amount: 0,
                        notes: ''
                    }));
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
            return this.overtimes.reduce((sum, item) => sum + (this.parseDecimal(item.amount) || 0), 0).toFixed(2);
        },

        addProduct(detail) {
            if (detail.targetId === 'productAdd') {
                const products = detail.products || (detail.product ? [detail.product] : []);

                products.forEach(prod => {
                    this.products.push(this.createProductItem({
                        id: prod.id || 0,
                        code: prod.code,
                        name: prod.name,
                        categoryId: prod.categoryId || 0,
                        categoryName: prod.categoryName || prod.category || '',
                        quantity: prod.quantity,
                        price: prod.price || 0,
                        netAmount: prod.netAmount,
                        notes: ''
                    }));
                });

                this.recalculateProductCategories();
            }
        },

        removeProduct(index) {
            this.products.splice(index, 1);
            this.recalculateProductCategories();
        },

        toggleProductSelection(productKey) {
            const index = this.selectedProductKeys.indexOf(productKey);
            if (index > -1) {
                this.selectedProductKeys.splice(index, 1);
                return;
            }

            this.selectedProductKeys.push(productKey);
        },

        isProductSelected(productKey) {
            return this.selectedProductKeys.includes(productKey);
        },

        removeSelectedProducts() {
            if (this.selectedProductKeys.length === 0) {
                return;
            }

            const selectedKeys = new Set(this.selectedProductKeys);
            this.products = this.products.filter(item => !selectedKeys.has(item.clientKey));
            this.selectedProductKeys = [];
            this.recalculateProductCategories();
        }
    };
}
