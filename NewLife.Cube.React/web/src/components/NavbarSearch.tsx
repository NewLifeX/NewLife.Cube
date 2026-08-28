/**
 * 导航搜索（菜单快速搜索，对齐 Vue 皮肤 components/NavbarSearch.vue）
 */
import { useMemo, useState } from 'react';
import { AutoComplete, Input } from 'antd';
import { SearchOutlined } from '@ant-design/icons';
import { useNavigate } from 'react-router-dom';
import { useMenuStore } from '@/stores/menu';

export default function NavbarSearch() {
  const navigate = useNavigate();
  const flatMenus = useMenuStore((s) => s.flatMenus);
  const [value, setValue] = useState('');

  const options = useMemo(() => {
    if (!value) return [];
    const kw = value.toLowerCase();
    return flatMenus
      .filter((m) => m.path && (m.title || m.name).toLowerCase().includes(kw))
      .slice(0, 10)
      .map((m) => ({ value: m.path, label: `${m.title || m.name}（${m.path}）` }));
  }, [flatMenus, value]);

  return (
    <AutoComplete
      options={options}
      value={value}
      onChange={setValue}
      onSelect={(path) => {
        navigate(path);
        setValue('');
      }}
      style={{ width: 220 }}
    >
      <Input prefix={<SearchOutlined />} placeholder="搜索菜单" allowClear />
    </AutoComplete>
  );
}
