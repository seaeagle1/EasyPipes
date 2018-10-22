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
    public class Server
    {

        public string PipeName { get; private set; }
        public CancellationTokenSource CancellationToken { get; private set; }
        public List<Type> KnownTypes { get; private set; }

        const int NumberOfThreads = 2;
        readonly Dictionary<string, object> services = new Dictionary<string, object>();
        readonly Dictionary<string, Type> types = new Dictionary<string, Type>();
        private List<Task> serverTask;

        public Server(string pipe)
        {
            PipeName = pipe;
            CancellationToken = new CancellationTokenSource();
            KnownTypes = new List<Type>();
        }

        public void RegisterService<T>(T instance)
        {
            services[typeof(T).Name] = instance;
            types[typeof(T).Name] = typeof(T);

            IpcStream.ScanInterfaceForTypes(typeof(T), KnownTypes);
        }

        public void DeregisterService<T>()
        {
            services.Remove(typeof(T).Name);
            types.Remove(typeof(T).Name);
        }

        public void Start()
        {
            if (serverTask != null)
                throw new InvalidOperationException("Already running");

            // asynchonously start the receiving threads, store task for later await
            serverTask = new List<Task>();
            DoStart();
        }

        protected virtual void DoStart()
        {
            // Start +1 receive actions
            for(short i=0; i<NumberOfThreads; ++i)
            {
                Task t = Task.Factory.StartNew(ReceiveAction);
                serverTask.Add(t);
            }
        }

        public virtual void Stop()
        {
            // send cancel token and wait for threads to end
            CancellationToken.Cancel();
            Task.WaitAll(serverTask.ToArray(), 500);
            serverTask = null;
        }

        protected virtual void ReceiveAction()
        {
            try
            {
                using (NamedPipeServerStream serverStream =
                    new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.InOut,
                        NumberOfThreads,
                        PipeTransmissionMode.Byte,
                        PipeOptions.Asynchronous))
                {
                    Task t = serverStream.WaitForConnectionAsync(CancellationToken.Token);
                    t.GetAwaiter().GetResult();

                    ProcessMessage(serverStream); 
                }

                serverTask.Add(Task.Factory.StartNew(ReceiveAction));
            }
            catch (OperationCanceledException) { }
        }

        public void ProcessMessage(Stream source)
        {
            IpcStream stream = new IpcStream(source, KnownTypes);

            IpcMessage msg = stream.ReadMessage();

            bool processedOk = false;
            string error = "";
            object rv = null;
            // find the service
            if (services.TryGetValue(msg.Service, out object instance) && instance != null)
            {
                // get the method
                System.Reflection.MethodInfo method = instance.GetType().GetMethod(msg.Method);

                // double check method existence against type-list for security
                // typelist will contain interfaces instead of instances
                if (types[msg.Service].GetMethod(msg.Method) != null && method != null)
                {
                    try
                    {
                        // invoke method
                        rv = method.Invoke(instance, msg.Parameters);
                        processedOk = true;
                    }
                    catch (Exception e) { error = e.ToString(); }

                }
                else
                    error = "Could not find method";
            }
            else
                error = "Could not find service";

            // return either return value or error message
            IpcMessage returnMsg;
            if (processedOk)
                returnMsg = new IpcMessage() { Return = rv };
            else
                returnMsg = new IpcMessage() { Error = error };

            stream.WriteMessage(returnMsg);
        }
    }
}
