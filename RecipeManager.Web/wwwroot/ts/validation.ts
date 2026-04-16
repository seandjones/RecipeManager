// Shared Validation Utilities - RecipeManager Authentication
// Email validation, code formatting, and input helpers

/**
 * Email validation using RFC 5322 compliant regex
 * @param email - The email address to validate
 * @returns True if email is valid, false otherwise
 */
export function validateEmail(email: string): boolean {
    if (!email || email.trim().length === 0) {
        return false;
    }
    
    // RFC 5322 compliant email regex (simplified but robust)
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email.trim());
}

/**
 * Normalize email address (trim and lowercase)
 * @param email - The email address to normalize
 * @returns Normalized email address
 */
export function normalizeEmail(email: string): string {
    return email.trim().toLowerCase();
}

/**
 * Validate 6-digit numeric code
 * @param code - The code to validate
 * @returns True if code is exactly 6 digits, false otherwise
 */
export function validateCode(code: string): boolean {
    if (!code) {
        return false;
    }
    
    const codeRegex = /^\d{6}$/;
    return codeRegex.test(code);
}

/**
 * Format code for display (adds spaces every 3 digits: 123456 -> 123 456)
 * @param code - The code to format
 * @returns Formatted code string
 */
export function formatCode(code: string): string {
    if (!code) {
        return '';
    }
    
    // Remove non-digits
    const digitsOnly = code.replace(/\D/g, '');
    
    // Add space every 3 digits
    return digitsOnly.replace(/(\d{3})(\d{3})/, '$1 $2');
}

/**
 * Extract digits from clipboard text (handles pasted codes with spaces, dashes, etc.)
 * @param text - The clipboard text to parse
 * @returns Array of individual digits (max 6)
 */
export function extractDigits(text: string): string[] {
    if (!text) {
        return [];
    }
    
    // Remove all non-digit characters
    const digitsOnly = text.replace(/\D/g, '');
    
    // Take first 6 digits and split into array
    return digitsOnly.slice(0, 6).split('');
}

/**
 * Validate password strength (for future use)
 * @param password - The password to validate
 * @returns Object with isValid flag and messages array
 */
export function validatePassword(password: string): { isValid: boolean; messages: string[] } {
    const messages: string[] = [];
    
    if (!password || password.length < 8) {
        messages.push('Password must be at least 8 characters long');
    }
    
    if (!/[A-Z]/.test(password)) {
        messages.push('Password must contain at least one uppercase letter');
    }
    
    if (!/[a-z]/.test(password)) {
        messages.push('Password must contain at least one lowercase letter');
    }
    
    if (!/\d/.test(password)) {
        messages.push('Password must contain at least one number');
    }
    
    if (!/[!@#$%^&*(),.?":{}|<>]/.test(password)) {
        messages.push('Password must contain at least one special character');
    }
    
    return {
        isValid: messages.length === 0,
        messages
    };
}

/**
 * Format retry time for display (e.g., "2 minutes 30 seconds" or "45 seconds")
 * @param seconds - Total seconds remaining
 * @returns Formatted time string
 */
export function formatRetryTime(seconds: number): string {
    if (seconds <= 0) {
        return '0 seconds';
    }
    
    const minutes = Math.floor(seconds / 60);
    const remainingSeconds = seconds % 60;
    
    if (minutes > 0) {
        if (remainingSeconds > 0) {
            return `${minutes} ${minutes === 1 ? 'minute' : 'minutes'} ${remainingSeconds} ${remainingSeconds === 1 ? 'second' : 'seconds'}`;
        }
        return `${minutes} ${minutes === 1 ? 'minute' : 'minutes'}`;
    }
    
    return `${remainingSeconds} ${remainingSeconds === 1 ? 'second' : 'seconds'}`;
}

/**
 * Add validation classes to input element
 * @param element - The input element
 * @param isValid - Whether the input is valid
 */
export function updateInputValidation(element: HTMLInputElement, isValid: boolean): void {
    if (!element) {
        return;
    }
    
    if (element.value.length > 0) {
        if (isValid) {
            element.classList.remove('is-invalid');
            element.classList.add('is-valid');
        } else {
            element.classList.remove('is-valid');
            element.classList.add('is-invalid');
        }
    } else {
        element.classList.remove('is-valid', 'is-invalid');
    }
}

/**
 * Clear validation classes from input element
 * @param element - The input element
 */
export function clearInputValidation(element: HTMLInputElement): void {
    if (!element) {
        return;
    }
    
    element.classList.remove('is-valid', 'is-invalid');
}

/**
 * Auto-trim input value on blur
 * @param element - The input element
 */
export function setupAutoTrim(element: HTMLInputElement): void {
    if (!element) {
        return;
    }
    
    element.addEventListener('blur', () => {
        element.value = element.value.trim();
    });
}

/**
 * Debounce function for input validation
 * @param func - The function to debounce
 * @param wait - Wait time in milliseconds
 * @returns Debounced function
 */
export function debounce<T extends (...args: any[]) => any>(
    func: T,
    wait: number
): (...args: Parameters<T>) => void {
    let timeout: NodeJS.Timeout | null = null;
    
    return (...args: Parameters<T>) => {
        if (timeout) {
            clearTimeout(timeout);
        }
        
        timeout = setTimeout(() => {
            func(...args);
        }, wait);
    };
}

/**
 * Check if input is numeric
 * @param value - The value to check
 * @returns True if value contains only digits
 */
export function isNumeric(value: string): boolean {
    return /^\d+$/.test(value);
}

/**
 * Sanitize input (remove potentially harmful characters)
 * @param input - The input to sanitize
 * @returns Sanitized string
 */
export function sanitizeInput(input: string): string {
    if (!input) {
        return '';
    }
    
    // Remove control characters and trim
    return input.replace(/[\x00-\x1F\x7F]/g, '').trim();
}
