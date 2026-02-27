function functionResoCatSelectModal() {
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
                const result = await API.get('/Function/SearchResoCategories', params);
                this.items = result.items || [];
                this.totalCount = result.totalCount || 0;
                this.page = result.page || nextPage;
                this.hasMore = this.page * this.filter.pageSize < this.totalCount;
            } catch (error) {
                console.error('Function reso category modal search error:', error);
                this.items = [];
                this.totalCount = 0;
                this.hasMore = false;
                if (window.Toast && typeof window.Toast.error === 'function') {
                    window.Toast.error((window.imajTexts && window.imajTexts.genericError) || 'An error occurred.');
                }
            }
        },

        toggleSelection(resoCatItem) {
            if (!this.isMultiSelect) {
                return;
            }

            const resoCatId = Number(resoCatItem.id);
            const index = this.selectedItems.findIndex(x => Number(x.id) === resoCatId);
            if (index >= 0) {
                this.selectedItems.splice(index, 1);
            } else {
                this.selectedItems.push(resoCatItem);
            }
        },

        isSelected(resoCatItem) {
            const resoCatId = Number(resoCatItem.id);
            return this.selectedItems.some(x => Number(x.id) === resoCatId);
        },

        select(resoCatItem) {
            this.$dispatch('function-resocat-selected', {
                resoCatItem: {
                    resoCatId: resoCatItem.id,
                    name: resoCatItem.name,
                    invisible: resoCatItem.invisible
                },
                resoCats: [{
                    resoCatId: resoCatItem.id,
                    name: resoCatItem.name,
                    invisible: resoCatItem.invisible
                }],
                targetId: this.targetId
            });
            this.closeModal();
        },

        submitMultiSelection() {
            const mapped = this.selectedItems.map(item => ({
                resoCatId: item.id,
                name: item.name,
                invisible: item.invisible
            }));

            this.$dispatch('function-resocat-selected', {
                resoCats: mapped,
                targetId: this.targetId
            });
            this.closeModal();
        }
    };
}

window.functionResoCatSelectModal = functionResoCatSelectModal;
