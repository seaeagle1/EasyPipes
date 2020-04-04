using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace EasyPipes
{
    public class EncryptedStream : Stream
    {
        public Stream BaseStream { get; private set; }

        private AesManaged aes;
        private MemoryStream encryptedBuffer;
        private CryptoStream encryptionStream;
        private CryptoStream decryptionStream;
        private ICryptoTransform encryptor;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="basestream"></param>
        /// <param name="key">Hex-encoded 32-byte key</param>
        /// <param name="iv">8-character IV (16-byte UTF16)</param>
        public EncryptedStream(Stream basestream, string key, string iv)
        {
            BaseStream = basestream;

            aes = new AesManaged();
            aes.Mode = CipherMode.CBC;
            byte[] keyBytes = BigInteger.Parse(key, System.Globalization.NumberStyles.HexNumber)
                .ToByteArray();

            UnicodeEncoding encoding = new UnicodeEncoding();
            var decryptor = aes.CreateDecryptor(keyBytes, encoding.GetBytes(iv));
            decryptionStream = new CryptoStream(BaseStream, decryptor, CryptoStreamMode.Read);

            // encrypted data is stored in memory buffer until Flush is called
            // since AES is block-coding we need an 'end of message' signal

            encryptor = aes.CreateEncryptor(keyBytes, encoding.GetBytes(iv));
            encryptedBuffer = new MemoryStream();
            encryptionStream = new CryptoStream(encryptedBuffer, encryptor, CryptoStreamMode.Write);
        }

        protected override void Dispose(bool disposing)
        {
            encryptionStream.Dispose();
            decryptionStream.Dispose();
            aes.Dispose();
            BaseStream.Dispose();
            base.Dispose(disposing);
        }

        public override bool CanRead => true;

        public override bool CanSeek => false;

        public override bool CanWrite => true;

        public override long Length => throw new NotImplementedException();

        public override long Position { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }

        public override void Flush()
        {
            // write encrypted data to basestream
            encryptionStream.FlushFinalBlock();
            encryptedBuffer.WriteTo(BaseStream);

            // create new buffer
            encryptionStream.Dispose();
            encryptedBuffer = new MemoryStream();
            encryptionStream = new CryptoStream(encryptedBuffer, encryptor, CryptoStreamMode.Write);
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            return decryptionStream.Read(buffer, offset, count);
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotImplementedException();
        }

        public override void SetLength(long value)
        {
            throw new NotImplementedException();
        }

        public override void Write(byte[] buffer, int offset, int count)
        {
            encryptionStream.Write(buffer, offset, count);
        }
    }
}
