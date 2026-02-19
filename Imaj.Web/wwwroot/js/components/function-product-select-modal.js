function functionProductSelectModal() {
    return {
        isOpen: false,
        targetId: '',
        isMultiSelect: false,

        filter: {
            code: '',
            name: '',
            isInvalid: '',
            excludeIds: '',
            page: 1,
            pageSize: 10
        },

        items: [],
        selectedItems: [],
        totalCount: 0,
        page: 1,
        hasMore: false,

        openModal(detail) {
            this.isOpen = true;
            this.targetId = detail?.targetId || '';
            this.isMultiSelect = !!detail?.isMultiSelect;
            this.selectedItems = [];

            this.filter = {
                code: '',
                name: '',
                isInvalid: '',
                excludeIds: detail?.excludeIds || '',
                page: 1,
                pageSize: 10
            };

            this.items = [];
            this.totalCount = 0;
            this.page = 1;
            this.hasMore = false;

            this.search(1);
        },

        closeModal() {
            this.isOpen = false;
        },

        resetFilter() {
            const excludeIds = this.filter.excludeIds;
            this.filter = {
                code: '',
                name: '',
                isInvalid: '',
                excludeIds: excludeIds,
                page: 1,
                pageSize: 10
            };
        },

        async search(page) {
            const nextPage = page && page > 0 ? page : 1;
            this.filter.page = nextPage;
            this.page = nextPage;

            const params = {
                Code: this.filter.code,
                Name: this.filter.name,
                IsInvalid: this.filter.isInvalid === '' ? null : this.filter.isInvalid,
                ExcludeIds: this.filter.excludeIds,
                Page: this.filter.page,
                PageSize: this.filter.pageSize
            };

            try {
                const result = await API.get('/Function/SearchProducts', params);
                this.items = result.items || [];
                this.totalCount = result.totalCount || 0;
                this.page = result.page || nextPage;
                this.hasMore = this.page * this.filter.pageSize < this.totalCount;
            } catch (error) {
                console.error('Function product modal search error:', error);
                this.items = [];
                this.totalCount = 0;
                this.hasMore = false;
                if (window.Toast && typeof window.Toast.error === 'function') {
                    window.Toast.error('Urun listesi alinirken hata olustu.');
                }
            }
        },

        toggleSelection(productItem) {
            if (!this.isMultiSelect) {
                return;
            }

            const productId = Number(productItem.id);
            const index = this.selectedItems.findIndex(x => Number(x.id) === productId);
            if (index >= 0) {
                this.selectedItems.splice(index, 1);
            } else {
                this.selectedItems.push(productItem);
            }
        },

        isSelected(productItem) {
            const productId = Number(productItem.id);
            return this.selectedItems.some(x => Number(x.id) === productId);
        },

        select(productItem) {
            this.$dispatch('function-product-selected', {
                productItem: {
                    productId: productItem.id,
                    code: productItem.code,
                    name: productItem.name,
                    invisible: productItem.invisible
                },
                products: [{
                    productId: productItem.id,
                    code: productItem.code,
                    name: productItem.name,
                    invisible: productItem.invisible
                }],
                targetId: this.targetId
            });
            this.closeModal();
        },

        submitMultiSelection() {
            const mapped = this.selectedItems.map(item => ({
                productId: item.id,
                code: item.code,
                name: item.name,
                invisible: item.invisible
            }));

            this.$dispatch('function-product-selected', {
                products: mapped,
                targetId: this.targetId
            });
            this.closeModal();
        }
    };
}

window.functionProductSelectModal = functionProductSelectModal;
