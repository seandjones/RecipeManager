// verify-code.js - Code verification page JavaScript module
// Compiled from TypeScript - Provides auto-tab, paste support, and auto-submit functionality
// Uses shared validation utilities (DRY principle)
import { extractDigits, isNumeric } from './validation.js';

class CodeInputHandler {
    constructor() {
        this.autoSubmitEnabled = true;
        this.state = {
            inputs: [],
            currentIndex: 0
        };
        this.initialize();
    }

    initialize() {
        // Get all code digit inputs
        const inputs = document.querySelectorAll('.code-digit');
        this.state.inputs = Array.from(inputs);

        if (this.state.inputs.length === 0) {
            console.warn('No code digit inputs found');
            return;
        }

        // Attach event listeners
        this.state.inputs.forEach((input, index) => {
            input.addEventListener('input', (e) => this.handleInput(e, index));
            input.addEventListener('keydown', (e) => this.handleKeyDown(e, index));
            input.addEventListener('paste', (e) => this.handlePaste(e, index));
            input.addEventListener('focus', () => this.handleFocus(index));
        });

        // Auto-focus first input
        this.focusInput(0);
    }

    handleInput(event, index) {
        const input = event.target;
        let value = input.value;

        // Only allow numeric input (using shared utility)
        value = value.replace(/[^0-9]/g, '');

        // Limit to single digit
        if (value.length > 1) {
            value = value.charAt(0);
        }

        input.value = value;

        // Update Blazor binding
        input.dispatchEvent(new Event('change', { bubbles: true }));

        // Auto-tab to next input if digit entered
        if (value.length === 1 && index < this.state.inputs.length - 1) {
            this.focusInput(index + 1);
        }

        // Check if all digits entered for auto-submit
        if (this.isComplete()) {
            this.triggerAutoSubmit();
        }
    }

    handleKeyDown(event, index) {
        const input = event.target;

        // Backspace: move to previous input if current is empty
        if (event.key === 'Backspace' && !input.value && index > 0) {
            event.preventDefault();
            this.focusInput(index - 1);
            this.state.inputs[index - 1].value = '';
            this.state.inputs[index - 1].dispatchEvent(new Event('change', { bubbles: true }));
        }

        // Arrow keys navigation
        if (event.key === 'ArrowLeft' && index > 0) {
            event.preventDefault();
            this.focusInput(index - 1);
        }

        if (event.key === 'ArrowRight' && index < this.state.inputs.length - 1) {
            event.preventDefault();
            this.focusInput(index + 1);
        }

        // Home: jump to first input
        if (event.key === 'Home') {
            event.preventDefault();
            this.focusInput(0);
        }

        // End: jump to last input
        if (event.key === 'End') {
            event.preventDefault();
            this.focusInput(this.state.inputs.length - 1);
        }

        // Enter: trigger submit if complete
        if (event.key === 'Enter' && this.isComplete()) {
            event.preventDefault();
            this.triggerSubmit();
        }
    }

    handlePaste(event, index) {
        event.preventDefault();

        const pasteData = event.clipboardData?.getData('text') || '';

        // Use shared utility to extract digits
        const digits = extractDigits(pasteData);

        if (digits.length === 0) {
            return;
        }

        // Fill inputs starting from current position
        let currentIndex = index;
        digits.forEach((digit) => {
            if (currentIndex < this.state.inputs.length) {
                this.state.inputs[currentIndex].value = digit;
                this.state.inputs[currentIndex].dispatchEvent(new Event('change', { bubbles: true }));
                currentIndex++;
            }
        });

        // Focus next empty input or last input
        if (currentIndex < this.state.inputs.length) {
            this.focusInput(currentIndex);
        } else {
            this.focusInput(this.state.inputs.length - 1);
        }

        // Check for auto-submit
        if (this.isComplete()) {
            this.triggerAutoSubmit();
        }
    }

    handleFocus(index) {
        this.state.currentIndex = index;
        
        // Select content on focus for easy replacement
        const input = this.state.inputs[index];
        if (input.value) {
            input.select();
        }
    }

    focusInput(index) {
        if (index >= 0 && index < this.state.inputs.length) {
            this.state.inputs[index].focus();
            this.state.currentIndex = index;
        }
    }

    isComplete() {
        return this.state.inputs.every(input => input.value.length === 1);
    }

    triggerAutoSubmit() {
        if (!this.autoSubmitEnabled) {
            return;
        }

        // Wait a brief moment for visual feedback
        setTimeout(() => {
            const submitButton = document.querySelector('.btn-verify');
            if (submitButton && !submitButton.disabled) {
                submitButton.click();
            }
        }, 300);
    }

    triggerSubmit() {
        const submitButton = document.querySelector('.btn-verify');
        if (submitButton && !submitButton.disabled) {
            submitButton.click();
        }
    }

    clearAll() {
        this.state.inputs.forEach(input => {
            input.value = '';
            input.dispatchEvent(new Event('change', { bubbles: true }));
        });
        this.focusInput(0);
    }

    focusFirst() {
        this.focusInput(0);
    }
}

// Initialize when module loads
let codeInputHandler = null;

function initializeCodeInput() {
    if (document.querySelector('.code-inputs')) {
        codeInputHandler = new CodeInputHandler();
    }
}

// Global functions for Blazor to call
window.focusFirstDigit = () => {
    codeInputHandler?.focusFirst();
};

window.clearCodeInputs = () => {
    codeInputHandler?.clearAll();
};

// Initialize when module loads or DOM is ready
if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', initializeCodeInput);
} else {
    initializeCodeInput();
}

export { CodeInputHandler, initializeCodeInput };
