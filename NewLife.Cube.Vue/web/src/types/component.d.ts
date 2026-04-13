import Page from '../components/page/index.vue'

// 声明全局组件
declare module 'vue' {
  export interface GlobalComponents {
    Page: typeof Page
  }
}
