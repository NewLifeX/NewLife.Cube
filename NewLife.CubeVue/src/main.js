import { createApp } from 'vue'
import * as VueRouter from 'vue-router'
import Vuex from 'vuex'
import App from './App.vue'
import * as Element from 'element-plus'
import * as ElementIcons from '@element-plus/icons'
import { createCubeUI } from './index'

import 'element-plus/dist/index.css'

let cubeUI = createCubeUI(VueRouter, Vuex, Element, ElementIcons)

const app = createApp(App)
app.use(cubeUI)

let store = cubeUI.store

// store.dispatch('setUrls', { baseUrl: 'http://localhost:5000' })
store.dispatch('setUrls', { baseUrl: 'http://81.69.253.197:8000' })

app.mount('#app')
