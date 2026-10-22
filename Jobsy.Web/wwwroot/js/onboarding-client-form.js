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
        if (!text || text.trim().length < 8) return null;
        var parts = text.trim().replace(/\./g, "-").replace(/\//g, "-").split("-");
        if (parts.length !== 3) return null;
        var d = parseInt(parts[0], 10);
        var m = parseInt(parts[1], 10);
        var y = parseInt(parts[2], 10);
        if (y < 100) y += y >= 50 ? 1900 : 2000;
        if (!d || !m || !y) return null;
        return { d: d, m: m, y: y, text: text.trim() };
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
            return {
                firstName: first ? first.value : "",
                lastName: last ? last.value : "",
                phone: phone ? phone.value : "",
                whatsAppAllowed: false,
                workStatuses: readWorkStatusesFromDom(),
                addressQuery: address ? address.value : "",
                homeLat: state.homeLat,
                homeLng: state.homeLng,
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
        var availFrom = root.querySelector("[data-ob-client=availableFrom]");

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

        function applyPresetCompute() {
            if (state.presets.has("custom") || state.presetsOverridden) return;
            var c = computePresets(state.presets);
            if (c.min != null) state.minHours = c.min;
            if (c.max != null) state.maxHours = c.max;
            state.availability = new Set(c.availability);
            syncHoursUi();
            renderMatrix();
        }

        function renderPresets() {
            if (!presetGrid) return;
            presetGrid.innerHTML = "";
            (initial.presetTiles || []).forEach(function (tile) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "lobsy-choice-tile";
                btn.setAttribute("aria-pressed", state.presets.has(tile.code) ? "true" : "false");
                btn.innerHTML = "<strong>" + escapeHtml(tile.title) + "</strong><small>" + escapeHtml(tile.lead) + "</small>";
                btn.addEventListener("click", function () {
                    var code = tile.code;
                    if (code === "custom") {
                        if (state.presets.has("custom")) state.presets.delete("custom");
                        else {
                            state.presets.clear();
                            state.presets.add("custom");
                            state.fineTune = true;
                            state.presetsOverridden = true;
                        }
                    } else {
                        state.presets.delete("custom");
                        if (state.presets.has(code)) state.presets.delete(code);
                        else state.presets.add(code);
                        state.presetsOverridden = false;
                    }
                    applyPresetCompute();
                    renderPresets();
                });
                presetGrid.appendChild(btn);
            });
        }

        function renderMatrix() {
            if (!matrix) return;
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
                    btn.setAttribute("aria-pressed", on ? "true" : "false");
                    btn.addEventListener("click", function () {
                        if (state.availability.has(key)) state.availability.delete(key);
                        else state.availability.add(key);
                        state.presetsOverridden = true;
                        state.presets.clear();
                        state.presets.add("custom");
                        state.fineTune = true;
                        renderPresets();
                        renderMatrix();
                    });
                    matrix.appendChild(btn);
                });
            });
        }

        renderPresets();
        renderMatrix();
        syncHoursUi();

        function onHoursInput() {
            state.minHours = minR ? parseInt(minR.value, 10) : state.minHours;
            state.maxHours = maxR ? parseInt(maxR.value, 10) : state.maxHours;
            if (state.minHours > state.maxHours) state.minHours = state.maxHours;
            state.presetsOverridden = true;
            state.presets.clear();
            state.presets.add("custom");
            syncHoursUi();
            renderPresets();
        }
        if (minR) minR.addEventListener("input", onHoursInput);
        if (maxR) maxR.addEventListener("input", onHoursInput);

        if (startImmediate) startImmediate.addEventListener("change", function () { state.immediate = true; if (availFrom) availFrom.hidden = true; });
        if (startFromDate) startFromDate.addEventListener("change", function () {
            state.immediate = false;
            if (availFrom) availFrom.hidden = false;
        });
        if (availFrom) {
            availFrom.addEventListener("input", function () {
                availFrom.value = applyDateMask(availFrom.value);
            });
        }

        state.collect = function () {
            var fromIso = null;
            if (!state.immediate && availFrom) {
                var p = parseFlexibleDate(availFrom.value);
                if (p) fromIso = p.y + "-" + String(p.m).padStart(2, "0") + "-" + String(p.d).padStart(2, "0");
            }
            return {
                presets: Array.from(state.presets),
                presetsOverridden: state.presetsOverridden,
                fineTune: state.fineTune,
                minHours: state.minHours,
                maxHours: state.maxHours,
                availability: Array.from(state.availability),
                immediate: state.immediate,
                availableFrom: fromIso
            };
        };
        return state;
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
            btn.textContent = label;
            container.appendChild(btn);
        });
    }

    function syncChoiceGridPressed(container, selectedSet, multi) {
        if (!container) return;
        container.querySelectorAll(".lobsy-choice-tile[data-ob-code]").forEach(function (btn) {
            var code = btn.getAttribute("data-ob-code");
            var on = selectedSet.has(code);
            btn.setAttribute("aria-pressed", on ? "true" : "false");
        });
    }

    function mountChoiceGrid(container, options, selectedSet, multi, onChange) {
        if (!container || container.getAttribute("data-ob-choice-bound") === "1") return;
        container.setAttribute("data-ob-choice-bound", "1");
        paintChoiceGrid(container, options, selectedSet, multi);
        container.addEventListener("click", function (ev) {
            var btn = ev.target.closest(".lobsy-choice-tile[data-ob-code]");
            if (!btn || !container.contains(btn)) return;
            var code = btn.getAttribute("data-ob-code");
            if (multi) {
                if (selectedSet.has(code)) selectedSet.delete(code);
                else selectedSet.add(code);
            } else {
                selectedSet.clear();
                selectedSet.add(code);
            }
            syncChoiceGridPressed(container, selectedSet, multi);
            if (onChange) onChange();
        });
    }

    function readChoiceCodes(container) {
        var codes = [];
        if (!container) return codes;
        container.querySelectorAll('.lobsy-choice-tile[aria-pressed="true"]').forEach(function (btn) {
            var code = btn.getAttribute("data-ob-code");
            if (code) codes.push(code);
        });
        return codes;
    }

    function mountStep3(root, initial) {
        var selected = new Set(initial.transport || []);
        var travel = initial.maxTravel || 30;
        var custom = !!initial.customTravel;
        var tGrid = root.querySelector("[data-ob-client=transportGrid]");
        var vGrid = root.querySelector("[data-ob-client=travelGrid]");
        var customInput = root.querySelector("[data-ob-client=customTravel]");

        function paintTravel() {
            if (!vGrid || vGrid.getAttribute("data-ob-travel-bound") === "1") return;
            vGrid.setAttribute("data-ob-travel-bound", "1");
            vGrid.innerHTML = "";
            (initial.travelChoices || [10, 20, 30, 45]).forEach(function (m) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "lobsy-choice-tile";
                btn.setAttribute("data-ob-travel", String(m));
                btn.setAttribute("aria-pressed", (!custom && travel === m) ? "true" : "false");
                btn.textContent = String(m);
                vGrid.appendChild(btn);
            });
            vGrid.addEventListener("click", function (ev) {
                var btn = ev.target.closest(".lobsy-choice-tile[data-ob-travel]");
                if (!btn) return;
                custom = false;
                travel = parseInt(btn.getAttribute("data-ob-travel"), 10) || travel;
                if (customInput) customInput.hidden = true;
                vGrid.querySelectorAll(".lobsy-choice-tile[data-ob-travel]").forEach(function (b) {
                    b.setAttribute("aria-pressed", b === btn ? "true" : "false");
                });
            });
        }

        mountChoiceGrid(tGrid, initial.transportOptions || [], selected, true, function () { });
        paintTravel();
        if (customInput) {
            customInput.hidden = !custom;
            customInput.value = custom ? travel : travel;
            customInput.addEventListener("change", function () {
                custom = true;
                travel = parseInt(customInput.value, 10) || travel;
            });
        }

        return {
            collect: function () {
                var transport = readChoiceCodes(tGrid);
                var pickedTravel = null;
                var isCustom = custom;
                if (vGrid) {
                    var pressed = vGrid.querySelector('.lobsy-choice-tile[aria-pressed="true"]');
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
        };
    }

    function mountStep4(root, initial) {
        var noWorkEl = root.querySelector("[data-ob-client=noWork]");
        var jobs = (initial.jobs || []).slice();
        var list = root.querySelector("[data-ob-client=jobsList]");
        var addBtn = root.querySelector("[data-ob-client=addJob]");

        function render() {
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

        if (noWorkEl) noWorkEl.checked = !!initial.noWork;
        render();
        if (addBtn) addBtn.addEventListener("click", function () {
            jobs.push({ employer: "", startMonth: null, endMonth: null, isCurrent: false });
            render();
        });

        return {
            collect: function () {
                var noWork = noWorkEl ? noWorkEl.checked : false;
                return { noWork: noWork, jobs: noWork ? [] : jobs };
            }
        };
    }

    function mountStep5(root, initial) {
        var selected = new Set();
        if (initial.education) selected.add(initial.education);
        var grid = root.querySelector("[data-ob-client=educationGrid]");
        mountChoiceGrid(grid, initial.levels || [], selected, false, function () { });
        return {
            collect: function () {
                var codes = readChoiceCodes(grid);
                return { education: codes.length ? codes[0] : "" };
            }
        };
    }

    function mountStep6(root, initial) {
        var input = root.querySelector("[data-ob-client=dreamInput]");
        var grid = root.querySelector("[data-ob-client=dreamGrid]");
        if (input) input.value = initial.dreamSearch || initial.dream || "";

        function syncDreamTiles(active) {
            if (!grid) return;
            grid.querySelectorAll(".ob-rustig__dream").forEach(function (btn) {
                var title = btn.getAttribute("data-ob-dream") || btn.textContent;
                var on = title === active;
                btn.classList.toggle("is-on", on);
                btn.setAttribute("aria-pressed", on ? "true" : "false");
            });
        }

        function paintDreamGrid() {
            if (!grid || grid.getAttribute("data-ob-dream-bound") === "1") return;
            grid.setAttribute("data-ob-dream-bound", "1");
            grid.innerHTML = "";
            (initial.dreamTitles || []).forEach(function (title) {
                var btn = document.createElement("button");
                btn.type = "button";
                btn.className = "ob-rustig__dream";
                btn.setAttribute("data-ob-dream", title);
                btn.setAttribute("aria-pressed", "false");
                btn.textContent = title;
                grid.appendChild(btn);
            });
            grid.addEventListener("click", function (ev) {
                var btn = ev.target.closest(".ob-rustig__dream[data-ob-dream]");
                if (!btn) return;
                var title = btn.getAttribute("data-ob-dream");
                if (input) input.value = title;
                syncDreamTiles(title);
            });
            syncDreamTiles(input ? input.value.trim() : "");
        }
        paintDreamGrid();
        if (input) {
            input.addEventListener("input", function () {
                syncDreamTiles(input.value.trim());
            });
        }

        return {
            collect: function () {
                var val = input ? input.value.trim() : "";
                return { dream: val, dreamSearch: val };
            }
        };
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

    function mountStep7Combined(root, initial) {
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
        document.querySelectorAll(".ob-rustig__canvas").forEach(function (canvas) {
            var footerNext = canvas.querySelector(".ob-rustig__footer-actions [data-ob-nav-next]");
            if (!footerNext) return;
            canvas.querySelectorAll(".ob-step-actions").forEach(function (nav) {
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
            if (step !== 1) return;
            ev.preventDefault();
            triggerWizardAdvance(false);
        }, true);
        document.addEventListener("click", function (ev) {
            if (!ev.target.closest(".ob-wizard")) return;

            var nextBtn = ev.target.closest("[data-ob-nav-next]");
            if (!nextBtn) return;
            if (nextBtn.disabled) return;

            var root = resolveClientRoot(nextBtn);
            var step = resolveNavStep(root, nextBtn);
            if (!root || step !== 1) return;

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
        if (step === 1) return;
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
            window.jobsyOnboardingClient.mount(root, step);
        });
        bindWelcomeBegin();
        bindKompasComplete();
    }

    function bootOnboardingClient() {
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
