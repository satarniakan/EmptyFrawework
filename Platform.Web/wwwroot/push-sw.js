/* Platform.Web/wwwroot/push-sw.js — service worker اعلان‌های وب‌پوش.
   ثبت از PushSubscribeButton انجام می‌شود. پیام سرور JSON است: {title, body, url}. */
self.addEventListener('push', (event) => {
    let data = {};
    try {
        data = event.data ? event.data.json() : {};
    } catch {
        data = { body: event.data ? event.data.text() : '' };
    }

    event.waitUntil(
        self.registration.showNotification(data.title || 'اعلان', {
            body: data.body || '',
            dir: 'rtl',
            lang: 'fa',
            data: { url: data.url || '/' }
        })
    );
});

self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const url = (event.notification.data && event.notification.data.url) || '/';
    event.waitUntil(clients.openWindow(url));
});
