// Service Worker: يحمي الزائر من قطع الخادم للطلبات.
// خادم الاستضافة يقطع طلبات الزائر على HTTP/2 لثوانٍ إذا وصلته دفعة من نحو 10 طلبات متزامنة أو أكثر
// (ERR_HTTP2_PROTOCOL_ERROR)، فيرى الزائر صفحة «تعذر الوصول». هنا:
//  1. لا يُرسَل إلى الخادم أكثر من MAX_CONCURRENT طلبًا في الوقت نفسه (الصور والملفات تنتظر دورها بدل أن تنطلق دفعة واحدة).
//  2. طلب الصفحة نفسها (التنقل) لا ينتظر في الطابور، ويُعاد عند الفشل مرات أكثر.
//  3. أي طلب يقطعه الخادم يُعاد تلقائيًا بعد مهلة قصيرة.
// لا يُخزَّن أي شيء هنا: كل طلب يذهب إلى الخادم كالمعتاد.
const MAX_CONCURRENT = 6;
const NAVIGATION_RETRY_MS = [300, 800, 1500, 2500, 4000];
const RESOURCE_RETRY_MS = [600, 1500];

self.addEventListener('install', () => self.skipWaiting());
self.addEventListener('activate', (event) => event.waitUntil(self.clients.claim()));

self.addEventListener('fetch', (event) => {
    const request = event.request;
    if (request.method !== 'GET' || !request.url.startsWith(self.location.origin)) return;
    event.respondWith(request.mode === 'navigate'
        ? fetchWithRetry(request, NAVIGATION_RETRY_MS)
        : withSlot(() => fetchWithRetry(request, RESOURCE_RETRY_MS)));
});

async function fetchWithRetry(request, delays) {
    for (let attempt = 0; ; attempt++) {
        try {
            return await fetch(request.clone());
        } catch (err) {
            // الصفحة نفسها ألغت الطلب (انتقل الزائر إلى صفحة أخرى): لا تُعِد المحاولة
            if (err && err.name === 'AbortError') throw err;
            if (attempt >= delays.length) throw err;
            await new Promise((resolve) => setTimeout(resolve, delays[attempt]));
        }
    }
}

// طابور بسيط: MAX_CONCURRENT طلبات في الوقت نفسه، والباقي ينتظر تحرر مكان
let active = 0;
const waiting = [];

async function withSlot(work) {
    if (active >= MAX_CONCURRENT) await new Promise((resolve) => waiting.push(resolve));
    active++;
    try {
        return await work();
    } finally {
        active--;
        const next = waiting.shift();
        if (next) next();
    }
}
