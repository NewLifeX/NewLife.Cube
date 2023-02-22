const isProduction = process.env.NODE_ENV === 'production'
console.log(isProduction)

module.exports = {
  chainWebpack: (config) => {},
  configureWebpack: (config) => {
    if (isProduction) {
      config.externals = {
        // vue: {
        //   root: 'Vue', // 指向全局变量
        //   commonjs: 'vue',
        //   commonjs2: 'vue',
        //   amd: 'vue'
        // }
        // vue: 'Vue',
        // vuex: 'Vuex',
        // 'vue-router': 'VueRouter',
        // axios: 'axios',
        // 'element-plus': 'ELEMENT'
      }
    }

    // 源码映射，用于调试
    config.devtool = 'source-map'
  }
}
