// تم روشن/تاریک — باید در <head> و قبل از رندر بدنه بارگذاری شود تا صفحه بدون پرش رنگ بالا بیاید
(function () {
    const KEY = 'platform-theme';
    const docEl = document.documentElement;

    // آیکون کلید تم همیشه تم «مقابل» را نشان می‌دهد (در تاریک: خورشید، در روشن: ماه)
    function syncToggleIcons(theme) {
        document.querySelectorAll('.theme-toggle i').forEach((icon) => {
            icon.className = 'bi ' + (theme === 'dark' ? 'bi-sun-fill' : 'bi-moon-stars-fill');
        });
    }

    function apply(theme) {
        if (!theme) theme = 'light';
        docEl.setAttribute('data-theme', theme);
        docEl.setAttribute('data-bs-theme', theme);
        syncToggleIcons(theme);
    }

    // تم ذخیره‌شده یا تم سیستم‌عامل
    const saved = localStorage.getItem(KEY);
    const systemDark = window.matchMedia('(prefers-color-scheme: dark)');
    const initialTheme = saved === 'dark' || saved === 'light' ? saved : (systemDark.matches ? 'dark' : 'light');
    apply(initialTheme);

    // آیکون‌ها بعد از رندر بدنه هم همگام می‌شوند
    document.addEventListener('DOMContentLoaded', () => {
        syncToggleIcons(docEl.getAttribute('data-theme') || 'light');
        // navigationsِ Blazor و ریدایرکت‌های خطا می‌توانند data-theme را از دست دهند؛ بعد از لود دوباره اعمال می‌کنیم
        apply(docEl.getAttribute('data-theme') || initialTheme);
    });

    // اگر کاربر تم دستی انتخاب نکرده باشد، تغییر سیستم‌عامل زنده اعمال شود
    if (saved !== 'dark' && saved !== 'light') {
        systemDark.addEventListener('change', (e) => {
            if (localStorage.getItem(KEY) !== 'dark' && localStorage.getItem(KEY) !== 'light') {
                apply(e.matches ? 'dark' : 'light');
            }
        });
    }

    // ”zelf-healing”: اگر هر کد/js nào data-theme را بگیرد، بلافاصله به حالت قبل بازمی‌گردد
    const observer = new MutationObserver((mutations) => {
        for (const m of mutations) {
            if (m.attributeName === 'data-theme' || m.attributeName === 'data-bs-theme') {
                const current = docEl.getAttribute('data-theme');
                if (current !== initialTheme && current !== saved) {
                    // فقط وقتی keystroke‌های خارجی هستند، بازگردانی می‌کنیم
                    apply(current || initialTheme);
                }
            }
        }
    });
    observer.observe(docEl, { attributes: true, attributeFilter: ['data-theme', 'data-bs-theme'] });

    // Blazor navigate events (client-side navigation) را شنونده می‌گیرد تا تم حفظ شود
    if (window.Blazor && window.Blazor.navigation) {
        window.Blazor.navigation.addEventListener('navigate', () => {
            apply(docEl.getAttribute('data-theme') || initialTheme);
        });
    }

    window.themeInterop = {
        get: () => docEl.getAttribute('data-theme') || 'light',
        set: (theme) => {
            localStorage.setItem(KEY, theme);
            apply(theme);
            return theme;
        },
        reapply: () => {
            const t = localStorage.getItem(KEY) || initialTheme;
            apply(t);
            return t;
        },
        toggle: () => {
            const next = window.themeInterop.get() === 'dark' ? 'light' : 'dark';
            return window.themeInterop.set(next);
        }
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
