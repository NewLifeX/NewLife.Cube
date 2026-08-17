import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { applyDocumentFavicon } from './favicon';

type FakeLink = {
  rel: string;
  href: string;
  getAttribute: (name: string) => string | null;
  setAttribute: (name: string, value: string) => void;
  remove: () => void;
  attrs: Record<string, string>;
};

function createFakeDocument() {
  const links: FakeLink[] = [];
  return {
    createElement(tag: string) {
      if (tag !== 'link') throw new Error(`unexpected tag ${tag}`);
      const el: FakeLink = {
        rel: '',
        href: '',
        attrs: {},
        getAttribute(name) {
          return this.attrs[name] ?? null;
        },
        setAttribute(name, value) {
          this.attrs[name] = value;
        },
        remove() {
          const i = links.indexOf(el);
          if (i >= 0) links.splice(i, 1);
        },
      };
      return el;
    },
    querySelector(sel: string) {
      if (!sel.includes('data-cube-favicon')) return null;
      return links[0] ?? null;
    },
    querySelectorAll(sel: string) {
      if (!sel.includes('data-cube-favicon')) return [];
      return [...links];
    },
    head: {
      appendChild(el: FakeLink) {
        links.push(el);
      },
    },
    get links() {
      return links;
    },
  };
}

describe('applyDocumentFavicon', () => {
  let fake: ReturnType<typeof createFakeDocument>;

  beforeEach(() => {
    fake = createFakeDocument();
    vi.stubGlobal('document', fake);
  });

  afterEach(() => {
    vi.unstubAllGlobals();
  });

  it('有 URL 时创建 favicon link', () => {
    applyDocumentFavicon('/Uploads/Cube/logo.png');
    expect(fake.links).toHaveLength(1);
    expect(fake.links[0].rel).toBe('icon');
    expect(fake.links[0].href).toBe('/Uploads/Cube/logo.png');
  });

  it('空 URL 时移除动态 favicon', () => {
    applyDocumentFavicon('/a.png');
    applyDocumentFavicon('');
    expect(fake.links).toHaveLength(0);
  });

  it('同一 URL 重复设置不额外创建节点', () => {
    applyDocumentFavicon('/a.png');
    applyDocumentFavicon('/a.png');
    expect(fake.links).toHaveLength(1);
  });
});
