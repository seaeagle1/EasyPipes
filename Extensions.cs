/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace EasyPipes
{
    static class Extensions
    {
        public static async Task<System.Net.Sockets.TcpClient> AcceptTcpClientAsync(this TcpListener listener, CancellationToken ct)
        {
            using (ct.Register(listener.Stop))
            {
                try
                {
                    return await listener.AcceptTcpClientAsync();
                }
                catch (SocketException e) when (e.SocketErrorCode == SocketError.Interrupted)
                {
                    throw new OperationCanceledException();
                }
                catch (ObjectDisposedException) when (ct.IsCancellationRequested)
                {
                    throw new OperationCanceledException();
                }
            }
        }
    }
}
