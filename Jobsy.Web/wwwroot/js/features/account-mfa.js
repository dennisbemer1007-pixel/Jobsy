(function () {
  function textOf(el) {
    return (el && (el.textContent || '') || '').replace(/\s+/g, '');
  }

  function setStatus(root, label) {
    var status = root.querySelector('[data-copy-status]');
    if (!status) return;
    status.textContent = label || '';
    window.setTimeout(function () {
      if (status.textContent === label) status.textContent = '';
    }, 3000);
  }

  document.addEventListener('click', function (e) {
    var btn = e.target.closest('[data-copy-target]');
    if (btn) {
      var sel = btn.getAttribute('data-copy-target');
      var target = sel ? document.querySelector(sel) : null;
      var value = textOf(target);
      if (value && navigator.clipboard && navigator.clipboard.writeText) {
        navigator.clipboard.writeText(value).then(function () {
          setStatus(btn.parentElement || document, btn.getAttribute('data-copied-label') || 'Gekopieerd');
        }).catch(function () {});
      }
      return;
    }

    var dl = e.target.closest('[data-download-codes]');
    if (dl) {
      var list = document.querySelector('#mfa-recovery-list');
      if (!list) return;
      var codes = Array.prototype.map.call(list.querySelectorAll('code'), function (c) {
        return (c.textContent || '').trim();
      }).filter(Boolean);
      var lines = [];
      var headerEmail = dl.getAttribute('data-header-email') || '';
      var headerDate = dl.getAttribute('data-header-date') || '';
      var headerNote = dl.getAttribute('data-header-note') || '';
      if (headerEmail) lines.push(headerEmail);
      if (headerDate) lines.push(headerDate);
      if (headerNote) lines.push(headerNote);
      if (lines.length) lines.push('');
      lines = lines.concat(codes);
      var blob = new Blob([lines.join('\n')], { type: 'text/plain' });
      var url = URL.createObjectURL(blob);
      var a = document.createElement('a');
      a.href = url;
      a.download = 'lobsy-herstelcodes.txt';
      a.click();
      URL.revokeObjectURL(url);
      return;
    }

    if (e.target.closest('[data-print-codes]')) {
      window.print();
    }
  });

  function wireContinue() {
    var form = document.getElementById('mfa-recovery-continue');
    if (!form) return;
    var saved = document.getElementById('mfa-codes-saved');
    var btn = document.getElementById('mfa-continue-btn');
    if (!saved || !btn) return;
    btn.disabled = !saved.checked;
    saved.addEventListener('change', function () {
      btn.disabled = !saved.checked;
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', wireContinue);
  } else {
    wireContinue();
  }
})();
