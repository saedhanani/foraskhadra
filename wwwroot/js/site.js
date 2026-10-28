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
