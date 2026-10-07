// تم روشن/تاریک — باید در <head> و قبل از رندر بدنه بارگذاری شود تا صفحه بدون پرش رنگ بالا بیاید
(function () {
    const KEY = 'platform-theme';

    // آیکون کلید تم همیشه تم «مقابل» را نشان می‌دهد (در تاریک: خورشید، در روشن: ماه)
    function syncToggleIcons(theme) {
        document.querySelectorAll('.theme-toggle i').forEach((icon) => {
            icon.className = 'bi ' + (theme === 'dark' ? 'bi-sun-fill' : 'bi-moon-stars-fill');
        });
    }

    function apply(theme) {
        document.documentElement.setAttribute('data-theme', theme);
        document.documentElement.setAttribute('data-bs-theme', theme);
        syncToggleIcons(theme);
    }

    // اگر کاربر انتخابی ذخیره نکرده باشد، پیش‌فرض تم سیستم‌عامل است
// و تا وقتی دستی انتخاب نکرده، تغییر تم سیستم به‌صورت زنده هم اعمال می‌شود
const saved = localStorage.getItem(KEY);
const systemDark = window.matchMedia('(prefers-color-scheme: dark)');
apply(saved === 'dark' || saved === 'light' ? saved : (systemDark.matches ? 'dark' : 'light'));

// آیکون‌ها بعد از رندر بدنه هم همگام می‌شوند (apply اول در head است و بدنه هنوز نیست)
document.addEventListener('DOMContentLoaded', () =>
    syncToggleIcons(document.documentElement.getAttribute('data-theme') || 'light'));

if (saved !== 'dark' && saved !== 'light') {
    systemDark.addEventListener('change', (e) => {
        if (localStorage.getItem(KEY) !== 'dark' && localStorage.getItem(KEY) !== 'light') {
            apply(e.matches ? 'dark' : 'light');
        }
    });
}

    window.themeInterop = {
        get: () => document.documentElement.getAttribute('data-theme') || 'light',
        set: (theme) => {
            localStorage.setItem(KEY, theme);
            apply(theme);
            return theme;
        },
        toggle: () => {
            const next = window.themeInterop.get() === 'dark' ? 'light' : 'dark';
            return window.themeInterop.set(next);
        }
    };
})();

// فراخوانی ساده‌ی endpoint های JSON برای کامپوننت‌های تعاملی (مثل زنگ اعلان‌ها)
window.platformApi = {
    get: async (url) => (await fetch(url)).json(),
    post: async (url) => { await fetch(url, { method: 'POST' }); }
};
