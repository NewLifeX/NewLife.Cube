import axios, { type InternalAxiosRequestConfig } from 'axios';
import type { TokenManager } from './token';

/** 同一策略对象上的可选令牌恢复。不传时保持原来的 401 处理。 */
export interface TokenRefreshOptions {
  /** 当前会话是否允许恢复。 */
  isEnabled(): boolean;
  /** 令牌轮换期间保持不变，换登录后必须变化。 */
  getSessionKey?(): string | number;
  /** 刷新并保存令牌。失败时拒绝。 */
  refresh(): Promise<void>;
}

/** 单次请求上的刷新标记。 */
export interface RefreshRequestConfig extends InternalAxiosRequestConfig {
  __cubeRefreshRetry?: boolean;
  __cubeRefreshActive?: boolean;
  __cubeRefreshFailed?: boolean;
  __cubeRefreshSessionKey?: string | number;
  __cubeRefreshReplayToken?: string;
}

interface RefreshSession {
  key: string | number | undefined;
  task?: Promise<void>;
  /** 本次刷新开始时的令牌。新令牌写入后，仍持有它的请求并入同一次刷新。 */
  rotatingFrom?: string;
  previousToken?: string;
  refreshedToken?: string;
  failed?: boolean;
  clearNotified?: boolean;
}

interface SessionKeyRead {
  ok: boolean;
  key?: string | number;
}

interface RefreshCoordinatorOptions {
  tokenManager: TokenManager;
  tokenHeaderPrefix: string;
  policy?: TokenRefreshOptions;
}

/** 请求拦截与 401 恢复的协作面。 */
export interface RefreshCoordinator {
  /** 发出前记下会话代次。重放前核对代次和令牌。 */
  prepare(config: RefreshRequestConfig): void;
  /** 刷新，或沿用刚刚轮换出的令牌。可以重放时返回 true。 */
  recover(config: RefreshRequestConfig | undefined): Promise<boolean>;
  /** 未开启自动刷新时按原逻辑清除。开启后刷新失败才清除，已经换成另一枚令牌时不清除。 */
  shouldClearToken(config: InternalAxiosRequestConfig | undefined): boolean;
}

const refreshSessions = new WeakMap<TokenRefreshOptions, RefreshSession>();
const refreshLockName = 'cube.token-refresh';

interface CrossTabLock {
  request(name: string, options: { mode?: 'exclusive' | 'shared' }, callback: () => Promise<void> | void): Promise<void>;
}

/** 有 Web Locks 时串起各页签的刷新。没有时直接执行。申请失败由调用方当成刷新失败。 */
function runExclusive(work: () => Promise<void>): Promise<void> {
  const locks = (globalThis as { navigator?: { locks?: CrossTabLock } }).navigator?.locks;
  if (!locks?.request) return work();
  return locks.request(refreshLockName, { mode: 'exclusive' }, work);
}

function readSessionKey(policy: TokenRefreshOptions | undefined): SessionKeyRead {
  try {
    return { ok: true, key: policy?.getSessionKey?.() };
  } catch {
    return { ok: false };
  }
}

function isRefreshEnabled(policy: TokenRefreshOptions | undefined): boolean {
  try {
    return policy?.isEnabled() === true;
  } catch {
    return false;
  }
}

function readAuthorization(config: InternalAxiosRequestConfig | undefined): string {
  const headers = config?.headers as { get?: (name: string) => unknown; Authorization?: unknown } | undefined;
  return String(headers?.get?.('Authorization') ?? headers?.Authorization ?? '');
}

function sameSession(config: RefreshRequestConfig, policy: TokenRefreshOptions): boolean {
  const read = readSessionKey(policy);
  return read.ok && config.__cubeRefreshSessionKey === read.key;
}

/** 轮换请求本身。它若再进入刷新，会等待自己正在执行的刷新。 */
function isRefreshExchange(config: InternalAxiosRequestConfig | undefined): boolean {
  const path = String(config?.url ?? '').split('?')[0];
  return path.endsWith('/Auth/Refresh');
}

/**
 * 创建令牌刷新协调器。同页并发按策略对象合并。跨页轮换用浏览器独占锁包住刷新和失败时的清令牌。
 * @param options 令牌管理器、头前缀和可选策略
 * @returns 请求准备、恢复和清令牌判断
 */
export function createRefreshCoordinator(options: RefreshCoordinatorOptions): RefreshCoordinator {
  const { tokenManager, tokenHeaderPrefix, policy } = options;

  function currentAuthorization(): string {
    const token = tokenManager.getToken();
    return token ? `${tokenHeaderPrefix}${token}` : '';
  }

  function shouldClearToken(config: InternalAxiosRequestConfig | undefined): boolean {
    const refreshConfig = config as RefreshRequestConfig | undefined;
    const sent = readAuthorization(refreshConfig);
    const current = currentAuthorization();
    // 刷新过程中令牌已换成另一枚：保留新登录。中途关掉自动刷新也不清。
    if (refreshConfig?.__cubeRefreshActive && current && sent && current !== sent) return false;
    // 未开启或策略不可用：每个 401 都按原逻辑清除。
    if (!policy || !isRefreshEnabled(policy)) return true;
    // 轮换接口的 401 交给刷新函数自己失败，不在这里清令牌。
    if (isRefreshExchange(refreshConfig)) return false;
    // 重放仍是 401：新令牌不可用，按原来的过期处理登出。
    if (refreshConfig?.__cubeRefreshRetry) return true;
    if (!refreshConfig) return false;
    const session = refreshSessions.get(policy);
    if (refreshConfig.__cubeRefreshFailed) {
      const token = tokenManager.getToken();
      // 刷新失败前令牌已被换成另一枚，不能清掉新登录。
      if (token && readAuthorization(refreshConfig) !== currentAuthorization()) return false;
      if (session?.clearNotified) return false;
      if (session) session.clearNotified = true;
      return true;
    }
    // 没有令牌时刷新跑不起来。同一次失败已经通知过就不再通知。
    if (!tokenManager.getToken()) {
      if (session?.clearNotified) return false;
      return true;
    }
    return false;
  }

  function prepare(config: RefreshRequestConfig): void {
    const token = tokenManager.getToken();
    if (config.__cubeRefreshRetry) {
      if (!policy || !sameSession(config, policy) || !isRefreshEnabled(policy) || token !== config.__cubeRefreshReplayToken) {
        throw new axios.CanceledError('Session changed before request replay');
      }
      return;
    }
    const read = readSessionKey(policy);
    if (read.ok) config.__cubeRefreshSessionKey = read.key;
  }

  async function recover(config: RefreshRequestConfig | undefined): Promise<boolean> {
    if (!config || !policy || config.__cubeRefreshRetry) return false;
    if (!isRefreshEnabled(policy)) return false;
    if (isRefreshExchange(config)) return false;
    if (policy.getSessionKey && !sameSession(config, policy)) {
      config.__cubeRefreshActive = true;
      return false;
    }
    config.__cubeRefreshActive = true;
    const current = tokenManager.getToken();
    if (!current) return false;
    const sent = readAuthorization(config);
    const sessionKey = config.__cubeRefreshSessionKey;
    let session = refreshSessions.get(policy);
    if (!session || session.key !== sessionKey) {
      session = { key: sessionKey };
      refreshSessions.set(policy, session);
    }
    const activeSession = session;
    const authorization = (token: string) => `${tokenHeaderPrefix}${token}`;
    const sentCurrent = sent === authorization(current);
    const sentPrevious = !!activeSession.previousToken
      && sent === authorization(activeSession.previousToken)
      && activeSession.refreshedToken === current;
    const sentRotating = !!activeSession.task
      && !!activeSession.rotatingFrom
      && sent === authorization(activeSession.rotatingFrom);
    if (!sentCurrent && !sentRotating) {
      if (!sentPrevious) return false;
      config.__cubeRefreshReplayToken = current;
      return true;
    }

    if (!activeSession.task) {
      activeSession.failed = false;
      activeSession.clearNotified = false;
      activeSession.rotatingFrom = current;
      const startedWith = current;
      activeSession.task = Promise.resolve().then(async () => {
        if (!sameSession(config, policy) || !isRefreshEnabled(policy) || tokenManager.getToken() !== startedWith) return;
        try {
          await runExclusive(async () => {
            if (!sameSession(config, policy) || !isRefreshEnabled(policy)) return;
            const latest = tokenManager.getToken();
            // 等锁期间另一页已写好新令牌，直接沿用，不再请求刷新。
            if (latest && latest !== startedWith) {
              activeSession.previousToken = startedWith;
              activeSession.refreshedToken = latest;
              return;
            }
            if (latest !== startedWith) {
              activeSession.failed = true;
              return;
            }
            try {
              await policy.refresh();
            } catch {
              // 仍是出发时那枚才清。另一页已经写成新令牌时留下。
              if (tokenManager.getToken() === startedWith) {
                activeSession.failed = true;
                tokenManager.clearToken();
              }
              return;
            }
            if (!sameSession(config, policy) || !isRefreshEnabled(policy) || refreshSessions.get(policy) !== activeSession) return;
            const refreshed = tokenManager.getToken();
            if (!refreshed || refreshed === startedWith) {
              activeSession.failed = true;
              if (tokenManager.getToken() === startedWith) tokenManager.clearToken();
              return;
            }
            activeSession.previousToken = startedWith;
            activeSession.refreshedToken = refreshed;
          });
        } catch {
          // 锁申请失败，刷新没跑。按失败登出，但不清掉已经换成的另一枚令牌。
          if (tokenManager.getToken() === startedWith) {
            activeSession.failed = true;
            tokenManager.clearToken();
          } else if (!tokenManager.getToken()) {
            activeSession.failed = true;
          }
        }
      }).finally(() => { activeSession.task = undefined; });
    }
    try {
      await activeSession.task;
    } catch {
      if (activeSession.failed) config.__cubeRefreshFailed = true;
      return false;
    }
    if (activeSession.failed) {
      config.__cubeRefreshFailed = true;
      return false;
    }
    const refreshed = tokenManager.getToken();
    const replayToken = activeSession.refreshedToken;
    if (!sameSession(config, policy) || !isRefreshEnabled(policy) || !refreshed || replayToken !== refreshed) return false;
    const startedHere = activeSession.previousToken === current;
    const joinedRotation = !!activeSession.previousToken && sent === authorization(activeSession.previousToken);
    if (!startedHere && !joinedRotation) return false;
    config.__cubeRefreshReplayToken = refreshed;
    return true;
  }

  return { prepare, recover, shouldClearToken };
}
