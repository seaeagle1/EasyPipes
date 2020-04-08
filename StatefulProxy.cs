/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPipes
{
    /// <summary>
    /// Server-side Proxy class for statefull services. Calls to the ipc-service get routed through here
    /// and StatefulProxy will invoke the requested method on the appropriate service instance for 
    /// the active connection.
    /// </summary>
    class StatefulProxy
    {
        /// <summary>
        /// The Type that is being proxied
        /// </summary>
        public Type Type { get; private set; }
        /// <summary>
        /// List of instances that is being managed
        /// </summary>
        private Dictionary<Guid, object> instances = new Dictionary<Guid, object>();

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="t">Type to proxy</param>
        public StatefulProxy(Type t)
        {
            Type = t;
            proxyList.Add(this);
        }

        /// <summary>
        /// Invoke a specified method on either the existing or new instance of <see cref="Type"/> that 
        /// is linked to the connection identified by id.
        /// </summary>
        /// <param name="id">Connection identifier</param>
        /// <param name="method">Method to invoke</param>
        /// <param name="parameters">Method parameters</param>
        /// <returns>Method return value</returns>
        public object Invoke(Guid id, System.Reflection.MethodInfo method, object[] parameters)
        {
            object instance;

            // find matching instance
            if (!instances.TryGetValue(id, out instance))
            {
                // if instance doesn't exist, create it
                instance = Activator.CreateInstance(Type);
                instances[id] = instance;
            }

            // invoke method
            return method.Invoke(instance, parameters);
        }

        /// <summary>
        /// Static list of all StatefulProxies
        /// </summary>
        private static List<StatefulProxy> proxyList = new List<StatefulProxy>();

        /// <summary>
        /// Notification of a disconnect, searches through all StatefulProxies to release the service
        /// instances that were linked to this connection.
        /// </summary>
        /// <param name="id">Connection identifier</param>
        public static void NotifyDisconnect(Guid id)
        {
            foreach(StatefulProxy proxy in proxyList)
                proxy.instances.Remove(id);
        }
    }
}
