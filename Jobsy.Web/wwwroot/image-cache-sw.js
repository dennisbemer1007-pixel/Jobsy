/* Legacy image-cache service worker — no longer used.
 * Kept as a tiny self-unregistering stub so old clients drop the registration. */
self.addEventListener("install", function (event) {
    self.skipWaiting();
});
self.addEventListener("activate", function (event) {
    event.waitUntil(
        self.registration.unregister().then(function () {
            return self.clients.matchAll();
        }).then(function (clients) {
            clients.forEach(function (client) {
                if (client && typeof client.navigate === "function") {
                    try { client.navigate(client.url); } catch (e) { }
                }
            });
        })
    );
});
