(function () {
  "use strict";

  function classList(el) {
    return el && el.classList ? el.classList : null;
  }

  function toggleClass(el, className, on) {
    var list = classList(el);
    if (!list) {
      return;
    }
    list.toggle(className, on);
  }

  function addClass(el, className) {
    var list = classList(el);
    if (!list) {
      return;
    }
    list.add(className);
  }

  function removeClass(el, className) {
    var list = classList(el);
    if (!list) {
      return;
    }
    list.remove(className);
  }

  function query(root, selector) {
    var scope = root || document;
    if (!scope || typeof scope.querySelector !== "function") {
      return null;
    }
    return scope.querySelector(selector);
  }

  function byId(id) {
    if (!id || typeof document === "undefined") {
      return null;
    }
    return document.getElementById(id);
  }

  window.jobsyDomSafe = {
    classList: classList,
    toggleClass: toggleClass,
    addClass: addClass,
    removeClass: removeClass,
    query: query,
    byId: byId,
  };
})();
