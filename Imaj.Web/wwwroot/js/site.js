/**
 * Imaj - Ana JavaScript dosyası
 * Site genelinde kullanılan ortak fonksiyonlar
 */

// ============================================
// Mobile Menu Toggle
// ============================================
document.addEventListener('DOMContentLoaded', function() {
    const mobileMenuButton = document.querySelector('[aria-controls="mobile-menu"]');
    const mobileMenu = document.getElementById('mobile-menu');

    if (mobileMenuButton && mobileMenu) {
        mobileMenuButton.addEventListener('click', function() {
            const isExpanded = mobileMenuButton.getAttribute('aria-expanded') === 'true';
            
            // Toggle menu visibility
            mobileMenu.classList.toggle('hidden');
            
            // Update aria-expanded attribute
            mobileMenuButton.setAttribute('aria-expanded', !isExpanded);
            
            // Toggle hamburger/close icon
            const icons = mobileMenuButton.querySelectorAll('svg');
            icons.forEach(icon => icon.classList.toggle('hidden'));
        });
    }
});

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
            const response = await fetch(url, {
                method: 'POST',
                headers: { 
                    'Content-Type': 'application/json',
                    'X-Requested-With': 'XMLHttpRequest'
                },
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
            const queryString = new URLSearchParams(params).toString();
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
    /**
     * Başarı mesajı gösterir
     * @param {string} message - Gösterilecek mesaj
     */
    success(message) {
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'success',
                title: 'Başarılı!',
                text: message,
                confirmButtonColor: '#3085d6',
                confirmButtonText: 'Tamam'
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
        if (typeof Swal !== 'undefined') {
            Swal.fire({
                icon: 'error',
                title: 'Hata!',
                text: message,
                confirmButtonColor: '#d33',
                confirmButtonText: 'Tamam'
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
        if (typeof Swal !== 'undefined') {
            const result = await Swal.fire({
                title: title,
                text: text,
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#3085d6',
                cancelButtonColor: '#d33',
                confirmButtonText: 'Evet',
                cancelButtonText: 'İptal'
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

// Global olarak API ve Toast'u dışa aktar
window.API = API;
window.Toast = Toast;
window.FormHelper = FormHelper;
