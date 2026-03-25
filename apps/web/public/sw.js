// Service Worker for Web Push Notifications — HydroEspinaca
// This file lives in /public so Next.js serves it at the root scope.

self.addEventListener('install', (event) => {
  self.skipWaiting();
});

self.addEventListener('activate', (event) => {
  event.waitUntil(self.clients.claim());
});

self.addEventListener('push', (event) => {
  if (!event.data) return;

  let payload;
  try {
    payload = event.data.json();
  } catch {
    payload = { title: 'HydroEspinaca', body: event.data.text() };
  }

  const title = payload.title || 'HydroEspinaca';
  const options = {
    body: payload.body || '',
    icon: '/icon-192x192.png',
    badge: '/badge-72x72.png',
    tag: payload.tag || 'hydroespinaca-notification',
    data: payload.data || {},
    vibrate: [100, 50, 100],
    actions: payload.actions || [],
  };

  event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
  event.notification.close();

  const data = event.notification.data || {};
  const url = data.url || data.deeplink || '/dashboard';

  event.waitUntil(
    self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
      // Focus existing window if found
      for (const client of clientList) {
        if (client.url.includes(self.location.origin) && 'focus' in client) {
          client.focus();
          client.navigate(url);
          return;
        }
      }
      // Otherwise open new window
      if (self.clients.openWindow) {
        return self.clients.openWindow(url);
      }
    })
  );
});
