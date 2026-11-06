(function () {
  "use strict";

  var READ_PENDING_KEY = "sollicitaties-threads-read-pending-v1";
  var READ_ACK_KEY = "sollicitaties-threads-read-ack-v1";
  var MARK_READ_PATH = "/api/sollicitaties-client/berichten/mark-read";
  var LEGACY_KEYS = [
    "sollicitaties-threads-read-v1",
    "sollicitaties-threads-read-pending",
    "sollicitaties-threads-read-ack",
  ];

  function readIdSet(key) {
    try {
      var raw = sessionStorage.getItem(key);
      if (!raw) {
        return [];
      }
      var parsed = JSON.parse(raw);
      return Array.isArray(parsed)
        ? parsed.filter(function (id) {
            return typeof id === "string" && id;
          })
        : [];
    } catch (_e) {
      return [];
    }
  }

  function writeIdSet(key, ids) {
    try {
      sessionStorage.setItem(key, JSON.stringify(ids));
    } catch (_e2) {
      /* ignore */
    }
  }

  function purgeLegacyStorage() {
    LEGACY_KEYS.forEach(function (key) {
      try {
        sessionStorage.removeItem(key);
      } catch (_e) {
        /* ignore */
      }
    });
  }

  function markAcked(threadId) {
    var ack = readIdSet(READ_ACK_KEY);
    if (ack.indexOf(threadId) < 0) {
      ack.push(threadId);
      writeIdSet(READ_ACK_KEY, ack);
    }
    writeIdSet(
      READ_PENDING_KEY,
      readIdSet(READ_PENDING_KEY).filter(function (id) {
        return id !== threadId;
      })
    );
  }

  function flushPendingViaBff() {
    if (document.querySelector("[data-testid=\"sollicitaties-berichten-tab\"]")) {
      return;
    }
    var pending = readIdSet(READ_PENDING_KEY);
    pending.forEach(function (threadId) {
      fetch(MARK_READ_PATH + "?applicationId=" + encodeURIComponent(threadId), {
        method: "POST",
        credentials: "same-origin",
        keepalive: true,
        headers: { "X-Requested-With": "XMLHttpRequest" },
      })
        .then(function (res) {
          if (res.ok) {
            markAcked(threadId);
          }
        })
        .catch(function () {
          /* keep pending for sollicitaties tab */
        });
    });
  }

  purgeLegacyStorage();
  flushPendingViaBff();
})();
