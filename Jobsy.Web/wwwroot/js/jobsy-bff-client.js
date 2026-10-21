(function () {
    "use strict";

    function antiforgeryToken(root) {
        var input = document.querySelector("input[name='__RequestVerificationToken']");
        if ((!input || !input.value) && root) {
            input = root.querySelector("input[name='__RequestVerificationToken']");
        }
        if (input && input.value) {
            return input.value;
        }
        var meta = document.querySelector("meta[name='request-verification-token']");
        return meta ? meta.getAttribute("content") : "";
    }

    function showError(root, message) {
        if (!root) {
            return;
        }
        var box = root.querySelector("[data-bff-error]");
        if (!box) {
            box = document.createElement("p");
            box.setAttribute("data-bff-error", "1");
            box.setAttribute("role", "alert");
            box.className = "form-error state-message error";
            root.insertBefore(box, root.firstChild);
        }
        if (message) {
            box.hidden = false;
            box.textContent = message;
        } else {
            box.hidden = true;
            box.textContent = "";
        }
    }

    function clearError(root) {
        showError(root, "");
    }

    function debounce(fn, ms) {
        var t;
        return function () {
            var args = arguments;
            var self = this;
            clearTimeout(t);
            t = setTimeout(function () {
                fn.apply(self, args);
            }, ms);
        };
    }

    function postForm(url, formData, root) {
        var token = antiforgeryToken(root);
        if (token && !formData.has("__RequestVerificationToken")) {
            formData.append("__RequestVerificationToken", token);
        }
        return fetch(url, {
            method: "POST",
            credentials: "same-origin",
            headers: {
                "X-Jobsy-Bff": "1",
                Accept: "application/json",
            },
            body: formData,
        }).then(function (res) {
            return res.json().catch(function () {
                return { ok: false, error: "Kon niet opslaan." };
            }).then(function (body) {
                if (!res.ok || !body || body.ok === false) {
                    var msg = (body && body.error) || "Kon niet opslaan.";
                    showError(root, msg);
                    throw new Error(msg);
                }
                clearError(root);
                if (body.redirect) {
                    window.location.assign(body.redirect);
                }
                return body;
            });
        });
    }

    function formFromContainer(root) {
        var fd = new FormData();
        root.querySelectorAll("input, select, textarea").forEach(function (el) {
            if (!el.name || el.disabled) {
                return;
            }
            if (el.type === "checkbox") {
                if (el.checked) {
                    fd.append(el.name, el.value || "1");
                }
                return;
            }
            if (el.type === "radio") {
                if (el.checked) {
                    fd.append(el.name, el.value);
                }
                return;
            }
            fd.append(el.name, el.value);
        });
        return fd;
    }

    function wireAutosave(root, url, delayMs) {
        if (!root || root.__jobsyBffAutosave) {
            return;
        }
        root.__jobsyBffAutosave = true;
        var save = debounce(function () {
            postForm(url, formFromContainer(root), root).catch(function () { });
        }, delayMs || 600);
        root.addEventListener("input", save);
        root.addEventListener("change", save);
    }

    function wireClick(root, selector, url, buildExtra) {
        root.querySelectorAll(selector).forEach(function (btn) {
            if (btn.__jobsyBffClick) {
                return;
            }
            btn.__jobsyBffClick = true;
            btn.addEventListener("click", function () {
                if (btn.disabled) {
                    return;
                }
                btn.disabled = true;
                var fd = formFromContainer(root);
                if (typeof buildExtra === "function") {
                    buildExtra(fd, btn);
                }
                postForm(url, fd, root)
                    .catch(function () { })
                    .finally(function () {
                        btn.disabled = false;
                    });
            });
        });
    }

    window.jobsyBff = {
        postForm: postForm,
        formFromContainer: formFromContainer,
        wireAutosave: wireAutosave,
        wireClick: wireClick,
        showError: showError,
        debounce: debounce,
    };
})();
