/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using Castle.DynamicProxy;

namespace EasyPipes
{
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

        public string PipeName { get; private set; }
        protected IpcStream Stream { get; set; }

        public Client(string pipeName)
        {
            PipeName = pipeName;
        }

        public T GetServiceProxy<T>()
        {
            return (T)new ProxyGenerator().CreateInterfaceProxyWithoutTarget(typeof(T), new Proxy<T>(this));
        }

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

            Stream = new IpcStream(source);
            return true;
        }

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

        protected virtual void Disconnect()
        {
            if(Stream != null)
                Stream.Dispose();
            Stream = null;
        }
    }
}
