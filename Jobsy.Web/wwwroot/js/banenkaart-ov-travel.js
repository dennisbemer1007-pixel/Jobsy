(function () {
    "use strict";

    /**
     * OV banenkaart: pins and list use server-side / catalog travel filtering.
     * No isochrone ring on the map; iso-mode becomes "none" only after map pins are applied.
     */
    var ovIsoPending = false;

    function normalizeTransport(t) {
        return String(t || "").trim();
    }

    function isOvTransport(t) {
        return normalizeTransport(t).toUpperCase() === "OV";
    }

    window.jobsyBanenkaartOvTravel = {
        isOvTransport: isOvTransport,
        skipClientRingOutside: isOvTransport,
        pinsCacheIncludesTransport: isOvTransport,
        beginOvSession: function () {
            ovIsoPending = true;
        },
        isOvIsoPending: function () {
            return ovIsoPending;
        },
        completeAfterMapPins: function () {
            if (!ovIsoPending) {
                return;
            }
            ovIsoPending = false;
            try {
                document.dispatchEvent(new CustomEvent("jobsy-bk-ov-pins-applied", { bubbles: true }));
            } catch (eEv) { }
        }
    };
})();
