/**
 * 地图 JS API 动态加载（OSC-261004d7f4）
 *
 * - 服务商默认地址：高德 v2.0 / 百度 v3.0 / 腾讯 gljs v1.exp
 * - 同地址去重（并发调用复用同一 Promise）；加载超时 15s
 * - 支持 CubeSetting.MapScriptUrl 覆盖默认地址（内网代理/私有化部署）
 */
import type { MapProviderKind } from '@/core/utils/mapConfig';

type AnySdk = Record<string, any>;

/** 加载超时（毫秒） */
const SCRIPT_TIMEOUT = 15000;

const GLOBAL_NAMES: Record<MapProviderKind, string> = {
  amap: 'AMap',
  baidu: 'BMap',
  tencent: 'TMap',
};

/** SDK 全局对象名（用于就绪检查） */
export function mapGlobalName(provider: MapProviderKind): string {
  return GLOBAL_NAMES[provider];
}

function defaultUrl(provider: MapProviderKind, key: string): string {
  switch (provider) {
    case 'amap':
      return `https://webapi.amap.com/maps?v=2.0&key=${encodeURIComponent(key)}`;
    case 'baidu':
      return `https://api.map.baidu.com/api?v=3.0&ak=${encodeURIComponent(key)}`;
    case 'tencent':
      return `https://map.qq.com/api/gljs?v=1.exp&key=${encodeURIComponent(key)}`;
  }
}

/** 已加载/加载中的脚本（按脚本地址去重） */
const pending = new Map<string, Promise<void>>();

/** 加载地图脚本；成功后对应全局对象可用 */
export function loadMapScript(provider: MapProviderKind, key: string, scriptUrl?: string): Promise<void> {
  const url = (scriptUrl ?? '').trim() || defaultUrl(provider, key);
  const cached = pending.get(url);
  if (cached) return cached;

  const task = new Promise<void>((resolve, reject) => {
    if ((window as AnySdk)[GLOBAL_NAMES[provider]]) {
      resolve();
      return;
    }
    const script = document.createElement('script');
    script.src = url;
    script.async = true;
    script.dataset.cubeMap = provider;
    const cleanup = () => {
      window.clearTimeout(timer);
      script.removeEventListener('load', onLoad);
      script.removeEventListener('error', onError);
    };
    const onLoad = () => {
      cleanup();
      if ((window as AnySdk)[GLOBAL_NAMES[provider]]) resolve();
      else {
        // 失败任务不能永久缓存，否则重进视图永远直接失败（onError/超时路径同步清理）
        pending.delete(url);
        reject(new Error(`地图脚本已加载但未找到全局对象 ${GLOBAL_NAMES[provider]}（${provider}）`));
      }
    };
    const onError = () => {
      cleanup();
      pending.delete(url);
      reject(new Error(`地图脚本加载失败（${provider}）`));
    };
    const timer = window.setTimeout(() => {
      cleanup();
      pending.delete(url);
      reject(new Error(`地图脚本加载超时（${provider}）`));
    }, SCRIPT_TIMEOUT);
    script.addEventListener('load', onLoad);
    script.addEventListener('error', onError);
    document.head.appendChild(script);
  });
  pending.set(url, task);
  return task;
}
