import { defineStore } from 'pinia';
import { Session } from '/@/utils/storage';
import { useUserApi } from '../api/user';
import { usePageApi } from '../api/page';
import { toCamelCase } from '../utils/other';

/**
 * 用户信息
 * @methods setUserInfos 设置用户信息
 */
type Options = {
  options: {
    [k in string]: EmptyObjectType[] | undefined;
  }
}
export const useEnumOptions = defineStore('enumOptions', {
	state: (): Options => ({
    options: {}
  }),
	actions: {
		async setOptions(type: string) {
			if (!this.options[type]) {
        this[type] = [];
        return usePageApi().lookUp(type).then(res => {
          this.options[type] = res.data[toCamelCase(type)]
          return this.options[type]
        }).catch(() => {
          this.options[type] = undefined
        })
      }
      return Promise.resolve(this.options[type])
		},
	},
});
