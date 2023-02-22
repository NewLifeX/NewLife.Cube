import upperFirst from 'lodash/upperFirst'
import camelCase from 'lodash/camelCase'

/*
 * 组件自动化全局注册
 */

const requireComponent = require.context(
  // 组件文件夹的相对路径
  '@/views/components',
  // 是否查找子目录
  true,
  // 匹配基础组件文件名的正则表达式
  /\w+\.(vue|js)$/
)

export default {
  install(app) {
    requireComponent.keys().forEach((fileName) => {
      // 获取组件配置
      const componentConfig = requireComponent(fileName)

      // 获取组件的 PascalCase 名
      const componentName = upperFirst(
        camelCase(
          // 获取目录深度无关的文件名
          fileName
            .split('/')
            .pop()
            .replace(/\.\w+$/, '')
        )
      )

      app.component(
        componentName,
        // 在 `.default` 上查找组件选项。
        // 如果组件导出了 `export default` 的话，该选项会存在。
        // 否则回退到模块的根。
        componentConfig.default || componentConfig
      )
    })
  }
}
