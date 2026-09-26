/* Lobsy PWA service worker — development / always-on shell.
 * Caches static assets for instant loads and handles Web Push. */
var CACHE_VERSION = "lobsy-shell-published-v20260926-device-sessions";
var SHELL_CACHE = CACHE_VERSION + "-shell";
var IMAGE_CACHE = "lobsy-images-v2";
var OFFLINE_URL = "/offline.html";

var PRECACHE = [
    OFFLINE_URL,
    "/manifest.webmanifest?v=20260925-coral",
    "/css/app.min.css?v=20260926-career-progress",
    "/js/app-core.js?v=20260925-coralicon",
    "/icons/icon-192.png?v=20260925-coral",
    "/icons/icon-512.png?v=20260925-coral",
    "/favicon.png?v=20260925-coral",
    "/images/brand/lobsy-128.webp"
];

self.addEventListener("install", function (event) {
    event.waitUntil(
        caches.open(SHELL_CACHE).then(function (cache) {
            return cache.addAll(PRECACHE.map(function (url) {
                return new Request(url, { cache: "reload" });
            })).catch(function () {
                /* Offline install still activates; missing assets filled on fetch. */
            });
        }).then(function () {
            return self.skipWaiting();
        })
    );
});

self.addEventListener("activate", function (event) {
    event.waitUntil(
        caches.keys().then(function (keys) {
            return Promise.all(keys.map(function (key) {
                if (key !== SHELL_CACHE && key !== IMAGE_CACHE && key.indexOf("lobsy-") === 0) {
                    return caches.delete(key);
                }
            }));
        }).then(function () {
            return self.clients.claim();
        })
    );
});

function isSameOrigin(url) {
    try {
        return new URL(url).origin === self.location.origin;
    } catch (e) {
        return false;
    }
}

function isStaticAsset(pathname) {
    return pathname.indexOf("/css/") === 0
        || pathname.indexOf("/js/") === 0
        || pathname.indexOf("/icons/") === 0
        || pathname.indexOf("/lib/") === 0
        || pathname === "/manifest.webmanifest"
        || pathname === "/favicon.ico"
        || pathname === "/favicon.png"
        || pathname.indexOf("/_framework/") === 0;
}

self.addEventListener("fetch", function (event) {
    var request = event.request;
    if (request.method !== "GET" || !isSameOrigin(request.url)) {
        return;
    }

    var url = new URL(request.url);

    /* Never cache API / auth / Blazor circuit traffic. */
    if (url.pathname.indexOf("/api/") === 0
        || url.pathname.indexOf("/account/") === 0
        || url.pathname.indexOf("/_blazor") === 0
        || url.search.indexOf("_blazor") >= 0) {
        return;
    }

    if (url.pathname.indexOf("/images/") === 0) {
        event.respondWith(cacheFirst(IMAGE_CACHE, request));
        return;
    }

    if (request.mode === "navigate") {
        event.respondWith(
            fetch(request).then(function (response) {
                return response;
            }).catch(function () {
                return caches.match(OFFLINE_URL).then(function (offline) {
                    return offline || caches.match(request);
                });
            })
        );
        return;
    }

    if (isStaticAsset(url.pathname)) {
        event.respondWith(staleWhileRevalidate(SHELL_CACHE, request));
    }
});

function cacheFirst(cacheName, request) {
    return caches.open(cacheName).then(function (cache) {
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
    });
}

function staleWhileRevalidate(cacheName, request) {
    return caches.open(cacheName).then(function (cache) {
        return cache.match(request).then(function (hit) {
            var network = fetch(request).then(function (response) {
                if (response && response.ok) {
                    cache.put(request, response.clone());
                }
                return response;
            }).catch(function () {
                return hit;
            });
            return hit || network;
        });
    });
}

self.addEventListener("push", function (event) {
    var payload = { title: "Lobsy", body: "Je hebt een nieuwe update.", url: "/" };
    try {
        if (event.data) {
            var data = event.data.json();
            payload.title = data.title || payload.title;
            payload.body = data.body || payload.body;
            payload.url = data.url || data.deepLink || payload.url;
            payload.tag = data.tag || data.category || "lobsy";
        }
    } catch (e) {
        try {
            payload.body = event.data ? event.data.text() : payload.body;
        } catch (ignore) { }
    }

    event.waitUntil(
        self.registration.showNotification(payload.title, {
            body: payload.body,
            icon: "/icons/icon-192.png?v=20260925-coral",
            badge: "/icons/icon-192.png?v=20260925-coral",
            tag: payload.tag || "lobsy",
            renotify: true,
            data: { url: payload.url },
            vibrate: [40, 30, 40]
        })
    );
});

self.addEventListener("notificationclick", function (event) {
    event.notification.close();
    var target = (event.notification.data && event.notification.data.url) || "/";
    event.waitUntil(
        self.clients.matchAll({ type: "window", includeUncontrolled: true }).then(function (clients) {
            for (var i = 0; i < clients.length; i++) {
                var client = clients[i];
                if ("focus" in client) {
                    client.navigate(target);
                    return client.focus();
                }
            }
            if (self.clients.openWindow) {
                return self.clients.openWindow(target);
            }
        })
    );
});
