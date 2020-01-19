/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
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
        /// The Tcp connection
        /// </summary>
        protected TcpListener listener;

        /// <summary>
        /// Construct the server
        /// </summary>
        /// <param name="address">Address and port to bind for the server</param>
        public TcpServer(IPEndPoint address) : base(null)
        {
            EndPoint = address;
        }

        protected override void DoStart()
        {
            listener = new TcpListener(EndPoint);
            listener.ExclusiveAddressUse = false;
            listener.Start();

            Task t = Task.Factory.StartNew(ReceiveAction);
            serverTask.Add(t);
        }

        public override void Stop()
        {
            base.Stop();
            listener.Stop();
        }

        protected override void ReceiveAction()
        {
            try
            {
                var t = listener.AcceptTcpClientAsync(CancellationToken.Token);

                using (System.Net.Sockets.TcpClient client = t.GetAwaiter().GetResult())
                {
                    // Start new connection waiter before anything can go wrong here,
                    // leaving us without a server
                    serverTask.Add(Task.Factory.StartNew(ReceiveAction));

                    using (NetworkStream serverStream = client.GetStream())
                    {
                        serverStream.ReadTimeout = Server.ReadTimeOut;

                        Guid id = Guid.NewGuid();
                        while (ProcessMessage(serverStream, id))
                        { }
                        StatefulProxy.NotifyDisconnect(id);
                    }
                }

            }
            catch (OperationCanceledException) { }
        }
    }
}
