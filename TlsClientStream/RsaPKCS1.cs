﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TlsClientStream
{
    public static class RsaPKCS1
    {
        public static bool VerifyRsaPKCS1(RSACryptoServiceProvider key, byte[] signature, byte[] hash, bool allowNoPadding)
        {
            var parameters = key.ExportParameters(false);

            var e = Utils.BigIntegerFromBigEndian(parameters.Exponent, 0, parameters.Exponent.Length);
            var mod = Utils.BigIntegerFromBigEndian(parameters.Modulus, 0, parameters.Modulus.Length);
            var m = Utils.BigIntegerFromBigEndian(signature, 0, signature.Length);
            var decryptedArr = Utils.BigEndianFromBigInteger(BigInteger.ModPow(m, e, mod));

            /*
            PKCS padding used in TLS 1.0/TLS 1.1:
            00 01 [k-3-hashlen 0xff bytes] 00 (hash)
            OR, for only TLS 1.0, there may be no padding (or equivalently, 00 00 [k-3-hashlen 00 bytes] 00 (hash))
            where k is the keylen
            */

            if (allowNoPadding && decryptedArr.Length <= hash.Length)
            {
                int zeros = hash.Length - decryptedArr.Length;
                for (var i = 0; i < zeros; i++)
                {
                    if (hash[i] != 0)
                        return false;
                }
                return Utils.ArraysEqual(decryptedArr, 0, hash, zeros, hash.Length - zeros);
            }

            if (decryptedArr.Length != parameters.Modulus.Length - 1)
                return false;

            if (decryptedArr[0] != 1)
                return false;

            for (var i = 1; i < decryptedArr.Length - hash.Length - 1; i++)
            {
                if (decryptedArr[i] != 0xff)
                    return false;
            }
            if (decryptedArr[decryptedArr.Length - hash.Length - 1] != 0)
                return false;

            return Utils.ArraysEqual(decryptedArr, decryptedArr.Length - hash.Length, hash, 0, hash.Length);
        }
        public static byte[] SignRsaPKCS1(RSACryptoServiceProvider key, byte[] hash)
        {
            // NOTE: The X509Certificate2 must be initialized with the X509KeyStorageFlags.Exportable flag
            var parameters = key.ExportParameters(true);

            var dp = Utils.BigIntegerFromBigEndian(parameters.DP, 0, parameters.DP.Length);
            var dq = Utils.BigIntegerFromBigEndian(parameters.DQ, 0, parameters.DQ.Length);
            var qinv = Utils.BigIntegerFromBigEndian(parameters.InverseQ, 0, parameters.InverseQ.Length);
            var p = Utils.BigIntegerFromBigEndian(parameters.P, 0, parameters.P.Length);
            var q = Utils.BigIntegerFromBigEndian(parameters.Q, 0, parameters.Q.Length);

            var data = new byte[parameters.D.Length - 1];
            data[0] = 1;
            for (var i = 1; i < data.Length - hash.Length - 1; i++)
            {
                data[i] = 0xff;
            }
            data[data.Length - hash.Length - 1] = 0;
            Buffer.BlockCopy(hash, 0, data, data.Length - hash.Length, hash.Length);

            var m = Utils.BigIntegerFromBigEndian(data, 0, data.Length);

            var m1 = BigInteger.ModPow(m, dp, p);
            var m2 = BigInteger.ModPow(m, dq, q);
            var h = qinv * (m1 - m2) % p;
            if (h.Sign == -1)
                h += p;
            var signature = Utils.BigEndianFromBigInteger(m2 + h * q);

            Utils.ClearArray(parameters.D);
            Utils.ClearArray(parameters.DP);
            Utils.ClearArray(parameters.DQ);
            Utils.ClearArray(parameters.InverseQ);
            Utils.ClearArray(parameters.P);
            Utils.ClearArray(parameters.Q);

            return signature;
        }
    }
}
