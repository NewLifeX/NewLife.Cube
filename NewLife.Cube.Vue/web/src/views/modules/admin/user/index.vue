<template>
	<Page>
		<template #mail> 测试 </template>
		<template #row-handle> </template>
	</Page>
</template>

<script setup lang="ts">
import usePage from '/@/hook/usePage';
import { ColumnKind, usePageApi } from '/@/api/page';
const { columns, forms } = usePage({
	columns: [
		// {
		//   in: ColumnKind.ADD,
		//   prop: 'sex',
		//   component: 'radioGroup',
		//   props: {
		//     options: [{ id: 1, name: '男' }, { id: 2, name: '女' }]
		//   },
		// },
		// {
		//   in: [ColumnKind.SEARCH, ColumnKind.LIST, ColumnKind.ADD],
		//   prop: 'mail',
		//   slot: 'mail',
		// },
		{
			prop: 'departmentID',
			component: 'select',
			props: {
				api: () => usePageApi().getTableData('/admin/department', { pageIndex: 0 }),
			},
		},
		{
			prop: 'roleID',
			component: 'select',
			props: {
				url: '/admin/role',
			},
		},
		{
			prop: 'avatar',
			component: 'upload',
			props: {
				url: '/api/Upload/UploadFiles',
				data: {
					proName: 'avatar'
				}
			}
		}
		// {
		//   in: [ColumnKind.ADD, ColumnKind.EDIT],
		//   prop: 'name',
		//   props: {
		//     onChange: (val: string) => {
		//       forms.data!.mail = val
		//       columns.add!.find(item => item.prop === 'sex')!.if = !val
		//     }
		//   }
		// }
	],
	onEditBefore: (data, fun) => {
		data.value.ceshi = 123;
		fun();
	},
});
</script>
