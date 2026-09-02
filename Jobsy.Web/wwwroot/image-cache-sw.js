/* Same-origin image cache for Render egress: brand, logos, vacancy SVGs. */
var CACHE = "lobsy-images-v1";

self.addEventListener("install", function (event) {
    self.skipWaiting();
});

self.addEventListener("activate", function (event) {
    event.waitUntil(self.clients.claim());
});

self.addEventListener("fetch", function (event) {
    var request = event.request;
    if (request.method !== "GET") {
        return;
    }
    if (request.mode === "navigate") {
        return;
    }
    var url;
    try {
        url = new URL(request.url);
    } catch (e) {
        return;
    }
    if (url.origin !== self.location.origin) {
        return;
    }
    if (!url.pathname.startsWith("/images/")) {
        return;
    }

    event.respondWith(
        caches.open(CACHE).then(function (cache) {
            return cache.match(request).then(function (hit) {
                if (hit) {
                    return hit;
                }
                return fetch(request).then(function (response) {
                    if (response && response.ok) {
                        cache.put(request, response.clone());
                    }
                    return response;
                });
            });
        })
    );
});
