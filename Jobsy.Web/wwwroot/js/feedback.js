(function () {
    "use strict";

    var html2canvasPromise = null;

    function loadHtml2Canvas() {
        if (window.html2canvas) {
            return Promise.resolve(window.html2canvas);
        }
        if (html2canvasPromise) {
            return html2canvasPromise;
        }
        html2canvasPromise = new Promise(function (resolve, reject) {
            var script = document.createElement("script");
            script.src = "lib/html2canvas/html2canvas.min.js";
            script.async = true;
            script.onload = function () {
                if (window.html2canvas) {
                    resolve(window.html2canvas);
                } else {
                    reject(new Error("html2canvas missing"));
                }
            };
            script.onerror = function () {
                reject(new Error("html2canvas load failed"));
            };
            document.head.appendChild(script);
        });
        return html2canvasPromise;
    }

    function shouldIgnore(el) {
        if (!el || !el.closest) {
            return false;
        }
        return !!(el.closest(".feedback-widget")
            || el.closest(".feedback-dialog")
            || el.closest(".lobsy-dialog")
            || el.closest(".lobsy-dialog-backdrop")
            || el.closest(".share-modal-backdrop")
            || el.closest(".lobsy-assistant"));
    }

    window.lobsyFeedback = {
        getMetadata: function () {
            return {
                userAgent: navigator.userAgent || "",
                platform: navigator.platform || "",
                language: navigator.language || "",
                viewportWidth: window.innerWidth || 0,
                viewportHeight: window.innerHeight || 0
            };
        },
        captureScreenshot: function () {
            return loadHtml2Canvas().then(function (html2canvas) {
                return html2canvas(document.body, {
                    scale: Math.min(1, 1100 / Math.max(document.documentElement.scrollWidth || 1, 1)),
                    useCORS: true,
                    logging: false,
                    backgroundColor: "#ffffff",
                    ignoreElements: shouldIgnore
                });
            }).then(function (canvas) {
                var maxWidth = 1400;
                if (canvas.width > maxWidth) {
                    var copy = document.createElement("canvas");
                    var ratio = maxWidth / canvas.width;
                    copy.width = maxWidth;
                    copy.height = Math.round(canvas.height * ratio);
                    var ctx = copy.getContext("2d");
                    ctx.drawImage(canvas, 0, 0, copy.width, copy.height);
                    return copy.toDataURL("image/jpeg", 0.72);
                }
                return canvas.toDataURL("image/jpeg", 0.72);
            });
        }
    };
})();
