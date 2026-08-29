/**
 * 主题切换器（下拉选择主题家族）
 */
import { Button, Dropdown } from 'antd';
import { BgColorsOutlined } from '@ant-design/icons';
import { THEME_GROUPS, useThemeStore, type ThemeFamily } from '@/stores/theme';

export default function ThemeSwitcher() {
  const family = useThemeStore((s) => s.family);
  const setFamily = useThemeStore((s) => s.setFamily);

  const items = THEME_GROUPS.map((g) => ({
    key: g.id,
    label: `${g.icon} ${g.label}`,
    onClick: () => setFamily(g.id as ThemeFamily),
  }));

  return (
    <Dropdown menu={{ items, selectable: true, selectedKeys: [family] }} placement="bottomRight" trigger={['click']}>
      <Button type="text" className="cube-header-action" icon={<BgColorsOutlined />} aria-label="切换主题" />
    </Dropdown>
  );
}
