/* This Source Code Form is subject to the terms of the Mozilla Public
 * License, v. 2.0. If a copy of the MPL was not distributed with this
 * file, You can obtain one at http://mozilla.org/MPL/2.0/. */
 
using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPipes
{
    class StatefulProxy
    {
        public Type Type { get; private set; }
        private Dictionary<Guid, object> instances = new Dictionary<Guid, object>();

        public StatefulProxy(Type t)
        {
            Type = t;
            proxyList.Add(this);
        }

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

        private static List<StatefulProxy> proxyList = new List<StatefulProxy>();

        public static void NotifyDisconnect(Guid id)
        {
            foreach(StatefulProxy proxy in proxyList)
                proxy.instances.Remove(id);
        }
    }
}
