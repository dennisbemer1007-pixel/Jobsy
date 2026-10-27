(function () {
    "use strict";

    function qs(sel, root) {
        return (root || document).querySelector(sel);
    }

    function buildListUrl(form) {
        var action = form.getAttribute("action") || "/werkgever/sollicitaties";
        var parts = [];
        var fd = new FormData(form);
        fd.forEach(function (value, key) {
            if (value === null || value === undefined || String(value).trim() === "") {
                return;
            }

            parts.push(encodeURIComponent(key) + "=" + encodeURIComponent(String(value)));
        });
        return parts.length === 0 ? action : action + "?" + parts.join("&");
    }

    function bindForm(form) {
        if (!form || form.getAttribute("data-wg-list-filter-bound") === "1") {
            return;
        }

        form.setAttribute("data-wg-list-filter-bound", "1");
        var vacancy = form.querySelector('select[name="vacancyId"]');
        var overdue = form.querySelector('input[name="filter"]');
        var onChange = function () {
            applyFilters(form);
        };
        if (vacancy) {
            vacancy.addEventListener("change", onChange);
        }

        if (overdue) {
            overdue.addEventListener("change", onChange);
        }
    }

    function applyFilters(form) {
        var url = buildListUrl(form);
        fetch(url, {
            credentials: "same-origin",
            headers: { Accept: "text/html" }
        }).then(function (res) {
            if (!res.ok) {
                window.location.assign(url);
                return null;
            }

            return res.text();
        }).then(function (html) {
            if (!html) {
                return;
            }

            var doc = new DOMParser().parseFromString(html, "text/html");
            var next = doc.querySelector('[data-wg-list-ssr="1"]');
            var current = qs('[data-wg-list-ssr="1"]');
            if (!next || !current) {
                window.location.assign(url);
                return;
            }

            current.replaceWith(next);
            if (window.history && window.history.pushState) {
                window.history.pushState({ wgListSsr: true }, "", url);
            }

            var freshForm = qs('[data-wg-list-ssr="1"] form[data-wg-list-filter-form]');
            bindForm(freshForm);
        }).catch(function () {
            window.location.assign(url);
        });
    }

    function init() {
        bindForm(qs('[data-wg-list-ssr="1"] form[data-wg-list-filter-form]'));
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", init);
    } else {
        init();
    }

    window.addEventListener("popstate", function () {
        window.location.reload();
    });
})();
