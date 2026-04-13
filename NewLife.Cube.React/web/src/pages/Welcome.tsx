import {
  PageContainer,
  ProCard,
  ProForm,
  ProFormText,
  ProFormSwitch,
  ProFormDigit,
  FooterToolbar,
} from '@ant-design/pro-components';
import { FormInstance, Segmented } from 'antd';
import React, { useEffect, useRef, useState } from 'react';
import { queryIndex } from '@/services/ant-design-pro/api';

type PropertieItem = {
  name: string;
  displayName: string;
  description: any;
  category: string;
  itemType: any;
  length: number;
  precision: number;
  scale: number;
  nullable: boolean;
  primaryKey: boolean;
  readonly: boolean;
  field: any;
  mapField: any;
  dataSource: any;
  dataVisible: any;
};
type ValueItem = {
  debug: boolean;
  showRunTime: boolean;
  avatarPath: string;
  uploadPath: string;
  webRootPath: string;
  resourceUrl: any;
  corsOrigins: string;
  xFrameOptions: any;
  sameSiteMode: number;
  shareExpire: number;
  robotError: number;
  defaultRole: string;
  allowLogin: boolean;
  allowRegister: boolean;
  autoRegister: boolean;
  paswordStrength: string;
  maxLoginError: number;
  loginForbiddenTime: number;
  forceBindUser: boolean;
  forceBindUserCode: boolean;
  forceBindUserMobile: boolean;
  forceBindUserMail: boolean;
  forceBindNickName: boolean;
  useSsoRole: boolean;
  useSsoDepartment: boolean;
  logoutAll: boolean;
  sessionTimeout: number;
  refreshUserPeriod: number;
  jwtSecret: string;
  tokenExpire: number;
  startPage: string;
  theme: string;
  skin: string;
  loginTip: any;
  formGroupClass: string;
  bootstrapSelect: boolean;
  maxDropDownList: number;
  copyright: any;
  registration: string;
  enableNewUI: boolean;
  eChartsTheme: string;
  titlePrefix: boolean;
  enableTableDoubleClick: boolean;
  starWeb: string;
};

const Welcome: React.FC = () => {
  const formRef = useRef<FormInstance>();
  const [options, setOptions] = useState<string[]>([]);
  const [currentValue, setCurrentValue] = useState<string>();
  const [loading, setLoading] = useState(true);
  const [properties, setProperties] = useState<Record<string, PropertieItem[]>>({});
  const [value, setValue] = useState<ValueItem>({} as ValueItem);
  if (Object.keys(properties).length > 0) {
    formRef.current?.setFieldsValue(value);
  }
  useEffect(() => {
    const fetch = async () => {
      setLoading(false);
      const res = await queryIndex();
      console.log(res);
      if (res.code === 0) {
        const data = res.data;
        console.log(data);
        const segmenteds = Object.keys(data.properties).map((key) => key);
        setOptions(segmenteds);
        setCurrentValue(segmenteds[0]);
        setProperties(data.properties);
        setValue(data.value);
      }
    };
    if (loading) {
      fetch();
    }
  }, [loading]);
  return (
    <PageContainer title={false}>
      <div style={{ textAlign: 'center' }}>
        <Segmented
          size={'large'}
          options={options}
          value={currentValue}
          onChange={(value) => {
            setCurrentValue(value.toString());
          }}
        />
      </div>
      <div style={{ marginTop: 16 }}>
        <ProForm
          formRef={formRef}
          submitter={{
            render(props, dom) {
              return <FooterToolbar>{dom}</FooterToolbar>;
            },
          }}
        >
          {Object.keys(properties).length > 0 &&
            Object.keys(properties).map((key, index) => {
              return (
                <div key={index} style={{ display: currentValue === key ? 'block' : 'none' }}>
                  <ProCard title={key} style={{ marginBottom: 16 }}>
                    {properties[key].map((item, index) => {
                      // @ts-ignore
                      const val = value[item.name];
                      const type = typeof val;
                      switch (type) {
                        case 'string':
                        case 'boolean':
                          return (
                            <ProFormSwitch
                              tooltip={item.description}
                              label={item.displayName}
                              name={item.name}
                              key={index}
                            />
                          );
                        case 'number':
                          return (
                            <ProFormDigit
                              tooltip={item.description}
                              label={item.displayName}
                              name={item.name}
                              key={index}
                            />
                          );
                        default:
                          console.log(val, item.displayName);
                          break;
                      }
                      return (
                        <ProFormText
                          tooltip={item.description}
                          label={item.displayName}
                          name={item.name}
                          key={index}
                        />
                      );
                    })}
                  </ProCard>
                </div>
              );
            })}
        </ProForm>
      </div>
    </PageContainer>
  );
};

export default Welcome;
