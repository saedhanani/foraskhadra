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

// ===== تثبيت الموقع كتطبيق (PWA، انظر Views/Shared/_InstallPrompt.cshtml و wwwroot/manifest.webmanifest) =====
// Chrome/Edge يطلقان beforeinstallprompt حين يكون التثبيت ممكنًا؛ Safari على iPhone/iPad لا يطلقه، فنعرض له خطوات الإضافة يدويًا.
// الشريط يظهر من الصفحة الثانية التي يفتحها الزائر، وإن أغلقه لا يعود قبل 30 يومًا؛ رابط التذييل يبقى متاحًا دائمًا.
(function () {
    var banner = document.getElementById('installBanner');
    if (!banner) return;
    var standalone = window.matchMedia('(display-mode: standalone)').matches || window.navigator.standalone === true;
    if (standalone) return;

    var DISMISS_KEY = 'fk-install-dismissed';
    var VIEWS_KEY = 'fk-page-views';
    var isIos = /iphone|ipad|ipod/i.test(navigator.userAgent) || (navigator.platform === 'MacIntel' && navigator.maxTouchPoints > 1);
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

    function showBanner(mode) {
        banner.setAttribute('data-mode', mode);
        banner.hidden = false;
    }

    // mode: "prompt" (زر تثبيت حقيقي) أو "ios" (شرح الخطوات)
    function enable(mode) {
        document.querySelectorAll('[data-install-trigger]').forEach(function (el) { el.hidden = false; });
        if (pageViews >= 2 && !dismissedRecently()) showBanner(mode);
    }

    window.addEventListener('beforeinstallprompt', function (e) {
        e.preventDefault();
        deferredPrompt = e;
        enable('prompt');
    });

    window.addEventListener('appinstalled', function () {
        deferredPrompt = null;
        banner.hidden = true;
        document.querySelectorAll('[data-install-trigger]').forEach(function (el) { el.hidden = true; });
    });

    if (isIos) enable('ios');

    document.addEventListener('click', function (e) {
        if (e.target.closest('[data-install-dismiss]')) {
            banner.hidden = true;
            try { localStorage.setItem(DISMISS_KEY, String(Date.now())); } catch (err) { }
            return;
        }
        if (!e.target.closest('[data-install-action]')) return;
        e.preventDefault();
        if (deferredPrompt) {
            deferredPrompt.prompt();
            deferredPrompt.userChoice.finally(function () { deferredPrompt = null; banner.hidden = true; });
        } else if (isIos) {
            showBanner('ios');
        }
    });
})();

// ===== قائمة الهاتف: تُغلق عند النقر خارجها (الروابط تغلقها بـ data-close-menu أعلاه) =====
document.addEventListener('click', function (e) {
    var nav = document.getElementById('navContent');
    if (!nav || !nav.classList.contains('show')) return;
    if (e.target.closest('#navContent') || e.target.closest('.mobile-menu-btn')) return;
    nav.classList.remove('show');
});
