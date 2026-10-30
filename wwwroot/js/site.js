function toggleDropdown(id, event) {
  if (event) event.stopPropagation();

  const menu = document.getElementById(id);
  if (!menu) return;

  document.querySelectorAll(".dropdown-list").forEach((item) => {
    if (item !== menu) item.classList.remove("show");
  });

  menu.classList.toggle("show");
}

function toggleMobileMenu(event) {
  if (event) event.stopPropagation();

  const nav = document.getElementById("navContent");
  if (nav) nav.classList.toggle("show");
}

document.addEventListener("click", (event) => {
  if (!event.target.closest(".custom-dropdown")) {
    document.querySelectorAll(".dropdown-list").forEach((item) => {
      item.classList.remove("show");
    });
  }

  const link = event.target.closest("[data-close-menu]");
  const nav = document.getElementById("navContent");
  if (link && nav && window.innerWidth <= 1400) nav.classList.remove("show");
});

window.addEventListener("resize", () => {
  const nav = document.getElementById("navContent");
  if (nav && window.innerWidth > 1400) nav.classList.remove("show");
});

document.addEventListener("DOMContentLoaded", () => {
  const nav = document.getElementById("navContent");
  if (nav) nav.classList.remove("show");
});

// ===== حفظ الفرصة / إلغاء الحفظ (الأزرار ذات data-save-opportunity، انظر Views/Shared/_SaveButton.cshtml) =====
document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-save-opportunity]');
    if (!btn) return;
    e.preventDefault();

    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    if (!tokenInput) return;

    btn.disabled = true;
    fetch('/Opportunity/ToggleSave/' + btn.getAttribute('data-save-opportunity'), {
        method: 'POST',
        headers: { 'RequestVerificationToken': tokenInput.value },
        credentials: 'same-origin'
    })
        .then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); })
        .then(function (data) {
            var label = data.saved ? btn.getAttribute('data-label-saved') : btn.getAttribute('data-label-save');
            btn.classList.toggle('is-saved', data.saved);
            btn.setAttribute('aria-pressed', data.saved ? 'true' : 'false');
            btn.title = label;
            btn.setAttribute('aria-label', label);
            var icon = btn.querySelector('i');
            if (icon) icon.className = data.saved ? 'bi bi-bookmark-fill' : 'bi bi-bookmark';
        })
        .catch(function () { })
        .finally(function () { btn.disabled = false; });
});

// ===== جرس الإشعارات (زر notifMenu في _Layout): جلب القائمة عند الفتح ثم تعليم الكل مقروءًا =====
var fkNotifLoaded = false;
function fkLoadNotifications() {
    var menu = document.getElementById('notifMenu');
    if (!menu || fkNotifLoaded) return;
    fkNotifLoaded = true;

    fetch('/Notifications/Menu', { credentials: 'same-origin' })
        .then(function (r) { if (!r.ok) throw new Error(r.status); return r.text(); })
        .then(function (html) {
            menu.innerHTML = html;

            var badge = document.getElementById('notifBadge');
            if (!badge) return;
            var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
            if (!tokenInput) return;
            fetch('/Notifications/MarkAllRead', {
                method: 'POST',
                headers: { 'RequestVerificationToken': tokenInput.value },
                credentials: 'same-origin'
            }).then(function (r) { if (r.ok) badge.remove(); }).catch(function () { });
        })
        .catch(function () { fkNotifLoaded = false; });
}

// ===== الجهاز والمتصفح: يستخدمه شريط التثبيت وصفحة /app وبطاقة التنبيهات =====
var fkDevice = (function () {
    var ua = navigator.userAgent;
    var isIos = /iphone|ipad|ipod/i.test(ua) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
    // متصفحات داخل التطبيقات (إنستغرام، فيسبوك، تيك توك...) لا تستطيع التثبيت
    var inApp = /FBAN|FBAV|FB_IAB|Instagram|Line\/|Snapchat|musical_ly|Bytedance|TikTok|LinkedInApp|Twitter|MicroMessenger|GSA\//i.test(ua);
    var iosOtherBrowser = isIos && /CriOS|FxiOS|EdgiOS|OPiOS|OPT\/|YaBrowser|DuckDuckGo/i.test(ua);
    return {
        isIos: isIos,
        isAndroid: /android/i.test(ua),
        inApp: inApp,
        // على iPhone: Safari هو الطريق المضمون للإضافة إلى الشاشة الرئيسية
        iosSafari: isIos && !inApp && !iosOtherBrowser,
        standalone: window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true
    };
})();

// ===== تثبيت الموقع كتطبيق (PWA، انظر Views/Shared/_InstallPrompt.cshtml و Views/Home/App.cshtml) =====
// Chrome/Edge يطلقان beforeinstallprompt حين يكون التثبيت ممكنًا فيظهر زر تثبيت حقيقي ([data-install-native])؛
// على iPhone نعرض خطوات Safari، وفي متصفحات التطبيقات نطلب الفتح في Safari/Chrome — والشرح الكامل في /app.
// الشريط يظهر من الصفحة الثانية التي يفتحها الزائر، وإن أغلقه لا يعود قبل 30 يومًا.
(function () {
    if (fkDevice.standalone) return;
    var banner = document.getElementById('installBanner');

    var DISMISS_KEY = 'fk-install-dismissed';
    var VIEWS_KEY = 'fk-page-views';
    var deferredPrompt = null;
    var pageViews = 0;
    try {
        pageViews = (parseInt(localStorage.getItem(VIEWS_KEY), 10) || 0) + 1;
        localStorage.setItem(VIEWS_KEY, String(pageViews));
    } catch (e) { }

    function dismissedRecently() {
        try {
            var at = parseInt(localStorage.getItem(DISMISS_KEY), 10);
            return at && Date.now() - at < 30 * 24 * 60 * 60 * 1000;
        } catch (e) { return false; }
    }

    // mode: "prompt" (زر تثبيت حقيقي) / "ios" (خطوات Safari) / "ios-other" (افتح في Safari) / "inapp" (افتح في المتصفح)
    function enable(mode) {
        if (!banner || pageViews < 2 || dismissedRecently()) return;
        banner.setAttribute('data-mode', mode);
        banner.hidden = false;
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        document.querySelectorAll('[data-install-native]').forEach(function (el) { el.hidden = false; });
        enable('prompt');
    });

    window.addEventListener('appinstalled', function () {
        deferredPrompt = null;
        if (banner) banner.hidden = true;
        document.querySelectorAll('[data-install-native]').forEach(function (el) { el.hidden = true; });
    });

    if (fkDevice.isIos) enable(fkDevice.iosSafari ? 'ios' : 'ios-other');
    else if (fkDevice.inApp) enable('inapp');

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-install-dismiss]')) {
            banner.hidden = true;
            try { localStorage.setItem(DISMISS_KEY, String(Date.now())); } catch (err) { }
            return;
        }
        if (!e.target.closest('[data-install-action]') || !deferredPrompt) return;
        e.preventDefault();
        deferredPrompt.prompt();
        deferredPrompt.userChoice.finally(function () { deferredPrompt = null; if (banner) banner.hidden = true; });
    });
})();

// ===== إشعارات الدفع: بطاقات [data-push-card] (Views/Shared/_PushCard.cshtml، Controllers/PushController.cs، الاستقبال في sw.js) =====
// الحالات: ios-install (iPhone قبل التثبيت) / off / on / denied / error / unsupported (تختفي البطاقة).
(function () {
    var cards = document.querySelectorAll('[data-push-card]');
    if (!cards.length) return;
    var tokenInput = document.querySelector('input[name="__RequestVerificationToken"]');
    var SYNC_KEY = 'fk-push-synced';

    function render(state) {
        cards.forEach(function (card) {
            card.setAttribute('data-state', state);
            // المختصرة (الرئيسية) لا تزعج من فعّل أو من حظر عمدًا؛ الكاملة في "فرصي" و/app تشرح الحالتين
            var quiet = card.getAttribute('data-variant') === 'compact' && (state === 'on' || state === 'denied');
            card.hidden = state === 'unsupported' || quiet;
        });
    }

    function post(url, body) {
        return fetch(url, {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', 'RequestVerificationToken': tokenInput.value },
            credentials: 'same-origin',
            body: JSON.stringify(body)
        }).then(function (r) { if (!r.ok) throw new Error(r.status); return r.json(); });
    }

    function toBody(subscription) {
        var json = subscription.toJSON();
        return { endpoint: json.endpoint, p256dh: json.keys.p256dh, auth: json.keys.auth, tz: new Date().getTimezoneOffset() };
    }

    function keyBytes(base64url) {
        var base64 = (base64url + '==='.slice((base64url.length + 3) % 4)).replace(/-/g, '+').replace(/_/g, '/');
        var raw = atob(base64), bytes = new Uint8Array(raw.length);
        for (var i = 0; i < raw.length; i++) bytes[i] = raw.charCodeAt(i);
        return bytes;
    }

    function markSynced() {
        try { localStorage.setItem(SYNC_KEY, String(Date.now())); } catch (e) { }
    }

    function turnOn() {
        // الإذن يُطلب من نقرة المستخدم نفسها (شرط Safari على iPhone)
        return Notification.requestPermission().then(function (permission) {
            if (permission !== 'granted') { render(permission === 'denied' ? 'denied' : 'off'); return; }
            return navigator.serviceWorker.ready
                .then(function (reg) {
                    return reg.pushManager.getSubscription().then(function (sub) {
                        return sub || reg.pushManager.subscribe({ userVisibleOnly: true, applicationServerKey: keyBytes(cards[0].getAttribute('data-key')) });
                    });
                })
                .then(function (sub) { return post('/Push/Subscribe', toBody(sub)); })
                .then(function () { markSynced(); render('on'); });
        }).catch(function () { render('error'); });
    }

    function turnOff() {
        return navigator.serviceWorker.ready
            .then(function (reg) { return reg.pushManager.getSubscription(); })
            .then(function (sub) {
                if (!sub) return;
                var body = { endpoint: sub.endpoint };
                return sub.unsubscribe().then(function () { return post('/Push/Unsubscribe', body); });
            })
            .then(function () { render('off'); })
            .catch(function () { render('error'); });
    }

    document.addEventListener('click', function (e) {
        var btn = e.target.closest('[data-push-enable], [data-push-disable]');
        if (!btn) return;
        btn.disabled = true;
        (btn.hasAttribute('data-push-enable') ? turnOn() : turnOff()).finally(function () { btn.disabled = false; });
    });

    if (fkDevice.isIos && !fkDevice.standalone) { render('ios-install'); return; }
    if (!('serviceWorker' in navigator) || !('PushManager' in window) || !('Notification' in window) || !tokenInput) { render('unsupported'); return; }
    if (Notification.permission === 'denied') { render('denied'); return; }

    navigator.serviceWorker.ready
        .then(function (reg) { return reg.pushManager.getSubscription(); })
        .then(function (sub) {
            if (!sub || Notification.permission !== 'granted') { render('off'); return; }
            render('on');
            // مزامنة يومية صامتة: إن فقد الخادم الاشتراك أو تغيّرت اللغة/التوقيت
            var last = 0;
            try { last = parseInt(localStorage.getItem(SYNC_KEY), 10) || 0; } catch (e) { }
            if (Date.now() - last > 24 * 60 * 60 * 1000) post('/Push/Subscribe', toBody(sub)).then(markSynced).catch(function () { });
        })
        .catch(function () { render('off'); });
})();

// ===== قائمة الهاتف: تُغلق عند النقر خارجها (الروابط تغلقها بـ data-close-menu أعلاه) =====
document.addEventListener('click', function (e) {
    var nav = document.getElementById('navContent');
    if (!nav || !nav.classList.contains('show')) return;
    if (e.target.closest('#navContent') || e.target.closest('.mobile-menu-btn')) return;
    nav.classList.remove('show');
});
