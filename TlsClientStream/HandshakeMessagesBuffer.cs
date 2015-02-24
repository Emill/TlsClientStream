﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TlsClientStream
{
    internal class HandshakeMessagesBuffer : IDisposable
    {
        readonly List<byte[]> _messages = new List<byte[]>();

        byte[] _headerBuffer;
        int _headerBufferLen;
        byte[] _buffer;
        int _bufferLen;
        
        public List<byte[]> Messages { get { return _messages; } }

        public bool HasServerHelloDone { get; private set; }
        public bool HasBufferedData { get { return _headerBuffer != null; } }

        bool _hasFinished;
        bool _hasHelloRequest;

        void CheckType(HandshakeType type)
        {
            if (type == HandshakeType.ServerHelloDone)
                HasServerHelloDone = true;
            else if (type == HandshakeType.Finished)
                _hasFinished = true;
            else if (type == HandshakeType.HelloRequest)
                _hasHelloRequest = true;
        }

        public static bool IsHelloRequest(byte[] message)
        {
            return message[0] == (byte)HandshakeType.HelloRequest && message[1] == 0 && message[2] == 0 && message[3] == 0;
        }

        public int AddBytes(byte[] buffer, int offset, int length, IgnoreHelloRequestsSettings ignoreHelloRequests = IgnoreHelloRequestsSettings.IncludeHelloRequests)
        {
            var numAdded = 0;
            var end = offset + length;

            while (true)
            {
                if (_headerBufferLen == 0)
                {
                    while (offset + 4 <= end)
                    {
                        // We can read at least the header
                        int start = offset;
                        offset++;
                        var messageLen = Utils.ReadUInt24(buffer, ref offset);
                        offset += messageLen;
                        if (offset <= end)
                        {
                            // Whole message fits in buffer, this is the common case
                            var message = new byte[4 + messageLen];
                            Buffer.BlockCopy(buffer, start, message, 0, 4 + messageLen);
                            if ((!_hasHelloRequest && (ignoreHelloRequests == IgnoreHelloRequestsSettings.IncludeHelloRequests ||
                                (ignoreHelloRequests == IgnoreHelloRequestsSettings.IgnoreHelloRequestsUntilFinished && _hasFinished))) ||
                                !IsHelloRequest(message))
                            {
                                _messages.Add(message);
                                CheckType((HandshakeType)message[0]);
                                numAdded++;
                            }
                        }
                        else
                        {
                            // The header fits in the buffer, but not the entire message
                            _headerBuffer = new byte[4];
                            _headerBufferLen = 4;
                            Buffer.BlockCopy(buffer, start, _headerBuffer, 0, 4);
                            _buffer = new byte[messageLen];
                            _bufferLen = messageLen - (offset - end);
                            Buffer.BlockCopy(buffer, start + 4, _buffer, 0, _bufferLen);
                        }
                    }
                    if (offset < end)
                    {
                        // Else, the whole header does not fit in the buffer
                        _headerBuffer = new byte[4];
                        _headerBufferLen = end - offset;
                        Buffer.BlockCopy(buffer, offset, _headerBuffer, 0, _headerBufferLen);
                    }
                    return numAdded;
                }
                else
                {
                    // We have previously buffered up a part of a message that needs to be completed
                    
                    if (_headerBufferLen < 4)
                    {
                        var toCopy = Math.Min(end - offset, 4 - _headerBufferLen);
                        Buffer.BlockCopy(buffer, offset, _headerBuffer, _headerBufferLen, toCopy);
                        _headerBufferLen += toCopy;
                        offset += toCopy;

                        if (_headerBufferLen < 4)
                            return numAdded;
                    }

                    // Now header buffer is complete, so we can fetch message len and fill rest of message buffer as much as possible
                    var tmpOffset = 1;
                    var messageLen = Utils.ReadUInt24(_headerBuffer, ref tmpOffset);
                    var bytesToCopy = Math.Min(end - offset, messageLen - _bufferLen);
                    if (_buffer == null)
                        _buffer = new byte[messageLen];
                    Buffer.BlockCopy(buffer, offset, _buffer, _bufferLen, bytesToCopy);
                    offset += bytesToCopy;
                    _bufferLen += bytesToCopy;
                    if (_bufferLen != messageLen)
                    {
                        return numAdded;
                    }

                    // Now we have a complete message to insert to the queue
                    var message = new byte[4 + messageLen];
                    Buffer.BlockCopy(_headerBuffer, 0, message, 0, 4);
                    Buffer.BlockCopy(_buffer, 0, message, 4, messageLen);
                    if ((!_hasHelloRequest && (ignoreHelloRequests == IgnoreHelloRequestsSettings.IncludeHelloRequests ||
                        (ignoreHelloRequests == IgnoreHelloRequestsSettings.IgnoreHelloRequestsUntilFinished && _hasFinished))) ||
                        !IsHelloRequest(message))
                    {
                        _messages.Add(message);
                        CheckType((HandshakeType)message[0]);
                        numAdded++;
                    }
                    _headerBuffer = null;
                    _headerBufferLen = 0;
                    Utils.ClearArray(_buffer);
                    _buffer = null;
                    _bufferLen = 0;
                }
            }
        }

        public void RemoveFirst()
        {
            if ((HandshakeType)_messages[0][0] == HandshakeType.Finished)
                _hasFinished = false;
            
            // Ignore HasServerHelloDone and _hasHelloRequest

            Utils.ClearArray(_messages[0]);
            _messages.RemoveAt(0);
        }
        /*public void RemoveRange(int index, int count)
        {
            for (var i = 0; i < count; i++)
                Utils.ClearArray(_messages[index + i]);

            _messages.RemoveRange(index, count);
        }*/

        public void ClearMessages()
        {
            foreach (var arr in _messages)
            {
                Utils.ClearArray(arr);
            }
            _messages.Clear();
            HasServerHelloDone = false;
            _hasFinished = false;
            _hasHelloRequest = false;
        }

        public void Dispose()
        {
            ClearMessages();
        }

        public enum IgnoreHelloRequestsSettings
        {
            IncludeHelloRequests,
            IgnoreHelloRequests,
            IgnoreHelloRequestsUntilFinished
        }
    }
}
