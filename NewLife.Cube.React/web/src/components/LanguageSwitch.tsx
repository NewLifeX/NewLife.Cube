/**
 * 语言切换器
 */
import { Dropdown } from 'antd';
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
      <GlobalOutlined style={{ fontSize: 16, cursor: 'pointer', padding: '0 8px' }} />
    </Dropdown>
  );
}
