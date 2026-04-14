import { createRouter, createWebHistory } from 'vue-router';
const routes = [
    { path: '/login', component: () => import('@/views/login/index.vue') },
    {
        path: '/',
        component: () => import('@/layouts/default.vue'),
        children: [
            { path: '', component: () => import('@/views/home/index.vue') },
            { path: ':type(.*)', component: () => import('@/views/dynamic/DynamicPage.vue') },
        ],
    },
];
const router = createRouter({
    history: createWebHistory(),
    routes,
});
export default router;
