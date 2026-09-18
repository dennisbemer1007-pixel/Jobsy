/* Kill-switch: do not intercept fetches. Unregister so MapLibre/Blazor stay on the network. */
self.addEventListener("install", function () {
    self.skipWaiting();
});

self.addEventListener("activate", function (event) {
    event.waitUntil(
        self.registration.unregister().then(function () {
            return self.clients.matchAll({ type: "window" });
        }).then(function (clients) {
            clients.forEach(function (client) {
                if (client && typeof client.navigate === "function") {
                    client.navigate(client.url);
                }
            });
        })
    );
});
