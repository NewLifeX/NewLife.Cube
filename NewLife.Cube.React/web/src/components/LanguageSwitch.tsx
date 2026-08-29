/**
 * 语言切换器
 */
import { Button, Dropdown } from 'antd';
import { GlobalOutlined } from '@ant-design/icons';
import { useTranslation } from 'react-i18next';

const LANGS = [
  { key: 'zh-CN', label: '简体中文' },
  { key: 'en-US', label: 'English' },
];

export default function LanguageSwitch() {
  const { i18n } = useTranslation();

  const items = LANGS.map((l) => ({
    key: l.key,
    label: l.label,
    onClick: () => void i18n.changeLanguage(l.key),
  }));

  return (
    <Dropdown menu={{ items, selectable: true, selectedKeys: [i18n.language] }} placement="bottomRight" trigger={['click']}>
      <Button type="text" className="cube-header-action" icon={<GlobalOutlined />} aria-label="切换语言" />
    </Dropdown>
  );
}
