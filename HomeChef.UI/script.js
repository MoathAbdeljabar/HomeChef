// ============================================
// GLOBAL CONFIGURATION & UTILITIES
// ============================================

const API_BASE_URL = 'https://api.homechef.local'; // Change to your API URL
const STORAGE_KEY = 'homechef_user';

// ============================================
// UTILITY FUNCTIONS
// ============================================

/**
 * Get stored user data from localStorage
 */
function getStoredUser() {
    const userData = localStorage.getItem(STORAGE_KEY);
    return userData ? JSON.parse(userData) : null;
}

/**
 * Store user data in localStorage
 */
function storeUser(userData) {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(userData));
}

/**
 * Remove user data from localStorage
 */
function clearStoredUser() {
    localStorage.removeItem(STORAGE_KEY);
}

/**
 * Check if user is authenticated
 */
function isAuthenticated() {
    return getStoredUser() !== null;
}

/**
 * Get authentication token
 */
function getAuthToken() {
    const user = getStoredUser();
    return user?.accessToken || null;
}

/**
 * Make API request with authentication
 */
async function apiCall(endpoint, options = {}) {
    const headers = {
        'Content-Type': 'application/json',
        ...options.headers
    };

    const token = getAuthToken();
    if (token) {
        headers['Authorization'] = `Bearer ${token}`;
    }

    try {
        const response = await fetch(`${API_BASE_URL}${endpoint}`, {
            ...options,
            headers
        });

        if (response.status === 401) {
            // Token expired, redirect to login
            clearStoredUser();
            window.location.href = 'login.html';
            return null;
        }

        return await response.json();
    } catch (error) {
        console.error('API Error:', error);
        return null;
    }
}

/**
 * Show alert message
 */
function showAlert(elementId, message, type = 'info') {
    const alertElement = document.getElementById(elementId);
    if (alertElement) {
        alertElement.textContent = message;
        alertElement.className = `alert alert-${type}`;
        alertElement.classList.remove('hidden');
        
        // Auto-hide success messages
        if (type === 'success') {
            setTimeout(() => {
                alertElement.classList.add('hidden');
            }, 5000);
        }
    }
}

/**
 * Validate email format
 */
function isValidEmail(email) {
    const regex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return regex.test(email);
}

/**
 * Validate phone number format
 */
function isValidPhone(phone) {
    const regex = /^\+?1?\d{9,15}$/;
    return regex.test(phone.replace(/\D/g, ''));
}

/**
 * Validate password strength
 */
function validatePassword(password) {
    return {
        length: password.length >= 8,
        uppercase: /[A-Z]/.test(password),
        digit: /\d/.test(password),
        special: /[!@#$%^&*()_+\-=\[\]{};':"\\|,.<>\/?]/.test(password)
    };
}

/**
 * Disable button and show loading state
 */
function setButtonLoading(buttonId, isLoading = true) {
    const button = document.getElementById(buttonId);
    if (!button) return;

    const textSpan = button.querySelector('.btn-text');
    const loaderSpan = button.querySelector('.btn-loader');

    if (isLoading) {
        button.disabled = true;
        if (textSpan) textSpan.classList.add('hidden');
        if (loaderSpan) loaderSpan.classList.remove('hidden');
    } else {
        button.disabled = false;
        if (textSpan) textSpan.classList.remove('hidden');
        if (loaderSpan) loaderSpan.classList.add('hidden');
    }
}

/**
 * Clear form errors
 */
function clearFormErrors(formId) {
    const form = document.getElementById(formId);
    if (!form) return;

    form.querySelectorAll('.error-message').forEach(el => {
        el.textContent = '';
    });
}

/**
 * Show form field error
 */
function setFieldError(fieldId, message) {
    const errorElement = document.getElementById(fieldId + 'Error');
    if (errorElement) {
        errorElement.textContent = message;
    }
}

/**
 * Smooth scroll to element
 */
function smoothScroll(elementId) {
    const element = document.getElementById(elementId);
    if (element) {
        element.scrollIntoView({ behavior: 'smooth' });
    }
}

// ============================================
// NAVIGATION & MOBILE MENU
// ============================================

document.addEventListener('DOMContentLoaded', function() {
    const hamburger = document.querySelector('.hamburger');
    const navMenu = document.querySelector('.nav-menu');

    if (hamburger && navMenu) {
        hamburger.addEventListener('click', function() {
            navMenu.classList.toggle('active');
            hamburger.classList.toggle('active');
        });

        // Close menu when link is clicked
        navMenu.querySelectorAll('a').forEach(link => {
            link.addEventListener('click', function() {
                navMenu.classList.remove('active');
                hamburger.classList.remove('active');
            });
        });
    }

    // Update user display if logged in
    updateUserDisplay();
});

/**
 * Update navbar with user information
 */
function updateUserDisplay() {
    const user = getStoredUser();
    const navActions = document.querySelector('.nav-actions');

    if (navActions && user) {
        navActions.innerHTML = `
            <div class="user-menu">
                <span class="user-name">${user.fullName}</span>
                <button class="btn-secondary" onclick="logout()">Logout</button>
            </div>
        `;
    }
}

/**
 * Logout user
 */
function logout() {
    clearStoredUser();
    window.location.href = 'index.html';
}

// ============================================
// FORM VALIDATION & SUBMISSION
// ============================================

/**
 * Contact form submission
 */
document.getElementById('contactForm')?.addEventListener('submit', async function(e) {
    e.preventDefault();

    const formData = new FormData(this);
    const data = {
        name: formData.get('name'),
        email: formData.get('email'),
        message: formData.get('message')
    };

    try {
        // In a real app, send to backend
        console.log('Contact form:', data);
        showAlert('loginMessage', 'Message sent successfully! We\'ll be in touch soon.', 'success');
        this.reset();
    } catch (error) {
        console.error('Error:', error);
        showAlert('loginMessage', 'Failed to send message. Please try again.', 'danger');
    }
});

// ============================================
// PASSWORD VISIBILITY TOGGLE
// ============================================

document.querySelectorAll('.toggle-password').forEach(button => {
    button.addEventListener('click', function(e) {
        e.preventDefault();
        const input = this.parentElement.querySelector('input');
        const icon = this.querySelector('i');

        if (input.type === 'password') {
            input.type = 'text';
            icon.classList.remove('fa-eye');
            icon.classList.add('fa-eye-slash');
        } else {
            input.type = 'password';
            icon.classList.add('fa-eye');
            icon.classList.remove('fa-eye-slash');
        }
    });
});

// ============================================
// RESPONSIVE NAVIGATION
// ============================================

window.addEventListener('resize', function() {
    if (window.innerWidth > 768) {
        const hamburger = document.querySelector('.hamburger');
        const navMenu = document.querySelector('.nav-menu');
        if (hamburger && navMenu) {
            hamburger.classList.remove('active');
            navMenu.classList.remove('active');
        }
    }
});

// ============================================
// ANIMATIONS ON SCROLL
// ============================================

const observerOptions = {
    threshold: 0.1,
    rootMargin: '0px 0px -50px 0px'
};

const observer = new IntersectionObserver(function(entries) {
    entries.forEach(entry => {
        if (entry.isIntersecting) {
            entry.target.classList.add('visible');
        }
    });
}, observerOptions);

document.querySelectorAll('.feature-card, .menu-card, .testimonial-card').forEach(el => {
    observer.observe(el);
});

// ============================================
// EXPORT FUNCTIONS FOR USE IN OTHER FILES
// ============================================

window.apiCall = apiCall;
window.getAuthToken = getAuthToken;
window.isAuthenticated = isAuthenticated;
window.getStoredUser = getStoredUser;
window.storeUser = storeUser;
window.clearStoredUser = clearStoredUser;
window.showAlert = showAlert;
window.isValidEmail = isValidEmail;
window.isValidPhone = isValidPhone;
window.validatePassword = validatePassword;
window.setButtonLoading = setButtonLoading;
window.clearFormErrors = clearFormErrors;
window.setFieldError = setFieldError;
window.logout = logout;
