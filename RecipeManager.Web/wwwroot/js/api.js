// Shared API Utilities - RecipeManager Authentication
// Error handling, HTTP helpers, and response parsing
// Compiled from api.ts
/**
 * Parse error response from fetch
 * @param response - The fetch response
 * @returns Parsed error object
 */
export async function parseErrorResponse(response) {
    const statusCode = response.status;
    let message = response.statusText || 'An error occurred';
    let errors;
    try {
        const contentType = response.headers.get('content-type');
        if (contentType?.includes('application/json')) {
            const json = await response.json();
            message = json.message || json.error || message;
            errors = json.errors;
        }
        else {
            const text = await response.text();
            if (text) {
                message = text;
            }
        }
    }
    catch (err) {
        // If parsing fails, use default message
        console.error('Failed to parse error response:', err);
    }
    return {
        message,
        statusCode,
        errors
    };
}
/**
 * Parse rate limit error (429 status)
 * @param response - The fetch response
 * @returns Parsed rate limit error
 */
export async function parseRateLimitError(response) {
    const error = await parseErrorResponse(response);
    // Extract Retry-After header (in seconds)
    const retryAfterHeader = response.headers.get('Retry-After');
    const retryAfterSeconds = retryAfterHeader ? parseInt(retryAfterHeader, 10) : 60;
    return {
        ...error,
        retryAfterSeconds
    };
}
/**
 * Check if response is successful (200-299)
 * @param response - The fetch response
 * @returns True if successful, false otherwise
 */
export function isSuccessResponse(response) {
    return response.ok && response.status >= 200 && response.status < 300;
}
/**
 * Check if response is rate limited (429)
 * @param response - The fetch response
 * @returns True if rate limited, false otherwise
 */
export function isRateLimitError(response) {
    return response.status === 429;
}
/**
 * Check if response is unauthorized (401)
 * @param response - The fetch response
 * @returns True if unauthorized, false otherwise
 */
export function isUnauthorizedError(response) {
    return response.status === 401;
}
/**
 * Check if response is forbidden (403)
 * @param response - The fetch response
 * @returns True if forbidden, false otherwise
 */
export function isForbiddenError(response) {
    return response.status === 403;
}
/**
 * Check if response is not found (404)
 * @param response - The fetch response
 * @returns True if not found, false otherwise
 */
export function isNotFoundError(response) {
    return response.status === 404;
}
/**
 * Check if response is server error (500-599)
 * @param response - The fetch response
 * @returns True if server error, false otherwise
 */
export function isServerError(response) {
    return response.status >= 500 && response.status < 600;
}
/**
 * Handle fetch errors (network errors, timeouts, etc.)
 * @param error - The error object
 * @returns User-friendly error message
 */
export function handleFetchError(error) {
    if (error instanceof TypeError) {
        if (error.message.includes('Failed to fetch') || error.message.includes('NetworkError')) {
            return 'Network error. Please check your internet connection and try again.';
        }
        return 'A network error occurred. Please try again.';
    }
    if (error.name === 'AbortError') {
        return 'Request was cancelled.';
    }
    if (error.message) {
        return error.message;
    }
    return 'An unexpected error occurred. Please try again.';
}
/**
 * Create abort controller with timeout
 * @param timeoutMs - Timeout in milliseconds
 * @returns Object with signal and cleanup function
 */
export function createTimeoutController(timeoutMs) {
    const controller = new AbortController();
    const timeoutId = setTimeout(() => controller.abort(), timeoutMs);
    return {
        signal: controller.signal,
        cleanup: () => clearTimeout(timeoutId)
    };
}
/**
 * Retry failed request with exponential backoff
 * @param fn - The function to retry
 * @param maxRetries - Maximum number of retries
 * @param delayMs - Initial delay in milliseconds
 * @returns Promise with result
 */
export async function retryWithBackoff(fn, maxRetries = 3, delayMs = 1000) {
    let lastError;
    for (let attempt = 0; attempt <= maxRetries; attempt++) {
        try {
            return await fn();
        }
        catch (error) {
            lastError = error;
            if (attempt < maxRetries) {
                // Exponential backoff: 1s, 2s, 4s, etc.
                const delay = delayMs * Math.pow(2, attempt);
                await new Promise(resolve => setTimeout(resolve, delay));
            }
        }
    }
    throw lastError;
}
/**
 * Build query string from object
 * @param params - Query parameters object
 * @returns Query string (without leading ?)
 */
export function buildQueryString(params) {
    const searchParams = new URLSearchParams();
    Object.entries(params).forEach(([key, value]) => {
        if (value !== null && value !== undefined) {
            searchParams.append(key, String(value));
        }
    });
    return searchParams.toString();
}
/**
 * Safe JSON parse with fallback
 * @param json - JSON string to parse
 * @param fallback - Fallback value if parse fails
 * @returns Parsed object or fallback
 */
export function safeJsonParse(json, fallback) {
    try {
        return JSON.parse(json);
    }
    catch (err) {
        console.error('Failed to parse JSON:', err);
        return fallback;
    }
}
/**
 * Format HTTP error for display
 * @param error - The API error
 * @returns User-friendly error message
 */
export function formatApiError(error) {
    // If we have validation errors, format them
    if (error.errors && Object.keys(error.errors).length > 0) {
        const messages = Object.entries(error.errors)
            .flatMap(([field, fieldErrors]) => fieldErrors.map(err => `${field}: ${err}`));
        return messages.join('\n');
    }
    // Use status code specific messages
    switch (error.statusCode) {
        case 400:
            return error.message || 'Invalid request. Please check your input.';
        case 401:
            return 'You are not authorized. Please log in.';
        case 403:
            return 'You do not have permission to perform this action.';
        case 404:
            return 'The requested resource was not found.';
        case 429:
            return error.message || 'Too many requests. Please try again later.';
        case 500:
            return 'A server error occurred. Please try again later.';
        case 503:
            return 'Service temporarily unavailable. Please try again later.';
        default:
            return error.message || 'An unexpected error occurred.';
    }
}
/**
 * Log API call for debugging (development only)
 * @param method - HTTP method
 * @param url - Request URL
 * @param body - Request body
 */
export function logApiCall(method, url, body) {
    if (process.env.NODE_ENV === 'development') {
        console.log(`[API] ${method} ${url}`, body);
    }
}
/**
 * Log API response for debugging (development only)
 * @param method - HTTP method
 * @param url - Request URL
 * @param response - Response object
 */
export function logApiResponse(method, url, response) {
    if (process.env.NODE_ENV === 'development') {
        console.log(`[API] ${method} ${url} -> ${response.status} ${response.statusText}`);
    }
}
