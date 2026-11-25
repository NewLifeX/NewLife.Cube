const { test } = require('node:test');
const assert = require('node:assert/strict');
const { createApiClient, TokenManager } = require('../dist/index.cjs');

function setup(policyFactory) {
  let token = 'old';
  let unauthorized = 0;
  const tm = new TokenManager({
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  });
  const policy = policyFactory?.(tm);
  const client = createApiClient({
    tokenManager: tm,
    tokenRefresh: policy,
    onUnauthorized: () => { unauthorized++; },
  });
  client.defaults.adapter = async (config) => ({
    config,
    status: 200,
    headers: {},
    data: config.headers.Authorization === 'new' ? { code: 0, data: 'ok' } : { code: 401 },
  });
  return {
    client,
    tm,
    get unauthorized() { return unauthorized; },
  };
}

function deferred() {
  let resolve;
  const promise = new Promise((done) => { resolve = done; });
  return { promise, resolve };
}

test('refresh retries once and updates authorization', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => { count++; tm.setToken('new'); },
  }));
  assert.equal((await s.client.get('/test')).data.data, 'ok');
  assert.equal(count, 1);
  assert.equal(s.unauthorized, 0);
});

test('concurrent requests sharing policy refresh once', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => {
      count++;
      await new Promise((resolve) => setTimeout(resolve, 5));
      tm.setToken('new');
    },
  }));
  await Promise.all([s.client.get('/a'), s.client.get('/b')]);
  assert.equal(count, 1);
});

test('disabled preserves unauthorized behavior', async () => {
  const s = setup(() => ({
    isEnabled: () => false,
    refresh: async () => { throw new Error('must not run'); },
  }));
  await assert.rejects(s.client.get('/a'));
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('failed refresh cannot clear another login', async () => {
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => { tm.setToken('other'); throw new Error('changed'); },
  }));
  await assert.rejects(s.client.get('/a'));
  assert.equal(s.tm.getToken(), 'other');
  assert.equal(s.unauthorized, 0);
});

test('refresh writes a new token then throws and keeps it when session key matches', async () => {
  const s = setup((tm) => ({
    isEnabled: () => true,
    getSessionKey: () => 1,
    refresh: async () => {
      tm.setToken('fresh');
      throw new Error('write then fail');
    },
  }));
  await assert.rejects(s.client.get('/test'));
  assert.equal(s.tm.getToken(), 'fresh');
  assert.equal(s.unauthorized, 0);
});

test('replay that stays unauthorized clears the token and notifies', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => { count++; tm.setToken('bad'); },
  }));
  await assert.rejects(s.client.get('/a'));
  assert.equal(count, 1);
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('different clients share refresh policy', async () => {
  let token = 'old';
  let count = 0;
  const tm = new TokenManager({
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  });
  const policy = {
    isEnabled: () => true,
    refresh: async () => {
      count++;
      await new Promise((resolve) => setTimeout(resolve, 5));
      tm.setToken('new');
    },
  };
  const clients = [0, 1].map(() => createApiClient({
    tokenManager: tm,
    tokenRefresh: policy,
    onUnauthorized: () => {},
  }));
  for (const client of clients) {
    client.defaults.adapter = async (config) => ({
      config,
      status: 200,
      headers: {},
      data: { code: config.headers.Authorization === 'new' ? 0 : 401 },
    });
  }
  await Promise.all(clients.map((client) => client.get('/test')));
  assert.equal(count, 1);
});

test('HTTP 401 refreshes before unauthorized', async () => {
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => tm.setToken('new'),
  }));
  s.client.defaults.adapter = async (config) => {
    if (config.headers.Authorization === 'new') {
      return { config, status: 200, headers: {}, data: { code: 0 } };
    }
    const error = new Error('expired');
    error.config = config;
    error.response = { status: 401, config };
    throw error;
  };
  await s.client.get('/test');
  assert.equal(s.unauthorized, 0);
});

test('refresh rejection falls back to original business unauthorized', async () => {
  const s = setup(() => ({
    isEnabled: () => true,
    refresh: async () => { throw new Error('expired'); },
  }));
  await assert.rejects(s.client.get('/test'));
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('late unauthorized cannot replay under another login', async () => {
  let count = 0;
  const s = setup(() => ({
    isEnabled: () => true,
    refresh: async () => { count++; },
  }));
  s.client.defaults.adapter = async (config) => {
    s.tm.setToken('other');
    return { config, status: 200, headers: {}, data: { code: 401 } };
  };
  await assert.rejects(s.client.get('/test'));
  assert.equal(s.tm.getToken(), 'other');
  assert.equal(count, 0);
  assert.equal(s.unauthorized, 0);
});

test('late unauthorized replays after another request refreshed the same session', async () => {
  const delayedRequestStarted = deferred();
  const releaseDelayedResponse = deferred();
  let refreshCount = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    getSessionKey: () => 1,
    refresh: async () => {
      refreshCount++;
      tm.setToken('new');
    },
  }));
  const delayedTokens = [];
  s.client.defaults.adapter = async (config) => {
    const token = config.headers.Authorization;
    if (config.url.endsWith('/delayed')) {
      delayedTokens.push(token);
      if (token === 'old') {
        delayedRequestStarted.resolve();
        await releaseDelayedResponse.promise;
      }
    }
    return { config, status: 200, headers: {}, data: { code: token === 'old' ? 401 : 0 } };
  };

  const delayed = s.client.get('/delayed');
  await delayedRequestStarted.promise;
  await s.client.get('/first');
  releaseDelayedResponse.resolve();
  await delayed;

  assert.deepEqual(delayedTokens, ['old', 'new']);
  assert.equal(refreshCount, 1);
  assert.equal(s.unauthorized, 0);
});

test('same session keeps the newest token after an older request is unauthorized', async () => {
  const lateStarted = deferred();
  const releaseLate = deferred();
  let token = 't1';
  let refreshCount = 0;
  let unauthorized = 0;
  const tm = new TokenManager({
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  });
  const client = createApiClient({
    tokenManager: tm,
    onUnauthorized: () => { unauthorized++; },
    tokenRefresh: {
      isEnabled: () => true,
      getSessionKey: () => 1,
      refresh: async () => {
        refreshCount++;
        token = refreshCount === 1 ? 't2' : 't3';
      },
    },
  });
  client.defaults.adapter = async (config) => {
    const auth = config.headers.Authorization;
    if (config.url.endsWith('/late')) {
      if (auth === 't1') {
        lateStarted.resolve();
        await releaseLate.promise;
      }
      return { config, status: 200, headers: {}, data: { code: 401 } };
    }
    if (config.url.endsWith('/step1')) {
      return { config, status: 200, headers: {}, data: { code: auth === 't2' ? 0 : 401 } };
    }
    return { config, status: 200, headers: {}, data: { code: auth === 't3' ? 0 : 401 } };
  };

  const late = client.get('/late');
  await lateStarted.promise;
  await client.get('/step1');
  await client.get('/step2');
  releaseLate.resolve();
  await assert.rejects(late);

  assert.equal(token, 't3');
  assert.equal(refreshCount, 2);
  assert.equal(unauthorized, 0);
});

test('shared storage across two policies does not clear the other tab token', async () => {
  let token = 'old';
  let refreshes = 0;
  let unauthorized = 0;
  const storage = {
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  };
  const first = new TokenManager(storage);
  const second = new TokenManager(storage);
  const firstClient = createApiClient({
    tokenManager: first,
    onUnauthorized: () => { unauthorized++; },
    tokenRefresh: {
      isEnabled: () => true,
      getSessionKey: () => 1,
      refresh: async () => { refreshes++; first.setToken('new'); },
    },
  });
  const secondClient = createApiClient({
    tokenManager: second,
    onUnauthorized: () => { unauthorized++; },
    tokenRefresh: {
      isEnabled: () => true,
      getSessionKey: () => 1,
      refresh: async () => { refreshes++; throw new Error('other tab already rotated'); },
    },
  });
  firstClient.defaults.adapter = async (config) => ({
    config,
    status: 200,
    headers: {},
    data: { code: config.headers.Authorization === 'new' ? 0 : 401 },
  });
  secondClient.defaults.adapter = async (config) => {
    await firstClient.get('/tab-a');
    return { config, status: 200, headers: {}, data: { code: 401 } };
  };

  await assert.rejects(secondClient.get('/tab-b'));
  assert.equal(token, 'new');
  assert.equal(refreshes, 1);
  assert.equal(unauthorized, 0);
});

test('new session refresh proceeds while an older session refresh is pending', async () => {
  const oldRefreshStarted = deferred();
  const releaseOldRefresh = deferred();
  let session = 1;
  const refreshedSessions = [];
  const s = setup((tm) => ({
    isEnabled: () => true,
    getSessionKey: () => session,
    refresh: async () => {
      refreshedSessions.push(session);
      if (session === 1) {
        oldRefreshStarted.resolve();
        await releaseOldRefresh.promise;
      } else {
        tm.setToken('new');
      }
    },
  }));
  const sentRequests = [];
  s.client.defaults.adapter = async (config) => {
    const token = config.headers.Authorization;
    sentRequests.push([config.url, token]);
    return { config, status: 200, headers: {}, data: { code: token === 'new' ? 0 : 401 } };
  };

  const oldRequest = assert.rejects(s.client.get('/old-session'));
  await oldRefreshStarted.promise;
  session = 2;
  s.tm.setToken('other');
  const newer = s.client.get('/new-session');
  releaseOldRefresh.resolve();
  await newer;
  await oldRequest;

  assert.deepEqual(refreshedSessions, [1, 2]);
  assert.deepEqual(sentRequests.map(([, token]) => token), ['old', 'other', 'new']);
  assert.equal(s.tm.getToken(), 'new');
  assert.equal(s.unauthorized, 0);
});

test('late unauthorized does not replay after session key changes', async () => {
  let token = 'old';
  let session = 1;
  let count = 0;
  const tm = new TokenManager({
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  });
  const policy = {
    isEnabled: () => true,
    getSessionKey: () => session,
    refresh: async () => { count++; token = 'new'; },
  };
  const client = createApiClient({ tokenManager: tm, tokenRefresh: policy, onUnauthorized: () => {} });
  let calls = 0;
  client.defaults.adapter = async (config) => {
    calls++;
    if (calls === 1) {
      session = 2;
      token = 'other';
      return { config, status: 200, headers: {}, data: { code: 401 } };
    }
    return { config, status: 200, headers: {}, data: { code: 0 } };
  };
  await assert.rejects(client.get('/test'));
  assert.equal(calls, 1);
  assert.equal(count, 0);
  assert.equal(token, 'other');
});

test('refresh policy state errors preserve unauthorized handling', async () => {
  const s = setup(() => ({
    isEnabled: () => { throw new Error('policy unavailable'); },
    refresh: async () => { throw new Error('must not run'); },
  }));
  await assert.rejects(s.client.get('/test'));
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('auth business failures do not refresh', async () => {
  const cases = [
    ['/Auth/Login', -1],
    ['/Auth/Register', -1],
    ['/Auth/Refresh', -4],
    ['/Sso/WxMiniLogin', -1],
    ['/Sso/WxAppLogin', -1],
  ];
  for (const [url, code] of cases) {
    let count = 0;
    const s = setup((tm) => ({
      isEnabled: () => true,
      refresh: async () => { count++; tm.setToken('new'); },
    }));
    s.client.defaults.adapter = async (config) => ({
      config,
      status: 200,
      headers: {},
      data: { code, message: 'fail' },
    });
    await assert.rejects(s.client.post(url));
    assert.equal(count, 0);
    assert.equal(s.tm.getToken(), 'old');
    assert.equal(s.unauthorized, 0);
  }
});

test('disabled policy preserves token clearing when token changes during request', async () => {
  const s = setup(() => ({
    isEnabled: () => false,
    refresh: async () => { throw new Error('must not run'); },
  }));
  s.client.defaults.adapter = async (config) => {
    s.tm.setToken('other');
    return { config, status: 200, headers: {}, data: { code: 401 } };
  };
  await assert.rejects(s.client.get('/test'));
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('disabled policy with session key still clears when the session changes', async () => {
  let session = 1;
  let count = 0;
  const s = setup(() => ({
    isEnabled: () => false,
    getSessionKey: () => session,
    refresh: async () => { count++; },
  }));
  s.client.defaults.adapter = async (config) => {
    session = 2;
    s.tm.setToken('other');
    return { config, status: 200, headers: {}, data: { code: 401 } };
  };
  await assert.rejects(s.client.get('/test'));
  assert.equal(count, 0);
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('disabled refresh notifies once per unauthorized response', async () => {
  const s = setup(() => ({
    isEnabled: () => false,
    refresh: async () => { throw new Error('must not run'); },
  }));
  await assert.rejects(s.client.get('/a'));
  await assert.rejects(s.client.get('/b'));
  assert.equal(s.unauthorized, 2);
  assert.equal(s.tm.getToken(), null);
});

test('omitted refresh policy keeps original unauthorized clearing', async () => {
  let token = 'old';
  let unauthorized = 0;
  const tm = new TokenManager({
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  });
  const client = createApiClient({
    tokenManager: tm,
    onUnauthorized: () => { unauthorized++; },
  });
  client.defaults.adapter = async (config) => ({
    config,
    status: 200,
    headers: {},
    data: { code: 401 },
  });
  await assert.rejects(client.get('/test'));
  assert.equal(token, null);
  assert.equal(unauthorized, 1);
});

test('enabled refresh without a token uses original unauthorized handling', async () => {
  let count = 0;
  const s = setup(() => ({
    isEnabled: () => true,
    refresh: async () => { count++; },
  }));
  s.tm.clearToken();
  await assert.rejects(s.client.get('/test'));
  assert.equal(count, 0);
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('business code 4001 refreshes once', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => { count++; tm.setToken('new'); },
  }));
  s.client.defaults.adapter = async (config) => ({
    config,
    status: 200,
    headers: {},
    data: { code: config.headers.Authorization === 'new' ? 0 : 4001 },
  });
  assert.equal((await s.client.get('/test')).data.code, 0);
  assert.equal(count, 1);
  assert.equal(s.unauthorized, 0);
});

test('refresh rewrites authorization with the configured prefix', async () => {
  let token = 'old';
  const tm = new TokenManager({
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  });
  const headers = [];
  const client = createApiClient({
    tokenManager: tm,
    tokenHeaderPrefix: 'bearer ',
    tokenRefresh: { isEnabled: () => true, refresh: async () => tm.setToken('new') },
    onUnauthorized: () => {},
  });
  client.defaults.adapter = async (config) => {
    headers.push(config.headers.Authorization);
    return {
      config,
      status: 200,
      headers: {},
      data: { code: config.headers.Authorization === 'bearer new' ? 0 : 401 },
    };
  };
  await client.get('/test');
  assert.deepEqual(headers, ['bearer old', 'bearer new']);
});

test('refresh that leaves the token unchanged is a failure', async () => {
  let count = 0;
  const s = setup(() => ({
    isEnabled: () => true,
    refresh: async () => { count++; },
  }));
  await assert.rejects(s.client.get('/test'));
  assert.equal(count, 1);
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('concurrent HTTP 401 requests refresh once', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => {
      count++;
      await new Promise((resolve) => setTimeout(resolve, 5));
      tm.setToken('new');
    },
  }));
  s.client.defaults.adapter = async (config) => {
    if (config.headers.Authorization === 'new') {
      return { config, status: 200, headers: {}, data: { code: 0 } };
    }
    const error = new Error('expired');
    error.config = config;
    error.response = { status: 401, config };
    throw error;
  };
  await Promise.all([s.client.get('/a'), s.client.get('/b')]);
  assert.equal(count, 1);
  assert.equal(s.unauthorized, 0);
  assert.equal(s.tm.getToken(), 'new');
});

test('business 401 and HTTP 401 share one refresh', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => {
      count++;
      await new Promise((resolve) => setTimeout(resolve, 5));
      tm.setToken('new');
    },
  }));
  s.client.defaults.adapter = async (config) => {
    if (config.headers.Authorization === 'new') {
      return { config, status: 200, headers: {}, data: { code: 0 } };
    }
    if (String(config.url).endsWith('/http')) {
      const error = new Error('expired');
      error.config = config;
      error.response = { status: 401, config };
      throw error;
    }
    return { config, status: 200, headers: {}, data: { code: 401 } };
  };
  await Promise.all([s.client.get('/biz'), s.client.get('/http')]);
  assert.equal(count, 1);
  assert.equal(s.unauthorized, 0);
});

test('concurrent refresh failure clears and notifies once', async () => {
  let count = 0;
  const s = setup(() => ({
    isEnabled: () => true,
    refresh: async () => {
      count++;
      await new Promise((resolve) => setTimeout(resolve, 5));
      throw new Error('expired');
    },
  }));
  await Promise.all([
    assert.rejects(s.client.get('/a')),
    assert.rejects(s.client.get('/b')),
  ]);
  assert.equal(count, 1);
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('concurrent refresh that writes then throws keeps the new token', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    getSessionKey: () => 1,
    refresh: async () => {
      count++;
      await new Promise((resolve) => setTimeout(resolve, 5));
      tm.setToken('fresh');
      throw new Error('write then fail');
    },
  }));
  await Promise.all([
    assert.rejects(s.client.get('/a')),
    assert.rejects(s.client.get('/b')),
  ]);
  assert.equal(count, 1);
  assert.equal(s.tm.getToken(), 'fresh');
  assert.equal(s.unauthorized, 0);
});

test('request already sent joins a refresh that wrote the token before returning', async () => {
  const bothSent = deferred();
  const releaseFirst = deferred();
  const releaseSecond = deferred();
  const wrote = deferred();
  const releaseRefresh = deferred();
  let count = 0;
  const seen = [];
  const s = setup((tm) => ({
    isEnabled: () => true,
    getSessionKey: () => 1,
    refresh: async () => {
      count++;
      tm.setToken('new');
      wrote.resolve();
      await releaseRefresh.promise;
    },
  }));
  s.client.defaults.adapter = async (config) => {
    const token = config.headers.Authorization;
    if (token === 'new') {
      return { config, status: 200, headers: {}, data: { code: 0 } };
    }
    seen.push(config.url);
    if (seen.length === 2) bothSent.resolve();
    if (String(config.url).endsWith('/a')) await releaseFirst.promise;
    else await releaseSecond.promise;
    return { config, status: 200, headers: {}, data: { code: 401 } };
  };
  const first = s.client.get('/a');
  const second = s.client.get('/b');
  await bothSent.promise;
  releaseFirst.resolve();
  await wrote.promise;
  releaseSecond.resolve();
  await new Promise((resolve) => setTimeout(resolve, 0));
  releaseRefresh.resolve();
  await Promise.all([first, second]);
  assert.equal(count, 1);
  assert.equal(s.unauthorized, 0);
  assert.equal(s.tm.getToken(), 'new');
});

test('a later refresh failure clears the rotated token', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => {
      count++;
      if (count === 1) {
        tm.setToken('new');
        return;
      }
      throw new Error('refresh expired');
    },
  }));
  let calls = 0;
  s.client.defaults.adapter = async (config) => {
    calls++;
    const token = config.headers.Authorization;
    const ok = calls === 2 && token === 'new';
    return { config, status: 200, headers: {}, data: { code: ok ? 0 : 401 } };
  };
  assert.equal((await s.client.get('/first')).data.code, 0);
  await assert.rejects(s.client.get('/second'));
  assert.equal(count, 2);
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('failed refresh notifies once and a following 401 does not notify again', async () => {
  const s = setup(() => ({
    isEnabled: () => true,
    refresh: async () => { throw new Error('expired'); },
  }));
  await assert.rejects(s.client.get('/a'));
  await assert.rejects(s.client.get('/b'));
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('HTTP 401 refresh rejection clears the token', async () => {
  const s = setup(() => ({
    isEnabled: () => true,
    refresh: async () => { throw new Error('expired'); },
  }));
  s.client.defaults.adapter = async (config) => {
    const error = new Error('expired');
    error.config = config;
    error.response = { status: 401, config };
    throw error;
  };
  await assert.rejects(s.client.get('/test'));
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('HTTP 401 replay that stays unauthorized clears the token', async () => {
  let count = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => { count++; tm.setToken('bad'); },
  }));
  s.client.defaults.adapter = async (config) => {
    const error = new Error('expired');
    error.config = config;
    error.response = { status: 401, config };
    throw error;
  };
  await assert.rejects(s.client.get('/test'));
  assert.equal(count, 1);
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('HTTP 403 while refresh is enabled does not refresh or clear', async () => {
  let count = 0;
  const s = setup(() => ({
    isEnabled: () => true,
    refresh: async () => { count++; },
  }));
  s.client.defaults.adapter = async (config) => {
    const error = new Error('forbidden');
    error.config = config;
    error.response = { status: 403, config, data: { message: 'forbidden' } };
    throw error;
  };
  await assert.rejects(s.client.get('/test'));
  assert.equal(count, 0);
  assert.equal(s.tm.getToken(), 'old');
  assert.equal(s.unauthorized, 0);
});

test('business forbidden and server failure do not refresh or clear', async () => {
  for (const code of [403, 500]) {
    let count = 0;
    const s = setup(() => ({
      isEnabled: () => true,
      refresh: async () => { count++; },
    }));
    s.client.defaults.adapter = async (config) => ({
      config,
      status: 200,
      headers: {},
      data: { code, message: 'fail' },
    });
    await assert.rejects(s.client.get('/test'));
    assert.equal(count, 0);
    assert.equal(s.tm.getToken(), 'old');
    assert.equal(s.unauthorized, 0);
  }
});

test('session key throw while refresh is enabled does not clear', async () => {
  let count = 0;
  const s = setup(() => ({
    isEnabled: () => true,
    getSessionKey: () => { throw new Error('unavailable'); },
    refresh: async () => { count++; },
  }));
  await assert.rejects(s.client.get('/test'));
  assert.equal(count, 0);
  assert.equal(s.tm.getToken(), 'old');
  assert.equal(s.unauthorized, 0);
});

function withTimeout(work, ms = 500) {
  return Promise.race([
    work,
    new Promise((_, reject) => setTimeout(() => reject(new Error('refresh wait timed out')), ms)),
  ]);
}

test('refresh through the same client does not wait on its own unauthorized response', async () => {
  let calls = 0;
  let exchangeHits = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => {
      calls++;
      await s.client.post('/Auth/Refresh', { refreshToken: 'r', userName: 'u' });
      tm.setToken('new');
    },
  }));
  s.client.defaults.adapter = async (config) => {
    const url = String(config.url);
    if (url.endsWith('/Auth/Refresh')) {
      exchangeHits++;
      return { config, status: 200, headers: {}, data: { code: 401 } };
    }
    return {
      config,
      status: 200,
      headers: {},
      data: { code: config.headers.Authorization === 'new' ? 0 : 401 },
    };
  };
  await withTimeout(assert.rejects(s.client.get('/data')));
  assert.equal(calls, 1);
  assert.equal(exchangeHits, 1);
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('refresh through the same client can still rotate after a successful exchange', async () => {
  let calls = 0;
  const s = setup((tm) => ({
    isEnabled: () => true,
    refresh: async () => {
      calls++;
      await s.client.post('/Auth/Refresh', { refreshToken: 'r', userName: 'u' });
      tm.setToken('new');
    },
  }));
  s.client.defaults.adapter = async (config) => {
    const url = String(config.url);
    if (url.endsWith('/Auth/Refresh')) {
      return { config, status: 200, headers: {}, data: { code: 0, data: { accessToken: 'new' } } };
    }
    return {
      config,
      status: 200,
      headers: {},
      data: { code: config.headers.Authorization === 'new' ? 0 : 401 },
    };
  };
  const body = await withTimeout(s.client.get('/data'));
  assert.equal(body.data.code, 0);
  assert.equal(calls, 1);
  assert.equal(s.tm.getToken(), 'new');
  assert.equal(s.unauthorized, 0);
});

test('refresh through another client sharing the policy does not wait', async () => {
  let token = 'old';
  let calls = 0;
  let unauthorized = 0;
  const tm = new TokenManager({
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  });
  let exchange;
  const policy = {
    isEnabled: () => true,
    refresh: async () => {
      calls++;
      await exchange.post('/Auth/Refresh', { refreshToken: 'r', userName: 'u' });
      throw new Error('exchange unauthorized');
    },
  };
  const data = createApiClient({
    tokenManager: tm,
    tokenRefresh: policy,
    onUnauthorized: () => { unauthorized++; },
  });
  exchange = createApiClient({
    tokenManager: tm,
    tokenRefresh: policy,
    onUnauthorized: () => { unauthorized++; },
  });
  const adapter = async (config) => {
    const url = String(config.url);
    if (url.endsWith('/Auth/Refresh')) {
      return { config, status: 200, headers: {}, data: { code: 401 } };
    }
    return { config, status: 200, headers: {}, data: { code: 401 } };
  };
  data.defaults.adapter = adapter;
  exchange.defaults.adapter = adapter;
  await withTimeout(assert.rejects(data.get('/data')));
  assert.equal(calls, 1);
  assert.equal(token, null);
  assert.equal(unauthorized, 1);
});

function useRefreshLock() {
  let tail = Promise.resolve();
  const calls = [];
  const previous = Object.getOwnPropertyDescriptor(globalThis, 'navigator');
  Object.defineProperty(globalThis, 'navigator', {
    configurable: true,
    enumerable: true,
    writable: true,
    value: {
      locks: {
        request(name, options, callback) {
          const work = typeof options === 'function' ? options : callback;
          calls.push(name);
          const run = tail.then(() => work());
          tail = run.then(() => {}, () => {});
          return run;
        },
      },
    },
  });
  return {
    calls,
    restore() {
      if (previous) Object.defineProperty(globalThis, 'navigator', previous);
      else delete globalThis.navigator;
    },
  };
}

function sharedToken(initial) {
  let token = initial;
  let unauthorized = 0;
  const tm = new TokenManager({
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  });
  return {
    tm,
    note() { unauthorized++; },
    get token() { return token; },
    get unauthorized() { return unauthorized; },
  };
}

test('two tabs share one refresh while the cross-tab lock is held', async () => {
  const gate = useRefreshLock();
  try {
    const store = sharedToken('old');
    let calls = 0;
    const make = () => createApiClient({
      tokenManager: store.tm,
      onUnauthorized: () => store.note(),
      tokenRefresh: {
        isEnabled: () => true,
        refresh: async () => {
          calls++;
          await new Promise((resolve) => setTimeout(resolve, 5));
          store.tm.setToken('new');
        },
      },
    });
    const first = make();
    const second = make();
    const adapter = async (config) => ({
      config,
      status: 200,
      headers: {},
      data: { code: config.headers.Authorization === 'new' ? 0 : 401 },
    });
    first.defaults.adapter = adapter;
    second.defaults.adapter = adapter;
    await Promise.all([first.get('/a'), second.get('/b')]);
    assert.equal(calls, 1);
    assert.equal(store.token, 'new');
    assert.equal(store.unauthorized, 0);
    assert.deepEqual(gate.calls, ['cube.token-refresh', 'cube.token-refresh']);
  } finally {
    gate.restore();
  }
});

test('cross-tab lock clears a failed refresh before the other tab runs', async () => {
  const gate = useRefreshLock();
  try {
    const store = sharedToken('old');
    let calls = 0;
    const make = () => createApiClient({
      tokenManager: store.tm,
      onUnauthorized: () => store.note(),
      tokenRefresh: {
        isEnabled: () => true,
        refresh: async () => {
          calls++;
          throw new Error('expired');
        },
      },
    });
    const first = make();
    const second = make();
    const adapter = async (config) => ({
      config,
      status: 200,
      headers: {},
      data: { code: 401 },
    });
    first.defaults.adapter = adapter;
    second.defaults.adapter = adapter;
    await Promise.all([
      assert.rejects(first.get('/a')),
      assert.rejects(second.get('/b')),
    ]);
    assert.equal(calls, 1);
    assert.equal(store.token, null);
    assert.equal(store.unauthorized, 2);
    assert.deepEqual(gate.calls, ['cube.token-refresh', 'cube.token-refresh']);
  } finally {
    gate.restore();
  }
});

test('disabled refresh does not take the cross-tab lock', async () => {
  const gate = useRefreshLock();
  try {
    const s = setup(() => ({
      isEnabled: () => false,
      refresh: async () => { throw new Error('must not run'); },
    }));
    await assert.rejects(s.client.get('/a'));
    assert.equal(s.tm.getToken(), null);
    assert.equal(s.unauthorized, 1);
    assert.deepEqual(gate.calls, []);
  } finally {
    gate.restore();
  }
});

test('lock rejection logs out without calling refresh', async () => {
  const previous = Object.getOwnPropertyDescriptor(globalThis, 'navigator');
  Object.defineProperty(globalThis, 'navigator', {
    configurable: true,
    enumerable: true,
    writable: true,
    value: { locks: { request: () => Promise.reject(new Error('lock denied')) } },
  });
  try {
    let calls = 0;
    const s = setup(() => ({
      isEnabled: () => true,
      refresh: async () => { calls++; },
    }));
    await assert.rejects(s.client.get('/locked'));
    assert.equal(calls, 0);
    assert.equal(s.tm.getToken(), null);
    assert.equal(s.unauthorized, 1);
  } finally {
    if (previous) Object.defineProperty(globalThis, 'navigator', previous);
    else delete globalThis.navigator;
  }
});

test('failed refresh keeps a token another tab already wrote', async () => {
  let token = 'old';
  let unauthorized = 0;
  const storage = {
    getToken: () => token,
    setToken: (value) => { token = value; },
    clearToken: () => { token = null; },
  };
  const loserInside = deferred();
  const winnerDone = deferred();
  const loser = createApiClient({
    tokenManager: new TokenManager(storage),
    onUnauthorized: () => { unauthorized++; },
    tokenRefresh: {
      isEnabled: () => true,
      refresh: async () => {
        loserInside.resolve();
        await winnerDone.promise;
        throw new Error('lost');
      },
    },
  });
  const winner = createApiClient({
    tokenManager: new TokenManager(storage),
    onUnauthorized: () => { unauthorized++; },
    tokenRefresh: {
      isEnabled: () => true,
      refresh: async () => {
        await loserInside.promise;
        token = 'new';
        winnerDone.resolve();
      },
    },
  });
  const adapter = async (config) => ({
    config,
    status: 200,
    headers: {},
    data: { code: config.headers?.Authorization === 'new' ? 0 : 401 },
  });
  loser.defaults.adapter = adapter;
  winner.defaults.adapter = adapter;
  const lost = assert.rejects(loser.get('/lost'));
  await winner.get('/won');
  await lost;
  assert.equal(token, 'new');
  assert.equal(unauthorized, 0);
});

test('turning refresh off after a new token is written does not clear it', async () => {
  let enabled = true;
  const s = setup((tm) => ({
    isEnabled: () => enabled,
    refresh: async () => {
      tm.setToken('fresh');
      enabled = false;
      throw new Error('late');
    },
  }));
  await assert.rejects(s.client.get('/late'));
  assert.equal(s.tm.getToken(), 'fresh');
  assert.equal(s.unauthorized, 0);
});

test('disabled refresh still clears an unauthorized refresh exchange', async () => {
  const s = setup(() => ({
    isEnabled: () => false,
    refresh: async () => { throw new Error('must not run'); },
  }));
  s.client.defaults.adapter = async (config) => ({
    config,
    status: 200,
    headers: {},
    data: { code: 401 },
  });
  await assert.rejects(s.client.post('/Auth/Refresh'));
  assert.equal(s.tm.getToken(), null);
  assert.equal(s.unauthorized, 1);
});

test('session change before refresh does not clear the current token', async () => {
  let session = 1;
  let count = 0;
  const s = setup(() => ({
    isEnabled: () => true,
    getSessionKey: () => session,
    refresh: async () => { count++; },
  }));
  s.client.defaults.adapter = async (config) => {
    session = 2;
    return { config, status: 200, headers: {}, data: { code: 401 } };
  };
  await assert.rejects(s.client.get('/test'));
  assert.equal(count, 0);
  assert.equal(s.tm.getToken(), 'old');
  assert.equal(s.unauthorized, 0);
});

for (const kind of ['entity', 'service']) {
  for (const baseURL of ['', '/api', '/proxy/api', 'http://host/api', 'http://host']) {
    test(`refresh preserves resolved URL: ${kind} ${baseURL || '(empty)'}`, async () => {
      let token = 'old';
      const tm = new TokenManager({
        getToken: () => token,
        setToken: (value) => { token = value; },
        clearToken: () => { token = null; },
      });
      const client = createApiClient({
        baseURL,
        tokenManager: tm,
        tokenRefresh: { isEnabled: () => true, refresh: async () => tm.setToken('new') },
        onUnauthorized: () => {},
      });
      const urls = [];
      client.defaults.adapter = async (config) => {
        urls.push(config.url);
        return { config, status: 200, headers: {}, data: { code: urls.length === 1 ? 401 : 0 } };
      };
      await client.get(kind === 'entity' ? '/Admin/User/Index' : '/Cube/Info');
      assert.equal(urls.length, 2);
      assert.equal(urls[1], urls[0]);
    });
  }
}
