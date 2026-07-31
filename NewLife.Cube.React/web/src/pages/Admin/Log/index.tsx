import { PageContainer } from '@ant-design/pro-components';
import { useIntl } from '@umijs/max';
import { Alert, Card } from 'antd';
import React from 'react';

/**
 * 审计日志占位页
 * 说明：routes.ts 中已声明 /admin/log（审计日志）路由，但此前缺少对应组件，
 * 导致 `max setup` 在路由转换阶段因找不到 ./Admin/Log 而构建失败。
 * 此处先提供最小可用占位页解阻构建；后端 /Admin/Log（ReadOnlyEntityController<Log>）已就绪，
 * 后续可参照 Admin/Role 的 ProTable + GetFields 模式接入真实审计日志列表。
 */
const Log: React.FC = () => {
  const intl = useIntl();
  return (
    <PageContainer
      title={intl.formatMessage({
        id: 'pages.admin.log.title',
        defaultMessage: '审计日志',
      })}
    >
      <Card>
        <Alert
          message={intl.formatMessage({
            id: 'pages.admin.log.comingSoon',
            defaultMessage: '审计日志页面正在建设中，后续将接入后端 /Admin/Log 接口展示操作审计记录。',
          })}
          type="info"
          showIcon
        />
      </Card>
    </PageContainer>
  );
};

export default Log;
