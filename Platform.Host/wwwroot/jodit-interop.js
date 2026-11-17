// ویرایشگر غنی Jodit — بارگذاری تنبل assetها فقط وقتی کامپوننت واقعاً رندر شود.
// الگوی instances + destroy مطابق persian-datepicker-interop.js.
let assetsPromise = null;

function ensureAssets() {
    if (window.Jodit) return Promise.resolve();
    if (!assetsPromise) {
        assetsPromise = new Promise((resolve, reject) => {
            const link = document.createElement('link');
            link.rel = 'stylesheet';
            link.href = '/lib/jodit/jodit.min.css';
            document.head.appendChild(link);

            const script = document.createElement('script');
            script.src = '/lib/jodit/jodit.min.js';
            script.onload = () => resolve();
            script.onerror = () => reject(new Error('بارگذاری Jodit ناموفق بود'));
            document.head.appendChild(script);
        });
    }
    return assetsPromise;
}

const instances = {};

export async function init(elementId, dotNetRef, options) {
    await ensureAssets();
    const el = document.getElementById(elementId);
    if (!el || instances[elementId]) return;

    const editor = new window.Jodit(el, {
        language: 'fa',
        direction: 'rtl',
        height: (options && options.height) || 300,
        placeholder: (options && options.placeholder) || '',
        readonly: !!(options && options.disabled),
        toolbarAdaptive: false,
        buttons: 'bold,italic,underline,|,ul,ol,|,link,image,|,undo,redo,|,eraser'
    });

    editor.events.on('change', () => {
        dotNetRef.invokeMethodAsync('OnEditorChanged', editor.value);
    });

    instances[elementId] = editor;
}

export function setValue(elementId, value) {
    const editor = instances[elementId];
    if (editor && editor.value !== value) editor.value = value || '';
}

export function setDisabled(elementId, disabled) {
    const editor = instances[elementId];
    if (editor) editor.setReadOnly(!!disabled);
}

export function destroy(elementId) {
    const editor = instances[elementId];
    if (editor) {
        editor.destruct();
        delete instances[elementId];
    }
}