export function bindAdminSearchShortcuts(dotNetRef) {
  if (typeof window === "undefined" || !dotNetRef) return;
  if (window.__lobsyAdminSearchBound) return;
  window.__lobsyAdminSearchBound = true;
  window.__lobsyAdminSearchRef = dotNetRef;

  window.__lobsyAdminSearchHandler = function (e) {
    var tag = (e.target && e.target.tagName) || "";
    var typing = tag === "INPUT" || tag === "TEXTAREA" || tag === "SELECT" || (e.target && e.target.isContentEditable);
    var isCtrlK = (e.ctrlKey || e.metaKey) && (e.key === "k" || e.key === "K");
    var isSlash = e.key === "/" && !typing;
    if (!isCtrlK && !isSlash) return;
    e.preventDefault();
    var ref = window.__lobsyAdminSearchRef;
    if (ref && ref.invokeMethodAsync) {
      ref.invokeMethodAsync("FocusFromShortcutAsync");
    }
  };
  document.addEventListener("keydown", window.__lobsyAdminSearchHandler);
}

export function unbindAdminSearchShortcuts() {
  if (typeof window === "undefined") return;
  if (window.__lobsyAdminSearchHandler) {
    document.removeEventListener("keydown", window.__lobsyAdminSearchHandler);
  }
  window.__lobsyAdminSearchBound = false;
  window.__lobsyAdminSearchRef = null;
  window.__lobsyAdminSearchHandler = null;
}
