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
            string plainBase64Data = RijndaelEtM.Decrypt(msg, EncryptionKey, KeySize.Aes256);
            return Convert.FromBase64String(plainBase64Data);
        }

        public byte[] EncryptMessage(byte[] msg)
        {
            string plainBase64Data = Convert.ToBase64String(msg);
            string encryptedBase64Data = RijndaelEtM.Encrypt(plainBase64Data, EncryptionKey, KeySize.Aes256);
            return Convert.FromBase64String(encryptedBase64Data);
        }
    }
}
