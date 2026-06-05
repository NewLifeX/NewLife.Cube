import fs from 'fs'
import path from 'path'
import { PluginOption } from 'vite';

const langs = 'css|less'
const langsRegExp = new RegExp(`\\.(${langs})\\?modules$`)
const langsModuleRegExp = new RegExp(`\\.module\\.(${langs})\\?modules$`)

/** 强制使导入的css变成模块。
 *  vite中，导入的样式文件带module，就会被处理成模块，此插件正是利用这一原理，将查询参数?modules的样式文件名加上module。
 *  只要带查询参数?modules，无论是直接引入还是导出成模块，都会被处理成模块。
 */
const forceCssModulePlugin = (): PluginOption => {
  return {
    name: 'force-css-module',
    enforce: 'pre',
    resolveId(source, importer) {
      let id = source;
      // 导入样式的路径带有?modules，且没有.module
      if (langsRegExp.test(id) && !id.includes('.module')) {
        // 根据importer，补充完整id为绝对路径，增加.module并保留?modules，以便load时使用
        if (importer) {
          const importerDir = path.dirname(importer);

          // If source is a relative path, resolve it based on importer
          if (!path.isAbsolute(id) && !id.startsWith('http')) {
            id = path.resolve(importerDir, id);
          }
        }

        const newId = id.replace(langsRegExp, '.module.$1?modules');

        return newId;
      }

      return null;
    },
    load(id: string) {
      // 处理模块 CSS 的加载
      if (langsModuleRegExp.test(id)) {
        // 去掉 ?modules，读取原始 CSS 内容并返回
        const content = fs.readFileSync(id.replace('.module', '').replace('?modules', ''), 'utf-8');

        return content;
      }

      return null;
    },
  };
};

export default forceCssModulePlugin;