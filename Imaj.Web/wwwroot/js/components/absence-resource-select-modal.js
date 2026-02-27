function absenceResourceSelectModal() {
    return {
        isOpen: false,
        targetId: '',
        isMultiSelect: false,

        functionOptions: [],

        filter: {
            code: '',
            name: '',
            functionId: '',
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
            this.functionOptions = detail?.functionOptions || [];
            this.selectedItems = [];

            this.filter = {
                code: '',
                name: '',
                functionId: '',
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
                functionId: '',
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
                FunctionId: this.filter.functionId === '' ? null : this.filter.functionId,
                IsInvalid: this.filter.isInvalid === '' ? null : this.filter.isInvalid,
                ExcludeIds: this.filter.excludeIds,
                Page: this.filter.page,
                PageSize: this.filter.pageSize
            };

            try {
                const result = await API.get('/Absence/SearchResources', params);
                this.items = result.items || [];
                this.totalCount = result.totalCount || 0;
                this.page = result.page || nextPage;
                this.hasMore = this.page * this.filter.pageSize < this.totalCount;
            } catch (error) {
                console.error('Absence resource modal search error:', error);
                this.items = [];
                this.totalCount = 0;
                this.hasMore = false;
                if (window.Toast && typeof window.Toast.error === 'function') {
                    window.Toast.error((window.imajTexts && window.imajTexts.genericError) || 'An error occurred.');
                }
            }
        },

        toggleSelection(resourceItem) {
            if (!this.isMultiSelect) {
                return;
            }

            const resourceId = Number(resourceItem.resourceId);
            const index = this.selectedItems.findIndex(x => Number(x.resourceId) === resourceId);
            if (index >= 0) {
                this.selectedItems.splice(index, 1);
            } else {
                this.selectedItems.push(resourceItem);
            }
        },

        isSelected(resourceItem) {
            const resourceId = Number(resourceItem.resourceId);
            return this.selectedItems.some(x => Number(x.resourceId) === resourceId);
        },

        select(resourceItem) {
            this.$dispatch('absence-resource-selected', {
                targetId: this.targetId,
                resources: [this.mapResource(resourceItem)]
            });
            this.closeModal();
        },

        submitMultiSelection() {
            const resources = this.selectedItems.map(item => this.mapResource(item));
            this.$dispatch('absence-resource-selected', {
                targetId: this.targetId,
                resources: resources
            });
            this.closeModal();
        },

        mapResource(resourceItem) {
            return {
                resourceId: Number(resourceItem.resourceId),
                code: resourceItem.code || '',
                name: resourceItem.name || '',
                functionId: Number(resourceItem.functionId || 0),
                functionName: resourceItem.functionName || '',
                resoCatId: Number(resourceItem.resoCatId || 0),
                resoCatName: resourceItem.resoCatName || '',
                invisible: !!resourceItem.invisible
            };
        }
    };
}

window.absenceResourceSelectModal = absenceResourceSelectModal;
