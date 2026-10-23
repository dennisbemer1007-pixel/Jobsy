(function () {
    "use strict";

    var BIKE_SVG = "<svg class=\"ic\" viewBox=\"0 0 24 24\" width=\"16\" height=\"16\" aria-hidden=\"true\">"
        + "<circle cx=\"6\" cy=\"16\" r=\"3.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\"/>"
        + "<circle cx=\"18\" cy=\"16\" r=\"3.5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\"/>"
        + "<path d=\"M6 16l4-7h5l3 7M10 9 8.5 6H7M12.5 16 15 9\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"
        + "</svg>";

    var ICO = {
        truck: "<path d=\"M3 6.5h11v10H3zM14 10h4l3 3.5v3h-7\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/><circle cx=\"7\" cy=\"17.5\" r=\"1.8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/><circle cx=\"17.5\" cy=\"17.5\" r=\"1.8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/>",
        leaf: "<path d=\"M5 19c0-8 5-13 14-14 0 9-5 14-13 14\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/><path d=\"M5 19 13 11\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/>",
        care: "<path d=\"M12 20s-7.5-4.6-7.5-10.2A4.3 4.3 0 0 1 12 7.4a4.3 4.3 0 0 1 7.5 2.4C19.5 15.4 12 20 12 20z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>",
        box: "<path d=\"M4 8l8-4 8 4v8l-8 4-8-4z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/><path d=\"M4 8l8 4 8-4M12 12v8\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/>",
        cup: "<path d=\"M5 8h11v5a5 5 0 0 1-5 5h-1a5 5 0 0 1-5-5z\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/><path d=\"M16 9.5h1.5a2.5 2.5 0 0 1 0 5H16\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\"/>",
        shop: "<path d=\"M4 9l1.5-4.5h13L20 9M4 9h16v10H4zM9 19v-5h6v5\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.6\" stroke-linecap=\"round\" stroke-linejoin=\"round\"/>"
    };

    function workTypeKey(row) {
        var name = String((row && (row.categoryName || row.workTypes)) || "").toLowerCase();
        if (name.indexOf("logist") !== -1 || name.indexOf("truck") !== -1) {
            return "truck";
        }
        if (name.indexOf("tuin") !== -1 || name.indexOf("leaf") !== -1) {
            return "leaf";
        }
        if (name.indexOf("zorg") !== -1 || name.indexOf("care") !== -1) {
            return "care";
        }
        if (name.indexOf("product") !== -1 || name.indexOf("inpak") !== -1) {
            return "box";
        }
        if (name.indexOf("horeca") !== -1) {
            return "cup";
        }
        if (name.indexOf("winkel") !== -1) {
            return "shop";
        }
        return "box";
    }

    function workTypeColors(key) {
        var map = {
            leaf: { bg: "#E3EEDD", fg: "#3F6E31" },
            truck: { bg: "#F3EADB", fg: "#7E5E2C" },
            care: { bg: "#E5EEE9", fg: "#2F7D5B" },
            box: { bg: "#F5E8E1", fg: "#97563F" },
            cup: { bg: "#F5E8E1", fg: "#97563F" },
            shop: { bg: "#F3EADB", fg: "#7E5E2C" }
        };
        return map[key] || map.box;
    }

    function pinW(scale, selected) {
        var s = scale || 1;
        var w = 40 * s;
        var h = 50 * s;
        var sw = selected ? 3.5 : 2.5;
        return "<svg width=\"" + w + "\" height=\"" + h + "\" viewBox=\"0 0 40 50\" aria-hidden=\"true\">"
            + "<path d=\"M20 47C20 47 4 31 4 19A16 16 0 0 1 36 19C36 31 20 47 20 47Z\" fill=\"#1F2F55\" stroke=\"#fff\" stroke-width=\"" + sw + "\"/>"
            + "<g fill=\"none\" stroke=\"#fff\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\">"
            + "<rect x=\"13.5\" y=\"11\" width=\"13\" height=\"16\" rx=\"1.5\"/><path d=\"M17 15h1.5M21.5 15H23M17 19h1.5M21.5 19H23M18.5 27v-3.5h3V27\"/>"
            + "</g></svg>";
    }

    function pinI(scale, selected, badge) {
        var s = scale || 1;
        var w = 46 * s;
        var h = 50 * s;
        var sw = selected ? 3.5 : 2.5;
        var via = badge !== false
            ? "<g><rect x=\"27\" y=\"0\" width=\"19\" height=\"13\" rx=\"6.5\" fill=\"#fff\" stroke=\"#B4500F\" stroke-width=\"1.5\"/>"
            + "<text x=\"36.5\" y=\"9.8\" text-anchor=\"middle\" font-family=\"Noto Sans,sans-serif\" font-weight=\"800\" font-size=\"8.5\" fill=\"#8A3B08\">via</text></g>"
            : "";
        return "<svg width=\"" + w + "\" height=\"" + h + "\" viewBox=\"0 0 46 50\" aria-hidden=\"true\">"
            + "<path d=\"M10 4H34A7 7 0 0 1 41 11V32A7 7 0 0 1 34 39H28L22 47L16 39H10A7 7 0 0 1 3 32V11A7 7 0 0 1 10 4Z\" fill=\"#B4500F\" stroke=\"#fff\" stroke-width=\"" + sw + "\"/>"
            + "<g fill=\"none\" stroke=\"#fff\" stroke-width=\"1.9\" stroke-linecap=\"round\" stroke-linejoin=\"round\">"
            + "<circle cx=\"17\" cy=\"16\" r=\"3.2\"/><circle cx=\"28\" cy=\"16\" r=\"3.2\"/><path d=\"M11 30c.6-4 3-6 6-6s5.4 2 6 6M22 30c.6-4 3-6 6-6s5.4 2 6 6\"/>"
            + "</g>" + via + "</svg>";
    }

    function isIntermediaryRow(row) {
        if (!row) {
            return false;
        }
        if (row.publicMapRadiusKm > 0) {
            return true;
        }
        var ob = String(row.offeredBy || "").trim();
        if (!ob) {
            return false;
        }
        var co = String(row.company || "").trim();
        return ob.length > 0 && ob.toLowerCase() !== co.toLowerCase();
    }

    function typeMarkerHtml(row, mini) {
        var wScale = mini ? 0.44 : 0.48;
        var iScale = mini ? 0.44 : 0.48;
        if (isIntermediaryRow(row)) {
            return "<span class=\"tm i\">" + pinI(iScale, false, false)
                + "<span>Via intermediair</span></span>";
        }
        return "<span class=\"tm w\">" + pinW(wScale)
            + "<span>Bij werkgever</span></span>";
    }

    function workTypeIconSvg(row, big) {
        var key = workTypeKey(row);
        var cls = big ? " kb-wt-ico--big" : "";
        return "<svg class=\"kb-wt-ico" + cls + "\" viewBox=\"0 0 24 24\" aria-hidden=\"true\">" + (ICO[key] || ICO.box) + "</svg>";
    }

    window.jobsyBanenkaartMapIcons = {
        bikeSvg: BIKE_SVG,
        pinW: pinW,
        pinI: pinI,
        isIntermediaryRow: isIntermediaryRow,
        typeMarkerHtml: typeMarkerHtml,
        workTypeKey: workTypeKey,
        workTypeColors: workTypeColors,
        workTypeIconSvg: workTypeIconSvg
    };
})();
