using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;

namespace Ocrosoft
{
    /// <summary>
    ///     加解密及信息摘要功能类
    /// </summary>
    public class OSecurity
    {
        private static readonly string RSA_PublicKey =
            @"<RSAKeyValue><Modulus>MIGfMA0GCSqGSIb3DQEBAQUAA4GNADCBiQKBgQCbIGP2E/PxI0EEdIPMQTArwH+1505lzC20EBCSy8aqMcJVZx5AbvYj/nbI4v+N7xlmS4kciOqj1zaGj9QoivKVjsBLW6Hmhe8QCCw+MmR6jmWwLgmNAakOoDmlFjO8HVRZzTZEjiZX5LX9NG9FgYTGmZlJbnv48irG4BXyMpOdzQIDAQAB</Modulus><Exponent>AQAB</Exponent></RSAKeyValue>";

        private static readonly string RSA_PrivateKey =
            @"<RSAKeyValue><Modulus>MIICdwIBADANBgkqhkiG9w0BAQEFAASCAmEwggJdAgEAAoGBAJsgY/YT8/EjQQR0g8xBMCvAf7XnTmXMLbQQEJLLxqoxwlVnHkBu9iP+dsji/43vGWZLiRyI6qPXNoaP1CiK8pWOwEtboeaF7xAILD4yZHqOZbAuCY0BqQ6gOaUWM7wdVFnNNkSOJlfktf00b0WBhMaZmUlue/jyKsbgFfIyk53NAgMBAAECgYB8oeNuG83MGVTtbWdOvbkkDb8NuM9F/mth1d5a8pmkt+G4l+a4Qe5EMPfiom5L7KPtihaY9HAAPrKyHfCIukn3Gprv4ysrFKm2wdiBh6VojJ+0L3X2IwEVQ/BZLuT37Qis7P7WXVE1tSV/J6nLW9fUZg+ZXUmlzFAx2EiOWlK6AQJBAM4GFUGOFpyRvED0mNUdP3Jbx3fcf1v3/why4Deqx003YiUMjXQpjLseb1vvvnSfZa34gqnFPKG/fFsU7owoLI0CQQDAwaDUlegMCTWAD89yz+Ea98UlVCUqH+ldCVfC9RdhmT0OwrGvQDHEUmgBKKFTdM3n7FphycAMXKLvrrVb6QZBAkEAjC/TcuHuPOdlg4VsIUdfjr8owUSGXNwo62TPcNGB/+a5n6Ak+G/1VLXm7FX78Hstwu0ga8jL8vvK8GcT0sbbWQJANKTtcwIaJSdiuD4ZL0c9OKtQ6bgIim+6wZEqqfFcWGiMt3pPIwkKTo8fHqnlHbD6B4ySxsBeNkIashFqMNb8wQJBAKIQqEyTf+dynbjKdCccE7bALo1plmNgU82bU7vMOzHYHDsTAZ2CDmgAz78BZZbPiXDPOTOhUXESmK2lfiIIg4M=</D></RSAKeyValue>";

        private static readonly string AES_Key = "chenyanhong";

        /// <summary>
        ///     RSA加密
        /// </summary>
        /// 1
        /// <param name="content">加密内容</param>
        /// <returns></returns>
        public static string RSAEncrypt(string content)
        {
            var publickey = RSA_PublicKey;
            var rsa = new RSACryptoServiceProvider();
            byte[] cipherbytes;
            rsa.FromXmlString(publickey);
            cipherbytes = rsa.Encrypt(Encoding.UTF8.GetBytes(content), false);

            return Convert.ToBase64String(cipherbytes);
        }

        /// <summary>
        ///     RSA解密
        /// </summary>
        /// <param name="content">解密内容</param>
        /// <returns></returns>
        public static string RSADecrypt(string privatekey, string content)
        {
            privatekey = RSA_PrivateKey;
            var rsa = new RSACryptoServiceProvider();
            byte[] cipherbytes;
            rsa.FromXmlString(privatekey);
            cipherbytes = rsa.Decrypt(Convert.FromBase64String(content), false);

            return Encoding.UTF8.GetString(cipherbytes);
        }

        /// <summary>
        ///     MD5加密
        /// </summary>
        /// <param name="content">加密内容</param>
        /// <returns></returns>
        public static string MD5(string content)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            var result = md5.ComputeHash(Encoding.Default.GetBytes(content));
            return Encoding.Default.GetString(result);
        }

        /// <summary>
        ///     获取AES32位密钥
        /// </summary>
        /// <returns></returns>
        private static byte[] GetAesKey()
        {
            var key = AES_Key;
            if (key.Length < 32) key = key.PadRight(32, '0');
            if (key.Length > 32) key = key.Substring(0, 32);
            return Encoding.UTF8.GetBytes(key);
        }

        /// <summary>
        ///     AES加密，32位0补全，ECB，PKCS7，BASE64
        /// </summary>
        /// <param name="content">加密内容</param>
        /// <returns></returns>
        public static string AESEncrypt(string content)
        {
            using (var aesProvider = new AesCryptoServiceProvider())
            {
                aesProvider.Key = GetAesKey();
                aesProvider.Mode = CipherMode.ECB;
                aesProvider.Padding = PaddingMode.PKCS7;
                using (var cryptoTransform = aesProvider.CreateEncryptor())
                {
                    var inputBuffers = Encoding.UTF8.GetBytes(content);
                    var results = cryptoTransform.TransformFinalBlock(inputBuffers, 0, inputBuffers.Length);
                    aesProvider.Clear();
                    aesProvider.Dispose();
                    return Convert.ToBase64String(results, 0, results.Length);
                }
            }
        }

        /// <summary>
        ///     AES解密
        /// </summary>
        /// <param name="content">解密内容</param>
        /// <returns></returns>
        public static string AESDecrypt(string content)
        {
            using (var aesProvider = new AesCryptoServiceProvider())
            {
                aesProvider.Key = GetAesKey();
                aesProvider.Mode = CipherMode.ECB;
                aesProvider.Padding = PaddingMode.PKCS7;
                using (var cryptoTransform = aesProvider.CreateDecryptor())
                {
                    var inputBuffers = Convert.FromBase64String(content);
                    var results = cryptoTransform.TransformFinalBlock(inputBuffers, 0, inputBuffers.Length);
                    aesProvider.Clear();
                    return Encoding.UTF8.GetString(results);
                }
            }
        }

        // <summary>
        /// SHA1加密
        /// </summary>
        /// <param name="content">要加密的内容</param>
        /// <returns></returns>
        public static string SHA1(string content)
        {
            SHA1 sha1 = new SHA1CryptoServiceProvider();
            var bytes_in = Encoding.UTF8.GetBytes(content);
            var bytes_out = sha1.ComputeHash(bytes_in);
            sha1.Dispose();
            var result = BitConverter.ToString(bytes_out);
            result = result.Replace("-", "");
            return result.ToLower();
        }

        /// <summary>
        ///     DateTime转UNIX时间戳
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long DateTimeToTimeStamp(DateTime time)
        {
            var startTime = TimeZone.CurrentTimeZone
                .ToLocalTime(new DateTime(1970, 1, 1));
            return (int) (time - startTime).TotalSeconds;
        }

        /// <summary>
        ///     获得一个由数字大小写字母组成的随机字符串
        /// </summary>
        /// <param name="length">字符串长度</param>
        /// <returns></returns>
        public static string GetRandomString(int length)
        {
            var b = new byte[4];
            new RNGCryptoServiceProvider().GetBytes(b);
            var r = new Random(BitConverter.ToInt32(b, 0));
            string s = null;
            var str = "0123456789abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ";
            for (var i = 0; i < length; i++) s += str.Substring(r.Next(0, str.Length - 1), 1);
            return s;
        }

        /// <summary>
        ///     检查手机号是否正确
        /// </summary>
        /// <param name="phone">手机号</param>
        /// <returns></returns>
        public static bool ValidPhone(string phone)
        {
            if (Regex.IsMatch(phone, "^((13[0-9])|(14[5|7])|(15([0-3]|[5-9]))|(18[0,5-9]))\\d{8}$")) return true;
            return false;
        }

        /// <summary>
        ///     检查身份证是否正确
        /// </summary>
        /// <param name="IDCard">身份证</param>
        /// <returns></returns>
        public static bool ValidIDCard(string IDCard)
        {
            if (Regex.IsMatch(IDCard, "^(\\d{15}$|^\\d{18}$|^\\d{17}(\\d|X|x))$")) return true;
            return false;
        }
    }
}