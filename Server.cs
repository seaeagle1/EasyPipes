/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Threading;
using System.Threading.Tasks;

namespace EasyPipes
{
    /// <summary>
    /// <see cref="NamedPipeClientStream"/> based IPC server
    /// </summary>
    public class Server
    {
        /// <summary>
        /// Name of the pipe
        /// </summary>
        public string PipeName { get; private set; }
        /// <summary>
        /// Token to cancel server operations
        /// </summary>
        public CancellationTokenSource CancellationToken { get; private set; }
        /// <summary>
        /// List of types registered with serializer 
        /// <seealso cref="System.Runtime.Serialization.DataContractSerializer.KnownTypes"/>
        /// </summary>
        public List<Type> KnownTypes { get; private set; }

        const int NumberOfThreads = 2;
        public const int ReadTimeOut = 30000;
        readonly Dictionary<string, object> services = new Dictionary<string, object>();
        readonly Dictionary<string, Type> types = new Dictionary<string, Type>();
        protected List<Task> serverTask;
        protected Func<string, int, NamedPipeServerStream> pipeOpeningFunc;

        /// <summary>
        /// Construct server
        /// </summary>
        /// <param name="pipe">Name of the pipe to use</param>
        /// <param name="pipeOpening">Optional delegate to open PipeStream (for example through NamedPipeServerStreamAcl)</param>
        public Server(string pipe, Func<string, int, NamedPipeServerStream> pipeOpening = null)
        {
            PipeName = pipe;
            CancellationToken = new CancellationTokenSource();
            KnownTypes = new List<Type>();

            if (pipeOpening == null)
                pipeOpeningFunc = OpenPipe;
            else
                pipeOpeningFunc = pipeOpening;
        }

        /// <summary>
        /// Register a service interface on the server
        /// This is a one instance for all clients service
        /// </summary>
        /// <typeparam name="T">The interface of the service</typeparam>
        /// <param name="instance">Instance of a class implementing the service</param>
        public void RegisterService<T>(T instance)
        {
            if(!typeof(T).IsInterface)
                throw new InvalidOperationException("Service Type is not an interface");

            if (!(instance is T))
                throw new InvalidOperationException("Instance must implement service interface");

            services[typeof(T).Name] = instance;
            types[typeof(T).Name] = typeof(T);

            IpcStream.ScanInterfaceForTypes(typeof(T), KnownTypes);
        }

        /// <summary>
        /// Register a service interface on the server that keeps its state for a connection
        /// This a one instance for one client/connection service
        /// </summary>
        /// <typeparam name="T">The interface of the service</typeparam>
        /// <param name="t">Type of the class implementing the service, should have a default constructor
        /// which will be called on connection of a new client</param>
        public void RegisterStatefulService<T>(Type t)
        {
            if (!typeof(T).IsInterface)
                throw new InvalidOperationException("Service Type is not an interface");
            
            // check if service implements interface
            if (t.GetInterface(typeof(T).Name) == null)
                throw new InvalidOperationException("Instance must implement service interface");

            // check for default constructor
            if (t.GetConstructor(Type.EmptyTypes) == null || t.IsAbstract)
                throw new InvalidOperationException("Stateful service requires default constructor");

            services[typeof(T).Name] = new StatefulProxy(t);
            types[typeof(T).Name] = typeof(T);

            IpcStream.ScanInterfaceForTypes(typeof(T), KnownTypes);
        }

        /// <summary>
        /// Deregister a service interface from the server
        /// </summary>
        /// <typeparam name="T">The interface of the service</typeparam>
        public void DeregisterService<T>()
        {
            // TODO deregister StatefulProxy

            services.Remove(typeof(T).Name);
            types.Remove(typeof(T).Name);
        }

        /// <summary>
        /// Start running the server tasks
        /// Note, this will spawn asynchronous servers and return immediatly
        /// </summary>
        public void Start()
        {
            if (serverTask != null)
                throw new InvalidOperationException("Already running");

            // asynchonously start the receiving threads, store task for later await
            serverTask = new List<Task>();
            DoStart();
        }

        /// <summary>
        /// Spawn server tasks
        /// </summary>
        protected virtual void DoStart()
        {
            // Start +1 receive actions
            for(short i=0; i<NumberOfThreads; ++i)
            {
                Task t = Task.Factory.StartNew(ReceiveAction);
                serverTask.Add(t);
            }
        }

        /// <summary>
        /// Stop the server
        /// </summary>
        public virtual void Stop()
        {
            // send cancel token and wait for threads to end
            CancellationToken.Cancel();
            Task.WaitAll(serverTask.ToArray(), 500);
            serverTask = null;
        }

        /// <summary>
        /// Default creation of PipeStream
        /// </summary>
        /// <param name="name">Name of the pipe</param>
        /// <param name="number">Max number of instances</param>
        /// <returns>Opened Pipestream</returns>
        protected virtual NamedPipeServerStream OpenPipe(string name, int number)
        {
            return new NamedPipeServerStream(
                        name,
                        PipeDirection.InOut,
                        number,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous);
        }

        /// <summary>
        /// Wait on connection and received messages
        /// </summary>
        protected virtual void ReceiveAction()
        {
            try
            {
                using (NamedPipeServerStream serverStream = pipeOpeningFunc(PipeName, NumberOfThreads))
                {
                    Task t = serverStream.WaitForConnectionAsync(CancellationToken.Token);
                    t.GetAwaiter().GetResult();

                    Guid id = Guid.NewGuid();
                    IpcStream stream = new IpcStream(serverStream, KnownTypes);

                    while (ProcessMessage(stream, id))
                    { }
                    StatefulProxy.NotifyDisconnect(id);
                }

                // Todo: make sure there's a new listener, even when this isn't reached
                serverTask.Add(Task.Factory.StartNew(ReceiveAction));
            }
            catch (OperationCanceledException) { }
        }

        /// <summary>
        /// Process a received message by calling the corresponding method on the service instance and 
        /// returning the return value over the network.
        /// </summary>
        /// <param name="stream">The message stream</param>
        /// <param name="streamId">A GUID identifying this connection</param>
        public bool ProcessMessage(IpcStream stream, Guid streamId)
        {
            IpcMessage msg = stream.ReadMessage();

            // this was a close-connection notification
            if (msg.StatusMsg == StatusMessage.CloseConnection)
                return false;
            // or this is just a keepalive ping
            else if (msg.StatusMsg == StatusMessage.Ping)
                return true;

            bool processedOk = false;
            string error = "";
            object rv = null;
            IpcMessage returnMsg = new IpcMessage();
            // find the service
            if (services.TryGetValue(msg.Service, out object instance) && instance != null)
            {
                // double check method existence against type-list for security
                // typelist will contain interfaces instead of instances
                if (types[msg.Service].GetMethod(msg.Method) != null )
                {
                    // separate handling for stateful service
                    if (instance is StatefulProxy)
                    {
                        try
                        {
                            // invoke method
                            System.Reflection.MethodInfo method = 
                                (instance as StatefulProxy).Type.GetMethod(msg.Method);
                            if (method == null)
                                throw new InvalidOperationException("Method not found in stateful proxy");

                            rv = (instance as StatefulProxy).Invoke(streamId, method, msg.Parameters);
                            processedOk = true;

                            // check if encryption is required
                            if(Attribute.IsDefined(method, typeof(EncryptIfTrueAttribute))
                                && (bool)rv == true)
                            {
                                returnMsg.StatusMsg = StatusMessage.Encrypt;
                            }
                        }
                        catch (Exception e) { error = e.ToString(); }
                    }
                    else
                    {
                        // get the method
                        System.Reflection.MethodInfo method = instance.GetType().GetMethod(msg.Method);
                        if (method != null)
                        {
                            try
                            {
                                // invoke method
                                rv = method.Invoke(instance, msg.Parameters);
                                processedOk = true;

                                // check if encryption is required
                                if (Attribute.IsDefined(method, typeof(EncryptIfTrueAttribute))
                                    && (bool)rv == true)
                                    returnMsg.StatusMsg = StatusMessage.Encrypt;
                            }
                            catch (Exception e) { error = e.ToString(); }
                        }
                        else
                            error = "Could not find method";
                    }
                }
                else
                    error = "Could not find method in type";
            }
            else
                error = "Could not find service";

            // return either return value or error message
            if (processedOk)
                returnMsg.Return = rv;
            else
            {
                returnMsg.Error = error;
                returnMsg.StatusMsg = StatusMessage.None;
            }

            stream.WriteMessage(returnMsg);

            // if there's more to come, keep reading a next message
            if (msg.StatusMsg == StatusMessage.KeepAlive)
                return true;
            else // otherwise close the connection
                return false;
        }
    }
}
