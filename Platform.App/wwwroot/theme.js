// تم روشن/تاریک — باید در <head> و قبل از رندر بدنه بارگذاری شود تا صفحه بدون پرش رنگ بالا بیاید
(function () {
    const KEY = 'platform-theme';
    const docEl = document.documentElement;

    // تم ذخیره‌شده یا تم سیستم‌عامل
    const saved = localStorage.getItem(KEY);
    const systemDark = window.matchMedia('(prefers-color-scheme: dark)');
    let currentTheme = saved === 'dark' || saved === 'light' ? saved : (systemDark.matches ? 'dark' : 'light');

    // آیکون کلید تم همیشه تم «مقابل» را نشان می‌دهد (در تاریک: خورشید، در روشن: ماه)
    function syncToggleIcons(theme) {
        document.querySelectorAll('.theme-toggle i').forEach((icon) => {
            icon.className = 'bi ' + (theme === 'dark' ? 'bi-sun-fill' : 'bi-moon-stars-fill');
        });
    }

    function apply(theme) {
        if (!theme) theme = 'light';
        currentTheme = theme;
        // setAttribute با مقدار تکراری هم رکورد mutation می‌سازد؛ بدون این گارد،
        // observer پایین با هر بار اعمال تم دوباره فعال می‌شود و حلقهٔ بی‌پایان صفحه را فریز می‌کند
        if (docEl.getAttribute('data-theme') !== theme) docEl.setAttribute('data-theme', theme);
        if (docEl.getAttribute('data-bs-theme') !== theme) docEl.setAttribute('data-bs-theme', theme);
        syncToggleIcons(theme);
    }

    apply(currentTheme);

    // آیکون‌ها بعد از رندر بدنه هم همگام می‌شوند
    document.addEventListener('DOMContentLoaded', () => {
        apply(currentTheme);
        // ناوبری enhanced (فرم‌های SSR) بدنه را عوض می‌کند؛ آیکون‌ها باید دوباره همگام شوند
        if (window.Blazor && window.Blazor.addEventListener) {
            window.Blazor.addEventListener('enhancedload', () => syncToggleIcons(currentTheme));
        }
    });

    // اگر کاربر تم دستی انتخاب نکرده باشد، تغییر سیستم‌عامل زنده اعمال شود
    systemDark.addEventListener('change', (e) => {
        const stored = localStorage.getItem(KEY);
        if (stored !== 'dark' && stored !== 'light') {
            apply(e.matches ? 'dark' : 'light');
        }
    });

    // اگر کد خارجی data-theme را حذف یا عوض کرد، به تم جاری کاربر برگردانده می‌شود.
    // چون apply مقدار یکسان را دوباره ست نمی‌کند، این observer هرگز حلقه نمی‌شود.
    const observer = new MutationObserver(() => {
        if (docEl.getAttribute('data-theme') !== currentTheme ||
            docEl.getAttribute('data-bs-theme') !== currentTheme) {
            apply(currentTheme);
        }
    });
    observer.observe(docEl, { attributes: true, attributeFilter: ['data-theme', 'data-bs-theme'] });

    // کلیک کلید تم با JS مدیریت می‌شود تا در صفحات استاتیک (بدون مدار Blazor) هم کار کند
    document.addEventListener('click', (e) => {
        if (e.target.closest('.theme-toggle')) {
            e.preventDefault();
            window.themeInterop.toggle();
        }
    });

    window.themeInterop = {
        get: () => currentTheme,
        set: (theme) => {
            localStorage.setItem(KEY, theme);
            apply(theme);
            return theme;
        },
        reapply: () => {
            apply(localStorage.getItem(KEY) || currentTheme);
            return currentTheme;
        },
        toggle: () => window.themeInterop.set(currentTheme === 'dark' ? 'light' : 'dark')
    };
})();

// فراخوانی ساده‌ی endpoint های JSON برای کامپوننت‌های تعاملی (مثل زنگ اعلان‌ها)
// توکن ضدجعل (CSRF) از کوکی __Host-XSRF-TOKEN یا __XSRF-TOKEN خوانده و در هدر
// X-XSRF-TOKEN فرستاده می‌شود؛ endpointهای کوکیِ POST بدون آن 400 می‌گیرند.
// اگر کوکی هنوز ست نشده باشد، اول از /api/xsrf-token گرفته می‌شود (lazy).
window.platformApi = {
    get: async (url) => (await fetch(url)).json(),
    post: async (url) => {
        let token = window.platformApi.getXsrfToken();
        if (!token) {
            try { await fetch('/api/xsrf-token'); } catch { /* شبکه قطع است؛ همان بدون توکن تلاش می‌شود */ }
            token = window.platformApi.getXsrfToken();
        }
        await fetch(url, {
            method: 'POST',
            headers: token ? { 'X-XSRF-TOKEN': token } : {}
        });
    },
    getXsrfToken: () => {
        const parts = document.cookie.split(';');
        for (const part of parts) {
            const trimmed = part.trim();
            for (const name of ['__Host-XSRF-TOKEN', '__XSRF-TOKEN']) {
                if (trimmed.startsWith(name + '=')) {
                    return decodeURIComponent(trimmed.substring(name.length + 1));
                }
            }
        }
        return null;
    }
};
