import { type UserConfig } from 'vite';
import parentConfig from '../../vite.config';
import path from 'node:path';

const config: UserConfig = {
  ...parentConfig,
  root: path.resolve(__dirname, '../../'),
};

export default config;
