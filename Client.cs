/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO.Pipes;
using Castle.DynamicProxy;

namespace EasyPipes
{
    /// <summary>
    /// <see cref="NamedPipeClientStream"/> based IPC client
    /// </summary>
    public class Client
    {
        class Proxy<T> : IInterceptor
        {
            public Client Client { get; private set; }

            public Proxy(Client c)
            {
                Client = c;
            }

            public void Intercept(IInvocation invocation)
            {
                // build message for intercepted call
                IpcMessage msg = new IpcMessage()
                {
                    Service = typeof(T).Name,
                    Method = invocation.Method.Name,
                    Parameters = invocation.Arguments
                };

                // send message
                invocation.ReturnValue = Client.SendMessage(msg);
            }
        }

        /// <summary>
        /// Name of the pipe
        /// </summary>
        public string PipeName { get; private set; }
        /// <summary>
        /// Pipe data stream
        /// </summary>
        protected IpcStream Stream { get; set; }
        /// <summary>
        /// List of types registered with serializer 
        /// <seealso cref="System.Runtime.Serialization.DataContractSerializer.KnownTypes"/>
        /// </summary>
        public List<Type> KnownTypes { get; private set; }

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="pipeName">Name of the pipe</param>
        public Client(string pipeName)
        {
            PipeName = pipeName;
            KnownTypes = new List<Type>();
        }

        /// <summary>
        /// Scans the service interface and builds proxy class
        /// </summary>
        /// <typeparam name="T">Service interface, must equal server-side</typeparam>
        /// <returns>Proxy class for remote calls</returns>
        public T GetServiceProxy<T>()
        {
            IpcStream.ScanInterfaceForTypes(typeof(T), KnownTypes);

            return (T)new ProxyGenerator().CreateInterfaceProxyWithoutTarget(typeof(T), new Proxy<T>(this));
        }

        /// <summary>
        /// Connect to server
        /// </summary>
        /// <returns>True if succeeded, false if not</returns>
        protected virtual bool Connect()
        {
            NamedPipeClientStream source = new NamedPipeClientStream(
                ".",
                PipeName,
                PipeDirection.InOut,
                PipeOptions.Asynchronous);

            try
            {
                source.Connect(500);
            } catch(TimeoutException)
            {
                return false;
            }

            Stream = new IpcStream(source, KnownTypes);
            return true;
        }

        /// <summary>
        /// Send the provided <see cref="IpcMessage"/> over the datastream
        /// Opens and closes a connection if not open yet
        /// </summary>
        /// <param name="message">The message to send</param>
        /// <returns>Return value from the Remote call</returns>
        protected object SendMessage(IpcMessage message)
        {
            bool closeStream = false;
            if (Stream == null)
            {
                if (!Connect())
                    throw new TimeoutException("Unable to connect");
                closeStream = true;
            }

            IpcMessage rv;
            lock (Stream)
            {
                Stream.WriteMessage(message);

                rv = Stream.ReadMessage();
            }

            if (closeStream)
                Disconnect();

            if (rv.Error != null)
                throw new InvalidOperationException(rv.Error);

            return rv.Return;
        }

        /// <summary>
        /// Disconnect from the server
        /// </summary>
        protected virtual void Disconnect()
        {
            if(Stream != null)
                Stream.Dispose();
            Stream = null;
        }
    }
}
