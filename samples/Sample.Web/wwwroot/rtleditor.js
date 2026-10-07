// samples/Sample.Web/wwwroot/rtleditor.js
// interop ویرایشگر متن راست‌چین (contenteditable) برای صورت‌جلسه‌ها.
window.rtlEditor = {
    init: function (id, initialHtml, minHeight) {
        var el = document.getElementById(id);
        if (!el) return;
        el.innerHTML = initialHtml || '';
        if (minHeight) el.style.minHeight = minHeight + 'px';

        // وقتی محتوا واقعاً خالی است، placeholder سی‌اس‌اس نمایش داده شود
        function syncEmpty() {
            if (el.textContent.trim() === '' && el.querySelector('img') === null) {
                el.classList.add('is-empty');
            } else {
                el.classList.remove('is-empty');
            }
        }
        el.addEventListener('input', syncEmpty);
        el.addEventListener('blur', syncEmpty);
        syncEmpty();
    },

    exec: function (id, command, value) {
        var el = document.getElementById(id);
        if (!el) return;
        el.focus();
        document.execCommand(command, false, value || null);
    },

    getHtml: function (id) {
        var el = document.getElementById(id);
        if (!el) return '';
        // خالی واقعی را به رشتهٔ خالی برمی‌گردانیم نه <br> و ...
        if (el.textContent.trim() === '' && el.querySelector('img') === null) return '';
        return el.innerHTML;
    },

    setHtml: function (id, html) {
        var el = document.getElementById(id);
        if (!el) return;
        el.innerHTML = html || '';
        if (el.textContent.trim() === '') {
            el.classList.add('is-empty');
        } else {
            el.classList.remove('is-empty');
        }
    }
};
