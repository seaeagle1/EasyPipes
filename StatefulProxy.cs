using System;
using System.Collections.Generic;
using System.Text;

namespace EasyPipes
{
    class StatefulProxy
    {
        private Type type;
        private Dictionary<Guid, object> instances = new Dictionary<Guid, object>();

        public StatefulProxy(Type t)
        {
            type = t;
            proxyList.Add(this);
        }

        public object Invoke(Guid id, string methodName, object[] parameters)
        {
            object instance;

            // find matching instance
            if (!instances.TryGetValue(id, out instance))
            {
                // if instance doesn't exist, create it
                instance = Activator.CreateInstance(type);
                instances[id] = instance;
            }

            // get the method
            System.Reflection.MethodInfo method = instance.GetType().GetMethod(methodName);
            if (method == null)
                throw new InvalidOperationException("Method not found in stateful proxy");

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
