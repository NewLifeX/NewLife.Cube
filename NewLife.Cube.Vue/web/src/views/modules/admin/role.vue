<template>
	<Page>
		<template #permission>
			<el-checkbox-group v-model="permission" class="w-full">
				<el-table :data="menus" style="width: 100%; margin-bottom: 20px" row-key="id" size="small" border default-expand-all>
					<el-table-column prop="name" label="标识" width="150" />
					<el-table-column prop="displayName" label="菜单名" width="150" />
					<el-table-column label="全部授权" width="70" align="center">
						<template #default="scope">
							<el-checkbox
								@change="(val: boolean) => changeCheckboxAll(scope.row, val)"
								:label="`${scope.row.id}#255`"
								:load="loadCheckbox(scope.row)"
								><span></span
							></el-checkbox>
						</template>
					</el-table-column>
					<el-table-column label="权限">
						<template #default="scope">
							<el-checkbox :label="`${scope.row.id}#${index}`" v-for="(item, index) in scope.row.permissions" :key="index">{{ item }}</el-checkbox>
						</template>
					</el-table-column>
				</el-table>
			</el-checkbox-group>
		</template>
	</Page>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue';
import { useMenuApi } from '/@/api/menu';
import { ColumnKind } from '/@/api/page';
import usePage from '/@/hook/usePage';

const { infoForm } = usePage({
	columns: [
		{
			in: ColumnKind.ADD,
			prop: 'sort',
			component: 'select',
			label: '33333',
			props: {
				options: [{ id: 1, name: '1111' }],
			},
		},
		{
			prop: 'permission',
			slot: 'permission',
			in: [ColumnKind.EDIT, ColumnKind.ADD],
		},
	],
});

const loadCheckbox = (row: EmptyObjectType) => {
	if (!row.permissionsArr) {
		row.permissionsArr = Object.keys(row.permissions).map((key) => `${row.id}#${key}`);
	}
};

const permission = computed({
	get() {
		let value = infoForm.value.permission ? infoForm.value.permission.split(',') : [];
		let menuStr = JSON.stringify(menus.value);
		value.forEach((val: string) => {
			let auth = val.split('#');
			if (Number(auth[1]) === 255) {
				let arr = menuStr.match(new RegExp(auth[0] + '#[0-9][0-9]*', 'g'));
				value = Array.from(new Set(value.concat(arr)));
			}
		});
		return value;
	},
	set(val) {
		infoForm.value.permission = val.toString();
	},
});

const menus = ref<EmptyObjectType[]>([]);
useMenuApi()
	.getMenu({ pageSize: 0 })
	.then((res) => {
		menus.value = res.data;
	});

const changeCheckboxAll = (row: EmptyObjectType, val: boolean) => {
	if (val) {
		permission.value = Array.from(new Set(permission.value.concat(row.permissionsArr)));
		if (row.children) {
			row.children.forEach((child: EmptyObjectType) => {
				permission.value = Array.from(new Set(permission.value.concat([child.id + '#255'])));
				changeCheckboxAll(child, true);
			});
		}
	} else {
		permission.value = permission.value.filter((val: string) => val.indexOf(row.id + '#') !== 0);
		if (row.children) {
			permission.value = permission.value.filter((val: string) =>
				row.children.every((item: EmptyObjectType) => item.id !== Number(val.split('#')[0]))
			);
		}
	}
};
</script>
