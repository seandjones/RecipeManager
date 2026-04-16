// Login page client-side validation and enhancements
// Uses shared validation utilities (DRY principle)
import { validateEmail, setupAutoTrim, updateInputValidation } from './validation.js';

export function initializeLoginPage() {
    const emailInput = document.getElementById('email');

    if (emailInput) {
        // Auto-trim on blur using shared utility
        setupAutoTrim(emailInput);

        // Real-time validation feedback using shared utility
        emailInput.addEventListener('input', function() {
            const isValid = validateEmail(this.value);
            updateInputValidation(this, isValid);
        });
    }
}

// Initialize when module loads
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeLoginPage);
} else {
    initializeLoginPage();
}
