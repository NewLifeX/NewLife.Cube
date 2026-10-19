/**
 * jsdom 兼容垫片（组件测试专用，非用例文件）
 *
 * Element Plus 的表格与弹层（`el-table` / `el-popover` / `el-date-picker`）依赖
 * `ResizeObserver`、`matchMedia`、`scrollIntoView`，jsdom 没有这三件时
 * 表格会「渲染成功但一行都不出」——表现成 `wrapper.text()` 全空，
 * 容易误判成组件 bug。口径与 `core/__tests__/FormContent.fieldtypes.spec.ts`
 * 和 `LovSelectTable.spec.ts` 里的既有垫片一致。
 */
class ResizeObserverStub {
  observe() {}
  unobserve() {}
  disconnect() {}
}

if (!globalThis.ResizeObserver) {
  globalThis.ResizeObserver = ResizeObserverStub as unknown as typeof ResizeObserver;
}

if (typeof window !== 'undefined' && !window.matchMedia) {
  window.matchMedia = ((query: string) => ({
    matches: false,
    media: query,
    onchange: null,
    addListener() {},
    removeListener() {},
    addEventListener() {},
    removeEventListener() {},
    dispatchEvent() {
      return false;
    },
  })) as unknown as typeof window.matchMedia;
}

if (typeof Element !== 'undefined' && !Element.prototype.scrollIntoView) {
  Element.prototype.scrollIntoView = () => {};
}

export {};
