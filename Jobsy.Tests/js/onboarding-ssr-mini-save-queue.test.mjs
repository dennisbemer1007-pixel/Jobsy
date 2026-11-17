import assert from "node:assert/strict";
import test from "node:test";

test("enqueueSsrMiniSave runs puts sequentially", async () => {
  const ssrMiniSaveChains = new WeakMap();
  const form = {};

  function ssrMiniSaveChain(target) {
    if (!ssrMiniSaveChains.has(target)) {
      ssrMiniSaveChains.set(target, Promise.resolve());
    }
    return ssrMiniSaveChains.get(target);
  }

  function enqueueSsrMiniSave(target, task) {
    const next = ssrMiniSaveChain(target).then(task).catch(() => {});
    ssrMiniSaveChains.set(target, next);
    return next;
  }

  const order = [];
  enqueueSsrMiniSave(form, async () => {
    order.push(1);
    await new Promise((r) => setTimeout(r, 30));
  });
  enqueueSsrMiniSave(form, async () => {
    order.push(2);
  });
  enqueueSsrMiniSave(form, async () => {
    order.push(3);
  });
  await ssrMiniSaveChain(form);
  assert.deepEqual(order, [1, 2, 3]);
});