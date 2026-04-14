import { defineStore } from 'pinia';
import { ref, computed } from 'vue';
import { api } from '@/api';
/** 递归查找菜单树中 URL 匹配的节点 */
function findMenu(menus, path) {
    for (const m of menus) {
        if (m.url && path.toLowerCase().endsWith(m.url.toLowerCase()))
            return m;
        if (m.children?.length) {
            const found = findMenu(m.children, path);
            if (found)
                return found;
        }
    }
    return undefined;
}
export const useUserStore = defineStore('user', () => {
    const user = ref(null);
    const menus = ref([]);
    const isLoggedIn = computed(() => !!user.value);
    const displayName = computed(() => user.value?.displayName || user.value?.name || '');
    async function login(username, password) {
        const res = await api.user.login({ username, password });
        if (res?.data) {
            user.value = res.data;
            return true;
        }
        return false;
    }
    async function logout() {
        try {
            await api.user.logout();
        }
        catch { /* ignore */ }
        user.value = null;
        menus.value = [];
    }
    async function fetchUserInfo() {
        try {
            const res = await api.user.info();
            if (res?.data)
                user.value = res.data;
        }
        catch { /* ignore */ }
    }
    async function fetchMenus() {
        try {
            const res = await api.menu.getMenuTree();
            if (res?.data)
                menus.value = res.data;
        }
        catch { /* ignore */ }
    }
    /** 获取指定路径的菜单权限 */
    function getMenuPermission(path) {
        const item = findMenu(menus.value, path);
        return item?.permissions ?? {};
    }
    return { user, menus, isLoggedIn, displayName, login, logout, fetchUserInfo, fetchMenus, getMenuPermission };
});
