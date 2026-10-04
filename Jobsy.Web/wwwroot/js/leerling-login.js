// Outside the school-script guard so a second include still exposes the coach store.
window.lobsyLeerlingCoach = window.lobsyLeerlingCoach || {
    key: "lobsy.ll.coach.dismissed",
    dismissed: function () {
        try {
            var raw = sessionStorage.getItem(window.lobsyLeerlingCoach.key);
            if (!raw) {
                return [];
            }

            var parsed = JSON.parse(raw);
            return Array.isArray(parsed) ? parsed : [];
        } catch (e) {
            return [];
        }
    },
    dismiss: function (text) {
        if (!text) {
            return false;
        }

        try {
            var list = window.lobsyLeerlingCoach.dismissed();
            if (list.indexOf(text) < 0) {
                list.push(text);
            }

            sessionStorage.setItem(window.lobsyLeerlingCoach.key, JSON.stringify(list));
            return true;
        } catch (e) {
            return false;
        }
    }
};

(function () {
    if (window.__lobsyLeerlingSchool) {
        return;
    }

    window.__lobsyLeerlingSchool = true;

    // Interactive Server replaces the prerendered <select>. Bind on document so the
    // listener survives that swap. The "Kies" button still submits when JS is off.
    document.addEventListener("change", function (event) {
        var select = event.target;
        if (!select || !select.matches || !select.matches("select[data-leerling-school]")) {
            return;
        }

        if (!select.value || !select.form) {
            return;
        }

        select.form.submit();
    });
})();
