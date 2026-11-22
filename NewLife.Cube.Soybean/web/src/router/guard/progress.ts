import type { Router } from 'vue-router';

export function createProgressGuard(router: Router) {
  router.beforeEach(() => {
    window.NProgress?.start?.();
  });
  router.afterEach(() => {
    window.NProgress?.done?.();
  });
}
