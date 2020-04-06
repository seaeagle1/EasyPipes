/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Sockets;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace EasyPipes
{
    public class Encryptor
    {
        private AesManaged aes;
        private ICryptoTransform encryptor;
        private ICryptoTransform decryptor;

        protected string EncryptionKey { get; private set; }
        protected string IV { get; private set; }

        /// <param name="key">Hex-encoded 32-byte key</param>
        /// <param name="iv">8-character IV (16-byte UTF16)</param>
        public Encryptor(string key, string iv)
        {
            EncryptionKey = key;
            IV = iv;

            aes = new AesManaged();
            aes.Mode = CipherMode.CBC;
            byte[] keyBytes = BigInteger.Parse(key, System.Globalization.NumberStyles.HexNumber)
                .ToByteArray();

            UnicodeEncoding encoding = new UnicodeEncoding();
            decryptor = aes.CreateDecryptor(keyBytes, encoding.GetBytes(iv));
            encryptor = aes.CreateEncryptor(keyBytes, encoding.GetBytes(iv));
        }

        public byte[] DecryptMessage(byte[] msg)
        {
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Write))
            {
                cs.Write(msg, 0, msg.Length);
                cs.FlushFinalBlock();

                return ms.ToArray();
            }
        }

        public byte[] EncryptMessage(byte[] msg)
        {
            using (MemoryStream ms = new MemoryStream())
            using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
            {
                cs.Write(msg, 0, msg.Length);
                cs.FlushFinalBlock();

                return ms.ToArray();
            }
        }
    }
}
