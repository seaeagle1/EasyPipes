/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using System.Xml;

namespace EasyPipes
{
    /// <summary>
    /// Class implementing the communication protocol
    /// </summary>
    public class IpcStream : IDisposable
    {
        /// <summary>
        /// Underlying network stream
        /// </summary>
        public Stream BaseStream { get; private set; }

        /// <summary>
        /// Types registered with the serializer
        /// </summary>
        public IReadOnlyList<Type> KnownTypes { get; private set; }

        private bool _disposed = false;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="s">Network stream</param>
        /// <param name="knowntypes">List of types to register with serializer</param>
        public IpcStream(Stream s, IReadOnlyList<Type> knowntypes)
        {
            BaseStream = s;
            KnownTypes = knowntypes;
        }

        ~IpcStream()
        {
            if (!_disposed)
                Dispose();
        }

        /// <summary>
        /// Read the raw network message
        /// </summary>
        /// <returns>byte buffer</returns>
        protected byte[] ReadBytes()
        {
            byte[] buffer = new byte[2];
            if (BaseStream.Read(buffer, 0, 2) != 2)
                throw new EndOfStreamException("Insufficient bytes read from network stream");

            int length = buffer[0] * 256;
            length += buffer[1];

            buffer = new byte[length];
            int read = 0;
            while(read < length)
                read += BaseStream.Read(buffer, read, length-read);

            return buffer;
        }

        /// <summary>
        /// Write a raw network message
        /// </summary>
        /// <param name="buffer">byte buffer</param>
        protected void WriteBytes(byte[] buffer)
        {
            int length = buffer.Length;
            if (length > UInt16.MaxValue)
                throw new InvalidOperationException("Message is too long");

            BaseStream.Write(new byte[] { (byte)(length / 256), (byte)(length & 255) }, 0, 2);
            BaseStream.Write(buffer, 0, length);
            BaseStream.Flush();
        }

        /// <summary>
        /// Read the next <see cref="IpcMessage"/> from the network
        /// </summary>
        /// <returns>The received message</returns>
        public IpcMessage ReadMessage()
        {
            // read the raw message
            byte[] msg = this.ReadBytes();

            // deserialize
            DataContractSerializer serializer = new DataContractSerializer(typeof(IpcMessage), KnownTypes);
            XmlDictionaryReader rdr = XmlDictionaryReader
                .CreateBinaryReader(msg, XmlDictionaryReaderQuotas.Max);

            return (IpcMessage)serializer.ReadObject(rdr);
        }

        /// <summary>
        /// Write a <see cref="IpcMessage"/> to the network
        /// </summary>
        /// <param name="msg">Message to write</param>
        public void WriteMessage(IpcMessage msg)
        {
            // serialize
            DataContractSerializer serializer = new DataContractSerializer(typeof(IpcMessage), KnownTypes);
            using (MemoryStream stream = new MemoryStream())
            {
                XmlDictionaryWriter writer = XmlDictionaryWriter.CreateBinaryWriter(stream);

                serializer.WriteObject(writer, msg);
                writer.Flush();

                // write the raw message
                this.WriteBytes(stream.ToArray());
            }
        }

        public void Dispose()
        {
            BaseStream.Close();
            _disposed = true;
        }

        /// <summary>
        /// Scan an interface for parameter and return <see cref="Type"/>s
        /// </summary>
        /// <param name="T">The interface type</param>
        /// <param name="knownTypes">List to add found types to</param>
        public static void ScanInterfaceForTypes(Type T, IList<Type> knownTypes)
        {
            // scan used types
            foreach (MethodInfo mi in T.GetMethods())
            {
                Type t;
                foreach (ParameterInfo pi in mi.GetParameters())
                {
                    t = pi.ParameterType;

                    if (!t.IsClass && !t.IsInterface)
                        continue;

                    if (!knownTypes.Contains(t))
                        knownTypes.Add(t);
                }

                t = mi.ReturnType;
                if (!t.IsClass && !t.IsInterface)
                    continue;

                if (!knownTypes.Contains(t))
                    knownTypes.Add(t);
            }
        }
    }
}
