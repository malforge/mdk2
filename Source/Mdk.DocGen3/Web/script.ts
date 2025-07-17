const COOKIE_NAME = 'showNonPublic';
const COOKIE_EXPIRES_DAYS = 30;

/** Read a cookie by name */
function getCookie(name: string): string {
    const m = document.cookie.match(
        new RegExp('(^|;)\\s*' + name + '\\s*=\\s*([^;]+)')
    );
    return m ? decodeURIComponent(m[2]) : '';
}

/** Write a cookie (expires in days) */
function setCookie(name: string, value: string, days = COOKIE_EXPIRES_DAYS): void {
    const expires = new Date(Date.now() + days * 864e5).toUTCString();
    document.cookie = `${name}=${encodeURIComponent(value)}; expires=${expires}; path=/`;
}

/** Toggle handler */
function onToggleClick(btn: HTMLButtonElement): void {
    const isNowOn = !btn.classList.contains('active');
    document.body.classList.toggle('show-non-public', isNowOn);
    btn.classList.toggle('active', isNowOn);
    btn.textContent = isNowOn ? 'Hide non-public' : 'Show non-public';
    setCookie(COOKIE_NAME, String(isNowOn));
}

/** Initialize the toggle button */
function initToggle(): void {
    const btn = document.getElementById('toggleNonPublic') as HTMLButtonElement | null;
    if (!btn) return;

    // Set initial state from cookie
    const shown = getCookie(COOKIE_NAME) === 'true';
    document.body.classList.toggle('show-non-public', shown);
    btn.classList.toggle('active', shown);
    btn.textContent = shown ? 'Hide non-public' : 'Show non-public';

    // Wire up click
    btn.addEventListener('click', () => onToggleClick(btn));
}

// Kick it off once DOM is ready
document.addEventListener('DOMContentLoaded', initToggle);
