var COOKIE_NAME = 'showNonPublic';
var COOKIE_EXPIRES_DAYS = 30;
/** Read a cookie by name */
function getCookie(name) {
    var m = document.cookie.match(new RegExp('(^|;)\\s*' + name + '\\s*=\\s*([^;]+)'));
    return m ? decodeURIComponent(m[2]) : '';
}
/** Write a cookie (expires in days) */
function setCookie(name, value, days) {
    if (days === void 0) { days = COOKIE_EXPIRES_DAYS; }
    var expires = new Date(Date.now() + days * 864e5).toUTCString();
    document.cookie = "".concat(name, "=").concat(encodeURIComponent(value), "; expires=").concat(expires, "; path=/");
}
/** Toggle handler */
function onToggleClick(btn) {
    var isNowOn = !btn.classList.contains('active');
    document.body.classList.toggle('show-non-public', isNowOn);
    btn.classList.toggle('active', isNowOn);
    btn.textContent = isNowOn ? 'Hide non-public' : 'Show non-public';
    setCookie(COOKIE_NAME, String(isNowOn));
}
/** Initialize the toggle button */
function initToggle() {
    var btn = document.getElementById('toggleNonPublic');
    if (!btn)
        return;
    // Set initial state from cookie
    var shown = getCookie(COOKIE_NAME) === 'true';
    document.body.classList.toggle('show-non-public', shown);
    btn.classList.toggle('active', shown);
    btn.textContent = shown ? 'Hide non-public' : 'Show non-public';
    // Wire up click
    btn.addEventListener('click', function () { return onToggleClick(btn); });
}
// Kick it off once DOM is ready
document.addEventListener('DOMContentLoaded', initToggle);
//# sourceMappingURL=script.js.map