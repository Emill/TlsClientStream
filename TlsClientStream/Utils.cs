﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Text.RegularExpressions;

namespace TlsClientStream
{
    internal static class Utils
    {
        public static readonly Dictionary<string, string> HashNameToOID = new Dictionary<string, string>() {
            {"SHA1", "1.3.14.3.2.26"},
            {"SHA256", "2.16.840.1.101.3.4.2.1"},
            {"SHA384", "2.16.840.1.101.3.4.2.2"},
            {"SHA512", "2.16.840.1.101.3.4.2.3"}
        };

        public static int GetHashLen(TLSHashAlgorithm hashAlgorithm)
        {
            switch (hashAlgorithm)
            {
                case TLSHashAlgorithm.SHA1:
                    return 160;
                case TLSHashAlgorithm.SHA256:
                    return 256;
                case TLSHashAlgorithm.SHA384:
                    return 384;
                case TLSHashAlgorithm.SHA512:
                    return 512;
                default:
                    throw new NotSupportedException();
            }
        }

        public static void ClearArray(Array array)
        {
            Array.Clear(array, 0, array.Length);
        }

        public static bool ArraysEqual(byte[] arr1, int offset1, byte[] arr2, int offset2, int len)
        {
            var end = offset1 + len;
            for (int i1 = offset1, i2 = offset2; i1 < end; i1++, i2++)
            {
                if (arr1[i1] != arr2[i2])
                    return false;
            }
            return true;
        }

        public static int WriteUInt64(byte[] buf, int offset, ulong v)
        {
            buf[offset] = (byte)(v >> 56);
            buf[offset + 1] = (byte)(v >> 48);
            buf[offset + 2] = (byte)(v >> 40);
            buf[offset + 3] = (byte)(v >> 32);
            buf[offset + 4] = (byte)(v >> 24);
            buf[offset + 5] = (byte)(v >> 16);
            buf[offset + 6] = (byte)(v >> 8);
            buf[offset + 7] = (byte)v;

            return 8;
        }

        public static int WriteUInt32(byte[] buf, int offset, uint v)
        {
            buf[offset] = (byte)(v >> 24);
            buf[offset + 1] = (byte)(v >> 16);
            buf[offset + 2] = (byte)(v >> 8);
            buf[offset + 3] = (byte)v;

            return 4;
        }

        public static int WriteUInt24(byte[] buf, int offset, int v)
        {
            buf[offset] = (byte)(v >> 16);
            buf[offset + 1] = (byte)(v >> 8);
            buf[offset + 2] = (byte)v;

            return 3;
        }

        public static int WriteUInt16(byte[] buf, int offset, ushort v)
        {
            buf[offset] = (byte)(v >> 8);
            buf[offset + 1] = (byte)v;

            return 2;
        }

        public static ushort ReadUInt16(byte[] buf, ref int offset)
        {
            ushort res = (ushort)((buf[offset] << 8) | buf[offset + 1]);
            offset += 2;
            return res;
        }

        public static int ReadUInt24(byte[] buf, ref int offset)
        {
            var res = (buf[offset] << 16) | (buf[offset + 1] << 8) | buf[offset + 2];
            offset += 3;
            return res;
        }

        public static uint ReadUInt32(byte[] buf, ref int offset)
        {
            var res = (buf[offset] << 24) | (buf[offset + 1] << 16) | (buf[offset + 2] << 8) | buf[offset + 3];
            offset += 4;
            return (uint)res;
        }

        public static ulong ReadUInt64(byte[] buf, ref int offset)
        {
            var res = ((ulong)buf[offset] << 56) | ((ulong)buf[offset + 1] << 48) | ((ulong)buf[offset + 2] << 40) | ((ulong)buf[offset + 3] << 32) |
                ((ulong)buf[offset + 4] << 24) | ((ulong)buf[offset + 5] << 16) | ((ulong)buf[offset + 6] << 8) | (ulong)buf[offset + 7];
            offset += 8;
            return res;
        }

        /// <summary>
        /// hmac should be initialized with the secret key
        /// </summary>
        /// <param name="hmac"></param>
        /// <param name="label"></param>
        /// <param name="seed"></param>
        /// <param name="bytesNeeded"></param>
        /// <returns></returns>
        public static byte[] PRF(HMAC hmac, string label, byte[] seed, int bytesNeeded)
        {
            var blockSize = hmac.HashSize / 8;
            var rounds = (bytesNeeded + (blockSize - 1)) / blockSize;

            var labelLen = Encoding.ASCII.GetByteCount(label);
            var a = new byte[labelLen + seed.Length];
            Encoding.ASCII.GetBytes(label, 0, label.Length, a, 0);
            Buffer.BlockCopy(seed, 0, a, labelLen, seed.Length);

            byte[] ret = new byte[rounds * blockSize];
            byte[] input = new byte[blockSize + a.Length];
            Buffer.BlockCopy(a, 0, input, blockSize, a.Length);

            for (var i = 0; i < rounds; i++)
            {
                var aNew = hmac.ComputeHash(a);
                ClearArray(a);
                a = aNew;
                Buffer.BlockCopy(a, 0, input, 0, blockSize);
                byte[] temp = hmac.ComputeHash(input);
                Buffer.BlockCopy(temp, 0, ret, i * blockSize, blockSize);
                ClearArray(temp);
            }
            ClearArray(a);
            ClearArray(input);
            if (bytesNeeded == ret.Length)
                return ret;
            byte[] retTruncated = new byte[bytesNeeded];
            Buffer.BlockCopy(ret, 0, retTruncated, 0, bytesNeeded);
            ClearArray(ret);
            return retTruncated;
        }

        public static int GetASNLength(byte[] buf, ref int offset)
        {
            if ((buf[offset] & 0x80) == 0)
                return buf[offset++];
            var lenLen = buf[offset++] & ~0x80;
            if (lenLen > 3)
                throw new NotSupportedException("ASN sequences longer than 2^24 bytes not supported.");
            int len = 0;
            for (var i = 0; i < lenLen; i++)
            {
                len <<= 8;
                len += buf[offset++];
            }
            return len;
        }

        public static bool HostnameInCertificate(X509Certificate2 certificate, string hostname)
        {
            var ext = certificate.Extensions["2.5.29.17"];
            if (ext != null)
            {
                var bytes = ext.RawData;
                if (bytes[0] == 0x30) // General names tag
                {
                    var offset = 1;
                    var len = GetASNLength(bytes, ref offset);
                    var end = offset + len;
                    while (offset < end)
                    {
                        var tag = bytes[offset++];
                        var itemLen = GetASNLength(bytes, ref offset);
                        switch (tag)
                        {
                            case 0x82: // dNSName
                                var name = Encoding.ASCII.GetString(bytes, offset, itemLen);
                                if (MatchHostname(name, hostname))
                                    return true;
                                break;
                            case 0x87: // iPAddress
                                var ipBytes = new byte[itemLen];
                                Buffer.BlockCopy(bytes, offset, ipBytes, 0, itemLen);
                                var ip = new System.Net.IPAddress(ipBytes);
                                System.Net.IPAddress hostIp;
                                if (System.Net.IPAddress.TryParse(hostname, out hostIp) && ip.Equals(hostIp))
                                    return true;
                                break;
                            default: // Other types are not checked according to rfc2818, so skip
                                break;
                        }
                        offset += itemLen;
                    }
                }
            }
            var other = certificate.GetNameInfo(X509NameType.DnsName, false);
            if (!string.IsNullOrEmpty(other))
                return MatchHostname(other, hostname);
            return false;
        }

        public static bool MatchHostname(string altname, string hostname)
        {
            altname = altname.ToLower();
            hostname = hostname.ToLower();
            if (altname == hostname)
                return true;

            if (altname == "")
                return false;

            var dotIndex = altname.IndexOf('.');
            string firstPart;
            var rest = "";
            if (dotIndex != -1)
            {
                firstPart = altname.Substring(0, dotIndex);
                rest = altname.Substring(dotIndex);
            }
            else
            {
                firstPart = altname;
            }

            if (firstPart.Length > 0 && firstPart.All(c => c == '*'))
                return Regex.IsMatch(hostname, "^[^.]+" + Regex.Escape(rest) + "$");

            if (firstPart.StartsWith("xn--") || hostname.StartsWith("xn--"))
                return false;

            return Regex.IsMatch(hostname, "^" + Regex.Escape(firstPart).Replace(@"\*", "[^.]*") + Regex.Escape(rest) + "$");
        }

        public static byte[] DecodeDERSignature(byte[] signature, int offset, int len, int integerLength)
        {
            var decodedSignature = new byte[integerLength * 2];
            offset += 1; // Skip tag 0x30 (SEQUENCE)
            Utils.GetASNLength(signature, ref offset);
            offset += 1; // 0x02 (INTEGER)

            int len1 = Utils.GetASNLength(signature, ref offset);
            if (integerLength == len1 - 1)
            {
                // Remove sign byte
                offset++;
                len1--;
            }
            // NOTE: MSB zeros are not present in the ASN-encoding, so we must add them if the length is shorter than expected
            Buffer.BlockCopy(signature, offset, decodedSignature, integerLength - len1, len1);
            offset += len1;
            offset += 1; // 0x02 (INTEGER)
            int len2 = Utils.GetASNLength(signature, ref offset);
            if (integerLength == len2 - 1)
            {
                // Remove sign byte
                offset++;
                len2--;
            }
            Buffer.BlockCopy(signature, offset, decodedSignature, integerLength * 2 - len2, len2);

            return decodedSignature;
        }
    }
}
