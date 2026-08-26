import { describe, expect, it } from 'vitest';
import {
  isServiceControllerPath,
  parseServiceLeaf,
  resolveServicePageGuide,
} from './servicePage';

describe('isServiceControllerPath', () => {
  it('识别顶层与 Area 前缀下的服务控制器', () => {
    expect(isServiceControllerPath('/Auth')).toBe(true);
    expect(isServiceControllerPath('/vTest1/Auth')).toBe(true);
    expect(isServiceControllerPath('vTest1/Mfa')).toBe(true);
    expect(isServiceControllerPath('/vTest1/Sso')).toBe(true);
    expect(isServiceControllerPath('/Ai')).toBe(true);
    expect(isServiceControllerPath('/vTest1/Ai')).toBe(true);
    expect(isServiceControllerPath('/vTest1/Automation')).toBe(true);
    expect(isServiceControllerPath('/Cube/Automation')).toBe(true);
    expect(isServiceControllerPath('/Cube')).toBe(true);
    expect(isServiceControllerPath('/vTest1/Cube')).toBe(true);
  });

  it('Admin/Cube 是对象设置，Cube 区域实体不是服务页', () => {
    expect(isServiceControllerPath('/Admin/Cube')).toBe(false);
    expect(isServiceControllerPath('/Cube/App')).toBe(false);
    expect(isServiceControllerPath('/Admin/User')).toBe(false);
    expect(isServiceControllerPath('/School/Class')).toBe(false);
  });
});

describe('parseServiceLeaf / guide', () => {
  it('vTest1/Auth → auth 指南', () => {
    expect(parseServiceLeaf('/vTest1/Auth')).toBe('auth');
    expect(resolveServicePageGuide('/vTest1/Auth')?.title).toBe('认证');
  });

  it('非服务路径返回 null', () => {
    expect(parseServiceLeaf('/Admin/Cube')).toBeNull();
    expect(resolveServicePageGuide('/Admin/User')).toBeNull();
  });
});
