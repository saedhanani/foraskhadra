// Service Worker: يحمي الزائر من قطع الخادم للطلبات، ويعطي الموقع سلوك تطبيق (PWA) مع صفحة "لا يوجد اتصال".
// خادم الاستضافة يقطع طلبات الزائر على HTTP/2 لثوانٍ إذا وصلته دفعة من نحو 10 طلبات متزامنة أو أكثر
// (ERR_HTTP2_PROTOCOL_ERROR)، فيرى الزائر صفحة «تعذر الوصول». هنا:
//  1. لا يُرسَل إلى الخادم أكثر من MAX_CONCURRENT طلبًا في الوقت نفسه (الصور والملفات تنتظر دورها بدل أن تنطلق دفعة واحدة).
//  2. طلب الصفحة نفسها (التنقل) لا ينتظر في الطابور، ويُعاد عند الفشل مرات أكثر، وإن فشل كله تظهر offline.html.
//  3. أي طلب يقطعه الخادم يُعاد تلقائيًا بعد مهلة قصيرة.
//  4. الملفات الثابتة (css/js/lib/images) تُطلب من الشبكة أولًا وتُحفظ نسخة منها للعمل بلا اتصال؛
//     صفحات HTML لا تُخزَّن (تختلف بحسب المستخدم واللغة).
const CACHE_NAME = 'fk-static-v1'; // غيّر الرقم عند تغيير قائمة PRECACHE
const OFFLINE_URL = '/offline.html';
const PRECACHE = [
    OFFLINE_URL,
    '/images/icon-192.png',
    '/images/favicon.ico',
    '/lib/bootstrap/dist/css/bootstrap.min.css',
    '/css/layout_style.css'
];
const STATIC_PREFIXES = ['/css/', '/js/', '/lib/', '/images/'];

const MAX_CONCURRENT = 6;
const NAVIGATION_RETRY_MS = [300, 800, 1500, 2500, 4000];
const RESOURCE_RETRY_MS = [600, 1500];

self.addEventListener('install', (event) => {
    event.waitUntil(
        caches.open(CACHE_NAME)
            .then((cache) => cache.addAll(PRECACHE))
            .then(() => self.skipWaiting())
    );
});

self.addEventListener('activate', (event) => {
    event.waitUntil(
        caches.keys()
            .then((keys) => Promise.all(keys.filter((k) => k !== CACHE_NAME).map((k) => caches.delete(k))))
            .then(() => self.clients.claim())
    );
});

self.addEventListener('fetch', (event) => {
    const request = event.request;
    if (request.method !== 'GET' || !request.url.startsWith(self.location.origin)) return;

    if (request.mode === 'navigate') {
        event.respondWith(navigate(request));
        return;
    }

    const path = new URL(request.url).pathname;
    const isStatic = STATIC_PREFIXES.some((p) => path.startsWith(p));
    event.respondWith(withSlot(() => isStatic ? staticAsset(request) : fetchWithRetry(request, RESOURCE_RETRY_MS)));
});

async function navigate(request) {
    try {
        return await fetchWithRetry(request, NAVIGATION_RETRY_MS);
    } catch (err) {
        if (err && err.name === 'AbortError') throw err;
        const offline = await caches.match(OFFLINE_URL);
        return offline || new Response('Offline', { status: 503, headers: { 'Content-Type': 'text/plain' } });
    }
}

// الشبكة أولًا (حتى يصل أي تحديث فورًا)، وعند الفشل النسخة المحفوظة إن وُجدت
async function staticAsset(request) {
    try {
        const response = await fetchWithRetry(request, RESOURCE_RETRY_MS);
        if (response.ok && response.type === 'basic') {
            const copy = response.clone();
            caches.open(CACHE_NAME).then((cache) => cache.put(request, copy)).catch(() => { });
        }
        return response;
    } catch (err) {
        const cached = await caches.match(request, { ignoreSearch: true });
        if (cached) return cached;
        throw err;
    }
}

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

// ===== إشعارات الدفع (يرسلها Services/PushDispatcher.cs؛ الحمولة من Helpers/NotificationText.cs) =====
self.addEventListener('push', (event) => {
    let data = {};
    try { data = event.data ? event.data.json() : {}; } catch (err) { data = { body: event.data ? event.data.text() : '' }; }
    // Safari على iPhone يلغي الاشتراك إن وصل إشعار دون أن يُعرض، فنعرض دائمًا
    event.waitUntil(self.registration.showNotification(data.title || 'فرص خضراء', {
        body: data.body || '',
        icon: '/images/icon-192.png',
        tag: data.tag || undefined,
        lang: data.lang || 'ar',
        dir: data.dir || 'auto',
        data: { url: data.url || '/' }
    }));
});

// النقر على الإشعار: يفتح صفحته في نافذة الموقع المفتوحة إن وُجدت، وإلا في نافذة جديدة (روابط الموقع فقط)
self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const target = new URL((event.notification.data && event.notification.data.url) || '/', self.location.origin);
    const url = target.origin === self.location.origin ? target.href : self.location.origin + '/';
    event.waitUntil((async () => {
        const windows = await self.clients.matchAll({ type: 'window', includeUncontrolled: true });
        for (const client of windows) {
            if (client.url.startsWith(self.location.origin) && 'focus' in client && 'navigate' in client) {
                try {
                    await client.focus();
                    await client.navigate(url);
                    return;
                } catch (err) {
                    break; // نافذة لا يتحكم بها الـ worker: نفتح نافذة جديدة
                }
            }
        }
        await self.clients.openWindow(url);
    })());
});
