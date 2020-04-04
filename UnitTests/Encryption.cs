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

        private string EncryptedString = "fZvRFy90tna8xCEFPWEtq1bKqTng9CUYPeryftc6bbY=";

        [Fact]
        public void CheckEncryption()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                EncryptedStream encrypted = new EncryptedStream(ms, "2c70e12b7a0646f92279f427c7b38e7334d8e5389cff167a1dc30e73f826b683", "iv345678");

                StreamWriter writer = new StreamWriter(encrypted);
                writer.Write("This is a test string");
                writer.Flush();

                long pos = ms.Position;
                ms.Seek(0, SeekOrigin.Begin);

                byte[] buffer = new byte[pos];
                ms.Read(buffer, 0, (int)pos);
                string data = Convert.ToBase64String(buffer);

                Assert.Equal(EncryptedString, data);
            }
        }

        [Fact]
        public void CheckRepeatedEncryption()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                EncryptedStream encrypted = new EncryptedStream(ms, "2c70e12b7a0646f92279f427c7b38e7334d8e5389cff167a1dc30e73f826b683", "iv345678");

                StreamWriter writer = new StreamWriter(encrypted);
                writer.Write("This is a first test string");
                writer.Flush();

                long pos1 = ms.Position;

                writer.Write("This is a test string");
                writer.Flush();

                long pos2 = ms.Position;
                ms.Seek(pos1, SeekOrigin.Begin);

                byte[] buffer = new byte[pos2-pos1];
                ms.Read(buffer, 0, (int)(pos2-pos1));
                string data = Convert.ToBase64String(buffer);

                Assert.Equal(EncryptedString, data);
            }
        }

        [Fact]
        public void CheckDecryption()
        {
            using (MemoryStream ms = new MemoryStream())
            {
                byte[] data = Convert.FromBase64String(EncryptedString);
                ms.Write(data, 0, data.Length);
                ms.Flush();
                ms.Seek(0, SeekOrigin.Begin);

                EncryptedStream encrypted = new EncryptedStream(ms, "2c70e12b7a0646f92279f427c7b38e7334d8e5389cff167a1dc30e73f826b683", "iv345678");

                StreamReader reader = new StreamReader(encrypted);
                string text = reader.ReadToEnd();

                Assert.Equal("This is a test string", text);
            }

        }
    }
}
