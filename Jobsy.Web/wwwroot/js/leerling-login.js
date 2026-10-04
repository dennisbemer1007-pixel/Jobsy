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
