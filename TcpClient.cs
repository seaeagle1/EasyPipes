/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace EasyPipes
{
    public class TcpClient : Client
    {
        public IPEndPoint EndPoint { get; private set; }

        protected System.Net.Sockets.TcpClient connection;

        public TcpClient(IPEndPoint address) : base(null)
        {
            EndPoint = address;
        }

        protected override bool Connect()
        {
            try
            {
                connection = new System.Net.Sockets.TcpClient();
                connection.Connect(EndPoint);
                Stream = new IpcStream(connection.GetStream(), KnownTypes);
            } catch(SocketException e)
            {
                System.Diagnostics.Debug.WriteLine(e);
                return false;
            }

            return true;
        }

        protected override void Disconnect()
        {
            base.Disconnect();

            if (connection != null)
                connection.Close();
            connection = null;
        }
    }
}
