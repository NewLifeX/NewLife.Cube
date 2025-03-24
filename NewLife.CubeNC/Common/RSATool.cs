using System.Security.Cryptography;

public static class RSTool
{
    /// <summary>生成 PEM 格式的 RSA 密钥对</summary>
    /// <param name="keySize">密钥长度，默认2048位</param>
    /// <returns>包含私钥和公钥的字符串数组(顺序：[0]公钥，[1]私钥)</returns>
    public static String[] GeneratePemKey(Int32 keySize = 2048)
    {
        using var rsa = new RSACryptoServiceProvider(keySize);
        var privateKey = ExportPrivateKeyToPem(rsa);
        var publicKey = ExportPublicKeyToPem(rsa);
        return [publicKey, privateKey];
    }

    private static String ExportPrivateKeyToPem(RSACryptoServiceProvider rsa)
    {
        var parameters = rsa.ExportParameters(true);
        using var sw = new StringWriter();
        sw.WriteLine("-----BEGIN RSA PRIVATE KEY-----");
        sw.WriteLine(Convert.ToBase64String(EncodePrivateKey(parameters), Base64FormattingOptions.InsertLineBreaks));
        sw.WriteLine("-----END RSA PRIVATE KEY-----");
        return sw.ToString();
    }

    private static String ExportPublicKeyToPem(RSACryptoServiceProvider rsa)
    {
        var parameters = rsa.ExportParameters(false);
        using var sw = new StringWriter();
        sw.WriteLine("-----BEGIN PUBLIC KEY-----");
        sw.WriteLine(Convert.ToBase64String(EncodePublicKey(parameters), Base64FormattingOptions.InsertLineBreaks));
        sw.WriteLine("-----END PUBLIC KEY-----");
        return sw.ToString();
    }

    private static Byte[] EncodePrivateKey(RSAParameters parameters)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((Byte)0x30); // SEQUENCE
        using (var innerStream = new MemoryStream())
        {
            using var innerWriter = new BinaryWriter(innerStream);
            EncodeIntegerBigEndian(innerWriter, [0x00]); // Version
            EncodeIntegerBigEndian(innerWriter, parameters.Modulus);
            EncodeIntegerBigEndian(innerWriter, parameters.Exponent);
            EncodeIntegerBigEndian(innerWriter, parameters.D);
            EncodeIntegerBigEndian(innerWriter, parameters.P);
            EncodeIntegerBigEndian(innerWriter, parameters.Q);
            EncodeIntegerBigEndian(innerWriter, parameters.DP);
            EncodeIntegerBigEndian(innerWriter, parameters.DQ);
            EncodeIntegerBigEndian(innerWriter, parameters.InverseQ);
            var length = (int)innerStream.Length;
            EncodeLength(writer, length);
            writer.Write(innerStream.GetBuffer(), 0, length);
        }
        return ms.ToArray();
    }

    private static Byte[] EncodePublicKey(RSAParameters parameters)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        writer.Write((Byte)0x30); // SEQUENCE
        using (var innerStream = new MemoryStream())
        {
            using var innerWriter = new BinaryWriter(innerStream);
            innerWriter.Write((Byte)0x30); // SEQUENCE
            EncodeLength(innerWriter, 13);
            innerWriter.Write((Byte)0x06); // OBJECT IDENTIFIER
            var rsaOid = new Byte[] { 0x2A, 0x86, 0x48, 0x86, 0xF7, 0x0D, 0x01, 0x01, 0x01 };
            EncodeLength(innerWriter, rsaOid.Length);
            innerWriter.Write(rsaOid);
            innerWriter.Write((Byte)0x05); // NULL
            EncodeLength(innerWriter, 0);
            innerWriter.Write((Byte)0x03); // BIT STRING
            using (var bitStringStream = new MemoryStream())
            {
                using var bitStringWriter = new BinaryWriter(bitStringStream);
                bitStringWriter.Write((Byte)0x00);
                bitStringWriter.Write((Byte)0x30); // SEQUENCE
                using (var paramsStream = new MemoryStream())
                {
                    using var paramsWriter = new BinaryWriter(paramsStream);
                    EncodeIntegerBigEndian(paramsWriter, parameters.Modulus);
                    EncodeIntegerBigEndian(paramsWriter, parameters.Exponent);
                    var paramsLength = (Int32)paramsStream.Length;
                    EncodeLength(bitStringWriter, paramsLength);
                    bitStringWriter.Write(paramsStream.GetBuffer(), 0, paramsLength);
                }
                var bitStringLength = (Int32)bitStringStream.Length;
                EncodeLength(innerWriter, bitStringLength);
                innerWriter.Write(bitStringStream.GetBuffer(), 0, bitStringLength);
            }
            var length = (Int32)innerStream.Length;
            EncodeLength(writer, length);
            writer.Write(innerStream.GetBuffer(), 0, length);
        }
        return ms.ToArray();
    }

    private static void EncodeIntegerBigEndian(BinaryWriter writer, Byte[] value, Boolean forceUnsigned = true)
    {
        writer.Write((Byte)0x02); // INTEGER
        var prefixZeros = 0;
        for (var i = 0; i < value.Length; i++)
        {
            if (value[i] != 0) break;
            prefixZeros++;
        }
        if (value.Length - prefixZeros == 0)
        {
            EncodeLength(writer, 1);
            writer.Write((Byte)0);
        }
        else
        {
            if (forceUnsigned && value[prefixZeros] > 0x7f)
            {
                EncodeLength(writer, value.Length - prefixZeros + 1);
                writer.Write((Byte)0);
            }
            else
            {
                EncodeLength(writer, value.Length - prefixZeros);
            }
            for (var i = prefixZeros; i < value.Length; i++)
            {
                writer.Write(value[i]);
            }
        }
    }

    private static void EncodeLength(BinaryWriter writer, int length)
    {
        if (length < 0x80)
        {
            writer.Write((Byte)length);
        }
        else
        {
            var temp = length;
            var bytesRequired = 0;
            while (temp > 0)
            {
                temp >>= 8;
                bytesRequired++;
            }
            writer.Write((Byte)(bytesRequired | 0x80));
            for (var i = bytesRequired - 1; i >= 0; i--)
            {
                writer.Write((Byte)(length >> (8 * i) & 0xff));
            }
        }
    }
}