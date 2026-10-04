(function () {
    function bind(root) {
        var nodes = (root || document).querySelectorAll("select[data-leerling-school]");
        for (var i = 0; i < nodes.length; i++) {
            var select = nodes[i];
            if (select.dataset.bound === "1") {
                continue;
            }

            select.dataset.bound = "1";
            select.addEventListener("change", function () {
                if (!this.value || !this.form) {
                    return;
                }

                this.form.submit();
            });
        }
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", function () { bind(document); });
    } else {
        bind(document);
    }
})();
