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
    public class TcpServer : Server
    {
        public IPEndPoint EndPoint { get; private set; }

        protected TcpListener listener;

        public TcpServer(IPEndPoint address) : base(null)
        {
            EndPoint = address;
        }

        protected override void DoStart()
        {
            listener = new TcpListener(EndPoint);
            listener.Start();

            base.DoStart();
        }

        public override void Stop()
        {
            base.Stop();
            listener.Stop();
        }

        protected override async void ReceiveAction()
        {
            try
            {
                if (CancellationToken.IsCancellationRequested)
                    return;

                var t = listener.AcceptTcpClientAsync(CancellationToken.Token);
                t.Wait();

                using (System.Net.Sockets.TcpClient client = t.Result )
                {
                    if (CancellationToken.IsCancellationRequested)
                        return;

                    using (NetworkStream serverStream = client.GetStream())
                    {
                        ProcessMessage(serverStream);
                    }
                }
            }
            catch (OperationCanceledException) { }
        }
    }
}
