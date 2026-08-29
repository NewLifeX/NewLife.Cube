/**
 * 配置系统单元测试（E4）
 *
 * 覆盖：默认配置返回、window._CUBE_CONFIG_ 运行时覆盖（最高优先级）、
 * 深合并（部分字段覆盖不影响同级其它字段）、ui.layout 嵌套合并。
 */
import { describe, expect, it, beforeEach } from 'vitest';
import { getConfig, defaultConfig } from '@/configure';

/** 清理运行时配置，保证每个用例从默认配置开始 */
function clearRuntimeConfig() {
  delete (window as unknown as Record<string, unknown>)._CUBE_CONFIG_;
}

describe('configure 配置系统', () => {
  beforeEach(() => {
    clearRuntimeConfig();
  });

  it('无运行时配置时返回默认配置', () => {
    const cfg = getConfig();
    expect(cfg.base.title).toBe('魔方系统');
    expect(cfg.base.env).toBe('dev');
    expect(cfg.request.timeout).toBe(50000);
    expect(cfg.request.tokenHeaderPrefix).toBe('Bearer ');
    expect(cfg.auth.loginPageUrl).toBe('/login');
    expect(cfg.auth.redirectKey).toBe('r');
    expect(cfg.ui.layout.header.show).toBe(true);
    expect(cfg.ui.layout.sider.width).toBe(220);
    expect(cfg.menu.pathField).toBe('url');
    expect(cfg.theme.defaultMode).toBe('dark');
  });

  it('默认配置导出与 getConfig 基准一致', () => {
    expect(defaultConfig.base.title).toBe('魔方系统');
    expect(defaultConfig.auth.loginPageUrl).toBe('/login');
  });

  it('window._CUBE_CONFIG_ 覆盖顶层字段且保留其余默认', () => {
    (window as unknown as Record<string, unknown>)._CUBE_CONFIG_ = {
      base: { title: '自定义系统' },
    };
    const cfg = getConfig();
    expect(cfg.base.title).toBe('自定义系统');
    // 未被覆盖的字段保留默认
    expect(cfg.base.env).toBe('dev');
    expect(cfg.auth.loginPageUrl).toBe('/login');
    expect(cfg.request.timeout).toBe(50000);
  });

  it('深合并：覆盖 request 部分字段不影响同级其它字段', () => {
    (window as unknown as Record<string, unknown>)._CUBE_CONFIG_ = {
      request: { timeout: 10000 },
    };
    const cfg = getConfig();
    expect(cfg.request.timeout).toBe(10000);
    expect(cfg.request.baseUrl).toBe(defaultConfig.request.baseUrl);
    expect(cfg.request.tokenHeaderPrefix).toBe('Bearer ');
  });

  it('ui.layout 嵌套合并：覆盖 header.show 保留 fixed/height', () => {
    (window as unknown as Record<string, unknown>)._CUBE_CONFIG_ = {
      ui: { layout: { header: { show: false } } },
    };
    const cfg = getConfig();
    expect(cfg.ui.layout.header.show).toBe(false);
    expect(cfg.ui.layout.header.fixed).toBe(true);
    expect(cfg.ui.layout.header.height).toBe(56);
    // sider/footer 不受影响
    expect(cfg.ui.layout.sider.width).toBe(220);
    expect(cfg.ui.layout.footer.show).toBe(true);
  });

  it('运行时配置为 null 时不抛错且返回默认', () => {
    (window as unknown as Record<string, unknown>)._CUBE_CONFIG_ = null;
    const cfg = getConfig();
    expect(cfg.base.title).toBe('魔方系统');
  });
});
