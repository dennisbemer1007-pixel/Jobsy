(function () {
    function navigateFromForm(form) {
        if (!form) {
            return;
        }
        var params = new URLSearchParams();
        var className = form.querySelector('input[name="className"]');
        if (className && className.value.trim()) {
            params.set('className', className.value.trim());
        }
        var kind = form.querySelector('input[name="kind"]:checked');
        if (kind && kind.value) {
            params.set('kind', kind.value);
        }
        var level = form.querySelector('select[name="level"], input[name="level"]');
        if (level && level.value) {
            params.set('level', level.value);
        }
        var year = form.querySelector('input[name="year"]:checked');
        if (year && year.value) {
            params.set('year', year.value);
        }
        var pupilCount = form.querySelector('input[name="pupilCount"]');
        if (pupilCount && pupilCount.value) {
            params.set('pupilCount', pupilCount.value);
        }
        form.querySelectorAll('input[name="teacherIds"]:checked').forEach(function (box) {
            params.append('teacherIds', box.value);
        });
        var query = params.toString();
        window.location.href = '/school/klassen/nieuw' + (query ? '?' + query : '');
    }

    document.addEventListener('change', function (event) {
        var target = event.target;
        if (!target || !target.form) {
            return;
        }
        var form = target.form;
        if (form.getAttribute('action') !== '/school/klassen') {
            return;
        }
        if (target.name !== 'kind' && target.name !== 'year') {
            return;
        }
        navigateFromForm(form);
    });
})();