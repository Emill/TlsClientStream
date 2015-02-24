﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace TlsClientStream
{
    internal enum AlertLevel : byte
    {
        Warning = 1,
        Fatal = 2
    }

    public enum AlertDescription : byte
    {
        CloseNotify = 0,
        UnexpectedMessage = 10,
        BadRecordMac = 20,
        RecordOverflow = 22,
        HandshakeFailure = 40,
        CertificateRevoked = 44,
        CertificateExpired = 45,
        CertificateUnknown = 46,
        IllegalParameter = 47,
        DecodeError = 50,
        DecryptError = 51,
        ProtocolVersion = 70
    }

    internal enum ContentType : byte
    {
        ChangeCipherSpec = 20,
        Alert = 21,
        Handshake = 22,
        ApplicationData = 23
    }

    internal enum HandshakeType : byte
    {
        HelloRequest = 0,
        ClientHello = 1,
        ServerHello = 2,
        Certificate = 11,
        ServerKeyExchange = 12,
        CertificateRequest = 13,
        ServerHelloDone = 14,
        CertificateVerify = 15,
        ClientKeyExchange = 16,
        Finished = 20
    }

    internal enum CipherSuite : ushort
    {
        TLS_RSA_WITH_AES_128_CBC_SHA = 0x002F,
        TLS_DHE_DSS_WITH_AES_128_CBC_SHA = 0x0032,
        TLS_DHE_RSA_WITH_AES_128_CBC_SHA = 0x0033,
        TLS_RSA_WITH_AES_256_CBC_SHA = 0x0035,
        TLS_DHE_DSS_WITH_AES_256_CBC_SHA = 0x0038,
        TLS_DHE_RSA_WITH_AES_256_CBC_SHA = 0x0039,
        TLS_DHE_DSS_WITH_AES_128_CBC_SHA256 = 0x0040,
        TLS_RSA_WITH_AES_128_CBC_SHA256 = 0x003C,
        TLS_RSA_WITH_AES_256_CBC_SHA256 = 0x003D,
        TLS_DHE_RSA_WITH_AES_128_CBC_SHA256 = 0x0067,
        TLS_DHE_DSS_WITH_AES_256_CBC_SHA256 = 0x006A,
        TLS_DHE_RSA_WITH_AES_256_CBC_SHA256 = 0x006B,

        // RFC 5288
        TLS_RSA_WITH_AES_128_GCM_SHA256 = 0x009C,
        TLS_RSA_WITH_AES_256_GCM_SHA384 = 0x009D,
        TLS_DHE_RSA_WITH_AES_128_GCM_SHA256 = 0x009E,
        TLS_DHE_RSA_WITH_AES_256_GCM_SHA384 = 0x009F,
        TLS_DHE_DSS_WITH_AES_128_GCM_SHA256 = 0x00A2,
        TLS_DHE_DSS_WITH_AES_256_GCM_SHA384 = 0x00A3,

        // RFC 4492
        TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA = 0xC004,
        TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA = 0xC005,
        TLS_ECDH_RSA_WITH_AES_128_CBC_SHA = 0xC00E,
        TLS_ECDH_RSA_WITH_AES_256_CBC_SHA = 0xC00F,
        TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA = 0xC013,
        TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA = 0xC014,

        // RFC 5289 CBC
        TLS_ECDHE_ECDSA_WITH_AES_128_CBC_SHA256 = 0xC023,
        TLS_ECDHE_ECDSA_WITH_AES_256_CBC_SHA384 = 0xC024,
        TLS_ECDH_ECDSA_WITH_AES_128_CBC_SHA256 = 0xC025,
        TLS_ECDH_ECDSA_WITH_AES_256_CBC_SHA384 = 0xC026,
        TLS_ECDHE_RSA_WITH_AES_128_CBC_SHA256 = 0xC027,
        TLS_ECDHE_RSA_WITH_AES_256_CBC_SHA384 = 0xC028,
        TLS_ECDH_RSA_WITH_AES_128_CBC_SHA256 = 0xC029,
        TLS_ECDH_RSA_WITH_AES_256_CBC_SHA384 = 0xC02A,

        // RFC 5289 GCM
        TLS_ECDHE_ECDSA_WITH_AES_128_GCM_SHA256 = 0xC02B,
        TLS_ECDHE_ECDSA_WITH_AES_256_GCM_SHA384 = 0xC02C,
        TLS_ECDH_ECDSA_WITH_AES_128_GCM_SHA256 = 0xC02D,
        TLS_ECDH_ECDSA_WITH_AES_256_GCM_SHA384 = 0xC02E,
        TLS_ECDHE_RSA_WITH_AES_128_GCM_SHA256 = 0xC02F,
        TLS_ECDHE_RSA_WITH_AES_256_GCM_SHA384 = 0xC030,
        TLS_ECDH_RSA_WITH_AES_128_GCM_SHA256 = 0xC031,
        TLS_ECDH_RSA_WITH_AES_256_GCM_SHA384 = 0xC032
    }

    internal enum ExtensionType : ushort
    {
        ServerName = 0x0000,
        SupportedEllipticCurves = 0x000a,
        SupportedPointFormats = 0x000b,
        SignatureAlgorithms = 0x000d,
        RenegotiationInfo = 0xff01
    }

    internal enum TLSHashAlgorithm : byte
    {
        SHA1 = 2,
        SHA256 = 4,
        SHA384 = 5,
        SHA512 = 6
    }

    internal enum SignatureAlgorithm : byte
    {
        RSA = 1,
        DSA = 2,
        ECDSA = 3
    }

    internal enum NamedCurve : ushort
    {
        secp256r1 = 23,
        secp384r1 = 24,
        secp521r1 = 25
    }

    internal enum ClientCertificateType : byte
    {
        RSASign = 1,
        DSSSign = 2,
        RSAFixedDH = 3,
        DSSFixedDH = 4
    }

    internal enum KeyExchange : byte
    {
        NULL,
        RSA,
        DHE_RSA,
        DHE_DSS,
        ECDHE_RSA,
        ECDHE_ECDSA,
        ECDH_RSA,
        ECDH_ECDSA
    }

    internal enum PRFAlgorithm : byte
    {
        TLSPrfSHA256,
        TLSPrfSHA384
    }

    internal enum AesMode : byte
    {
        CBC,
        GCM
    }
}
