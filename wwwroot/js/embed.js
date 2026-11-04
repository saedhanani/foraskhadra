/* فرص خضراء — أداة التضمين (المرحلة 3.3)
   الاستعمال على أي موقع:
   <div class="fk-embed" data-lang="ar" data-type="Scholarships" data-country="JO" data-limit="6" data-theme="light"></div>
   <script async src="https://www.foraskhadra.com/js/embed.js"></script>
   data-type: Scholarships|Jobs|Internships|Fellowships|Volunteering|Conferences|Competitions (أو عدة قيم مفصولة بفاصلة)
   data-country: رمز ISO للدولة المقبولة (JO, EG, …) أو XA للوطن العربي · data-lang: ar|en|fr · data-limit: 1–20 · data-theme: light|dark */
(function () {
    var script = document.currentScript || (function () { var s = document.getElementsByTagName('script'); return s[s.length - 1]; })();
    var origin = 'https://www.foraskhadra.com';
    try { if (script && script.src) origin = new URL(script.src).origin; } catch (e) { }

    function build(el) {
        if (el.getAttribute('data-fk-ready')) return;
        el.setAttribute('data-fk-ready', '1');
        var lang = (el.getAttribute('data-lang') || 'ar').toLowerCase();
        var prefix = lang === 'en' || lang === 'fr' ? '/' + lang : '';
        var params = [];
        function add(name, attr) { var v = el.getAttribute(attr); if (v) params.push(name + '=' + encodeURIComponent(v)); }
        var types = el.getAttribute('data-type');
        if (types) types.split(',').forEach(function (t) { t = t.trim(); if (t) params.push('type=' + encodeURIComponent(t)); });
        add('country', 'data-country');
        add('funding', 'data-funding');
        add('degree', 'data-degree');
        add('field', 'data-field');
        add('theme', 'data-green-theme');
        add('host', 'data-host');
        add('limit', 'data-limit');
        var theme = el.getAttribute('data-theme');
        if (theme === 'dark') params.push('theme=dark');
        if (el.getAttribute('data-remote') === 'true') params.push('remote=true');
        if (el.getAttribute('data-women') === 'true') params.push('women=true');

        var iframe = document.createElement('iframe');
        iframe.src = origin + prefix + '/embed/opportunities' + (params.length ? '?' + params.join('&') : '');
        iframe.title = lang === 'en' ? 'Opportunities from Foras Khadra' : lang === 'fr' ? 'Opportunités de Foras Khadra' : 'فرص من فرص خضراء';
        iframe.loading = 'lazy';
        iframe.setAttribute('scrolling', 'no');
        iframe.style.cssText = 'width:100%;border:0;min-height:260px;display:block;overflow:hidden;';
        iframe.setAttribute('data-fk-frame', '1');
        el.appendChild(iframe);
    }

    function init() { var list = document.querySelectorAll('.fk-embed'); for (var i = 0; i < list.length; i++) build(list[i]); }

    window.addEventListener('message', function (e) {
        if (e.origin !== origin || !e.data || typeof e.data.fkEmbedHeight !== 'number') return;
        var frames = document.querySelectorAll('iframe[data-fk-frame]');
        for (var i = 0; i < frames.length; i++) {
            if (frames[i].contentWindow === e.source) frames[i].style.height = Math.max(120, e.data.fkEmbedHeight + 4) + 'px';
        }
    });

    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', init); else init();
    window.ForasKhadraEmbed = { refresh: init };
})();
