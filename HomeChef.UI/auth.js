// ============================================
// AUTHENTICATION PAGE LOGIC
// ============================================

// ============================================
// LOGIN PAGE
// ============================================

const loginForm = document.getElementById('loginForm');
if (loginForm) {
    loginForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        clearFormErrors('loginForm');

        const phone = document.getElementById('phone').value.trim();
        const password = document.getElementById('password').value;
        const remember = document.querySelector('input[name="remember"]').checked;

        // Validation
        let isValid = true;

        if (!isValidPhone(phone)) {
            setFieldError('phone', 'Please enter a valid phone number');
            isValid = false;
        }

        if (!password) {
            setFieldError('password', 'Password is required');
            isValid = false;
        }

        if (!isValid) return;

        // Submit login
        setButtonLoading('loginBtn', true);

        try {
            const response = await apiCall('/api/auth/login', {
                method: 'POST',
                body: JSON.stringify({
                    phoneNumber: phone,
                    password: password,
                    rememberDevice: remember
                })
            });

            if (response?.isSuccess && response?.data) {
                // Store user data
                storeUser({
                    accessToken: response.data.accessToken,
                    refreshToken: response.data.refreshToken,
                    userId: response.data.userId,
                    phoneNumber: response.data.phoneNumber,
                    fullName: response.data.fullName
                });

                showAlert('loginMessage', 'Login successful! Redirecting...', 'success');
                setTimeout(() => {
                    window.location.href = 'dashboard.html';
                }, 1500);
            } else {
                const errorMessage = response?.message || 'Login failed. Please try again.';
                
                // Check if phone verification is required
                if (response?.data?.requiresPhoneConfirmation) {
                    showAlert('loginMessage', 'Phone number must be verified first. Please check your SMS.', 'danger');
                } else {
                    showAlert('loginMessage', errorMessage, 'danger');
                }
            }
        } catch (error) {
            console.error('Login error:', error);
            showAlert('loginMessage', 'An error occurred. Please try again.', 'danger');
        } finally {
            setButtonLoading('loginBtn', false);
        }
    });

    // Social login handlers
    document.querySelector('.google-btn')?.addEventListener('click', function() {
        handleSocialLogin('google');
    });

    document.querySelector('.apple-btn')?.addEventListener('click', function() {
        handleSocialLogin('apple');
    });
}

/**
 * Handle social login
 */
function handleSocialLogin(provider) {
    // In a real app, this would open OAuth flow
    console.log(`Logging in with ${provider}`);
    showAlert('loginMessage', `${provider} login coming soon!`, 'info');
}

// ============================================
// REGISTRATION PAGE
// ============================================

const registerForm = document.getElementById('registerForm');
if (registerForm) {
    let currentStep = 1;

    // Handle step navigation
    document.querySelectorAll('.next-step').forEach(btn => {
        btn.addEventListener('click', function() {
            const nextStep = parseInt(this.dataset.step);
            if (validateCurrentStep()) {
                showStep(nextStep);
            }
        });
    });

    document.querySelectorAll('.prev-step').forEach(btn => {
        btn.addEventListener('click', function() {
            const prevStep = parseInt(this.dataset.step);
            showStep(prevStep);
        });
    });

    // Handle registration submission
    registerForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        clearFormErrors('registerForm');

        if (!validateCurrentStep()) return;

        const formData = {
            fullName: document.getElementById('fullname').value.trim(),
            email: document.getElementById('email').value.trim(),
            phoneNumber: document.getElementById('phone').value.trim(),
            password: document.getElementById('password').value,
            confirmPassword: document.getElementById('confirmPassword').value
        };

        // Final validation
        if (formData.password !== formData.confirmPassword) {
            setFieldError('confirmPassword', 'Passwords do not match');
            return;
        }

        if (!document.querySelector('input[name="terms"]').checked) {
            setFieldError('terms', 'You must accept the terms and conditions');
            return;
        }

        // Submit registration
        setButtonLoading('registerBtn', true);

        try {
            const response = await apiCall('/api/auth/register', {
                method: 'POST',
                body: JSON.stringify({
                    fullName: formData.fullName,
                    email: formData.email,
                    phoneNumber: formData.phoneNumber,
                    password: formData.password,
                    confirmPassword: formData.confirmPassword
                })
            });

            if (response?.isSuccess) {
                showAlert('registerMessage', 'Registration successful! Please verify your phone number.', 'success');
                setTimeout(() => {
                    window.location.href = `verify-phone.html?userId=${response.data}`;
                }, 1500);
            } else {
                showAlert('registerMessage', response?.message || 'Registration failed. Please try again.', 'danger');
            }
        } catch (error) {
            console.error('Registration error:', error);
            showAlert('registerMessage', 'An error occurred. Please try again.', 'danger');
        } finally {
            setButtonLoading('registerBtn', false);
        }
    });

    // Password strength indicator
    document.getElementById('password')?.addEventListener('input', function() {
        updatePasswordRequirements(this.value);
    });
}

/**
 * Show specific step
 */
function showStep(stepNumber) {
    document.querySelectorAll('.form-step').forEach(step => {
        step.style.display = 'none';
    });
    const targetStep = document.getElementById(`step${stepNumber}`);
    if (targetStep) {
        targetStep.style.display = 'block';
        currentStep = stepNumber;
        window.scrollTo({ top: 0, behavior: 'smooth' });
    }
}

/**
 * Validate current step
 */
function validateCurrentStep() {
    clearFormErrors('registerForm');
    let isValid = true;

    if (currentStep === 1) {
        const fullname = document.getElementById('fullname').value.trim();
        const email = document.getElementById('email').value.trim();

        if (!fullname) {
            setFieldError('fullname', 'Full name is required');
            isValid = false;
        }

        if (!email || !isValidEmail(email)) {
            setFieldError('email', 'Please enter a valid email address');
            isValid = false;
        }
    } else if (currentStep === 2) {
        const phone = document.getElementById('phone').value.trim();
        const password = document.getElementById('password').value;
        const confirmPassword = document.getElementById('confirmPassword').value;

        if (!isValidPhone(phone)) {
            setFieldError('phone', 'Please enter a valid phone number');
            isValid = false;
        }

        const passwordValidation = validatePassword(password);
        if (!passwordValidation.length || !passwordValidation.uppercase || 
            !passwordValidation.digit || !passwordValidation.special) {
            setFieldError('password', 'Password does not meet requirements');
            isValid = false;
        }

        if (password !== confirmPassword) {
            setFieldError('confirmPassword', 'Passwords do not match');
            isValid = false;
        }
    } else if (currentStep === 3) {
        const terms = document.querySelector('input[name="terms"]').checked;
        if (!terms) {
            setFieldError('terms', 'You must accept the terms and conditions');
            isValid = false;
        }
    }

    return isValid;
}

/**
 * Update password requirement indicators
 */
function updatePasswordRequirements(password) {
    const validation = validatePassword(password);

    // Update requirement indicators
    updateRequirement('length', validation.length);
    updateRequirement('upper', validation.uppercase);
    updateRequirement('digit', validation.digit);
    updateRequirement('special', validation.special);
}

/**
 * Update individual requirement indicator
 */
function updateRequirement(type, isValid) {
    const element = document.getElementById(`req-${type}`);
    if (!element) return;

    if (isValid) {
        element.classList.add('valid');
        element.textContent = '✓';
    } else {
        element.classList.remove('valid');
        element.textContent = '✗';
    }
}

// ============================================
// PHONE VERIFICATION PAGE
// ============================================

const verifyPhoneForm = document.getElementById('verifyPhoneForm');
if (verifyPhoneForm) {
    // Get userId from URL
    const urlParams = new URLSearchParams(window.location.search);
    const userId = urlParams.get('userId');

    verifyPhoneForm.addEventListener('submit', async function(e) {
        e.preventDefault();

        const verificationCode = document.getElementById('verificationCode').value.trim();

        if (!verificationCode) {
            showAlert('verifyMessage', 'Please enter the verification code', 'danger');
            return;
        }

        setButtonLoading('verifyBtn', true);

        try {
            const response = await apiCall('/api/auth/verify-phone', {
                method: 'POST',
                body: JSON.stringify({
                    userId: userId,
                    verificationCode: verificationCode
                })
            });

            if (response?.isSuccess) {
                showAlert('verifyMessage', 'Phone verified successfully! Redirecting to login...', 'success');
                setTimeout(() => {
                    window.location.href = 'login.html';
                }, 1500);
            } else {
                showAlert('verifyMessage', response?.message || 'Verification failed. Please try again.', 'danger');
            }
        } catch (error) {
            console.error('Verification error:', error);
            showAlert('verifyMessage', 'An error occurred. Please try again.', 'danger');
        } finally {
            setButtonLoading('verifyBtn', false);
        }
    });

    // Resend code handler
    document.getElementById('resendBtn')?.addEventListener('click', async function() {
        try {
            const response = await apiCall(`/api/auth/resend-verification-code?userId=${userId}`, {
                method: 'POST'
            });

            if (response?.isSuccess) {
                showAlert('verifyMessage', 'Verification code sent! Check your SMS.', 'success');
                startResendTimer();
            } else {
                showAlert('verifyMessage', 'Failed to resend code. Please try again.', 'danger');
            }
        } catch (error) {
            console.error('Resend error:', error);
            showAlert('verifyMessage', 'An error occurred. Please try again.', 'danger');
        }
    });
}

/**
 * Start countdown timer for resend button
 */
function startResendTimer() {
    const resendBtn = document.getElementById('resendBtn');
    if (!resendBtn) return;

    let timeLeft = 60;
    resendBtn.disabled = true;
    const originalText = resendBtn.textContent;

    const timer = setInterval(() => {
        timeLeft--;
        resendBtn.textContent = `Resend in ${timeLeft}s`;

        if (timeLeft <= 0) {
            clearInterval(timer);
            resendBtn.disabled = false;
            resendBtn.textContent = originalText;
        }
    }, 1000);
}

// ============================================
// PASSWORD RESET PAGE
// ============================================

const forgotPasswordForm = document.getElementById('forgotPasswordForm');
if (forgotPasswordForm) {
    forgotPasswordForm.addEventListener('submit', async function(e) {
        e.preventDefault();
        clearFormErrors('forgotPasswordForm');

        const email = document.getElementById('email').value.trim();

        if (!email || !isValidEmail(email)) {
            setFieldError('email', 'Please enter a valid email address');
            return;
        }

        setButtonLoading('forgotBtn', true);

        try {
            const response = await apiCall('/api/auth/forgot-password', {
                method: 'POST',
                body: JSON.stringify({ email: email })
            });

            if (response?.isSuccess) {
                showAlert('forgotMessage', 'Password reset link sent to your email!', 'success');
                this.reset();
            } else {
                showAlert('forgotMessage', response?.message || 'Failed to send reset link.', 'danger');
            }
        } catch (error) {
            console.error('Forgot password error:', error);
            showAlert('forgotMessage', 'An error occurred. Please try again.', 'danger');
        } finally {
            setButtonLoading('forgotBtn', false);
        }
    });
}

// ============================================
// EXPORT FUNCTIONS
// ============================================

window.validateCurrentStep = validateCurrentStep;
window.showStep = showStep;
window.handleSocialLogin = handleSocialLogin;
window.startResendTimer = startResendTimer;
