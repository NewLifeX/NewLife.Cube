/**
 * 认证页统一布局
 *
 * 为登录、注册、激活、找回密码等页面提供统一的品牌区与表单区壳层。
 */
import type { ReactNode } from 'react';
import { Card, Space, Tag } from 'antd';

export interface AuthHighlight {
  icon?: ReactNode;
  title: ReactNode;
  description: ReactNode;
}

export interface AuthLayoutProps {
  badge?: ReactNode;
  title: ReactNode;
  description?: ReactNode;
  logo?: ReactNode;
  brandTitle: ReactNode;
  brandSubtitle?: ReactNode;
  highlights?: AuthHighlight[];
  actions?: ReactNode;
  footer?: ReactNode;
  background?: string;
  children: ReactNode;
}

export default function AuthLayout({
  badge,
  title,
  description,
  logo,
  brandTitle,
  brandSubtitle,
  highlights = [],
  actions,
  footer,
  background,
  children,
}: AuthLayoutProps) {
  const shellStyle = background ? { background } : undefined;

  return (
    <div className="cube-auth-layout" style={shellStyle}>
      <div className="cube-auth-shell">
        <aside className="cube-auth-aside">
          <div className="cube-auth-aside-glow" />
          <div className="cube-auth-brand">
            <div className="cube-auth-brand-mark">{logo}</div>
            <div className="cube-auth-brand-text">
              <div className="cube-auth-brand-title">{brandTitle}</div>
              {brandSubtitle ? <div className="cube-auth-brand-subtitle">{brandSubtitle}</div> : null}
            </div>
          </div>

          <div className="cube-auth-intro">
            <Tag color="blue" variant="filled" className="cube-auth-eyebrow">
              React 19 + Ant Design 6
            </Tag>
            <h1 className="cube-auth-hero-title">管理后台，也该像个产品。</h1>
            <p className="cube-auth-hero-desc">
              统一身份入口、动态字段驱动、权限菜单与高频业务操作，在同一套现代化界面中自然协作。
            </p>
          </div>

          {highlights.length > 0 ? (
            <div className="cube-auth-highlights">
              {highlights.map((item, index) => (
                <div key={index} className="cube-auth-highlight">
                  <div className="cube-auth-highlight-icon">{item.icon}</div>
                  <div className="cube-auth-highlight-body">
                    <div className="cube-auth-highlight-title">{item.title}</div>
                    <div className="cube-auth-highlight-desc">{item.description}</div>
                  </div>
                </div>
              ))}
            </div>
          ) : null}

          {actions ? <div className="cube-auth-aside-actions">{actions}</div> : null}
        </aside>

        <section className="cube-auth-panel">
          <Card variant="borderless" className="cube-auth-card">
            <Space orientation="vertical" size={20} style={{ width: '100%' }}>
              <div className="cube-auth-card-head">
                {badge ? <div className="cube-auth-card-badge">{badge}</div> : null}
                <div className="cube-auth-card-title">{title}</div>
                {description ? <div className="cube-auth-card-desc">{description}</div> : null}
              </div>

              <div className="cube-auth-card-body">{children}</div>

              {footer ? <div className="cube-auth-card-footer">{footer}</div> : null}
            </Space>
          </Card>
        </section>
      </div>
    </div>
  );
}
