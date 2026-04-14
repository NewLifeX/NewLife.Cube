/**
 * @cube/api-core — 密码安全工具
 *
 * 使用浏览器原生 Web Crypto API（无需额外依赖）实现 RSA-OAEP 密码加密，
 * 配合后端 GET /Auth/Challenge 接口防止密码明文传输。
 */

/**
 * 将 PEM(SPKI) 格式公钥解析为 ArrayBuffer
 * @param pem PEM 格式字符串（含 BEGIN/END 行）
 */
function pemToArrayBuffer(pem: string): ArrayBuffer {
  const base64 = pem
    .replace(/-----BEGIN [^-]+-----/, '')
    .replace(/-----END [^-]+-----/, '')
    .replace(/\s/g, '');
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i++) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes.buffer;
}

/**
 * 使用 RSA-OAEP/SHA-256 加密密码（浏览器原生 Web Crypto API）
 *
 * 配合后端 GET /Auth/Challenge 接口使用，防止密码明文传输：
 * 1. 调用 getChallenge() 获取 { challengeId, publicKey }
 * 2. 调用本函数 encryptPassword(password, publicKey) 得到 encryptedBase64
 * 3. 提交 POST /Auth/Login 携带 { username, password: encryptedBase64, challengeId }
 *
 * @param password 用户输入的原始密码
 * @param publicKey 后端返回的 PEM(SPKI) 格式 RSA 公钥
 * @returns Base64 编码的加密密文
 */
export async function encryptPassword(password: string, publicKey: string): Promise<string> {
  const keyData = pemToArrayBuffer(publicKey);
  const cryptoKey = await crypto.subtle.importKey(
    'spki',
    keyData,
    { name: 'RSA-OAEP', hash: 'SHA-256' },
    false,
    ['encrypt'],
  );
  const encrypted = await crypto.subtle.encrypt(
    { name: 'RSA-OAEP' },
    cryptoKey,
    new TextEncoder().encode(password),
  );
  return btoa(String.fromCharCode(...new Uint8Array(encrypted)));
}
