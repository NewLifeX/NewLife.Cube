import axios, { AxiosInstance, AxiosRequestConfig, AxiosResponse } from 'axios';
import { ElMessage, ElMessageBox } from 'element-plus';
import { Session } from '/@/utils/storage';
import qs from 'qs';
import { ApiResult } from '../model/api/common';

// 配置新建一个 axios 实例
const service: AxiosInstance = axios.create({
	baseURL: import.meta.env.DEV ? '/base-api' : import.meta.env.VITE_API_URL,
	timeout: 50000,
	headers: { 'Content-Type': 'application/json' },
	paramsSerializer: {
		serialize(params) {
			return qs.stringify(params, { allowDots: true });
		},
	},
});

// 添加请求拦截器
service.interceptors.request.use(
	(config) => {
		// 在发送请求之前做些什么 token
		if (Session.get('token')) {
			config.headers!['Authorization'] = `${Session.get('token')}`;
		}
		return config;
	},
	(error) => {
		// 对请求错误做些什么
		return Promise.reject(error);
	}
);

// 添加响应拦截器
service.interceptors.response.use(
	(response) => {
		ElMessage
		// 对响应数据做点什么
		const res = response.data;
		if (res.code && res.code !== 0) {
			// `token` 过期或者账号已在别处登录
			if (res.code === 401 || res.code === 4001) {
				Session.clear(); // 清除浏览器全部临时缓存
				window.location.href = '/'; // 去登录页
				ElMessageBox.alert('你已被登出，请重新登录', '提示', {})
					.then(() => {})
					.catch(() => {});
			} else if (res.message) {
				ElMessage.error(res.message)
			}
			return Promise.reject(service.interceptors.response);
		} else {
			return response;
		}
	},
	(error) => {
		// 对响应错误做点什么
		if (error.response.status === 401) {
			Session.clear(); // 清除浏览器全部临时缓存
			window.location.href = '/'; // 去登录页
		} else if (error.response.status === 403) {
			Session.clear(); // 清除浏览器全部临时缓存
			ElMessageBox.alert('暂无权限，请登录其他账号', '提示', {})
			.then(() => {
					window.location.href = '/'; // 去登录页
				})
				.catch(() => {
					window.location.href = '/'; // 去登录页
				});
		} else if (error.message.indexOf('timeout') != -1) {
			ElMessage.error('网络超时');
		} else if (error.message == 'Network Error') {
			ElMessage.error('网络连接错误');
		} else {
			if (error.response.data) ElMessage.error(error.response.statusText);
			else ElMessage.error('接口路径找不到');
		}
		return Promise.reject(error);
	}
);

// 导出 axios 实例
// export default service;

export default <T> (config: AxiosRequestConfig) => {
  // 指定promise实例成功之后的回调函数的第一个参数的类型为Response<T>
  return service<ApiResult<T>>(config).then(res => {
		return res.data
	})
}
