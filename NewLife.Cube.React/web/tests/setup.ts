import '@testing-library/jest-dom';

// ── antd v5 在 jsdom 环境下需要的全局 mock ─────────────────────────
// matchMedia：antd Grid/响应式组件在 jsdom 缺失时抛错
if (typeof window !== 'undefined' && !window.matchMedia) {
  Object.defineProperty(window, 'matchMedia', {
    writable: true,
    value: (query: string) => ({
      matches: false,
      media: query,
      onchange: null,
      addListener: () => {},
      removeListener: () => {},
      addEventListener: () => {},
      removeEventListener: () => {},
      dispatchEvent: () => false,
    }),
  });
}

// ResizeObserver：antd 部分组件（如 Select 下拉）依赖
if (typeof window !== 'undefined' && !window.ResizeObserver) {
  class ResizeObserverMock {
    observe() {}
    unobserve() {}
    disconnect() {}
  }
  (window as unknown as Record<string, unknown>).ResizeObserver = ResizeObserverMock;
}

// getComputedStyle：jsdom 返回空对象可能导致 antd 计算异常，补最小实现
if (typeof window !== 'undefined' && !window.getComputedStyle) {
  (window as unknown as Record<string, unknown>).getComputedStyle = () => ({ getPropertyValue: () => '' });
}
