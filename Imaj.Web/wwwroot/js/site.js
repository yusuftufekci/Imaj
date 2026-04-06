/**
 * Imaj - Ana JavaScript dosyası
 * Site genelinde kullanılan ortak fonksiyonlar
 */

const ImajTexts = window.imajTexts || {};
const t = (key, fallback) => ImajTexts[key] || fallback;
const getCsrfToken = () => {
    const metaToken = document.querySelector('meta[name="csrf-token"]')?.getAttribute('content');
    if (metaToken) {
        return metaToken;
    }

    const formToken = document.querySelector('input[name="__RequestVerificationToken"]')?.value;
    return formToken || '';
};

// ============================================
// API Service - Merkezi fetch wrapper
// ============================================
const API = {
    /**
     * POST isteği gönderir
     * @param {string} url - Endpoint URL
     * @param {object} data - Gönderilecek veri
     * @returns {Promise<object>} - JSON response
     */
    async post(url, data) {
        try {
            const csrfToken = getCsrfToken();
            const headers = {
                'Content-Type': 'application/json',
                'X-Requested-With': 'XMLHttpRequest'
            };

            if (csrfToken) {
                headers['X-CSRF-TOKEN'] = csrfToken;
            }

            const response = await fetch(url, {
                method: 'POST',
                headers,
                body: JSON.stringify(data)
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    },

    /**
     * GET isteği gönderir
     * @param {string} url - Endpoint URL
     * @param {object} params - Query parametreleri
     * @returns {Promise<object>} - JSON response
     */
    async get(url, params = {}) {
        try {
            // Null veya undefined değerleri temizle
            const cleanParams = {};
            Object.keys(params).forEach(key => {
                const value = params[key];
                if (value !== null && value !== undefined && value !== '') {
                    cleanParams[key] = value;
                }
            });

            const queryString = new URLSearchParams(cleanParams).toString();
            const fullUrl = queryString ? `${url}?${queryString}` : url;

            const response = await fetch(fullUrl, {
                method: 'GET',
                headers: {
                    'X-Requested-With': 'XMLHttpRequest'
                }
            });

            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
            }

            return await response.json();
        } catch (error) {
            console.error('API Error:', error);
            throw error;
        }
    }
};

// ============================================
// Toast/Notification Helper
// ============================================
const Toast = {
    decodeMessage(message) {
        if (typeof message !== 'string' || !message) {
            return message;
        }

        const textarea = document.createElement('textarea');
        textarea.innerHTML = message;
        return textarea.value;
    },

    /**
     * Başarı mesajı gösterir
     * @param {string} message - Gösterilecek mesaj
     */
    success(message) {
        message = this.decodeMessage(message);

        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'success',
                title: t('successTitle', 'Success!'),
                text: message,
                confirmButtonColor: '#3085d6',
                confirmButtonText: t('ok', 'OK')
            });
        } else {
            alert(message);
        }
    },

    /**
     * Hata mesajı gösterir
     * @param {string} message - Gösterilecek mesaj
     */
    error(message) {
        message = this.decodeMessage(message);

        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'error',
                title: t('errorTitle', 'Error!'),
                text: message,
                confirmButtonColor: '#d33',
                confirmButtonText: t('ok', 'OK')
            });
        } else {
            alert(message);
        }
    },

    /**
     * Onay dialogu gösterir
     * @param {string} title - Başlık
     * @param {string} text - Açıklama metni
     * @returns {Promise<boolean>} - Onay verildi mi
     */
    async confirm(title, text) {
        title = this.decodeMessage(title);
        text = this.decodeMessage(text);

        if (typeof Swal !== 'undefined') {
            const result = await Swal.fire({
                title: title,
                text: text,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: t('yes', 'Yes'),
                cancelButtonText: t('cancel', 'Cancel')
            });
            return result.isConfirmed;
        } else {
            return confirm(text);
        }
    }
};

// ============================================
// Form Helpers
// ============================================
const FormHelper = {
    /**
     * Form verilerini JSON olarak döndürür
     * @param {HTMLFormElement} form - Form elementi
     * @returns {object} - Form verileri
     */
    toJson(form) {
        const formData = new FormData(form);
        const data = {};
        formData.forEach((value, key) => {
            data[key] = value;
        });
        return data;
    },

    /**
     * Form alanlarını temizler
     * @param {HTMLFormElement} form - Form elementi
     */
    reset(form) {
        form.reset();
    }
};

const WorkflowActionHelper = {
    init() {
        const forms = document.querySelectorAll('form.workflow-action-form');
        forms.forEach(form => {
            form.addEventListener('submit', async event => {
                if (form.dataset.submitting === 'true') {
                    event.preventDefault();
                    return;
                }

                if (form.dataset.workflowConfirmed === 'true') {
                    form.dataset.submitting = 'true';
                    const submitButtons = document.querySelectorAll('form.workflow-action-form button[type="submit"], form.workflow-action-form input[type="submit"]');
                    submitButtons.forEach(button => {
                        button.disabled = true;
                        button.classList.add('opacity-60', 'cursor-not-allowed');
                    });

                    if (typeof Swal !== 'undefined') {
                        Swal.fire({
                            icon: 'info',
                            title: t('workflowLoadingTitle', 'Processing'),
                            text: t('workflowLoadingText', 'Please wait...'),
                            allowOutsideClick: false,
                            allowEscapeKey: false,
                            showConfirmButton: false,
                            didOpen: () => {
                                Swal.showLoading();
                            }
                        });
                    }

                    return;
                }

                event.preventDefault();

                const isConfirmed = await Toast.confirm(
                    t('workflowConfirmTitle', 'Are you sure?'),
                    t('workflowConfirmText', 'Do you want to continue?')
                );

                if (!isConfirmed) {
                    return;
                }

                form.dataset.workflowConfirmed = 'true';
                if (typeof form.requestSubmit === 'function') {
                    form.requestSubmit();
                    return;
                }

                form.submit();
            });
        });
    }
};

const DateFieldEnhancer = (() => {
    const selector = 'input[type="date"], input[type="datetime-local"]';
    const instances = new Map();
    let domObserver = null;
    let syncTimer = null;

    const isTurkish = () => (document.documentElement.lang || '').toLowerCase().startsWith('tr');
    const getPickerLocale = () => {
        if (isTurkish() && window.flatpickr?.l10ns?.tr) {
            return window.flatpickr.l10ns.tr;
        }

        return 'default';
    };

    const getUiText = () => isTurkish()
        ? {
            clear: 'Temizle',
            today: 'Bugun',
            now: 'Simdi',
            datePlaceholder: 'GG.AA.YYYY',
            dateTimePlaceholder: 'GG.AA.YYYY SS:DD'
        }
        : {
            clear: 'Clear',
            today: 'Today',
            now: 'Now',
            datePlaceholder: 'DD.MM.YYYY',
            dateTimePlaceholder: 'DD.MM.YYYY HH:MM'
        };

    const notifyValueChange = input => {
        input.dispatchEvent(new Event('input', { bubbles: true }));
        input.dispatchEvent(new Event('change', { bubbles: true }));
    };

    const getMinuteIncrement = input => {
        const rawStep = Number(input.step);
        if (!Number.isFinite(rawStep) || rawStep <= 0) {
            return 5;
        }

        const minutes = Math.round(rawStep / 60);
        return minutes > 0 ? minutes : 5;
    };

    const getConfigFor = input => {
        const isDateTime = input.type === 'datetime-local';

        return {
            inputType: input.type,
            altFormat: isDateTime ? 'd.m.Y H:i' : 'd.m.Y',
            dateFormat: isDateTime ? 'Y-m-d\\TH:i' : 'Y-m-d',
            enableTime: isDateTime,
            placeholder: isDateTime ? getUiText().dateTimePlaceholder : getUiText().datePlaceholder,
            minuteIncrement: isDateTime ? getMinuteIncrement(input) : undefined
        };
    };

    const syncAltInputState = (input, altInput, placeholder) => {
        if (!altInput) {
            return;
        }

        altInput.disabled = input.disabled;
        altInput.required = input.required;
        altInput.placeholder = input.getAttribute('placeholder') || placeholder;
        altInput.dataset.imajDateDisplay = 'true';
        altInput.autocomplete = 'off';
    };

    const syncPickerValue = meta => {
        const value = meta.input.value || '';
        if (value === meta.lastValue) {
            syncAltInputState(meta.input, meta.instance.altInput, meta.placeholder);
            return;
        }

        meta.lastValue = value;

        if (!value) {
            meta.instance.clear(false);
            syncAltInputState(meta.input, meta.instance.altInput, meta.placeholder);
            return;
        }

        const parsedValue = meta.instance.parseDate(value, meta.dateFormat);
        if (!parsedValue) {
            return;
        }

        const currentDate = meta.instance.selectedDates[0];
        if (currentDate && currentDate.getTime() === parsedValue.getTime()) {
            syncAltInputState(meta.input, meta.instance.altInput, meta.placeholder);
            return;
        }

        meta.instance.setDate(parsedValue, false, meta.dateFormat);
        syncAltInputState(meta.input, meta.instance.altInput, meta.placeholder);
    };

    const createActionButton = (label, className, onClick) => {
        const button = document.createElement('button');
        button.type = 'button';
        button.className = className;
        button.textContent = label;
        button.addEventListener('click', onClick);
        return button;
    };

    const appendActionRow = meta => {
        if (!meta.instance.calendarContainer || meta.instance.calendarContainer.querySelector('.imaj-date-actions')) {
            return;
        }

        const texts = getUiText();
        const actions = document.createElement('div');
        actions.className = 'imaj-date-actions';

        const nowButtonLabel = meta.enableTime ? texts.now : texts.today;
        const selectCurrentButton = createActionButton(
            nowButtonLabel,
            'imaj-date-actions__button imaj-date-actions__button--primary',
            () => {
                const nextDate = new Date();
                if (!meta.enableTime) {
                    nextDate.setHours(0, 0, 0, 0);
                }

                meta.instance.setDate(nextDate, true, meta.dateFormat);
                meta.lastValue = meta.input.value || '';
                notifyValueChange(meta.input);
                meta.instance.close();
            }
        );

        const clearButton = createActionButton(
            texts.clear,
            'imaj-date-actions__button',
            () => {
                meta.instance.clear(true);
                meta.lastValue = '';
                notifyValueChange(meta.input);
                meta.instance.close();
            }
        );

        actions.appendChild(selectCurrentButton);
        actions.appendChild(clearButton);
        meta.instance.calendarContainer.appendChild(actions);
    };

    const enhanceInput = input => {
        if (!window.flatpickr || instances.has(input) || input.dataset.imajDateDisabled === 'true') {
            return;
        }

        const config = getConfigFor(input);
        const instance = window.flatpickr(input, {
            altInput: true,
            altInputClass: `${input.className} imaj-date-display`,
            altFormat: config.altFormat,
            allowInput: false,
            clickOpens: true,
            dateFormat: config.dateFormat,
            disableMobile: true,
            enableTime: config.enableTime,
            locale: getPickerLocale(),
            minDate: input.min || undefined,
            maxDate: input.max || undefined,
            minuteIncrement: config.minuteIncrement,
            time_24hr: true,
            onReady: (selectedDates, dateStr, fp) => {
                const meta = instances.get(input);
                syncAltInputState(input, fp.altInput, config.placeholder);

                if (fp.altInput) {
                    fp.altInput.addEventListener('keydown', event => {
                        if (event.key !== 'Backspace' && event.key !== 'Delete') {
                            return;
                        }

                        event.preventDefault();
                        fp.clear(true);
                        if (meta) {
                            meta.lastValue = '';
                        }
                        notifyValueChange(input);
                    });
                }

                appendActionRow(meta || {
                    input,
                    instance: fp,
                    dateFormat: config.dateFormat,
                    enableTime: config.enableTime,
                    lastValue: input.value || ''
                });
            },
            onChange: () => {
                const meta = instances.get(input);
                if (meta) {
                    meta.lastValue = input.value || '';
                }
                notifyValueChange(input);
            }
        });

        instances.set(input, {
            input,
            instance,
            inputType: config.inputType,
            dateFormat: config.dateFormat,
            enableTime: config.enableTime,
            lastValue: input.value || '',
            placeholder: config.placeholder
        });

        syncPickerValue(instances.get(input));
    };

    const enhanceWithin = root => {
        if (!root) {
            return;
        }

        const nodes = root.matches?.(selector)
            ? [root]
            : Array.from(root.querySelectorAll?.(selector) || []);

        nodes.forEach(enhanceInput);
    };

    const startSyncLoop = () => {
        if (syncTimer) {
            return;
        }

        syncTimer = window.setInterval(() => {
            instances.forEach((meta, input) => {
                if (!document.body.contains(input)) {
                    meta.instance.destroy();
                    instances.delete(input);
                    return;
                }

                syncPickerValue(meta);
            });
        }, 250);
    };

    const startObserver = () => {
        if (domObserver) {
            return;
        }

        domObserver = new MutationObserver(mutations => {
            mutations.forEach(mutation => {
                mutation.addedNodes.forEach(node => {
                    if (node.nodeType === Node.ELEMENT_NODE) {
                        enhanceWithin(node);
                    }
                });
            });
        });

        domObserver.observe(document.body, {
            childList: true,
            subtree: true
        });
    };

    const init = () => {
        if (!window.flatpickr) {
            return;
        }

        enhanceWithin(document);
        startSyncLoop();
        startObserver();
    };

    return { init };
})();

document.addEventListener('DOMContentLoaded', function () {
    WorkflowActionHelper.init();
    DateFieldEnhancer.init();
});

// Global olarak API ve Toast'u dışa aktar
window.API = API;
window.Toast = Toast;
window.FormHelper = FormHelper;
