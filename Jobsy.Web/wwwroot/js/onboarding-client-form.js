(function () {
    "use strict";

    var roots = typeof WeakMap === "function" ? new WeakMap() : null;
    var fallbackMap = roots;

    function applyDateMask(raw) {
        if (!raw) return "";
        var digits = String(raw).replace(/\D/g, "").slice(0, 8);
        if (!digits) return "";
        if (digits.length <= 2) return digits;
        if (digits.length <= 4) return digits.slice(0, 2) + "-" + digits.slice(2);
        return digits.slice(0, 2) + "-" + digits.slice(2, 4) + "-" + digits.slice(4);
    }

    function applyDateMaskToInput(input) {
        if (!input) return;
        var sel = input.selectionStart == null ? input.value.length : input.selectionStart;
        var digitsBefore = String(input.value.slice(0, sel)).replace(/\D/g, "").length;
        var masked = applyDateMask(input.value);
        input.value = masked;
        var pos = 0;
        var digits = 0;
        for (; pos < masked.length && digits < digitsBefore; pos++) {
            if (/\d/.test(masked.charAt(pos))) digits++;
        }
        try {
            input.setSelectionRange(pos, pos);
        } catch (e) { }
    }

    function parseFlexibleDate(text) {
        if (!text) return null;
        var raw = text.trim();
        if (!raw) return null;
        var digits = raw.replace(/\D/g, "");
        if (/^\d{4}-\d{2}-\d{2}$/.test(raw)) {
            var iso = raw.split("-");
            var yi = parseInt(iso[0], 10);
            var mi = parseInt(iso[1], 10);
            var di = parseInt(iso[2], 10);
            if (di && mi && yi) {
                return { d: di, m: mi, y: yi, text: raw };
            }
        }
        if (digits.length === 8) {
            var d8 = parseInt(digits.slice(0, 2), 10);
            var m8 = parseInt(digits.slice(2, 4), 10);
            var y8 = parseInt(digits.slice(4, 8), 10);
            if (d8 && m8 && y8) {
                return { d: d8, m: m8, y: y8, text: raw };
            }
        }
        if (raw.length < 8) return null;
        var parts = raw.replace(/\./g, "-").replace(/\//g, "-").split("-");
        if (parts.length !== 3) return null;
        var d = parseInt(parts[0], 10);
        var m = parseInt(parts[1], 10);
        var y = parseInt(parts[2], 10);
        if (y < 100) y += y >= 50 ? 1900 : 2000;
        if (!d || !m || !y) return null;
        return { d: d, m: m, y: y, text: raw };
    }

    function formatDisplayDateParts(p) {
        if (!p) return "";
        return String(p.d).padStart(2, "0") + "-" + String(p.m).padStart(2, "0") + "-" + String(p.y);
    }

    var presetDefs = {
        school: { min: 6, max: 16, days: ["Ma:Middag", "Di:Middag", "Wo:Middag", "Do:Middag", "Vr:Middag", "Za:Ochtend", "Za:Middag"] },
        weekend: { min: 8, max: 16, days: ["Za:Ochtend", "Za:Middag", "Zo:Ochtend", "Zo:Middag"] },
        evening: { min: 8, max: 20, days: ["Ma:Avond", "Di:Avond", "Wo:Avond", "Do:Avond", "Vr:Avond"] },
        office: { min: 32, max: 40, days: ["Ma:Ochtend", "Ma:Middag", "Di:Ochtend", "Di:Middag", "Wo:Ochtend", "Wo:Middag", "Do:Ochtend", "Do:Middag", "Vr:Ochtend", "Vr:Middag"] },
        holiday: { min: 16, max: 40, days: ["Ma:Ochtend", "Ma:Middag", "Di:Ochtend", "Di:Middag", "Wo:Ochtend", "Wo:Middag", "Do:Ochtend", "Do:Middag", "Vr:Ochtend", "Vr:Middag"] },
        parttime: { min: 12, max: 32, days: ["Ma:Ochtend", "Ma:Middag", "Di:Ochtend", "Di:Middag", "Wo:Ochtend", "Wo:Middag", "Do:Ochtend", "Do:Middag", "Vr:Ochtend", "Vr:Middag"] },
        fulltime: { min: 36, max: 40, days: ["Ma:Ochtend", "Ma:Middag", "Di:Ochtend", "Di:Middag", "Wo:Ochtend", "Wo:Middag", "Do:Ochtend", "Do:Middag", "Vr:Ochtend", "Vr:Middag"] },
        custom: { min: null, max: null, days: [] }
    };
    var daySpecific = { school: 1, weekend: 1, evening: 1, office: 1, holiday: 1 };
    var MIN_ADDRESS_SUGGEST_CHARS = 3;

    function computePresets(selectedSet) {
        var min = null;
        var max = null;
        var dayParts = {};
        var hasDaySpecific = false;
        selectedSet.forEach(function (code) {
            if (daySpecific[code]) hasDaySpecific = true;
        });
        selectedSet.forEach(function (code) {
            var def = presetDefs[code];
            if (!def) return;
            if (def.min != null) min = min == null ? def.min : Math.min(min, def.min);
            if (def.max != null) max = max == null ? def.max : Math.max(max, def.max);
            var contributes = daySpecific[code] || (!hasDaySpecific && (code === "parttime" || code === "fulltime"));
            if (contributes) {
                def.days.forEach(function (k) { dayParts[k] = true; });
            }
        });
        return { min: min, max: max, availability: Object.keys(dayParts) };
    }

    function readAntiforgery(root) {
        var host = (root && root.closest) ? root.closest("[data-ob-antiforgery-token]") : null;
        if (!host && root) host = root;
        if (!host) host = document.querySelector(".ob-wizard") || document.body;
        return {
            header: host.getAttribute("data-ob-antiforgery-header") || "RequestVerificationToken",
            token: host.getAttribute("data-ob-antiforgery-token") || ""
        };
    }

    function putOnboardingStep(root, step, collectFn, advance, skipOptional, goBack) {
        var body = collectFn();
        body.advance = !!advance;
        body.skipOptional = !!skipOptional;
        body.back = !!goBack;
        var af = readAntiforgery(root);
        var headers = { "Content-Type": "application/json" };
        if (af.token) headers[af.header] = af.token;
        var url = step === 1
            ? "/api/onboarding-client/step1"
            : ("/api/onboarding-client/steps/" + step);
        return fetch(url, {
            method: "PUT",
            credentials: "same-origin",
            headers: headers,
            body: JSON.stringify(body)
        });
    }

    function wireObStepBff(root, step, collectFn) {
        var nextBtn = root.querySelector("[data-ob-step-next]");
        var skipBtn = root.querySelector("[data-ob-step-skip]");
        var errEl = root.querySelector("[data-ob-step-error]");
        var saveTimer = null;

        function scheduleSave() {
            if (saveTimer) clearTimeout(saveTimer);
            saveTimer = setTimeout(function () {
                saveTimer = null;
                putOnboardingStep(root, step, collectFn, false, false, false).catch(function () { });
            }, 500);
        }

        function runAdvance(skipOptional) {
            if (errEl) {
                errEl.hidden = true;
                errEl.textContent = "";
            }
            if (nextBtn) nextBtn.disabled = true;
            if (skipBtn) skipBtn.disabled = true;
            if (saveTimer) {
                clearTimeout(saveTimer);
                saveTimer = null;
            }
            putOnboardingStep(root, step, collectFn, true, !!skipOptional, false)
                .then(function (res) {
                    return res.json().then(function (body) {
                        return { ok: res.ok, body: body };
                    }).catch(function () {
                        return { ok: res.ok, body: {} };
                    });
                })
                .then(function (result) {
                    if (result.ok && result.body && result.body.redirectUrl) {
                        window.location.assign(result.body.redirectUrl);
                        return;
                    }
                    if (result.ok) {
                        window.location.reload();
                        return;
                    }
                    var msg = (result.body && result.body.message)
                        || "Opslaan mislukt. Controleer je gegevens en probeer opnieuw.";
                    if (errEl) {
                        errEl.textContent = msg;
                        errEl.hidden = false;
                    }
                    if (nextBtn) nextBtn.disabled = false;
                    if (skipBtn) skipBtn.disabled = false;
                })
                .catch(function () {
                    if (errEl) {
                        errEl.textContent = "Verbinding mislukt. Probeer het opnieuw.";
                        errEl.hidden = false;
                    }
                    if (nextBtn) nextBtn.disabled = false;
                    if (skipBtn) skipBtn.disabled = false;
                });
        }

        if (nextBtn && !nextBtn.__obStepNextBound && !nextBtn.hasAttribute("data-ob-nav-next")) {
            nextBtn.__obStepNextBound = true;
            nextBtn.addEventListener("click", function () { runAdvance(false); });
        }
        if (skipBtn && !skipBtn.__obStepSkipBound) {
            skipBtn.__obStepSkipBound = true;
            skipBtn.addEventListener("click", function () { runAdvance(true); });
        }

        root.__obRunAdvance = runAdvance;

        return { scheduleSave: scheduleSave };
    }

    function findNavErrorEl(root) {
        if (root) {
            var local = root.querySelector("[data-ob-step-error]");
            if (local) return local;
        }
        return document.querySelector(".ob-wizard [data-ob-wizard-nav-error]");
    }

    function runStepAdvance(root, step, collectFn, skipOptional) {
        var errEl = findNavErrorEl(root);
        if (errEl) {
            errEl.hidden = true;
            errEl.textContent = "";
        }
        putOnboardingStep(root, step, collectFn, true, !!skipOptional, false)
            .then(function (res) {
                return res.json().then(function (body) {
                    return { ok: res.ok, body: body };
                }).catch(function () {
                    return { ok: res.ok, body: {} };
                });
            })
            .then(function (result) {
                if (result.ok && result.body && result.body.redirectUrl) {
                    window.location.assign(result.body.redirectUrl);
                    return;
                }
                if (result.ok) {
                    window.location.reload();
                    return;
                }
                var msg = (result.body && result.body.message)
                    || "Opslaan mislukt. Controleer je gegevens en probeer opnieuw.";
                if (errEl) {
                    errEl.textContent = msg;
                    errEl.hidden = false;
                }
            })
            .catch(function () {
                if (errEl) {
                    errEl.textContent = "Verbinding mislukt. Probeer het opnieuw.";
                    errEl.hidden = false;
                }
            });
    }

    function mountStep1(root, initial) {
        var ssrValues = root.getAttribute("data-ob-ssr-values") === "1";
        var state = {
            homeLat: initial.homeLat,
            homeLng: initial.homeLng,
            suggestGen: 0,
            strings: initial.strings || {},
            dirty: { first: false, last: false, address: false, birth: false, phone: false },
            saveTimer: null,
            saveInFlight: false
        };

        var first = root.querySelector("[data-ob-client=firstName]");
        var last = root.querySelector("[data-ob-client=lastName]");
        var address = root.querySelector("[data-ob-client=address]");
        var birth = root.querySelector("[data-ob-client=birthDate]");
        var phone = root.querySelector("[data-ob-client=phone]");
        var grid = root.querySelector("[data-ob-client=workStatusGrid]");
        var list = root.querySelector(".ob-client-suggest");
        var addressWrap = root.querySelector(".ob-fields__address");

        function bindDirty(input, key) {
            if (!input || input.__obDirtyBound) return;
            input.__obDirtyBound = true;
            input.addEventListener("input", function () {
                state.dirty[key] = true;
            });
        }

        bindDirty(first, "first");
        bindDirty(last, "last");
        bindDirty(address, "address");
        bindDirty(birth, "birth");
        bindDirty(phone, "phone");

        if (!ssrValues) {
            if (first && !state.dirty.first) first.value = initial.firstName || "";
            if (last && !state.dirty.last) last.value = initial.lastName || "";
            if (address && !state.dirty.address) address.value = initial.addressQuery || "";
            if (birth && !state.dirty.birth) birth.value = initial.birthDateText || "";
            if (phone && !state.dirty.phone) phone.value = initial.phone || "";
        }

        function collectPayload() {
            var lat = state.homeLat != null ? state.homeLat : initial.homeLat;
            var lng = state.homeLng != null ? state.homeLng : initial.homeLng;
            return {
                firstName: first ? first.value : "",
                lastName: last ? last.value : "",
                phone: phone ? phone.value : "",
                whatsAppAllowed: false,
                workStatuses: readWorkStatusesFromDom(),
                addressQuery: address ? address.value : "",
                homeLat: lat,
                homeLng: lng,
                birthDateText: birth ? birth.value : ""
            };
        }

        var bff = wireObStepBff(root, 1, collectPayload);
        function scheduleSave() {
            if (bff && bff.scheduleSave) bff.scheduleSave();
        }

        function bindSaveEvents(input) {
            if (!input || input.__obStep1SaveBound) return;
            input.__obStep1SaveBound = true;
            input.addEventListener("change", scheduleSave);
            input.addEventListener("blur", scheduleSave);
        }

        bindSaveEvents(first);
        bindSaveEvents(last);
        bindSaveEvents(address);
        bindSaveEvents(birth);
        bindSaveEvents(phone);

        function readWorkStatusesFromDom() {
            if (!grid) return [];
            var codes = [];
            grid.querySelectorAll(".work-status-chip[data-ob-code]").forEach(function (btn) {
                if (btn.getAttribute("aria-pressed") === "true") {
                    codes.push(btn.getAttribute("data-ob-code"));
                }
            });
            return codes;
        }

        function setWorkStatusPressed(btn, selected) {
            btn.classList.toggle("is-selected", selected);
            btn.setAttribute("aria-pressed", selected ? "true" : "false");
            var check = btn.querySelector(".lobsy-choice-tile__check");
            if (selected && !check) {
                btn.insertAdjacentHTML("beforeend", '<span class="lobsy-choice-tile__check" aria-hidden="true">✓</span>');
            } else if (!selected && check) {
                check.remove();
            }
        }

        function saveWorkStatusNow() {
            putOnboardingStep(root, 1, collectPayload, false, false, false).catch(function () { });
        }

        function bindWorkStatusChip(btn) {
            if (!btn || btn.__obWorkChipBound) return;
            btn.__obWorkChipBound = true;
            btn.addEventListener("click", function (ev) {
                ev.preventDefault();
                ev.stopPropagation();
                var selected = btn.getAttribute("aria-pressed") === "true";
                setWorkStatusPressed(btn, !selected);
                saveWorkStatusNow();
            }, true);
        }

        function ensureWorkStatusGrid() {
            if (!grid) return;
            var chips = grid.querySelectorAll(".work-status-chip[data-ob-code]");
            if (chips.length === 0) {
                (initial.workStatusOptions || []).forEach(function (opt) {
                    var btn = document.createElement("button");
                    btn.type = "button";
                    btn.className = "lobsy-choice-tile work-status-chip";
                    btn.setAttribute("data-ob-code", opt.code);
                    var selected = (initial.workStatusesSelected || []).indexOf(opt.code) >= 0;
                    btn.setAttribute("aria-pressed", selected ? "true" : "false");
                    if (selected) btn.classList.add("is-selected");
                    var iconHtml = opt.icon ? ('<span class="lobsy-choice-tile__icon" aria-hidden="true">' + opt.icon + "</span>") : "";
                    var checkHtml = selected ? '<span class="lobsy-choice-tile__check" aria-hidden="true">✓</span>' : "";
                    btn.innerHTML = iconHtml + '<span class="lobsy-choice-tile__label">' + escapeHtml(opt.label) + "</span>" + checkHtml;
                    grid.appendChild(btn);
                });
                chips = grid.querySelectorAll(".work-status-chip[data-ob-code]");
            }

            chips.forEach(bindWorkStatusChip);
        }

        ensureWorkStatusGrid();

        if (birth && !birth.__obBirthMask) {
            birth.__obBirthMask = true;
            birth.addEventListener("input", function () {
                applyDateMaskToInput(birth);
            });
        }

        var suggestReposition = null;

        function hideSuggest() {
            if (!list) return;
            list.hidden = true;
            list.classList.remove("ob-client-suggest--floating");
            list.style.cssText = "";
            if (addressWrap) addressWrap.classList.remove("is-suggest-open");
            if (address) address.setAttribute("aria-expanded", "false");
            if (suggestReposition) {
                window.removeEventListener("scroll", suggestReposition, true);
                window.removeEventListener("resize", suggestReposition);
                suggestReposition = null;
            }
        }

        function positionSuggestList() {
            if (!list || !address || list.hidden) return;
            var r = address.getBoundingClientRect();
            list.style.position = "fixed";
            list.style.left = r.left + "px";
            list.style.top = (r.bottom + 4) + "px";
            list.style.width = Math.max(r.width, 200) + "px";
            list.style.zIndex = "10000";
        }

        function showSuggest(html) {
            if (!list) return;
            list.innerHTML = html;
            list.hidden = false;
            list.classList.add("ob-client-suggest--floating");
            if (addressWrap) addressWrap.classList.add("is-suggest-open");
            if (address) address.setAttribute("aria-expanded", "true");
            positionSuggestList();
            if (!suggestReposition) {
                suggestReposition = function () { positionSuggestList(); };
                window.addEventListener("scroll", suggestReposition, true);
                window.addEventListener("resize", suggestReposition);
            }
        }

        var debounceTimer = null;
        if (address) {
            address.addEventListener("input", function () {
                state.homeLat = null;
                state.homeLng = null;
                var q = address.value.trim();
                if (debounceTimer) clearTimeout(debounceTimer);
                if (q.length < MIN_ADDRESS_SUGGEST_CHARS) {
                    hideSuggest();
                    return;
                }
                debounceTimer = setTimeout(function () { fetchSuggest(q); }, 300);
            });
            address.addEventListener("blur", function () {
                setTimeout(hideSuggest, 200);
            });
        }

        function fetchSuggest(q) {
            if (!q || q.trim().length < MIN_ADDRESS_SUGGEST_CHARS) {
                hideSuggest();
                return;
            }
            var gen = ++state.suggestGen;
            var loading = state.strings.suggestLoading || "Laden…";
            var empty = state.strings.suggestEmpty || "Geen resultaten";
            var fallback = state.strings.suggestFallback || "Geen suggesties, typ je adres verder";
            showSuggest("<li class=\"address-suggestions__status\" role=\"option\">" + loading + "</li>");

            var ctrl = new AbortController();
            var timer = setTimeout(function () { ctrl.abort(); }, 4500);

            fetch("/api/onboarding-client/address-suggest?q=" + encodeURIComponent(q), {
                credentials: "same-origin",
                signal: ctrl.signal
            })
                .then(function (r) { return r.ok ? r.json() : []; })
                .then(function (items) {
                    clearTimeout(timer);
                    if (gen !== state.suggestGen) return;
                    if (!items || !items.length) {
                        showSuggest("<li class=\"address-suggestions__status\" role=\"option\">" + (fallback || empty) + "</li>");
                        return;
                    }
                    var html = items.map(function (it, i) {
                        return "<li role=\"option\"><button type=\"button\" class=\"address-suggestions__item\" data-idx=\"" + i + "\">" +
                            escapeHtml(it.label) + "</button></li>";
                    }).join("");
                    showSuggest(html);
                    list.querySelectorAll(".address-suggestions__item").forEach(function (btn) {
                        btn.addEventListener("mousedown", function (ev) { ev.preventDefault(); });
                        btn.addEventListener("click", function () {
                            var idx = parseInt(btn.getAttribute("data-idx"), 10);
                            var pick = items[idx];
                            if (!pick) return;
                            address.value = pick.label;
                            state.homeLat = pick.latitude;
                            state.homeLng = pick.longitude;
                            hideSuggest();
                            scheduleSave();
                        });
                    });
                })
                .catch(function () {
                    clearTimeout(timer);
                    if (gen !== state.suggestGen) return;
                    showSuggest("<li class=\"address-suggestions__status\" role=\"option\">" + fallback + "</li>");
                });
        }

        state.collect = collectPayload;

        return state;
    }

    function escapeHtml(s) {
        return String(s).replace(/&/g, "&amp;").replace(/</g, "&lt;").replace(/"/g, "&quot;");
    }

    function mountStep2(root, initial) {
        var state = {
            presets: new Set(initial.presets || []),
            presetsOverridden: !!initial.presetsOverridden,
            fineTune: !!initial.fineTune,
            availability: new Set(initial.availability || []),
            minHours: initial.minHours || 12,
            maxHours: initial.maxHours || 40,
            immediate: initial.immediate !== false,
            availableFrom: initial.availableFrom || ""
        };

        var presetGrid = root.querySelector("[data-ob-client=presetGrid]");
        var matrix = root.querySelector("[data-ob-client=matrix]");
        var minR = root.querySelector("[data-ob-client=hoursMin]");
        var maxR = root.querySelector("[data-ob-client=hoursMax]");
        var hoursLabel = root.querySelector("[data-ob-client=hoursLabel]");
        var startImmediate = root.querySelector("[data-ob-client=startImmediate]");
        var startFromDate = root.querySelector("[data-ob-client=startFromDate]");
        var availFrom = root.querySelector("[data-ob-client=availableFrom]")
            || root.querySelector("#onboarding-available-from");

        function isoFromParsed(p) {
            if (!p) return null;
            return p.y + "-" + String(p.m).padStart(2, "0") + "-" + String(p.d).padStart(2, "0");
        }

        function parsedFromAvailInput() {
            if (!availFrom) return null;
            return parseFlexibleDate(availFrom.value);
        }

        function activateFromDateMode() {
            state.immediate = false;
            if (startImmediate) startImmediate.checked = false;
            if (startFromDate) startFromDate.checked = true;
            if (availFrom) availFrom.hidden = false;
        }

        function normalizeAvailFromField() {
            if (!availFrom) return null;
            var p = parseFlexibleDate(availFrom.value);
            if (!p) return null;
            availFrom.value = formatDisplayDateParts(p);
            activateFromDateMode();
            return p;
        }

        function collectPayload() {
            var p = normalizeAvailFromField() || parsedFromAvailInput();
            var fromIso = isoFromParsed(p);
            var immediate = fromIso ? false : state.immediate;
            return {
                presets: Array.from(state.presets),
                presetsOverridden: state.presetsOverridden,
                fineTune: state.fineTune,
                minHours: state.minHours,
                maxHours: state.maxHours,
                availability: Array.from(state.availability),
                immediate: immediate,
                availableFrom: fromIso
            };
        }

        var bff = wireObStepBff(root, 2, collectPayload);
        var ssrForm = root.closest("form[data-ob-ssr-form]");
        function saveNow() {
            if (ssrForm) {
                putOnboardingStep(root, 2, collectPayload, false, false, false).catch(function () { });
                return;
            }
            if (bff && bff.scheduleSave) bff.scheduleSave();
        }

        function syncHoursUi() {
            if (minR) minR.value = state.minHours;
            if (maxR) maxR.value = state.maxHours;
            if (hoursLabel) hoursLabel.textContent = state.minHours + " – " + state.maxHours;
            var span = Math.max(1, 59);
            var lowPct = ((state.minHours - 1) * 100) / span;
            var highPct = ((state.maxHours - 1) * 100) / span;
            var rail = root.querySelector(".hours-range");
            if (rail) rail.style.cssText = "--hours-low:" + lowPct + "%;--hours-high:" + highPct + "%;";
        }

        function syncMatrixUi() {
            if (!matrix) return;
            matrix.querySelectorAll(".availability-matrix__cell--btn[data-ob-key]").forEach(function (btn) {
                var key = btn.getAttribute("data-ob-key");
                var on = state.availability.has(key);
                btn.classList.toggle("is-on", on);
                btn.setAttribute("aria-pressed", on ? "true" : "false");
            });
        }

        function syncPresetUi() {
            if (!presetGrid) return;
            presetGrid.querySelectorAll(".lobsy-choice-tile[data-ob-code]").forEach(function (btn) {
                var code = btn.getAttribute("data-ob-code");
                var on = state.presets.has(code);
                btn.setAttribute("aria-pressed", on ? "true" : "false");
                btn.classList.toggle("is-selected", on);
            });
        }

        function applyPresetCompute() {
            if (state.presets.has("custom") || state.presetsOverridden) return;
            var c = computePresets(state.presets);
            if (c.min != null) state.minHours = c.min;
            if (c.max != null) state.maxHours = c.max;
            state.availability = new Set(c.availability);
            syncHoursUi();
            syncMatrixUi();
        }

        function applyPresetAvailabilityOnly() {
            if (state.presets.has("custom") || state.presetsOverridden) return;
            var c = computePresets(state.presets);
            state.availability = new Set(c.availability);
            syncMatrixUi();
        }

        function togglePresetCode(code) {
            if (code === "custom") {
                if (state.presets.has("custom")) state.presets.delete("custom");
                else {
                    state.presets.clear();
                    state.presets.add("custom");
                    state.fineTune = true;
                    state.presetsOverridden = true;
                }
                if (!ssrForm) applyPresetCompute();
            } else {
                state.presets.delete("custom");
                if (state.presets.has(code)) state.presets.delete(code);
                else state.presets.add(code);
                if (ssrForm) {
                    applyPresetAvailabilityOnly();
                } else {
                    state.presetsOverridden = false;
                    applyPresetCompute();
                }
            }
            syncPresetUi();
            saveNow();
        }

        function bindPresetGridClicks() {
            if (!presetGrid || presetGrid.__obPresetClick) return;
            presetGrid.__obPresetClick = true;
            presetGrid.addEventListener("click", function (ev) {
                var btn = ev.target.closest(".lobsy-choice-tile[data-ob-code]");
                if (!btn || !presetGrid.contains(btn)) return;
                ev.preventDefault();
                ev.stopPropagation();
                togglePresetCode(btn.getAttribute("data-ob-code"));
            }, true);
        }

        function ensurePresets() {
            if (!presetGrid) return;
            if (presetGrid.children.length > 0) {
                bindPresetGridClicks();
                syncPresetUi();
                return;
            }
            if (presetGrid.getAttribute("data-ob-preset-bound") === "1") {
                syncPresetUi();
                return;
            }
            presetGrid.setAttribute("data-ob-preset-bound", "1");
            presetGrid.innerHTML = "";
            (initial.presetTiles || []).forEach(function (tile) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "lobsy-choice-tile";
                btn.setAttribute("data-ob-code", tile.code);
                btn.setAttribute("aria-pressed", state.presets.has(tile.code) ? "true" : "false");
                btn.className = "lobsy-choice-tile lobsy-choice-tile--tall";
                btn.innerHTML = "<span class=\"lobsy-choice-tile__stack\"><strong>" + escapeHtml(tile.title) + "</strong><small>" + escapeHtml(tile.lead) + "</small></span>";
                presetGrid.appendChild(btn);
            });
            bindPresetGridClicks();
        }

        function bindMatrixClicks() {
            if (!matrix || matrix.__obMatrixClick) return;
            matrix.__obMatrixClick = true;
            matrix.addEventListener("click", function (ev) {
                var btn = ev.target.closest(".availability-matrix__cell--btn[data-ob-key]");
                if (!btn || !matrix.contains(btn)) return;
                ev.preventDefault();
                ev.stopPropagation();
                var key = btn.getAttribute("data-ob-key");
                if (state.availability.has(key)) state.availability.delete(key);
                else state.availability.add(key);
                state.presetsOverridden = true;
                state.presets.clear();
                state.presets.add("custom");
                state.fineTune = true;
                syncPresetUi();
                syncMatrixUi();
                saveNow();
            }, true);
        }

        function ensureMatrix() {
            if (!matrix) return;
            if (matrix.querySelector(".availability-matrix__cell--btn")) {
                bindMatrixClicks();
                syncMatrixUi();
                return;
            }
            if (matrix.getAttribute("data-ob-matrix-bound") === "1") {
                syncMatrixUi();
                return;
            }
            matrix.setAttribute("data-ob-matrix-bound", "1");
            matrix.innerHTML = "";
            var days = initial.days || [];
            var slots = initial.slots || [];
            var dayLabels = initial.dayLabels || {};
            var slotLabels = initial.slotLabels || {};
            var corner = document.createElement("div");
            corner.className = "availability-matrix__corner";
            matrix.appendChild(corner);
            slots.forEach(function (slot) {
                var h = document.createElement("div");
                h.className = "availability-matrix__slot";
                h.textContent = slotLabels[slot] || slot;
                matrix.appendChild(h);
            });
            days.forEach(function (day) {
                var dh = document.createElement("div");
                dh.className = "availability-matrix__day";
                dh.textContent = dayLabels[day] || day;
                matrix.appendChild(dh);
                slots.forEach(function (slot) {
                    var key = day + ":" + slot;
                    var on = state.availability.has(key);
                    var btn = document.createElement("button");
                    btn.type = "button";
                    btn.className = "availability-matrix__cell availability-matrix__cell--btn" + (on ? " is-on" : "");
                    btn.setAttribute("data-ob-key", key);
                    btn.setAttribute("aria-pressed", on ? "true" : "false");
                    matrix.appendChild(btn);
                });
            });
            bindMatrixClicks();
        }

        if (initial.availableFrom && availFrom) {
            var initP = parseFlexibleDate(String(initial.availableFrom));
            if (initP) {
                availFrom.value = formatDisplayDateParts(initP);
                activateFromDateMode();
            }
        } else if (availFrom && availFrom.value) {
            normalizeAvailFromField();
        }

        ensurePresets();
        ensureMatrix();
        syncHoursUi();

        function onHoursInput() {
            state.minHours = minR ? parseInt(minR.value, 10) : state.minHours;
            state.maxHours = maxR ? parseInt(maxR.value, 10) : state.maxHours;
            if (state.minHours > state.maxHours) state.minHours = state.maxHours;
            state.presetsOverridden = true;
            state.presets.clear();
            state.presets.add("custom");
            syncHoursUi();
            syncPresetUi();
            saveNow();
        }
        if (minR) minR.addEventListener("input", onHoursInput);
        if (maxR) maxR.addEventListener("input", onHoursInput);

        if (startImmediate) {
            startImmediate.addEventListener("change", function () {
                state.immediate = true;
                if (availFrom) availFrom.hidden = true;
                saveNow();
            });
        }
        if (startFromDate) {
            startFromDate.addEventListener("change", function () {
                state.immediate = false;
                if (availFrom) availFrom.hidden = false;
                saveNow();
            });
        }
        function onAvailFromEdited() {
            if (normalizeAvailFromField()) saveNow();
        }

        if (availFrom) {
            availFrom.addEventListener("change", onAvailFromEdited);
            availFrom.addEventListener("blur", onAvailFromEdited);
            availFrom.addEventListener("keydown", function (ev) {
                if (ev.key === "Tab" || ev.key === "Enter") onAvailFromEdited();
            });
            availFrom.addEventListener("input", function () {
                if (availFrom.value && availFrom.value.trim().length >= 8) activateFromDateMode();
            });
        }

        return { collect: collectPayload, scheduleSave: saveNow };
    }

    function paintChoiceGrid(container, options, selectedSet, multi) {
        if (!container) return;
        container.innerHTML = "";
        (options || []).forEach(function (opt) {
            var code = opt.code || opt;
            var label = opt.label || opt;
            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "lobsy-choice-tile";
            btn.setAttribute("data-ob-code", code);
            var on = selectedSet.has(code);
            btn.setAttribute("aria-pressed", on ? "true" : "false");
            if (on) btn.classList.add("is-selected");
            btn.textContent = label;
            container.appendChild(btn);
        });
    }

    function syncChoiceTileVisual(btn, on) {
        btn.setAttribute("aria-pressed", on ? "true" : "false");
        btn.classList.toggle("is-selected", on);
        if (!btn.querySelector(".lobsy-choice-tile__label")) return;
        var check = btn.querySelector(".lobsy-choice-tile__check");
        if (on && !check) {
            btn.insertAdjacentHTML("beforeend", '<span class="lobsy-choice-tile__check" aria-hidden="true">✓</span>');
        } else if (!on && check) {
            check.remove();
        }
    }

    function choiceCodeMatches(a, b) {
        return String(a || "") === String(b || "");
    }

    function choiceSetHas(selectedSet, code) {
        if (!code) return false;
        var found = false;
        selectedSet.forEach(function (c) {
            if (choiceCodeMatches(c, code)) found = true;
        });
        return found;
    }

    function choiceSetToggle(selectedSet, code) {
        if (!code) return;
        var existing = null;
        selectedSet.forEach(function (c) {
            if (choiceCodeMatches(c, code)) existing = c;
        });
        if (existing !== null) selectedSet.delete(existing);
        else selectedSet.add(String(code));
    }

    function readSelectedCodesFromGrid(container) {
        var codes = [];
        if (!container) return codes;
        container.querySelectorAll(".lobsy-choice-tile[data-ob-code]").forEach(function (btn) {
            var code = btn.getAttribute("data-ob-code");
            if (!code) return;
            var on = btn.getAttribute("aria-pressed") === "true" || btn.classList.contains("is-selected");
            if (on) codes.push(code);
        });
        return codes;
    }

    function bindChoiceTileActivation(container, tileSelector, handler) {
        if (!container || container.__obTileActivationBound) return;
        container.__obTileActivationBound = true;
        var lastAt = 0;
        var lastCode = "";
        var lastPointerAt = 0;
        function run(ev) {
            if (ev.type === "pointerup" && ev.pointerType === "mouse" && ev.button !== 0) return;
            if (ev.type === "click" && Date.now() - lastPointerAt < 450) return;
            var btn = ev.target.closest(tileSelector);
            if (!btn || !container.contains(btn)) return;
            ev.preventDefault();
            ev.stopPropagation();
            var code = btn.getAttribute("data-ob-code") || btn.getAttribute("data-ob-travel") || "";
            var now = Date.now();
            if (code === lastCode && now - lastAt < 400) return;
            lastAt = now;
            lastCode = code;
            if (ev.type === "pointerup") lastPointerAt = now;
            handler(btn, ev);
        }
        container.addEventListener("pointerup", run, true);
        container.addEventListener("click", run, true);
    }

    function bindChoiceTileClickOnly(container, tileSelector, handler) {
        if (!container || container.__obTileClickBound) return;
        container.__obTileClickBound = true;
        container.addEventListener("click", function (ev) {
            var btn = ev.target.closest(tileSelector);
            if (!btn || !container.contains(btn)) return;
            ev.preventDefault();
            ev.stopPropagation();
            handler(btn, ev);
        }, true);
    }

    function syncChoiceGridPressed(container, selectedSet, multi) {
        if (!container) return;
        container.querySelectorAll(".lobsy-choice-tile[data-ob-code]").forEach(function (btn) {
            var code = btn.getAttribute("data-ob-code");
            syncChoiceTileVisual(btn, choiceSetHas(selectedSet, code));
        });
    }

    function updateTravelLegend(root, minutes) {
        if (!root) return;
        var el = root.querySelector("[data-ob-client=travelLegendValue]");
        if (el) el.textContent = String(minutes);
    }

    function mountChoiceGrid(container, options, selectedSet, multi, onChange) {
        if (!container || container.getAttribute("data-ob-choice-bound") === "1") return;
        container.setAttribute("data-ob-choice-bound", "1");
        paintChoiceGrid(container, options, selectedSet, multi);
        bindChoiceTileActivation(container, ".lobsy-choice-tile[data-ob-code]", function (btn) {
            var code = btn.getAttribute("data-ob-code");
            if (multi) {
                choiceSetToggle(selectedSet, code);
            } else {
                selectedSet.clear();
                selectedSet.add(String(code));
            }
            syncChoiceGridPressed(container, selectedSet, multi);
            if (onChange) onChange();
        });
    }

    function readChoiceCodes(container) {
        return readSelectedCodesFromGrid(container);
    }

    function mountStep3(root, initial) {
        var tGridEarly = root.querySelector("[data-ob-client=transportGrid]");
        var domTransport = readSelectedCodesFromGrid(tGridEarly);
        var initialTransport = initial.transport || [];
        var selected = new Set(domTransport.length ? domTransport : initialTransport);
        var travel = initial.maxTravel || 30;
        var custom = !!initial.customTravel;
        var tGrid = root.querySelector("[data-ob-client=transportGrid]");
        var vGrid = root.querySelector("[data-ob-client=travelGrid]");
        var customInput = root.querySelector("[data-ob-client=customTravel]");
        var showCustomBtn = root.querySelector("[data-ob-client=showCustomTravel]");
        var ssrForm = root.closest("form[data-ob-ssr-form]");

        function collectPayload() {
            var transport = readChoiceCodes(tGrid);
            var pickedTravel = null;
            var isCustom = custom;
            if (vGrid) {
                var pressed = vGrid.querySelector('.lobsy-choice-tile[data-ob-travel].is-selected')
                    || vGrid.querySelector('.lobsy-choice-tile[data-ob-travel][aria-pressed="true"]');
                if (pressed) {
                    isCustom = false;
                    pickedTravel = parseInt(pressed.getAttribute("data-ob-travel"), 10);
                }
            }
            if (customInput && !customInput.hidden && customInput.value) {
                isCustom = true;
                pickedTravel = parseInt(customInput.value, 10) || pickedTravel || travel;
            }
            var maxT = pickedTravel || travel;
            return {
                transport: transport,
                maxTravel: maxT,
                selectedTravel: isCustom ? null : maxT,
                customTravel: isCustom
            };
        }

        function saveNow() {
            if (ssrForm) {
                var pending = putOnboardingStep(root, 3, collectPayload, false, false, false);
                root.__obStep3SaveInflight = pending;
                pending.catch(function () { });
                return pending;
            }
        }

        function bindTransportGrid() {
            if (!tGrid || tGrid.__obTransportBound) return;
            tGrid.__obTransportBound = true;
            if (!tGrid.querySelector(".lobsy-choice-tile[data-ob-code]")) {
                mountChoiceGrid(tGrid, initial.transportOptions || [], selected, true, saveNow);
                return;
            }
            syncChoiceGridPressed(tGrid, selected, true);
            var bindTiles = ssrForm ? bindChoiceTileClickOnly : bindChoiceTileActivation;
            bindTiles(tGrid, ".lobsy-choice-tile[data-ob-code]", function (btn) {
                var code = btn.getAttribute("data-ob-code");
                choiceSetToggle(selected, code);
                syncChoiceGridPressed(tGrid, selected, true);
                saveNow();
            });
        }

        function bindTravelGrid() {
            if (!vGrid || vGrid.__obTravelBound) return;
            vGrid.__obTravelBound = true;
            if (!vGrid.querySelector(".lobsy-choice-tile[data-ob-travel]")) {
                vGrid.innerHTML = "";
                (initial.travelChoices || [15, 30, 45, 60]).forEach(function (m) {
                    var btn = document.createElement("button");
                    btn.type = "button";
                    btn.className = "lobsy-choice-tile";
                    btn.setAttribute("data-ob-travel", String(m));
                    btn.setAttribute("aria-pressed", (!custom && travel === m) ? "true" : "false");
                    btn.textContent = String(m) + " min";
                    vGrid.appendChild(btn);
                });
            }
            var bindTravel = ssrForm ? bindChoiceTileClickOnly : bindChoiceTileActivation;
            bindTravel(vGrid, ".lobsy-choice-tile[data-ob-travel]", function (btn) {
                custom = false;
                travel = parseInt(btn.getAttribute("data-ob-travel"), 10) || travel;
                if (customInput) customInput.hidden = true;
                vGrid.querySelectorAll(".lobsy-choice-tile[data-ob-travel]").forEach(function (b) {
                    var on = b === btn;
                    b.setAttribute("aria-pressed", on ? "true" : "false");
                    b.classList.toggle("is-selected", on);
                });
                updateTravelLegend(root, travel);
                saveNow();
            });
        }

        bindTransportGrid();
        bindTravelGrid();
        updateTravelLegend(root, travel);
        if (customInput) {
            customInput.hidden = !custom;
            customInput.value = String(travel);
            customInput.addEventListener("input", function () {
                custom = true;
                travel = parseInt(customInput.value, 10) || travel;
                updateTravelLegend(root, travel);
                saveNow();
            });
            customInput.addEventListener("change", function () {
                custom = true;
                travel = parseInt(customInput.value, 10) || travel;
                updateTravelLegend(root, travel);
                saveNow();
            });
        }
        if (showCustomBtn && !showCustomBtn.__obShowCustomBound) {
            showCustomBtn.__obShowCustomBound = true;
            showCustomBtn.addEventListener("click", function (ev) {
                ev.preventDefault();
                custom = true;
                if (customInput) {
                    customInput.hidden = false;
                    customInput.focus();
                    travel = parseInt(customInput.value, 10) || travel;
                }
                if (vGrid) {
                    vGrid.querySelectorAll(".lobsy-choice-tile[data-ob-travel]").forEach(function (b) {
                        b.setAttribute("aria-pressed", "false");
                        b.classList.remove("is-selected");
                    });
                }
                updateTravelLegend(root, travel);
                saveNow();
            });
        }

        if (!ssrForm) {
            wireObStepBff(root, 3, collectPayload);
        }

        return { collect: collectPayload, scheduleSave: saveNow };
    }

    function mountStep4(root, initial) {
        var ssrForm = root.closest("form[data-ob-ssr-form]");
        var ssrValues = root.getAttribute("data-ob-ssr-values") === "1";
        var noWorkEl = root.querySelector("[data-ob-client=noWork]");
        var jobs = (initial.jobs || []).slice();
        var list = root.querySelector("[data-ob-client=jobsList]");
        var addBtn = root.querySelector("[data-ob-client=addJob]");

        function readJobsFromDom() {
            var out = [];
            if (!list) return out;
            list.querySelectorAll("input[data-field=employer]").forEach(function (inp) {
                out.push({
                    employer: inp.value || "",
                    startMonth: null,
                    endMonth: null,
                    isCurrent: false
                });
            });
            return out;
        }

        function collectPayload() {
            var noWork = noWorkEl ? noWorkEl.checked : false;
            return { noWork: noWork, jobs: noWork ? [] : readJobsFromDom() };
        }

        function saveNow() {
            if (ssrForm) {
                putOnboardingStep(root, 4, collectPayload, false, false, false).catch(function () { });
            }
        }

        function bindJobInput(inp) {
            if (!inp || inp.__obJobInputBound) return;
            inp.__obJobInputBound = true;
            inp.addEventListener("input", saveNow);
            inp.addEventListener("change", saveNow);
        }

        function appendJobRow(employer) {
            if (!list) return;
            var template = list.querySelector(".ob-client-job");
            var row;
            if (template) {
                row = template.cloneNode(true);
                var inp = row.querySelector("input[data-field=employer]");
                if (inp) inp.value = employer || "";
            } else {
                row = document.createElement("div");
                row.className = "ob-client-job ob-wizard__job ob-job-card";
                row.innerHTML = "<label><input type=\"text\" data-field=\"employer\" data-ob-client=\"jobEmployer\" value=\""
                    + escapeHtml(employer || "") + "\" autocomplete=\"organization\" /></label>";
            }
            row.setAttribute("data-job-index", String(list.querySelectorAll(".ob-client-job").length));
            list.appendChild(row);
            bindJobInput(row.querySelector("input[data-field=employer]"));
        }

        function renderWizardJobs() {
            if (!list) return;
            list.innerHTML = "";
            jobs.forEach(function (job, idx) {
                var row = document.createElement("div");
                row.className = "ob-client-job";
                row.innerHTML = "<input data-idx=\"" + idx + "\" data-field=\"employer\" value=\"" + escapeHtml(job.employer || "") + "\" />";
                list.appendChild(row);
                row.querySelector("input").addEventListener("input", function (ev) {
                    jobs[idx].employer = ev.target.value;
                });
            });
        }

        if (ssrValues) {
            if (list) {
                list.querySelectorAll("input[data-field=employer]").forEach(bindJobInput);
            }
        } else {
            if (noWorkEl) noWorkEl.checked = !!initial.noWork;
            renderWizardJobs();
            if (addBtn) addBtn.addEventListener("click", function () {
                jobs.push({ employer: "", startMonth: null, endMonth: null, isCurrent: false });
                renderWizardJobs();
            });
        }

        if (noWorkEl && !noWorkEl.__obNoWorkBound) {
            noWorkEl.__obNoWorkBound = true;
            noWorkEl.addEventListener("change", function () {
                if (noWorkEl.checked && list) {
                    list.innerHTML = "";
                } else if (!noWorkEl.checked && list && !list.querySelector(".ob-client-job")) {
                    appendJobRow("");
                }
                saveNow();
            });
        }

        if (addBtn && !addBtn.__obAddJobBound) {
            addBtn.__obAddJobBound = true;
            addBtn.addEventListener("click", function (ev) {
                ev.preventDefault();
                if (noWorkEl && noWorkEl.checked) return;
                if (ssrValues) {
                    appendJobRow("");
                    saveNow();
                } else {
                    jobs.push({ employer: "", startMonth: null, endMonth: null, isCurrent: false });
                    renderWizardJobs();
                }
            });
        }

        if (!ssrForm) {
            wireObStepBff(root, 4, collectPayload);
        }

        return { collect: collectPayload, scheduleSave: saveNow };
    }

    function mountStep5(root, initial) {
        var ssrForm = root.closest("form[data-ob-ssr-form]");
        var selected = new Set();
        if (initial.education) selected.add(initial.education);
        var grid = root.querySelector("[data-ob-client=educationGrid]");

        function collectPayload() {
            var codes = readChoiceCodes(grid);
            return { education: codes.length ? codes[0] : "" };
        }

        function saveNow() {
            if (ssrForm) {
                putOnboardingStep(root, 5, collectPayload, false, false, false).catch(function () { });
            }
        }

        function bindEducationGrid() {
            if (!grid || grid.__obEducationBound) return;
            grid.__obEducationBound = true;
            var domCodes = readSelectedCodesFromGrid(grid);
            if (domCodes.length) {
                selected.clear();
                domCodes.forEach(function (c) { selected.add(c); });
            }
            if (!grid.querySelector(".lobsy-choice-tile[data-ob-code]")) {
                mountChoiceGrid(grid, initial.levels || [], selected, false, saveNow);
                return;
            }
            syncChoiceGridPressed(grid, selected, false);
            bindChoiceTileActivation(grid, ".lobsy-choice-tile[data-ob-code]", function (btn) {
                selected.clear();
                selected.add(btn.getAttribute("data-ob-code"));
                syncChoiceGridPressed(grid, selected, false);
                saveNow();
            });
        }

        bindEducationGrid();

        if (!ssrForm) {
            wireObStepBff(root, 5, collectPayload);
        }

        return { collect: collectPayload, scheduleSave: saveNow };
    }

    function mountStep6(root, initial) {
        if (root.closest("form[data-ob-ssr-form]")) {
            var ssrInput = root.querySelector('input[name="dream"]');
            return {
                collect: function () {
                    var val = ssrInput ? ssrInput.value.trim() : "";
                    return { dream: val, dreamSearch: val };
                }
            };
        }

        var input = root.querySelector("[data-ob-client=dreamInput]");
        var grid = root.querySelector("[data-ob-client=dreamGrid]");

        function collectPayload() {
            var val = input ? input.value.trim() : "";
            return { dream: val, dreamSearch: val };
        }

        function saveNow() {
            putOnboardingStep(root, 6, collectPayload, false, false, false).catch(function () { });
        }

        function syncDreamTiles(active) {
            if (!grid) return;
            var needle = String(active || "").trim();
            grid.querySelectorAll(".ob-rustig__dream[data-ob-dream]").forEach(function (btn) {
                var title = btn.getAttribute("data-ob-dream") || btn.textContent || "";
                var on = needle.length > 0 && String(title).trim().toLowerCase() === needle.toLowerCase();
                btn.classList.toggle("is-on", on);
                btn.classList.toggle("is-selected", on);
                btn.setAttribute("aria-pressed", on ? "true" : "false");
            });
        }

        function bindDreamGrid() {
            if (!grid || grid.__obDreamGridBound) return;
            grid.__obDreamGridBound = true;
            if (!grid.querySelector(".ob-rustig__dream[data-ob-dream]")) {
                grid.innerHTML = "";
                (initial.dreamTitles || []).forEach(function (title) {
                    var btn = document.createElement("button");
                    btn.type = "button";
                    btn.className = "ob-rustig__dream lobsy-choice-tile";
                    btn.setAttribute("data-ob-dream", title);
                    btn.setAttribute("aria-pressed", "false");
                    btn.textContent = title;
                    grid.appendChild(btn);
                });
            }

            syncDreamTiles(input ? input.value.trim() : "");
            bindChoiceTileActivation(grid, ".ob-rustig__dream[data-ob-dream]", function (btn) {
                var title = btn.getAttribute("data-ob-dream") || "";
                if (input) {
                    input.value = title;
                }
                syncDreamTiles(title);
                saveNow();
            });
        }

        bindDreamGrid();

        if (input && !input.__obDreamInputBound) {
            input.__obDreamInputBound = true;
            input.value = initial.dreamSearch || initial.dream || "";
            input.addEventListener("input", function () {
                syncDreamTiles(input.value.trim());
                saveNow();
            });
        }

        wireObStepBff(root, 6, collectPayload);

        return { collect: collectPayload, scheduleSave: saveNow };
    }

    function readMiniAnswersFromDom(host) {
        var map = {};
        if (!host) return map;
        host.querySelectorAll(".ob-client-mini-q").forEach(function (card) {
            var qid = parseInt(card.getAttribute("data-ob-qid"), 10);
            if (!qid) return;
            var pressed = card.querySelector('.ob-client-mini-likert .lobsy-choice-tile[aria-pressed="true"]');
            if (pressed) {
                map[qid] = parseInt(pressed.textContent, 10);
            }
        });
        return map;
    }

    function mountMiniHost(host, initial) {
        if (!host || host.getAttribute("data-ob-mini-bound") === "1") return;
        host.setAttribute("data-ob-mini-bound", "1");
        host.innerHTML = "";
        (initial.questions || []).forEach(function (q) {
            var card = document.createElement("div");
            card.className = "ob-client-mini-q";
            card.setAttribute("data-ob-qid", String(q.id));
            card.innerHTML = "<p>" + escapeHtml(q.text) + "</p>";
            var row = document.createElement("div");
            row.className = "ob-client-mini-likert";
            var saved = (initial.answers || {})[q.id];
            for (var v = 1; v <= 5; v++) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "lobsy-choice-tile";
                btn.setAttribute("data-ob-likert", String(v));
                btn.setAttribute("aria-pressed", saved === v ? "true" : "false");
                btn.textContent = String(v);
                row.appendChild(btn);
            }
            card.appendChild(row);
            host.appendChild(card);
        });
        host.addEventListener("click", function (ev) {
            var btn = ev.target.closest(".lobsy-choice-tile[data-ob-likert]");
            if (!btn) return;
            var card = btn.closest(".ob-client-mini-q");
            if (!card) return;
            card.querySelectorAll(".lobsy-choice-tile[data-ob-likert]").forEach(function (b) {
                b.setAttribute("aria-pressed", b === btn ? "true" : "false");
            });
        });
    }

    function readMiniAnswersFromForm(form) {
        var map = {};
        if (!form) return map;
        form.querySelectorAll('input[name^="miniAnswers["]').forEach(function (input) {
            var name = input.getAttribute("name") || "";
            var m = name.match(/^miniAnswers\[(\d+)\]$/);
            if (!m) return;
            var qid = parseInt(m[1], 10);
            var val = parseInt(input.value, 10);
            if (qid && val >= 1 && val <= 5) map[qid] = val;
        });
        return map;
    }

    function mountStep7Combined(root, initial) {
        if (root.closest("form[data-ob-ssr-form]")) {
            var form = root.closest("form[data-ob-ssr-form]");
            return {
                collect: function () {
                    var consentBox = form ? form.querySelector('input[name="consentRequested"]') : null;
                    return {
                        consentRequested: !!(consentBox && consentBox.checked),
                        answers: readMiniAnswersFromForm(form)
                    };
                }
            };
        }

        var box = root.querySelector("[data-ob-client=consent]");
        if (box) box.checked = !!initial.consentRequested;
        var host = root.querySelector("[data-ob-client=miniRoot]");
        mountMiniHost(host, initial);
        return {
            collect: function () {
                var payload = { consentRequested: box ? box.checked : false };
                var answers = readMiniAnswersFromDom(host);
                if (Object.keys(answers).length) payload.answers = answers;
                return payload;
            }
        };
    }

    function mountMini(root, initial) {
        if (root.closest("form[data-ob-ssr-form]")) {
            var form = root.closest("form[data-ob-ssr-form]");
            var step = parseInt(root.getAttribute("data-ob-client-step") || "0", 10);
            return {
                collect: function () {
                    if (step === 7) {
                        var consentBox = form ? form.querySelector('input[name="consentRequested"]') : null;
                        return {
                            consentRequested: !!(consentBox && consentBox.checked),
                            answers: readMiniAnswersFromForm(form)
                        };
                    }
                    return { answers: readMiniAnswersFromForm(form) };
                }
            };
        }

        var host = root.querySelector("[data-ob-client=miniRoot]");
        if (!host) host = root;
        mountMiniHost(host, initial);
        return {
            collect: function () {
                return { answers: readMiniAnswersFromDom(host) };
            }
        };
    }

    function mountForStep(root, step, initial) {
        if (step === 2) return mountStep2(root, initial);
        if (step === 3) return mountStep3(root, initial);
        if (step === 4) return mountStep4(root, initial);
        if (step === 5) return mountStep5(root, initial);
        if (step === 6) return mountStep6(root, initial);
        if (step === 7) {
            if (initial.hasConsent) return mountMini(root, initial);
            return mountStep7Combined(root, initial);
        }
        if (step >= 8 && step <= 10) return mountMini(root, initial);
        return mountStep1(root, initial);
    }

    window.jobsyOnboardingClient = {
        isReady: function () {
            return typeof window.jobsyOnboardingClient.mount === "function";
        },
        isStepMounted: function (step) {
            var root = document.querySelector('[data-ob-client-step="' + step + '"]');
            return !!(root && root.getAttribute("data-ob-client-mounted") === "1");
        },
        mount: function (root, step) {
            if (!root) return;
            if (root.getAttribute("data-ob-client-mounted") === "1") return;
            var ssrForm = root.closest("form[data-ob-ssr-form]");
            if (ssrForm && step >= 2 && step <= 4) {
                root.setAttribute("data-ob-client-mounted", "1");
                return;
            }
            if (ssrForm && step >= 7 && step <= 10) {
                root.setAttribute("data-ob-client-mounted", "1");
                return;
            }
            var initial = {};
            try {
                initial = JSON.parse(root.getAttribute("data-initial") || "{}");
            } catch (e) { }
            var st = mountForStep(root, step, initial);
            attachStepBff(root, step, st);
            if (roots) roots.set(root, st);
            else fallbackMap = fallbackMap || {};
            if (!roots) fallbackMap[root.id || "x"] = st;
            root.setAttribute("data-ob-client-mounted", "1");
        },
        unmount: function (root) {
            if (!root) return;
            root.removeAttribute("data-ob-client-mounted");
            if (roots) roots.delete(root);
        },
        collect: function (step) {
            var root = document.querySelector("[data-ob-client-step=\"" + step + "\"]");
            if (!root || root.getAttribute("data-ob-client-mounted") !== "1") return "";
            var st = roots ? roots.get(root) : null;
            if (!st || !st.collect) return "";
            return JSON.stringify(st.collect());
        },
        mountKompasComplete: function (dotNetRef) {
            window.__jobsyKompasCompleteRef = dotNetRef;
            document.querySelectorAll("[data-ob-client-kompas-complete]").forEach(function (btn) {
                if (btn.__jobsyKompasBound) return;
                btn.__jobsyKompasBound = true;
                btn.addEventListener("click", function () {
                    if (window.__jobsyKompasCompleteRef) {
                        window.__jobsyKompasCompleteRef.invokeMethodAsync("CompleteAsync");
                    }
                });
            });
        },
        unmountKompasComplete: function () {
            window.__jobsyKompasCompleteRef = null;
        }
    };

    function activeClientStepRoot() {
        return document.querySelector("[data-ob-client-step]");
    }

    function activeClientStepNumber() {
        var root = activeClientStepRoot();
        if (!root) return 0;
        return parseInt(root.getAttribute("data-ob-client-step"), 10) || 0;
    }

    function readInitialFromRoot(root) {
        try {
            return JSON.parse(root.getAttribute("data-initial") || "{}");
        } catch (e) {
            return {};
        }
    }

    function collectSsrPostStep2FromDom(root) {
        var initial = readInitialFromRoot(root);
        var presets = [];
        root.querySelectorAll('button[name="obPreset"]').forEach(function (b) {
            if (b.classList.contains("is-selected") || b.getAttribute("aria-pressed") === "true") {
                var v = b.getAttribute("value");
                if (v) presets.push(v);
            }
        });
        var availability = [];
        root.querySelectorAll('button[name="obMatrix"]').forEach(function (b) {
            if (b.classList.contains("is-on") || b.getAttribute("aria-pressed") === "true") {
                var key = b.getAttribute("value");
                if (key) availability.push(key);
            }
        });
        var modeNow = root.querySelector('input[name="obStartMode"][value="now"]');
        var immediate = !modeNow || modeNow.checked;
        var availFromInput = root.querySelector('input[name="obAvailableFrom"]');
        var availableFrom = null;
        if (!immediate && availFromInput && availFromInput.value) {
            availableFrom = availFromInput.value.trim();
        }
        return {
            presets: presets,
            presetsOverridden: initial.presetsOverridden || presets.indexOf("custom") >= 0,
            fineTune: initial.fineTune || presets.indexOf("custom") >= 0,
            minHours: initial.minHours || 12,
            maxHours: initial.maxHours || 40,
            availability: availability,
            immediate: immediate,
            availableFrom: availableFrom
        };
    }

    function collectSsrPostStep3FromDom(root) {
        var initial = readInitialFromRoot(root);
        var transport = [];
        root.querySelectorAll('button[name="obTransport"]').forEach(function (b) {
            if (b.classList.contains("is-selected") || b.getAttribute("aria-pressed") === "true") {
                var code = b.getAttribute("value");
                if (code) transport.push(code);
            }
        });
        var travel = initial.maxTravel || 30;
        root.querySelectorAll('button[name="obTravel"]').forEach(function (b) {
            if (b.classList.contains("is-selected") || b.getAttribute("aria-pressed") === "true") {
                travel = parseInt(b.getAttribute("value"), 10) || travel;
            }
        });
        return {
            transport: transport,
            maxTravel: travel,
            selectedTravel: travel,
            customTravel: false
        };
    }

    function collectSsrPostStep4FromDom(root) {
        var noWorkEl = root.querySelector("[data-ob-client=noWork]");
        var noWork = noWorkEl ? noWorkEl.checked : false;
        var jobs = [];
        if (!noWork) {
            root.querySelectorAll("input[data-field=employer]").forEach(function (inp) {
                jobs.push({ employer: inp.value || "", startMonth: null, endMonth: null, isCurrent: false });
            });
        }
        return { noWork: noWork, jobs: jobs };
    }

    function collectSsrPostStepFromDom(root, step) {
        if (step === 2) return collectSsrPostStep2FromDom(root);
        if (step === 3) return collectSsrPostStep3FromDom(root);
        if (step === 4) return collectSsrPostStep4FromDom(root);
        return {};
    }

    function collectForRoot(root, step, state) {
        if (state && typeof state.collect === "function") {
            return state.collect();
        }
        if (roots && root) {
            var st = roots.get(root);
            if (st && typeof st.collect === "function") {
                return st.collect();
            }
        }
        return {};
    }

    function runStepBack(root, step, collectFn) {
        var errEl = root ? root.querySelector("[data-ob-step-error]") : null;
        if (errEl) {
            errEl.hidden = true;
            errEl.textContent = "";
        }
        return putOnboardingStep(root, step, collectFn, false, false, true)
            .then(function (res) {
                return res.json().then(function (body) {
                    return { ok: res.ok, body: body };
                }).catch(function () {
                    return { ok: res.ok, body: {} };
                });
            })
            .then(function (result) {
                if (result.ok && result.body && result.body.redirectUrl) {
                    window.location.assign(result.body.redirectUrl);
                    return;
                }
                if (result.ok) {
                    window.location.reload();
                    return;
                }
                var msg = (result.body && result.body.message) || "Terug gaan mislukt. Probeer het opnieuw.";
                if (errEl) {
                    errEl.textContent = msg;
                    errEl.hidden = false;
                }
            })
            .catch(function () {
                if (errEl) {
                    errEl.textContent = "Verbinding mislukt. Probeer het opnieuw.";
                    errEl.hidden = false;
                }
            });
    }

    function resolveClientRoot(fromBtn) {
        if (fromBtn) {
            var canvas = fromBtn.closest(".ob-rustig__canvas,.ob-client-step,.ob-wizard__content");
            if (canvas) {
                var inCanvas = canvas.querySelector("[data-ob-client-step]");
                if (inCanvas) return inCanvas;
            }
        }
        return activeClientStepRoot();
    }

    function resolveNavStep(root, btn) {
        if (root) {
            var fromRoot = parseInt(root.getAttribute("data-ob-client-step"), 10);
            if (fromRoot) return fromRoot;
        }
        if (btn) {
            var fromBtn = parseInt(btn.getAttribute("data-ob-nav-step") || "", 10);
            if (fromBtn) return fromBtn;
        }
        var wiz = document.querySelector(".ob-wizard[data-ob-active-step]");
        if (wiz) {
            var fromWiz = parseInt(wiz.getAttribute("data-ob-active-step"), 10);
            if (fromWiz) return fromWiz;
        }
        return activeClientStepNumber();
    }

    function reconcileRustigFooterNav() {
        document.querySelectorAll(".ob-rustig").forEach(function (shell) {
            var footer = shell.querySelector(".ob-rustig__footer-actions");
            if (!footer) return;
            var hasFooterNext = footer.querySelector("[data-ob-nav-next],[data-testid=onboarding-next]");
            if (!hasFooterNext) return;
            shell.querySelectorAll(".ob-step-actions").forEach(function (nav) {
                nav.setAttribute("hidden", "hidden");
                nav.setAttribute("aria-hidden", "true");
            });
        });
    }

    function triggerWizardAdvance(skipOptional) {
        var root = activeClientStepRoot();
        var step = resolveNavStep(root, null);
        if (!root || !step) return;
        if (typeof root.__obRunAdvance === "function") {
            root.__obRunAdvance(!!skipOptional);
            return;
        }
        var st = roots ? roots.get(root) : null;
        runStepAdvance(root, step, function () {
            return collectForRoot(root, step, st);
        }, !!skipOptional);
    }

    function bindObWizardNav() {
        if (window.__obWizardNavBound) return;
        if (document.querySelector("form[data-ob-ssr-form]")) return;
        window.__obWizardNavBound = true;
        document.addEventListener("keydown", function (ev) {
            if (ev.key !== "Enter" || ev.defaultPrevented || ev.isComposing) return;
            if (!ev.target || !ev.target.closest(".ob-wizard")) return;
            if (ev.target.closest(".address-suggestions:not([hidden])")) return;
            if (ev.target.closest("textarea")) return;
            if (ev.target.closest("button,[data-ob-nav-next],[data-ob-step-next]")) return;
            var wiz = document.querySelector(".ob-wizard[data-ob-active-step]");
            if (!wiz) return;
            var step = parseInt(wiz.getAttribute("data-ob-active-step") || "", 10);
            if (step !== 1 && step !== 2) return;
            ev.preventDefault();
            triggerWizardAdvance(false);
        }, true);
        document.addEventListener("click", function (ev) {
            if (!ev.target.closest(".ob-wizard")) return;

            var backBtn = ev.target.closest("[data-ob-nav-back]");
            if (backBtn) {
                if (backBtn.disabled) return;
                var rootBack = resolveClientRoot(backBtn);
                var stepBack = resolveNavStep(rootBack, backBtn);
                if (!rootBack || (stepBack !== 1 && stepBack !== 2)) return;
                if (stepBack <= 1) return;
                ev.preventDefault();
                ev.stopPropagation();
                backBtn.disabled = true;
                var stBack = roots ? roots.get(rootBack) : null;
                runStepBack(rootBack, stepBack, function () {
                    return collectForRoot(rootBack, stepBack, stBack);
                }).finally(function () {
                    backBtn.disabled = false;
                });
                return;
            }

            var nextBtn = ev.target.closest("[data-ob-nav-next]");
            if (!nextBtn) return;
            if (nextBtn.disabled) return;

            var root = resolveClientRoot(nextBtn);
            var step = resolveNavStep(root, nextBtn);
            if (!root || (step !== 1 && step !== 2)) return;

            ev.preventDefault();
            ev.stopPropagation();
            if (typeof root.__obRunAdvance === "function") {
                root.__obRunAdvance(false);
            } else {
                var stNext = roots ? roots.get(root) : null;
                runStepAdvance(root, step, function () {
                    return collectForRoot(root, step, stNext);
                }, false);
            }
        }, true);
    }

    function attachStepBff(root, step, state) {
        if (!state || typeof state.collect !== "function") return;
        if (root.closest("form[data-ob-ssr-form]") && step >= 1 && step <= 10) return;
        if (step === 1 || step === 2 || step === 3) return;
        var bff = wireObStepBff(root, step, function () { return state.collect(); });
        var touch = function () {
            if (bff && bff.scheduleSave) bff.scheduleSave();
        };
        state.scheduleSave = touch;
        root.addEventListener("change", touch);
        root.addEventListener("blur", touch, true);
        root.addEventListener("click", function (ev) {
            if (ev.target.closest("[data-ob-step-next],[data-ob-step-skip]")) return;
            touch();
        });
    }

    function bindWelcomeBegin() {
        document.querySelectorAll("[data-ob-welcome-begin]").forEach(function (btn) {
            if (btn.__obWelcomeBound) return;
            btn.__obWelcomeBound = true;
            btn.addEventListener("click", function (ev) {
                if (btn.disabled) return;
                ev.preventDefault();
                ev.stopPropagation();
                btn.disabled = true;
                var af = readAntiforgery(document.querySelector(".ob-wizard") || document.body);
                var headers = { "Content-Type": "application/json" };
                if (af.token) headers[af.header] = af.token;
                fetch("/api/onboarding-client/begin", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: headers,
                    body: "{}"
                }).then(function (res) {
                    return res.json().then(function (body) {
                        return { ok: res.ok, body: body };
                    }).catch(function () { return { ok: res.ok, body: {} }; });
                }).then(function (result) {
                    if (result.ok && result.body && result.body.redirectUrl) {
                        window.location.assign(result.body.redirectUrl);
                        return;
                    }
                    btn.disabled = false;
                }).catch(function () { btn.disabled = false; });
            }, true);
        });
    }

    function bindKompasComplete() {
        document.querySelectorAll("[data-ob-kompas-complete]").forEach(function (btn) {
            if (btn.__obKompasBound) return;
            btn.__obKompasBound = true;
            btn.addEventListener("click", function (ev) {
                ev.preventDefault();
                ev.stopPropagation();
                btn.disabled = true;
                var af = readAntiforgery(document.querySelector(".ob-wizard") || document.body);
                var headers = { "Content-Type": "application/json" };
                if (af.token) headers[af.header] = af.token;
                fetch("/api/onboarding-client/kompas/complete", {
                    method: "POST",
                    credentials: "same-origin",
                    headers: headers,
                    body: "{}"
                }).then(function (res) {
                    return res.json().then(function (body) {
                        return { ok: res.ok, body: body };
                    }).catch(function () { return { ok: res.ok, body: {} }; });
                }).then(function (result) {
                    if (result.ok && result.body && result.body.redirectUrl) {
                        window.location.assign(result.body.redirectUrl);
                        return;
                    }
                    btn.disabled = false;
                }).catch(function () { btn.disabled = false; });
            }, true);
        });
    }

    function autoMountClientSteps() {
        document.querySelectorAll("[data-ob-client-step]").forEach(function (root) {
            if (root.getAttribute("data-ob-client-mounted") === "1") return;
            var step = parseInt(root.getAttribute("data-ob-client-step"), 10);
            if (!step) return;
            var ssrForm = root.closest("form[data-ob-ssr-form]");
            if (ssrForm && step >= 2 && step <= 4) {
                root.setAttribute("data-ob-client-mounted", "1");
                return;
            }
            if (ssrForm && step >= 2 && step <= 4) {
                root.setAttribute("data-ob-client-mounted", "1");
                return;
            }
            if (ssrForm && step >= 7 && step <= 10) {
                root.setAttribute("data-ob-client-mounted", "1");
                return;
            }
            window.jobsyOnboardingClient.mount(root, step);
        });
        bindWelcomeBegin();
        bindKompasComplete();
    }

    function bindSsrOnboardingForm() {
        document.querySelectorAll("form[data-ob-ssr-form]").forEach(function (form) {
            if (form.__obSsrFormBound) return;
            form.__obSsrFormBound = true;
            form.addEventListener("submit", function () {
                var step = parseInt(form.getAttribute("data-ob-ssr-step") || "0", 10);
                var hidden = form.querySelector("[data-ob-ssr-payload]");
                if (step === 6) {
                    var dreamInput = form.querySelector('input[name="dream"]');
                    var dreamVal = dreamInput ? dreamInput.value.trim() : "";
                    if (hidden) {
                        hidden.value = JSON.stringify({ dream: dreamVal, dreamSearch: dreamVal });
                    }
                    return;
                }
                if (step >= 7 && step <= 10) {
                    var consentBox = form.querySelector('input[name="consentRequested"]');
                    var payloadMini = { answers: readMiniAnswersFromForm(form) };
                    if (step === 7 && consentBox) {
                        payloadMini.consentRequested = !!consentBox.checked;
                    }
                    if (hidden) {
                        hidden.value = JSON.stringify(payloadMini);
                    }
                    return;
                }
                var root = form.querySelector("[data-ob-client-step]");
                if (!root || !step) return;
                if (form.getAttribute("data-ob-ssr-post-tiles") === "1" && step >= 2 && step <= 4) {
                    if (hidden) {
                        hidden.value = JSON.stringify(collectSsrPostStepFromDom(root, step));
                    }
                    return;
                }
                var st = roots ? roots.get(root) : null;
                var payload = collectForRoot(root, step, st) || {};
                if (hidden) hidden.value = JSON.stringify(payload);
            }, true);
        });
    }

    function bootOnboardingClient() {
        bindSsrOnboardingForm();
        autoMountClientSteps();
        bindWelcomeBegin();
        bindKompasComplete();
        reconcileRustigFooterNav();
        bindObWizardNav();
    }

    if (document.readyState === "loading") {
        document.addEventListener("DOMContentLoaded", bootOnboardingClient);
    } else {
        bootOnboardingClient();
    }

    if (!window.__obOnboardingDomObserver) {
        window.__obOnboardingDomObserver = true;
        var domObs = new MutationObserver(function () {
            autoMountClientSteps();
            bindWelcomeBegin();
            bindKompasComplete();
            reconcileRustigFooterNav();
        });
        domObs.observe(document.documentElement, { childList: true, subtree: true });
    }
})();
