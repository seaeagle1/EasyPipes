/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using Rijndael256;

namespace EasyPipes
{
    public class Encryptor
    {
        protected string EncryptionKey { get; private set; }

        /// <param name="key">Hex-encoded 32-byte key</param>
        /// <param name="iv">8-character IV (16-byte UTF16)</param>
        public Encryptor(string key)
        {
            EncryptionKey = key;
        }

        public byte[] DecryptMessage(byte[] msg)
        {
            return RijndaelEtM.Decrypt(msg, EncryptionKey, KeySize.Aes256);
        }

        public byte[] EncryptMessage(byte[] msg)
        {
            return RijndaelEtM.Encrypt(msg, EncryptionKey, KeySize.Aes256);
        }
    }
}
