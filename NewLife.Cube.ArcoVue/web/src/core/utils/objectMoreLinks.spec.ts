import { describe, expect, it } from 'vitest';
import { buildObjectMoreLinks } from './objectMoreLinks';

describe('buildObjectMoreLinks', () => {
  it('omits sms when the menu tree has no sms url', () => {
    const links = buildObjectMoreLinks([
      { url: '/Admin/MailConfig', children: [{ url: '/Admin/OAuthConfig' }] },
    ]);
    expect(links.map((l) => l.path)).toEqual(['/Admin/MailConfig', '/Admin/OAuthConfig']);
  });

  it('includes a hidden menu url', () => {
    const links = buildObjectMoreLinks([
      { url: '/admin/smsconfig', visible: false },
      { url: '/Admin/AccessRule', visible: false },
    ]);
    expect(links.map((l) => l.title)).toEqual(['短信设置', '访问规则']);
  });
});
