// Service Worker: إعادة محاولة تلقائية للطلبات التي يقطعها الخادم.
// خادم الاستضافة يقطع أحيانًا طلبات الزائر على HTTP/2 لثوانٍ بعد دفعة طلبات متزامنة (ERR_HTTP2_PROTOCOL_ERROR)،
// فيرى الزائر صفحة «تعذر الوصول». هنا نعيد طلبات GET من موقعنا حتى 3 مرات قبل الاستسلام.
// لا يُخزَّن أي شيء هنا: كل طلب يذهب إلى الخادم كالمعتاد.
const RETRY_DELAYS_MS = [300, 800, 1500];

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));

self.addEventListener('fetch', (event) => {
    const request = event.request;
    if (request.method !== 'GET' || !request.url.startsWith(self.location.origin)) return;
    event.respondWith(fetchWithRetry(request));
});

async function fetchWithRetry(request) {
    for (let attempt = 0; ; attempt++) {
        try {
            return await fetch(request.clone());
        } catch (err) {
            // الصفحة نفسها ألغت الطلب (انتقل الزائر إلى صفحة أخرى): لا تُعِد المحاولة
            if (err && err.name === 'AbortError') throw err;
            if (attempt >= RETRY_DELAYS_MS.length) throw err;
            await new Promise((resolve) => setTimeout(resolve, RETRY_DELAYS_MS[attempt]));
        }
    }
}
