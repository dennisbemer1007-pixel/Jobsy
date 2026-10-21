(function () {
    function syncLayout() {
        try {
            if (window.lobsyPaspoortLayout?.syncSidebarSticky) window.lobsyPaspoortLayout.syncSidebarSticky();
            if (window.lobsyCandidateNav?.syncAppHeaderHeight) window.lobsyCandidateNav.syncAppHeaderHeight();
        } catch (e) {}
    }
    if (document.readyState === "loading") document.addEventListener("DOMContentLoaded", syncLayout); else syncLayout();
})();
