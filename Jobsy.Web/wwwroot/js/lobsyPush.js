/* Lobsy Web Push helpers — browser APIs only; API calls go through Blazor. */
window.lobsyPush = (function () {
    var STORAGE_PROMPT = "lobsy.push.prompt.dismissed";

    function urlBase64ToUint8Array(base64String) {
        var padding = "=".repeat((4 - (base64String.length % 4)) % 4);
        var base64 = (base64String + padding).replace(/-/g, "+").replace(/_/g, "/");
        var raw = atob(base64);
        var output = new Uint8Array(raw.length);
        for (var i = 0; i < raw.length; i++) {
            output[i] = raw.charCodeAt(i);
        }
        return output;
    }

    function supported() {
        return !!(
            "serviceWorker" in navigator
            && "PushManager" in window
            && "Notification" in window
        );
    }

    function permission() {
        return supported() ? Notification.permission : "unsupported";
    }

    function shouldShowPrompt() {
        if (!supported() || Notification.permission !== "default") {
            return false;
        }
        try {
            return localStorage.getItem(STORAGE_PROMPT) !== "1";
        } catch (e) {
            return true;
        }
    }

    function dismissPrompt() {
        try {
            localStorage.setItem(STORAGE_PROMPT, "1");
        } catch (e) { }
    }

    async function getRegistration() {
        var existing = await navigator.serviceWorker.getRegistration();
        if (existing) {
            return existing;
        }
        var isPublished = location.hostname !== "localhost" && location.hostname !== "127.0.0.1";
        var swUrl = isPublished
            ? "/service-worker.published.js?v=20260924-pwa"
            : "/service-worker.js?v=20260924-pwa";
        return navigator.serviceWorker.register(swUrl, { scope: "/" });
    }

    async function createSubscription(vapidPublicKey) {
        if (!supported()) {
            return { ok: false, reason: "unsupported" };
        }
        if (!vapidPublicKey) {
            return { ok: false, reason: "missing-key" };
        }

        var permissionResult = await Notification.requestPermission();
        if (permissionResult !== "granted") {
            dismissPrompt();
            return { ok: false, reason: permissionResult };
        }

        var reg = await getRegistration();
        var sub = await reg.pushManager.subscribe({
            userVisibleOnly: true,
            applicationServerKey: urlBase64ToUint8Array(vapidPublicKey)
        });
        var json = sub.toJSON();
        dismissPrompt();
        return {
            ok: true,
            endpoint: json.endpoint,
            p256dh: json.keys.p256dh,
            auth: json.keys.auth
        };
    }

    async function dropSubscription() {
        if (!supported()) {
            return { ok: false, reason: "unsupported", endpoint: null };
        }
        var reg = await navigator.serviceWorker.getRegistration();
        if (!reg) {
            return { ok: true, endpoint: null };
        }
        var sub = await reg.pushManager.getSubscription();
        var endpoint = sub ? sub.endpoint : null;
        if (sub) {
            await sub.unsubscribe();
        }
        return { ok: true, endpoint: endpoint };
    }

    return {
        supported: supported,
        permission: permission,
        shouldShowPrompt: shouldShowPrompt,
        dismissPrompt: dismissPrompt,
        createSubscription: createSubscription,
        dropSubscription: dropSubscription,
        ensureRegistration: getRegistration
    };
})();
