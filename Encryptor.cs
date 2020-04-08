/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using Rijndael256;

namespace EasyPipes
{
    /// <summary>
    /// Provides 256-bit symmetric AES encryption and decryption for TCP communication
    /// </summary>
    public class Encryptor
    {
        /// <summary>
        /// The encryption key
        /// </summary>
        protected string EncryptionKey { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="key">The encryption key</param>
        public Encryptor(string key)
        {
            EncryptionKey = key;
        }

        /// <summary>
        /// Decrypt a binary blob
        /// </summary>
        /// <param name="msg">The binary "ciphertext" with IV and MAC</param>
        /// <returns>The binary "plaintext"</returns>
        public byte[] DecryptMessage(byte[] msg)
        {
            return RijndaelEtM.Decrypt(msg, EncryptionKey, KeySize.Aes256);
        }

        /// <summary>
        /// Encrypt a binary blob
        /// </summary>
        /// <param name="msg">The binary "plaintext"</param>
        /// <returns>The binary "ciphertext" with IV and MAC</returns>
        public byte[] EncryptMessage(byte[] msg)
        {
            return RijndaelEtM.Encrypt(msg, EncryptionKey, KeySize.Aes256);
        }
    }
}
