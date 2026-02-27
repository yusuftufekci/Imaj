function functionSelectModal() {
    return {
        isOpen: false,
        targetId: '',
        isMultiSelect: false,

        filter: {
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
                Name: this.filter.name,
                IsInvalid: this.filter.isInvalid === '' ? null : this.filter.isInvalid,
                ExcludeIds: this.filter.excludeIds,
                Page: this.filter.page,
                PageSize: this.filter.pageSize
            };

            try {
                const result = await API.get('/User/SearchFunctions', params);
                this.items = result.items || [];
                this.totalCount = result.totalCount || 0;
                this.page = result.page || nextPage;
                this.hasMore = this.page * this.filter.pageSize < this.totalCount;
            } catch (error) {
                console.error('Function modal search error:', error);
                this.items = [];
                this.totalCount = 0;
                this.hasMore = false;
                if (window.Toast && typeof window.Toast.error === 'function') {
                    window.Toast.error((window.imajTexts && window.imajTexts.genericError) || 'An error occurred.');
                }
            }
        },

        toggleSelection(functionItem) {
            if (!this.isMultiSelect) {
                return;
            }

            const functionId = Number(functionItem.id);
            const index = this.selectedItems.findIndex(x => Number(x.id) === functionId);
            if (index >= 0) {
                this.selectedItems.splice(index, 1);
            } else {
                this.selectedItems.push(functionItem);
            }
        },

        isSelected(functionItem) {
            const functionId = Number(functionItem.id);
            return this.selectedItems.some(x => Number(x.id) === functionId);
        },

        select(functionItem) {
            this.$dispatch('user-function-selected', {
                functionItem: functionItem,
                functions: [functionItem],
                targetId: this.targetId
            });
            this.closeModal();
        },

        submitMultiSelection() {
            this.$dispatch('user-function-selected', {
                functions: this.selectedItems,
                targetId: this.targetId
            });
            this.closeModal();
        }
    };
}

window.functionSelectModal = functionSelectModal;
