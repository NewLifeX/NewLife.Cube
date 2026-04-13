import { nextTick, defineAsyncComponent } from 'vue';
import type { App } from 'vue';
import * as svg from '@element-plus/icons-vue';
import router from '/@/router/index';
import pinia from '/@/stores/index';
import { storeToRefs } from 'pinia';
import { useThemeConfig } from '/@/stores/themeConfig';
import { i18n } from '/@/i18n/index';
import { Local } from '/@/utils/storage';
import { verifyUrl } from '/@/utils/toolsValidate';
import { Column } from '../model/api/page';
import { ColumnConfig } from '../components/form/model/form';
import { TableColumn } from '../components/table/type';
import { h } from 'vue'
import { usePageApi } from '../api/page';
import { useEnumOptions } from '../stores/enumOptions';

// 引入组件
const SvgIcon = defineAsyncComponent(() => import('/@/components/svgIcon/index.vue'));

/**
 * 导出全局注册 element plus svg 图标
 * @param app vue 实例
 * @description 使用：https://element-plus.gitee.io/zh-CN/component/icon.html
 */
export function elSvg(app: App) {
	const icons = svg as any;
	for (const i in icons) {
		app.component(`ele-${icons[i].name}`, icons[i]);
	}
	app.component('SvgIcon', SvgIcon);
}

/**
 * 设置浏览器标题国际化
 * @method const title = useTitle(); ==> title()
 */
export function useTitle() {
	const stores = useThemeConfig(pinia);
	const { themeConfig } = storeToRefs(stores);
	nextTick(() => {
		let webTitle = '';
		let globalTitle: string = themeConfig.value.globalTitle;
		const { path, meta } = router.currentRoute.value;
		if (path === '/login') {
			webTitle = <string>meta.title;
		} else {
			webTitle = setTagsViewNameI18n(router.currentRoute.value);
		}
		document.title = `${webTitle} - ${globalTitle}` || globalTitle;
	});
}

/**
 * 设置 自定义 tagsView 名称、 自定义 tagsView 名称国际化
 * @param params 路由 query、params 中的 tagsViewName
 * @returns 返回当前 tagsViewName 名称
 */
export function setTagsViewNameI18n(item: any) {
	let tagsViewName: string = '';
	const { query, params, meta } = item;
	// 修复tagsViewName匹配到其他含下列单词的路由
	// https://gitee.com/lyt-top/vue-next-admin/pulls/44/files
	const pattern = /^\{("(zh-cn|en|zh-tw)":"[^,]+",?){1,3}}$/;
	if (query?.tagsViewName || params?.tagsViewName) {
		if (pattern.test(query?.tagsViewName) || pattern.test(params?.tagsViewName)) {
			// 国际化
			const urlTagsParams = (query?.tagsViewName && JSON.parse(query?.tagsViewName)) || (params?.tagsViewName && JSON.parse(params?.tagsViewName));
			tagsViewName = urlTagsParams[i18n.global.locale.value];
		} else {
			// 非国际化
			tagsViewName = query?.tagsViewName || params?.tagsViewName;
		}
	} else {
		// 非自定义 tagsView 名称
		tagsViewName = i18n.global.t(meta.title);
	}
	return tagsViewName;
}

/**
 * 图片懒加载
 * @param el dom 目标元素
 * @param arr 列表数据
 * @description data-xxx 属性用于存储页面或应用程序的私有自定义数据
 */
export const lazyImg = (el: string, arr: EmptyArrayType) => {
	const io = new IntersectionObserver((res) => {
		res.forEach((v: any) => {
			if (v.isIntersecting) {
				const { img, key } = v.target.dataset;
				v.target.src = img;
				v.target.onload = () => {
					io.unobserve(v.target);
					arr[key]['loading'] = false;
				};
			}
		});
	});
	nextTick(() => {
		document.querySelectorAll(el).forEach((img) => io.observe(img));
	});
};

/**
 * 全局组件大小
 * @returns 返回 `window.localStorage` 中读取的缓存值 `globalComponentSize`
 */
export const globalComponentSize = (): string => {
	const stores = useThemeConfig(pinia);
	const { themeConfig } = storeToRefs(stores);
	return Local.get('themeConfig')?.globalComponentSize || themeConfig.value?.globalComponentSize;
};

/**
 * 对象深克隆
 * @param obj 源对象
 * @returns 克隆后的对象
 */
export function deepClone(obj: EmptyObjectType) {
	let newObj: EmptyObjectType;
	try {
		newObj = obj.push ? [] : {};
	} catch (error) {
		newObj = {};
	}
	for (let attr in obj) {
		if (obj[attr] && typeof obj[attr] === 'object') {
			newObj[attr] = deepClone(obj[attr]);
		} else {
			newObj[attr] = obj[attr];
		}
	}
	return newObj;
}

/**
 * 判断是否是移动端
 */
export function isMobile() {
	if (
		navigator.userAgent.match(
			/('phone|pad|pod|iPhone|iPod|ios|iPad|Android|Mobile|BlackBerry|IEMobile|MQQBrowser|JUC|Fennec|wOSBrowser|BrowserNG|WebOS|Symbian|Windows Phone')/i
		)
	) {
		return true;
	} else {
		return false;
	}
}

/**
 * 判断数组对象中所有属性是否为空，为空则删除当前行对象
 * @description @感谢大黄
 * @param list 数组对象
 * @returns 删除空值后的数组对象
 */
export function handleEmpty(list: EmptyArrayType) {
	const arr = [];
	for (const i in list) {
		const d = [];
		for (const j in list[i]) {
			d.push(list[i][j]);
		}
		const leng = d.filter((item) => item === '').length;
		if (leng !== d.length) {
			arr.push(list[i]);
		}
	}
	return arr;
}

/**
 * 打开外部链接
 * @param val 当前点击项菜单
 */
export function handleOpenLink(val: RouteItem) {
	const { origin, pathname } = window.location;
	router.push(val.path);
	if (verifyUrl(<string>val.meta?.isLink)) window.open(val.meta?.isLink);
	else window.open(`${origin}${pathname}#${val.meta?.isLink}`);
}
/**
 * 字符串转小驼峰
 * @param str 转换的字符串
 */
export function toCamelCase(str = '') {
	return str.replace(/^[A-Z]*/, (str) => str.toLowerCase())
		.replace(/[-_](\w)/g, (_, p1) => {
			return p1.toUpperCase();
		}).replace(/^\w/, function(match) {
			return match.toLowerCase();
		});
}
export function getComponentBaseField (item: Column) {
	const { typeName, itemType } = item;
	const types = {
		Int32: 'inputNumber',
		Int64: 'inputNumber',
		String: 'input',
		Boolean: 'switch',
		DateTime: 'datePicker',
	}	
	const contents = {
		mail: 'input',
		mobile: 'input',
		image: 'upload'
	}
	if (typeName && !types[typeName]) {
		useEnumOptions().setOptions(typeName)
	}
	return {
		component: contents[itemType] || types[typeName] || (typeName ? 'select' : 'input'),
		label: item.displayName,
		prop: toCamelCase(item.mapField || item.name),
		props: typeName && !types[typeName] ? {
			storeKey: typeName,
			valueKey: 'value',
			labelKey: 'label',
		} : {}
	}
}

export function getInfoFields (fields: Column[] = []) {
	return fields.map(item => deepMerge(getComponentBaseField(item), {
		group: item.category,
	}))
}

export function getSearchFields (fields: Column[] = []) {
  const col = { xs: 24, sm: 12, md: 8, lg: 8, xl: 6 }
	return fields.map(item => deepMerge(getComponentBaseField(item), {
		required: false,
		col
	}))
}

export function getTableFields (fields: Column[] = []) {
	return fields.map(item => deepMerge(getComponentBaseField(item), {
		prop: toCamelCase(item.name),
	}))
}

export function is(val: unknown, type: string) {
  return toString.call(val) === `[object ${type}]`;
}

export function isObject(val: any): val is Record<any, any> {
  return val !== null && is(val, 'Object');
}
// 深度合并
export function deepMerge<T = any>(src: any = {}, ...targets: Array<any>): T {
  let key: string;
  targets.forEach((target) => {
    for (key in target) {
      src[key] = isObject(src[key]) ? deepMerge(src[key], target[key]) : (src[key] = target[key]);
    }
  });
  return src;
}
export function decomposePowerOfTwo (num: number) {
	let powers = [];
	let n = 0;
	while(num > 0) {
		if ((num & 1) === 1) {
			// 如果当前位是1，就添加对应的2的n次幂
			powers.push(Math.pow(2, n));
		}
		num >>= 1; // 右移一位
		n++;
	}
	return powers;
}

export function isObjTrue (prop?: boolean) {
	return prop || (prop === undefined)
}
export function toSomeLevelJSON(obj: EmptyObjectType) {
  const result = {};

  for (const key in obj) {
    const keys = key.split('.');
    let currentLevel = result;

    for (let i = 0; i < keys.length; i++) {
      const subKey = keys[i];
      if (!currentLevel[subKey]) {
        if (i === keys.length - 1) {
          currentLevel[subKey] = obj[key];
        } else {
          currentLevel[subKey] = {};
        }
      }
      currentLevel = currentLevel[subKey];
    }
  }
	
  return result;
}
export function toOneLevelJSON(obj: EmptyObjectType, parentKey = '') {
	const result = {};

	for (const key in obj) {
		const newKey = parentKey ? `${parentKey}.${key}` : key;
		if (typeof obj[key] === 'object' && !Array.isArray(obj[key])) {
			const nestedObj = toOneLevelJSON(obj[key], newKey);
			Object.assign(result, nestedObj);
		} else {
			result[newKey] = obj[key];
		}
	}

	return result;
}
export function assignJSON(target: EmptyObjectType, source: EmptyObjectType) {
  for (const key in target) {
    if (!(key in source)) {
      delete target[key];
    }
  }

  for (const key in source) {
    if (typeof source[key] === 'object' && !Array.isArray(source[key])) {
      if (!target[key]) {
        target[key] = {};
      }
      assignJSON(target[key], source[key]);
    } else {
      target[key] = source[key];
    }
  }
}
/**
 * 统一批量导出
 * @method elSvg 导出全局注册 element plus svg 图标
 * @method useTitle 设置浏览器标题国际化
 * @method setTagsViewNameI18n 设置 自定义 tagsView 名称、 自定义 tagsView 名称国际化
 * @method lazyImg 图片懒加载
 * @method globalComponentSize() element plus 全局组件大小
 * @method deepClone 对象深克隆
 * @method isMobile 判断是否是移动端
 * @method handleEmpty 判断数组对象中所有属性是否为空，为空则删除当前行对象
 * @method handleOpenLink 打开外部链接
 */
const other = {
	elSvg: (app: App) => {
		elSvg(app);
	},
	useTitle: () => {
		useTitle();
	},
	setTagsViewNameI18n(route: RouteToFrom) {
		return setTagsViewNameI18n(route);
	},
	lazyImg: (el: string, arr: EmptyArrayType) => {
		lazyImg(el, arr);
	},
	globalComponentSize: () => {
		return globalComponentSize();
	},
	deepClone: (obj: EmptyObjectType) => {
		return deepClone(obj);
	},
	isMobile: () => {
		return isMobile();
	},
	handleEmpty: (list: EmptyArrayType) => {
		return handleEmpty(list);
	},
	handleOpenLink: (val: RouteItem) => {
		handleOpenLink(val);
	},
	toCamelCase: (str: string) => {
		return toCamelCase(str);
	},
	deepMerge (src: any = {}, ...targets: Array<any>) {
		return deepMerge(src, ...targets)
	},
	isObjTrue,
	decomposePowerOfTwo,
	toSomeLevelJSON,
	toOneLevelJSON,
	assignJSON
};

// 统一批量导出
export default other;
