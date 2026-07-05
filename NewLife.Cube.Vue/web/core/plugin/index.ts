import path from 'path';
import { type PluginOption, type ResolvedConfig, type ViteDevServer } from 'vite';
import type { ConfigRoute } from '../typings.d';
import fs from 'fs';
import type { MicroAppConfig } from '../microAppRouter';

/**
 * 递归遍历对象，找出所有值为 BUILD_ 开头的配置项
 * 返回扁平化的路径数组，如 ["request.baseUrl"]
 */
function findBuildConfigKeys(obj: unknown, prefix = ''): Array<{ path: string; value: string }> {
  const results: Array<{ path: string; value: string }> = [];

  if (obj && typeof obj === 'object') {
    for (const [key, value] of Object.entries(obj)) {
      const currentPath = prefix ? `${prefix}.${key}` : key;

      if (typeof value === 'string' && value.startsWith('BUILD_')) {
        results.push({ path: currentPath, value });
      } else if (value && typeof value === 'object' && !Array.isArray(value)) {
        results.push(...findBuildConfigKeys(value, currentPath));
      }
    }
  }

  return results;
}

/**
 * 将 BUILD_ 配置项生成为内联脚本代码
 * 格式: let cubeConfig = window._CUBE_CONFIG_ || (window._CUBE_CONFIG_={});
 */
function generateBuildConfigScript(buildConfigs: Array<{ path: string; value: string }>): string {
  if (buildConfigs.length === 0) return '';

  const lines = ['let cubeConfig = window._CUBE_CONFIG_ || (window._CUBE_CONFIG_={});'];

  // 按顶层分组
  const grouped = new Map<string, Array<{ path: string; value: string }>>();
  for (const config of buildConfigs) {
    const topLevel = config.path.split('.')[0];
    if (!grouped.has(topLevel)) grouped.set(topLevel, []);
    grouped.get(topLevel)!.push(config);
  }

  for (const [topKey, configs] of grouped) {
    // 生成: cubeConfig["request"] || (cubeConfig["request"]={})
    lines.push(`let ${topKey} = cubeConfig["${topKey}"] || (cubeConfig["${topKey}"]={});`);

    for (const config of configs) {
      // 获取剩余路径部分
      const remainingPath = config.path.split('.').slice(1).join('"]["');
      // 生成: request["baseUrl"] = "BUILD_REQUEST_BASE_URL";
      lines.push(`${topKey}["${remainingPath}"] = "${config.value}";`);
    }
  }

  return lines.join('\n');
}

export default function vitePluginCubeFront() {
  const virtualModuleNamePrefix = 'cube';
  const virtualModuleIdPrefix = 'virtual:cube-front-';
  const resolvedVirtualModuleIdPrefix = '\0' + virtualModuleIdPrefix;

  // 虚拟模块名称常量
  const appName = 'app';
  const microAppsName = 'micro-apps';
  const configName = 'config';
  const sectionsName = 'sections';

  /** 配置信息 */
  let config: ResolvedConfig & { routes: ConfigRoute[] };
  /** 包含路由信息的字符串代码 */
  let routesStr: string | undefined;
  /** 生产配置内容（用于 transformIndexHtml 提取 BUILD_ 配置） */
  let productionConfigContent: string | undefined;

  const viteCubeApp: PluginOption = {
    name: `vite:${virtualModuleNamePrefix}-${appName}`,
    enforce: 'post',
    config: (_config, _m) => {
      return {
        // resolve: {
        //   alias: {
        //     'cube-front': path.resolve(__dirname, './'),
        //   },
        // },
      };
    },
    configResolved: (_config) => {
      // console.log('configResolved--------------------', config)
    },
    resolveId(id: string) {
      if (id === virtualModuleIdPrefix + appName) {
        return resolvedVirtualModuleIdPrefix + appName;
      }
    },
    load(id: string) {
      if (id === resolvedVirtualModuleIdPrefix + appName) {
        return `
        export const msg = "from virtual module"
        export { default as App } from 'cube-front/core/App.vue'
        `;
      }
    },
    transform(code: string, _id: string) {
      return code;
    },
    transformIndexHtml(html: string) {
      // 生产模式下，提取 BUILD_ 开头的配置项并注入到 html
      if (config.mode === 'production' && productionConfigContent) {
        // 解析嵌套配置：提取 parent: { key: '${BUILD_XXX}' } 格式
        const buildConfigs: Array<{ path: string; value: string }> = [];

        // 按行解析，追踪缩进层级
        const lines = productionConfigContent.split('\n');
        let currentParent: string | null = null;
        let parentIndent = 0;

        for (const line of lines) {
          const trimmed = line.trim();
          if (!trimmed) continue;
          if (trimmed.startsWith('//')) continue; // 跳过注释

          const indent = line.search(/\S/);
          // 对象开始：key: { 格式
          const objStartMatch = trimmed.match(/^(\w+)["']?\s*:\s*\{/);
          // BUILD_ 属性：key: '${BUILD_XXX}' 格式
          const buildPropMatch = trimmed.match(/^\w+["']?\s*:\s*["']?\$\{([^}]+)\}["']?[,\s]*/);

          if (objStartMatch) {
            currentParent = objStartMatch[1];
            parentIndent = indent;
          } else if (currentParent && buildPropMatch && indent > parentIndent) {
            // 嵌套属性
            const keyMatch = trimmed.match(/^(\w+)["']?\s*:/);
            if (keyMatch && buildPropMatch[1].startsWith('BUILD_')) {
              buildConfigs.push({
                path: `${currentParent}.${keyMatch[1]}`,
                value: buildPropMatch[1],
              });
            }
          }
        }

        if (buildConfigs.length > 0) {
          const scriptContent = generateBuildConfigScript(buildConfigs);
          return html.replace('<head>', `<head>\n    <script>${scriptContent}</script>`);
        }
      }
      return html;
    },
  };

  // 新增应用配置插件
  const viteCubeAppNames: PluginOption = {
    name: `vite:${virtualModuleNamePrefix}-${microAppsName}`,
    enforce: 'post',
    configResolved: (cfg) => {
      config = cfg as ResolvedConfig & { routes: ConfigRoute[] };
    },
    resolveId(id: string) {
      if (id === virtualModuleIdPrefix + microAppsName) {
        return resolvedVirtualModuleIdPrefix + microAppsName;
      }
    },
    load(id: string) {
      if (id === resolvedVirtualModuleIdPrefix + microAppsName) {
        try {
          // 读取microAppConfig.json文件
          const configPath = path.resolve(config.root, 'configs/microAppConfig.json');
          const microAppConfigs = JSON.parse(
            fs.readFileSync(configPath, 'utf-8'),
          ) as Array<MicroAppConfig>;

          // 判断当前是否在 cube-front 源码目录运行
          // 如果 config.root 包含 'cube-front' 但不是直接运行，说明是外部项目引用
          const isExternalProject =
            !config.root.endsWith('cube-front') && config.root.includes('cube-front');

          // 构建应用配置代码
          const microAppConfigsStrList = microAppConfigs.map((app) => {
            let importPath: string;

            if (app.packageName) {
              // 路径解析规则：
              // 1. 以 apps 或 /apps 或 ./apps 开头 → 本项目路径，自动拼接 /src/main.ts
              // 2. 以 ./src/ 开头 → 本项目 src 目录下的模块，直接使用该路径（不追加 /src/main.ts）
              // 3. 以 ./ 开头（其他）→ 本项目相对路径，直接使用
              // 4. 否则第一段视为包名，去 node_modules/{包名} 查找
              const pkgName = app.packageName;
              if (
                pkgName.startsWith('apps') ||
                pkgName.startsWith('/apps') ||
                pkgName.startsWith('./apps')
              ) {
                // 本项目路径，追加 /src/main.ts
                importPath = `./${pkgName.replace(/^\.\//, '')}/src/main.ts`;
              } else if (pkgName.startsWith('./src/') || pkgName.startsWith('./')) {
                // 本项目相对路径（如 ./src/routes），直接使用
                importPath = pkgName;
              } else {
                // 外部包路径，取第一段作为包名
                const firstSlash = pkgName.indexOf('/');
                let packageName: string;
                let packagePath: string;

                if (firstSlash === -1) {
                  packageName = pkgName;
                  packagePath = '';
                } else {
                  packageName = pkgName.substring(0, firstSlash);
                  packagePath = pkgName.substring(firstSlash);
                }

                // 验证包是否存在
                const packageMainPath = path.resolve(
                  config.root,
                  'node_modules',
                  packageName,
                  'package.json',
                );
                if (!fs.existsSync(packageMainPath)) {
                  throw new Error(
                    `[CubeFront] 微应用 ${app.name} 的包 "${packageName}" 未找到。\n` +
                      `请确保已安装：pnpm add ${packageName}\n` +
                      `或检查 node_modules 中是否存在该包。`,
                  );
                }

                importPath = `${packageName}${packagePath}/src/main.ts`;
              }
            } else if (isExternalProject) {
              // 外部项目引用 cube-front，使用 node_modules/cube-front 路径
              importPath = `node_modules/cube-front/apps/${app.name}/src/main.ts`;
            } else {
              // cube-front 源码本身运行，使用内置的 apps 目录
              importPath = `./apps/${app.name}/src/main.ts`;
            }

            return {
              name: app.name,
              prefix: app.prefix,
              module: `() => import('${importPath}')`,
            };
          });

          let code = `
const microAppConfigs = ${JSON.stringify(microAppConfigsStrList)}
export default microAppConfigs
`;
          code = code.replace(/"(\(\)\s+=>\s+import\([^\)]+\))"/g, '$1'); // 去掉属性名的引号
          return code;
        } catch (error) {
          console.error('Failed to load microAppConfig.json', error);
          return `
const microAppConfigs = []
export default microAppConfigs
`;
        }
      }
    },
  };

  // 配置虚拟模块插件
  const viteCubeConfig: PluginOption = {
    name: `vite:${virtualModuleNamePrefix}-${configName}`,
    enforce: 'post',
    configResolved: (cfg) => {
      config = cfg as ResolvedConfig & { routes: ConfigRoute[] };
    },
    resolveId(id: string) {
      if (id === virtualModuleIdPrefix + configName) {
        return resolvedVirtualModuleIdPrefix + configName;
      }
    },
    load(id: string) {
      if (id === resolvedVirtualModuleIdPrefix + configName) {
        try {
          const env = config.mode || 'development';

          // 在生产模式下，读取并存储环境配置文件内容（供 transformIndexHtml 提取 BUILD_ 配置）
          if (env === 'production') {
            const envConfigPath = path.resolve(config.root, `configs/config.${env}.ts`);
            if (fs.existsSync(envConfigPath)) {
              productionConfigContent = fs.readFileSync(envConfigPath, 'utf-8');
            }
          }

          // 构建配置代码，直接导入配置文件
          return `
// 导入基础配置和环境特定配置
import { config as baseConfig } from '${config.root}/configs/config'
import { config as envConfig } from '${config.root}/configs/config.${env}'

// 保持原始的configData结构
export const configData = {
  general: baseConfig,
  ${env}: envConfig
}
export const currentEnv = '${env}'
export default { configData, currentEnv }
`;
        } catch (error) {
          console.error('Failed to load config', error);
          return `
// 当配置加载失败时提供默认配置
export const configData = {};
export const currentEnv = '${config.mode || 'development'}';
export default { configData, currentEnv };
`;
        }
      }
    },
  };

  // Section 组件自动发现虚拟模块插件
  // 虚拟模块 ID: virtual:cube-front-sections
  // 运行时在 initApp.ts 中自动导入并调用 registerPageSections
  let sectionsConfig: (ResolvedConfig & { routes: ConfigRoute[] }) | null = null;

  const viteCubeSections: PluginOption = {
    name: `vite:${virtualModuleNamePrefix}-${sectionsName}`,
    enforce: 'pre',
    configResolved: (cfg) => {
      sectionsConfig = cfg as ResolvedConfig & { routes: ConfigRoute[] };
    },
    resolveId(id: string) {
      if (id === virtualModuleIdPrefix + sectionsName) {
        return resolvedVirtualModuleIdPrefix + sectionsName;
      }
    },
    load(id: string) {
      if (id === resolvedVirtualModuleIdPrefix + sectionsName) {
        if (!sectionsConfig) return generateSectionsCode([]);
        const viewsDir = resolveAppViewsDir(sectionsConfig.configFile, sectionsConfig.root);
        if (!viewsDir) return generateSectionsCode([]);
        const sections = scanSectionFiles(viewsDir);
        return generateSectionsCode(sections);
      }
    },
    // 开发服务器：监听 views 目录变化，新增/删除 Section 文件时失效虚拟模块
    configureServer(server: ViteDevServer) {
      const onFileChange = (filePath: string) => {
        // 只响应 views/ 目录下的 PascalCase Vue 文件变化
        if (/views[/\\][^/\\]+[/\\][A-Z][A-Za-z]+\.vue$/.test(filePath)) {
          const mod = server.moduleGraph.getModuleById(
            resolvedVirtualModuleIdPrefix + sectionsName,
          );
          if (mod) {
            server.moduleGraph.invalidateModule(mod);
            server.ws.send({ type: 'full-reload', path: '*' });
          }
        }
      };
      server.watcher.on('add', onFileChange);
      server.watcher.on('unlink', onFileChange);
    },
  };

  return [viteCubeApp, viteCubeAppNames, viteCubeConfig, viteCubeSections];
}

// ─── 工具函数（模块顶层，供插件复用）──────────────────────────────────────

/**
 * 根据当前 configFile 路径推断子应用的 views 目录。
 *
 * 约定：子应用的 vite.config.ts 位于 `<monorepo>/apps/<appName>/` 目录下，
 * 其 views 视图目录为 `<monorepo>/apps/<appName>/src/views/`。
 *
 * @param configFile  Vite 解析后的 config.configFile（绝对路径）
 * @param root        Vite 解析后的 config.root（monorepo 根目录）
 * @returns 子应用 views 目录的绝对路径，若无法推断则返回 null
 */
function resolveAppViewsDir(configFile: string | undefined, root: string): string | null {
  if (!configFile) return null;
  const configDir = path.dirname(configFile);
  // 判断 vite.config.ts 是否位于 apps/<xxx>/ 目录
  const rel = path.relative(root, configDir);
  // rel 在 Windows: "apps\cube-admin" / POSIX: "apps/cube-admin"
  const parts = rel.split(path.sep);
  if (parts.length >= 2 && parts[0] === 'apps') {
    return path.join(configDir, 'src', 'views');
  }
  return null;
}

/**
 * 递归扫描 views 目录，收集符合 Section 命名约定的 Vue 文件。
 *
 * 约定：文件名须为 PascalCase（首字母大写，仅含字母），例如 `ListSearchBar.vue`。
 * 排除 `index.vue`、`form.vue` 等小写开头的框架保留文件。
 *
 * @param viewsDir  views 目录绝对路径
 * @returns 所有符合条件文件的 { key, absPath } 列表
 *          key:     模块映射键（如 `./views/user/ListSearchBar.vue`）
 *          absPath: 绝对路径（POSIX 风格，用于生成 import() 语句）
 */
function scanSectionFiles(viewsDir: string): Array<{ key: string; absPath: string }> {
  if (!fs.existsSync(viewsDir)) return [];

  const result: Array<{ key: string; absPath: string }> = [];

  function walk(dir: string, relPath: string) {
    const entries = fs.readdirSync(dir, { withFileTypes: true });
    for (const entry of entries) {
      const fullPath = path.join(dir, entry.name);
      if (entry.isDirectory()) {
        walk(fullPath, `${relPath}/${entry.name}`);
      } else if (entry.isFile() && entry.name.endsWith('.vue')) {
        const nameWithoutExt = entry.name.slice(0, -4);
        // 只收集 PascalCase 文件名（首字母大写，仅含字母）
        if (/^[A-Z][A-Za-z]+$/.test(nameWithoutExt)) {
          result.push({
            key: `./views${relPath}/${entry.name}`,
            // 统一为 POSIX 正斜杠，Windows 环境下 import() 也可识别
            absPath: fullPath.replace(/\\/g, '/'),
          });
        }
      }
    }
  }

  walk(viewsDir, '');
  return result;
}

/**
 * 生成 virtual:cube-front-sections 的模块代码。
 * 导出一个 Record<string, () => Promise<{default:unknown}>> 对象，
 * 可直接传入 registerPageSections(app, modules)。
 */
function generateSectionsCode(sections: Array<{ key: string; absPath: string }>): string {
  if (sections.length === 0) {
    return `// virtual:cube-front-sections — no section overrides found\nconst modules = {};\nexport default modules;\n`;
  }
  const lines = sections
    .map(
      ({ key, absPath }) => `  ${JSON.stringify(key)}: () => import(${JSON.stringify(absPath)}),`,
    )
    .join('\n');
  return `// virtual:cube-front-sections — auto-generated\nconst modules = {\n${lines}\n};\nexport default modules;\n`;
}
