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
        /// <summary>
        /// Async awaiter for <see cref="TcpClient"/> connection, with cancellation support
        /// </summary>
        /// <param name="listener"></param>
        /// <param name="ct">Cancellation token to respond to</param>
        /// <returns>The connected client</returns>
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

        /// <summary>
        /// Blocks until condition is true or timeout occurs.
        /// </summary>
        /// <param name="condition">The break condition.</param>
        /// <param name="frequency">The frequency at which the condition will be checked.</param>
        /// <param name="timeout">The timeout in milliseconds.</param>
        /// <returns></returns>
        public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
        {
            var waitTask = Task.Run(async () =>
            {
                while (!condition()) await Task.Delay(frequency).ConfigureAwait(false);
            });

            if (waitTask != await Task.WhenAny(waitTask,
                    Task.Delay(timeout)).ConfigureAwait(false))
                throw new TimeoutException();
        }
    }
}
