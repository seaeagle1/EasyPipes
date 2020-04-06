/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPipes
{
    /// <summary>
    /// This class represents a message across the network
    /// </summary>
    public class IpcMessage
    {
        /// <summary>
        /// Service name
        /// </summary>
        public string Service;
        /// <summary>
        /// Method name
        /// </summary>
        public string Method;
        /// <summary>
        /// Method parameters
        /// </summary>
        public object[] Parameters;

        /// <summary>
        /// Method return value
        /// </summary>
        public object Return;


        /// <summary>
        /// Error produced during remote processing
        /// </summary>
        public string Error;
        /// <summary>
        /// Send a status message
        /// </summary>
        public StatusMessage StatusMsg;
    }


    /// <summary>
    /// Enumerator of status messages used in <see cref="IpcMessage"/>
    /// </summary>
    public enum StatusMessage
    {
        None = 0,
        KeepAlive,
        CloseConnection,
        Ping,
        Encrypt
    }
}
