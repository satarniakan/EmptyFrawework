// samples/Sample.Web/wwwroot/rtleditor.js
// interop ویرایشگر صورت‌جلسه بر پایهٔ Quill (open source) — همان API قبلی: init/getHtml/setHtml
window.rtlEditor = {
    _quills: {},

    init: function (id, initialHtml, minHeight) {
        var el = document.getElementById(id);
        if (!el) return;
        if (this._quills[id]) {
            if (initialHtml) this.setHtml(id, initialHtml);
            return;
        }

        var quill = new Quill(el, {
            theme: 'snow',
            direction: 'rtl',
            placeholder: el.getAttribute('data-placeholder') || '',
            modules: {
                toolbar: [
                    [{ header: [2, 3, false] }],
                    ['bold', 'italic', 'underline', 'strike'],
                    [{ list: 'ordered' }, { list: 'bullet' }],
                    [{ direction: 'rtl' }],
                    ['link', 'blockquote'],
                    ['clean']
                ]
            }
        });

        if (initialHtml) this.setHtml(id, initialHtml);
        if (minHeight) {
            quill.root.style.minHeight = minHeight + 'px';
        }

        this._quills[id] = quill;
    },

    getHtml: function (id) {
        var quill = this._quills[id];
        if (!quill) return '';
        // خالی واقعی را به رشتهٔ خالی برمی‌گردانیم نه <p><br></p> و ...
        if (quill.getText().trim() === '' && quill.root.querySelector('img') === null) return '';
        return quill.root.innerHTML;
    },

    setHtml: function (id, html) {
        var quill = this._quills[id];
        if (!quill) return;
        if (!html) {
            quill.setText('');
            return;
        }
        try {
            quill.clipboard.dangerouslyPasteHTML(html);
        } catch (e) {
            // HTML غیرمتعارف: مستقیم در ریشه می‌نشانیم
            quill.root.innerHTML = html;
        }
    }
};
