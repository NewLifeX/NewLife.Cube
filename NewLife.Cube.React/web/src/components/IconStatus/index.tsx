import React from 'react';
import { CheckOutlined, CloseOutlined } from '@ant-design/icons';

const IconStatus: React.FC<{ status: boolean }> = ({ status }) => {
  return status ? (
    <CheckOutlined
      style={{
        color: '#3bd27e',
        fontSize: 14,
      }}
    />
  ) : (
    <CloseOutlined
      style={{
        color: '#ff4d4f',
        fontSize: 14,
      }}
    />
  );
};

export default IconStatus;
