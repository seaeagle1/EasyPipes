/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace EasyPipes
{
    /// <summary>
    /// A <see cref="System.Net.Sockets.TcpClient"/> based IPC client
    /// </summary>
    public class TcpClient : Client
    {
        /// <summary>
        /// Ip and port to connect to
        /// </summary>
        public IPEndPoint EndPoint { get; private set; }

        protected Encryptor Encryptor { get; private set; }

        /// <summary>
        /// Tcp connection
        /// </summary>
        protected System.Net.Sockets.TcpClient connection;

        /// <summary>
        /// Construct the client
        /// </summary>
        /// <param name="address">Address and port to connect to</param>
        /// <param name="encyptionkey">Optional key for AES encryption of the stream</param>
        /// <param name="iv">Initialization vector for AES encryption</param>        
        public TcpClient(IPEndPoint address, Encryptor encryptor = null) : base(null)
        {
            EndPoint = address;
            Encryptor = encryptor;
        }

        public async Task<bool> ConnectAsync(bool keepalive = true)
        {
            try
            {
                // connect socket
                connection = new System.Net.Sockets.TcpClient();
                connection.NoDelay = true;
                connection.ReceiveTimeout = 2000;
                await connection.ConnectAsync(EndPoint.Address, EndPoint.Port).ConfigureAwait(false);
                System.IO.Stream connectionStream = connection.GetStream();

                Stream = new IpcStream(connectionStream, KnownTypes, Encryptor);

            }
            catch (SocketException e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return false;
            }

            if (keepalive)
                StartPing();

            return true;
        }

        public override bool Connect(bool keepalive = true)
        {
            try
            {
                // connect socket
                connection = new System.Net.Sockets.TcpClient();
                connection.ReceiveTimeout = 2000;
                connection.Connect(EndPoint);
                System.IO.Stream connectionStream = connection.GetStream();

                Stream = new IpcStream(connectionStream, KnownTypes, Encryptor);

            } catch(SocketException e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return false;
            }

            if(keepalive)
                StartPing();

            return true;
        }

        public override void Disconnect(bool sendCloseMessage = true)
        {
            base.Disconnect(sendCloseMessage);

            if (connection != null)
                connection.Close();
            connection = null;
        }
    }
}
