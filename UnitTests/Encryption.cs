/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using EasyPipes;
using Xunit;

namespace EPUnitTests
{
    public class Encryption
    {
        private string DecryptedString = "This is a test string";

        [Fact]
        public void CheckEncryptionAndDecryption()
        {
            Encryptor enc = new Encryptor("2c70e12b7a0646f92279f427c7b38e7334d8e5389cff167a1dc30e73f826b683");

            byte[] encoded = UnicodeEncoding.Unicode.GetBytes(DecryptedString);
            byte[] result = enc.EncryptMessage(encoded);
            string readableresult = Convert.ToBase64String(result);


            Encryptor enc2 = new Encryptor("2c70e12b7a0646f92279f427c7b38e7334d8e5389cff167a1dc30e73f826b683");

            byte[] base64 = Convert.FromBase64String(readableresult);
            byte[] result2 = enc.DecryptMessage(base64);
            string readableresult2 = UnicodeEncoding.Unicode.GetString(result2);

            Assert.Equal(DecryptedString, readableresult2);
        }
    }
}
