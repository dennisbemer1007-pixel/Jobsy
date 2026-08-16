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
            script.src = "/lib/html2canvas/html2canvas.min.js";
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

    function isChrome(el) {
        if (!el || !el.closest) {
            return false;
        }
        return !!(el.closest(".feedback-widget")
            || el.closest(".feedback-dialog")
            || el.closest(".lobsy-dialog")
            || el.closest(".lobsy-dialog-backdrop")
            || el.closest(".share-modal-backdrop")
            || el.closest(".lobsy-assistant")
            || el.closest(".cookie-consent"));
    }

    function hideChrome(doc) {
        var nodes = doc.querySelectorAll(
            ".feedback-widget, .feedback-dialog, .lobsy-dialog, .lobsy-dialog-backdrop, .share-modal-backdrop, .lobsy-assistant, .cookie-consent");
        for (var i = 0; i < nodes.length; i++) {
            nodes[i].style.visibility = "hidden";
        }
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
            var width = Math.max(1, window.innerWidth || 1);
            var height = Math.max(1, window.innerHeight || 1);
            return loadHtml2Canvas().then(function (html2canvas) {
                return html2canvas(document.documentElement, {
                    x: window.scrollX || 0,
                    y: window.scrollY || 0,
                    width: width,
                    height: height,
                    windowWidth: document.documentElement.scrollWidth,
                    windowHeight: document.documentElement.scrollHeight,
                    scale: Math.min(1, 1280 / width),
                    useCORS: true,
                    logging: false,
                    backgroundColor: "#ffffff",
                    ignoreElements: isChrome,
                    onclone: function (doc) {
                        hideChrome(doc);
                    }
                });
            }).then(function (canvas) {
                var maxWidth = 1280;
                if (canvas.width > maxWidth) {
                    var copy = document.createElement("canvas");
                    var ratio = maxWidth / canvas.width;
                    copy.width = maxWidth;
                    copy.height = Math.round(canvas.height * ratio);
                    var ctx = copy.getContext("2d");
                    ctx.drawImage(canvas, 0, 0, copy.width, copy.height);
                    return copy.toDataURL("image/jpeg", 0.7);
                }
                return canvas.toDataURL("image/jpeg", 0.7);
            });
        }
    };
})();
