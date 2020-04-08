/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyPipes
{
    /// <summary>
    /// A <see cref="TcpListener"/> based IPC server
    /// </summary>
    public class TcpServer : Server
    {
        /// <summary>
        /// IP and address bound by the server
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        /// <summary>
        /// Encryption algorithm
        /// </summary>
        protected Encryptor Encryptor { get; private set; }

        /// <summary>
        /// The Tcp connection
        /// </summary>
        protected TcpListener listener;

        /// <summary>
        /// Construct the server
        /// </summary>
        /// <param name="address">Address and port to bind for the server</param>
        /// <param name="encryptor">Optional encryption algorithm for the messages, will be enabled
        /// after call to an <see cref="EncryptIfTrueAttribute"/> labeled method</param>  
        public TcpServer(IPEndPoint address, Encryptor encryptor = null) : base(null)
        {
            EndPoint = address;
            Encryptor = encryptor;
        }

        /// <summary>
        /// Start listening on the TCP socket
        /// </summary>
        protected override void DoStart()
        {
            listener = new TcpListener(EndPoint);
            listener.ExclusiveAddressUse = false;
            listener.Start();

            Task t = Task.Factory.StartNew(ReceiveAction);
            serverTask.Add(t);
        }

        /// <summary>
        /// Stop listening on the TCP socket
        /// </summary>
        public override void Stop()
        {
            base.Stop();
            listener.Stop();
        }

        /// <summary>
        /// Main connection loop, waits for and handles connections
        /// </summary>
        protected override void ReceiveAction()
        {
            try
            {
                var t = listener.AcceptTcpClientAsync(CancellationToken.Token);

                // wait for connection
                using (System.Net.Sockets.TcpClient client = t.GetAwaiter().GetResult())
                {
                    // Start new connection waiter before anything can go wrong here,
                    // leaving us without a server
                    serverTask.Add(Task.Factory.StartNew(ReceiveAction));

                    using (NetworkStream networkStream = client.GetStream())
                    {
                        networkStream.ReadTimeout = Server.ReadTimeOut;
                        Stream serverStream = networkStream;

                        Guid id = Guid.NewGuid();
                        IpcStream stream = new IpcStream(serverStream, KnownTypes, Encryptor);

                        // process incoming messages until disconnect
                        while (ProcessMessage(stream, id))
                        { }
                        StatefulProxy.NotifyDisconnect(id);

                        serverStream.Close();
                    }
                }

            }
            catch (OperationCanceledException) { }
        }
    }
}
