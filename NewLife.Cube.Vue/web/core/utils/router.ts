import { type RouteLocationRaw } from "vue-router"

export const gotoPage = (to: RouteLocationRaw) => {
  window.router?.push(to)
}
