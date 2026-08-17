import { describe, expect, it } from 'vitest';
import { resolveOAuthBindKey } from './oauthBind';

describe('resolveOAuthBindKey', () => {
  it('prefers OAuth name over id', () => {
    expect(resolveOAuthBindKey({ name: 'GitHub', id: 7 })).toBe('GitHub');
  });

  it('falls back to id when name empty', () => {
    expect(resolveOAuthBindKey({ name: '', id: 3 })).toBe('3');
  });
});
