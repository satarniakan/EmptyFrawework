/* Platform.App/wwwroot/push-subscribe.js — ثبت service worker و اشتراک پوش.
   از PushSubscribeButton با import فراخوانی می‌شود. */
function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const raw = window.atob(base64);
    const output = new Uint8Array(raw.length);
    for (let i = 0; i < raw.length; i++) output[i] = raw.charCodeAt(i);
    return output;
}

export async function subscribePush(swUrl, publicKey, subscribeUrl) {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        return { ok: false, reason: 'unsupported' };
    }
    if (Notification.permission === 'denied') {
        return { ok: false, reason: 'denied' };
    }

    const permission = await Notification.requestPermission();
    if (permission !== 'granted') {
        return { ok: false, reason: 'denied' };
    }

    const registration = await navigator.serviceWorker.register(swUrl);
    let subscription = await registration.pushManager.getSubscription();
    if (!subscription) {
        subscription = await registration.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(publicKey)
        });
    }

    const xsrf = getXsrfToken();
    const response = await fetch(subscribeUrl, {
        method: 'POST',
        headers: {
            'Content-Type': 'application/json',
            // endpoint کوکیِ POST است و ضدجعل می‌خواهد؛ بدون این هدر 400 می‌گیرد
            ...(xsrf ? { 'X-XSRF-TOKEN': xsrf } : {})
        },
        body: JSON.stringify(subscription.toJSON())
    });

    return { ok: response.ok };
}

function getXsrfToken() {
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
