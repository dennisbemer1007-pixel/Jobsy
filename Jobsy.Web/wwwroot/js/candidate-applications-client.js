(function () {
    "use strict";

    function qs(sel, root) {
        return (root || document).querySelector(sel);
    }

    function qsa(sel, root) {
        return Array.prototype.slice.call((root || document).querySelectorAll(sel));
    }

    function isRunningStatus(status) {
        var s = (status || "").toLowerCase();
        return s === "pending" || s === "accepted" || s === "employercontacting";
    }

    function isDoneStatus(status) {
        var s = (status || "").toLowerCase();
        return s === "hired" || s === "rejected" || s === "filledelsewhere" || s === "withdrawn";
    }

    function isKompasWide() {
        return window.matchMedia && window.matchMedia("(min-width: 1024px)").matches;
    }

    function mount(root) {
        if (!root || root.__jobsyAppsMounted) {
            return;
        }
        root.__jobsyAppsMounted = true;

        var state = {
            filter: root.getAttribute("data-filter") || "all",
            selectedId: root.getAttribute("data-selected-id") || "",
            openMenuId: "",
            expandedId: ""
        };

        var withdrawDialog = qs("[data-apps-withdraw-dialog]", root);
        var withdrawPanel = withdrawDialog ? qs("[data-apps-withdraw-panel]", withdrawDialog) : null;
        var withdrawAppId = withdrawPanel ? qs("input[name='application_id']", withdrawPanel) : null;

        function visibleItems() {
            return qsa("[data-app-item]", root).filter(function (el) {
                if (el.hasAttribute("hidden")) {
                    return false;
                }
                var status = el.getAttribute("data-app-status") || "";
                if (state.filter === "running") {
                    return isRunningStatus(status);
                }
                if (state.filter === "done") {
                    return isDoneStatus(status);
                }
                return true;
            });
        }

        function applyFilter(nextFilter) {
            state.filter = nextFilter || "all";
            root.setAttribute("data-filter", state.filter);

            qsa("[data-filter-btn]", root).forEach(function (btn) {
                var key = btn.getAttribute("data-filter-btn") || "all";
                var selected = key === state.filter || (key === "all" && (state.filter === "all" || !state.filter));
                btn.classList.toggle("is-selected", selected);
                btn.setAttribute("aria-pressed", selected ? "true" : "false");
            });

            qsa("[data-app-item]", root).forEach(function (el) {
                var status = el.getAttribute("data-app-status") || "";
                var show =
                    state.filter === "all" ||
                    (state.filter === "running" && isRunningStatus(status)) ||
                    (state.filter === "done" && isDoneStatus(status));
                el.hidden = !show;
            });

            var visible = visibleItems();
            var emptyAll = qs("[data-apps-empty-all]", root);
            var emptyFilter = qs("[data-apps-empty-filter]", root);
            var content = qs("[data-apps-content]", root);
            var total = qsa("[data-app-item]", root).length;

            if (emptyAll) {
                emptyAll.hidden = total > 0;
            }
            if (emptyFilter) {
                emptyFilter.hidden = total === 0 || visible.length > 0;
            }
            if (content) {
                content.hidden = total === 0 || visible.length === 0;
            }

            if (visible.length === 0) {
                state.selectedId = "";
            } else if (!visible.some(function (el) { return el.getAttribute("data-app-id") === state.selectedId; })) {
                state.selectedId = visible[0].getAttribute("data-app-id") || "";
            }

            syncLayout();
        }

        function closeMenu() {
            state.openMenuId = "";
            qsa("[data-app-menu]", root).forEach(function (menu) {
                menu.classList.remove("is-menu-open");
                var panel = qs("[data-app-menu-panel]", menu);
                var toggle = qs("[data-menu-toggle]", menu);
                if (panel) {
                    panel.hidden = true;
                }
                if (toggle) {
                    toggle.setAttribute("aria-expanded", "false");
                    toggle.classList.remove("is-open");
                }
            });
            qsa("[data-app-menu-backdrop]", root).forEach(function (el) {
                el.hidden = true;
            });
        }

        function openMenu(appId) {
            closeMenu();
            state.openMenuId = appId;
            var menu = qs('[data-app-menu][data-app-id="' + appId + '"]', root);
            if (!menu) {
                return;
            }
            menu.classList.add("is-menu-open");
            var panel = qs("[data-app-menu-panel]", menu);
            var toggle = qs("[data-menu-toggle]", menu);
            var backdrop = qs("[data-app-menu-backdrop]", menu);
            if (panel) {
                panel.hidden = false;
            }
            if (toggle) {
                toggle.setAttribute("aria-expanded", "true");
                toggle.classList.add("is-open");
            }
            if (backdrop) {
                backdrop.hidden = false;
            }
        }

        function closeSheet() {
            state.expandedId = "";
            var sheet = qs("[data-apps-sheet]", root);
            var backdrop = qs("[data-apps-sheet-backdrop]", root);
            if (sheet) {
                sheet.hidden = true;
            }
            if (backdrop) {
                backdrop.hidden = true;
            }
        }

        function openSheet(appId) {
            if (isKompasWide()) {
                state.selectedId = appId;
                syncLayout();
                return;
            }
            state.expandedId = appId;
            var sheet = qs("[data-apps-sheet]", root);
            var backdrop = qs("[data-apps-sheet-backdrop]", root);
            if (!sheet) {
                return;
            }
            qsa("[data-apps-sheet-card]", sheet).forEach(function (card) {
                card.hidden = card.getAttribute("data-app-id") !== appId;
            });
            sheet.hidden = false;
            if (backdrop) {
                backdrop.hidden = false;
            }
        }

        function syncLayout() {
            var wide = isKompasWide();
            root.classList.toggle("kb-apps--wide", wide);

            qsa("[data-app-item]", root).forEach(function (wrap) {
                var id = wrap.getAttribute("data-app-id") || "";
                var visible = !wrap.hidden;
                var featured = qs('[data-app-mode="featured"]', wrap);
                var compact = qs("[data-app-compact]", wrap);
                if (!visible) {
                    if (compact) {
                        compact.classList.remove("is-selected");
                    }
                    return;
                }
                if (wide) {
                    if (featured) {
                        featured.hidden = true;
                    }
                    if (compact) {
                        compact.hidden = false;
                        compact.classList.toggle("is-selected", id === state.selectedId);
                    }
                } else {
                    var showFeatured = id === state.selectedId;
                    if (featured) {
                        featured.hidden = !showFeatured;
                    }
                    if (compact) {
                        compact.hidden = showFeatured;
                        compact.classList.toggle("is-selected", id === state.selectedId);
                    }
                }
            });

            qsa("[data-apps-detail-panel]", root).forEach(function (panel) {
                var id = panel.getAttribute("data-app-id") || "";
                panel.hidden = !wide || id !== state.selectedId || !state.selectedId;
            });

            var legend = qs("[data-apps-rail-legend]", root);
            if (legend) {
                legend.hidden = wide && !!state.selectedId;
            }
        }

        function openWithdraw(appId) {
            closeMenu();
            if (!withdrawDialog || !withdrawPanel || !withdrawAppId) {
                return;
            }
            withdrawAppId.value = appId;
            if (typeof withdrawDialog.showModal === "function") {
                withdrawDialog.showModal();
            } else {
                withdrawDialog.setAttribute("open", "");
            }
        }

        function closeWithdraw() {
            if (!withdrawDialog) {
                return;
            }
            if (typeof withdrawDialog.close === "function") {
                withdrawDialog.close();
            } else {
                withdrawDialog.removeAttribute("open");
            }
        }

        root.addEventListener("click", function (ev) {
            var target = ev.target;
            if (!target || !target.closest) {
                return;
            }

            var filterBtn = target.closest("[data-filter-btn]");
            if (filterBtn && root.contains(filterBtn)) {
                ev.preventDefault();
                var key = filterBtn.getAttribute("data-filter-btn") || "all";
                applyFilter(key);
                return;
            }

            var menuToggle = target.closest("[data-menu-toggle]");
            if (menuToggle && root.contains(menuToggle)) {
                ev.preventDefault();
                ev.stopPropagation();
                var appId = menuToggle.getAttribute("data-menu-toggle") || "";
                if (state.openMenuId === appId) {
                    closeMenu();
                } else {
                    openMenu(appId);
                }
                return;
            }

            if (target.closest("[data-app-menu-backdrop]")) {
                ev.preventDefault();
                closeMenu();
                return;
            }

            var compactOpen = target.closest("[data-app-compact-open]");
            if (compactOpen && root.contains(compactOpen)) {
                ev.preventDefault();
                var openId = compactOpen.getAttribute("data-app-compact-open") || "";
                openSheet(openId);
                return;
            }

            var withdrawBtn = target.closest("[data-app-withdraw]");
            if (withdrawBtn && root.contains(withdrawBtn)) {
                ev.preventDefault();
                openWithdraw(withdrawBtn.getAttribute("data-app-withdraw") || "");
                return;
            }

            var practiceBtn = target.closest("[data-app-practice]");
            if (practiceBtn && root.contains(practiceBtn)) {
                ev.preventDefault();
                var vacancyId = practiceBtn.getAttribute("data-app-practice") || "";
                if (vacancyId) {
                    window.location.href = "/vacancies/" + vacancyId;
                }
                return;
            }

            if (target.closest("[data-apps-sheet-close]")) {
                ev.preventDefault();
                closeSheet();
                return;
            }

            if (target.closest("[data-apps-sheet-backdrop]")) {
                ev.preventDefault();
                closeSheet();
                return;
            }

            if (target.closest("[data-apps-withdraw-cancel]")) {
                ev.preventDefault();
                closeWithdraw();
                return;
            }

            if (target.closest("[data-apps-withdraw-confirm]")) {
                ev.preventDefault();
                var url = withdrawPanel ? withdrawPanel.getAttribute("data-bff-url") : "";
                if (!url || !window.jobsyBff) {
                    return;
                }
                window.jobsyBff.postForm(url, window.jobsyBff.formFromContainer(withdrawPanel), withdrawPanel)
                    .then(function (body) {
                        closeWithdraw();
                        if (body && body.redirect) {
                            window.location.assign(body.redirect);
                        } else {
                            window.location.assign("/candidate/applications?withdrawn=1");
                        }
                    })
                    .catch(function () { });
                return;
            }
        });

        root.addEventListener("keydown", function (ev) {
            if (ev.key === "Escape") {
                if (state.openMenuId) {
                    closeMenu();
                } else if (state.expandedId) {
                    closeSheet();
                }
            }
        });

        if (withdrawDialog) {
            withdrawDialog.addEventListener("cancel", function (ev) {
                ev.preventDefault();
                closeWithdraw();
            });
            withdrawDialog.addEventListener("close", function () {
                if (withdrawAppId) {
                    withdrawAppId.value = "";
                }
            });
        }

        var mql = window.matchMedia ? window.matchMedia("(min-width: 1024px)") : null;
        if (mql) {
            var onWide = function () {
                closeSheet();
                syncLayout();
            };
            if (typeof mql.addEventListener === "function") {
                mql.addEventListener("change", onWide);
            } else if (typeof mql.addListener === "function") {
                mql.addListener(onWide);
            }
        }

        applyFilter(state.filter);
    }

    function boot() {
        qsa("[data-candidate-applications]").forEach(mount);
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", boot);
    } else {
        boot();
    }

    window.jobsyCandidateApplications = {
        mount: mount
    };
})();
