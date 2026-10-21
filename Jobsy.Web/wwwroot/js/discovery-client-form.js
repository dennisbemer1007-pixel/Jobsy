(function () {
    "use strict";
    var roots = typeof WeakMap === "function" ? new WeakMap() : null;

    function mountChoiceGrid(container, options, selectedSet, multi, onChange) {
        if (!container) return;
        container.innerHTML = "";
        (options || []).forEach(function (opt) {
            var code = opt.code || opt;
            var label = opt.label || opt;
            var btn = document.createElement("button");
            btn.type = "button";
            btn.className = "lobsy-choice-tile journey-chip";
            btn.setAttribute("aria-pressed", selectedSet.has(code) ? "true" : "false");
            btn.textContent = label;
            btn.addEventListener("click", function () {
                if (multi) {
                    if (selectedSet.has(code)) selectedSet.delete(code);
                    else selectedSet.add(code);
                } else {
                    selectedSet.clear();
                    selectedSet.add(code);
                }
                onChange();
                mountChoiceGrid(container, options, selectedSet, multi, onChange);
            });
            container.appendChild(btn);
        });
    }

    function mountForStep(root, step, initial) {
        if (step === 2) {
            var selectedPresets = new Set((initial.availability || {}).presets || []);
            var transportSel = new Set((initial.transport || {}).transport || []);
            var licenses = new Set(initial.drivingLicenses || []);
            mountChoiceGrid(root.querySelector("[data-disc-client=presetGrid]"), (initial.availability || {}).presetTiles || [], selectedPresets, true, function () { });
            mountChoiceGrid(root.querySelector("[data-disc-client=transportGrid]"), (initial.transport || {}).transportOptions || [], transportSel, true, function () { });
            mountChoiceGrid(root.querySelector("[data-disc-client=licenseGrid]"), initial.licenseOptions || [], licenses, true, function () { });
            return {
                collect: function () {
                    return {
                        availability: {
                            presets: Array.from(selectedPresets),
                            presetsOverridden: false,
                            fineTune: false,
                            minHours: (initial.availability || {}).minHours,
                            maxHours: (initial.availability || {}).maxHours,
                            availability: (initial.availability || {}).availability || [],
                            immediate: true,
                            availableFrom: null
                        },
                        transport: {
                            transport: Array.from(transportSel),
                            maxTravel: (initial.transport || {}).maxTravel || 30,
                            selectedTravel: (initial.transport || {}).maxTravel || 30,
                            customTravel: false
                        },
                        drivingLicenses: Array.from(licenses)
                    };
                }
            };
        }

        if (step === 5) {
            var hobbies = new Set(initial.hobbies || []);
            var about = root.querySelector("[data-disc-client=aboutMe]");
            mountChoiceGrid(root.querySelector("[data-disc-client=hobbyGrid]"), initial.hobbyOptions || [], hobbies, true, function () { });
            if (about) about.value = initial.aboutMe || "";
            return {
                collect: function () {
                    return {
                        hobbies: Array.from(hobbies),
                        hobbyFreeText: initial.hobbyFreeText || [],
                        aboutMe: about ? about.value : ""
                    };
                }
            };
        }

        if (step === 6) {
            var dislikes = new Set(initial.dislikes || []);
            mountChoiceGrid(root.querySelector("[data-disc-client=dislikeGrid]"), initial.dislikeOptions || [], dislikes, true, function () { });
            return {
                collect: function () {
                    return { dislikes: Array.from(dislikes), customDislikes: initial.customDislikes || [] };
                }
            };
        }

        return { collect: function () { return {}; } };
    }

    window.jobsyDiscoveryClient = {
        mount: function (root, step) {
            if (!root) return;
            var initial = {};
            try { initial = JSON.parse(root.getAttribute("data-initial") || "{}"); } catch (e) { }
            var st = mountForStep(root, step, initial);
            if (roots) roots.set(root, st);
        },
        unmount: function (root) {
            if (roots) roots.delete(root);
        },
        collect: function (step) {
            var root = document.querySelector("[data-discovery-client-step=\"" + step + "\"]");
            if (!root) return "{}";
            var st = roots ? roots.get(root) : null;
            if (!st || !st.collect) return "{}";
            return JSON.stringify(st.collect());
        }
    };
})();
