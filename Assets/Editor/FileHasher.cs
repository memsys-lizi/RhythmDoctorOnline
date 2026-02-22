using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Editor
{

    /// <summary>
    /// 支持的哈希算法类型
    /// </summary>
    public enum HashAlgorithmType
    {
        MD5,
        SHA1,
        SHA256,
        SHA384,
        SHA512
    }

    public static class FileHasher
    {
        /// <summary>
        /// 计算文件的哈希值
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="algorithmType">哈希算法类型</param>
        /// <returns>哈希值的十六进制字符串（小写）</returns>
        /// <exception cref="FileNotFoundException">文件不存在时抛出</exception>
        /// <exception cref="ArgumentException">不支持的哈希算法时抛出</exception>
        public static string ComputeFileHash(string filePath, HashAlgorithmType algorithmType)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("文件不存在", filePath);

            using (var stream = File.OpenRead(filePath))
            using (var hashAlgorithm = CreateHashAlgorithm(algorithmType))
            {
                byte[] hashBytes = hashAlgorithm.ComputeHash(stream);
                return ByteArrayToHexString(hashBytes);
            }
        }

        /// <summary>
        /// 根据枚举创建对应的哈希算法实例
        /// </summary>
        private static HashAlgorithm CreateHashAlgorithm(HashAlgorithmType algorithmType)
        {
            switch (algorithmType)
            {
                case HashAlgorithmType.MD5:
                    return MD5.Create();
                case HashAlgorithmType.SHA1:
                    return SHA1.Create();
                case HashAlgorithmType.SHA256:
                    return SHA256.Create();
                case HashAlgorithmType.SHA384:
                    return SHA384.Create();
                case HashAlgorithmType.SHA512:
                    return SHA512.Create();
                default:
                    throw new ArgumentException($"不支持的哈希算法: {algorithmType}", nameof(algorithmType));
            }
        }

        /// <summary>
        /// 将字节数组转换为小写十六进制字符串
        /// </summary>
        private static string ByteArrayToHexString(byte[] bytes)
        {
            StringBuilder sb = new StringBuilder(bytes.Length * 2);
            foreach (byte b in bytes)
                sb.Append(b.ToString("x2"));
            return sb.ToString();
        }
    }
}