using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Windows.Kits.Hardware.ObjectModel;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Intel.WHQLCert
{
    public class WHCKHelper
    {
        public static object getPrivateFieldValue(string Name, object o)
        {
            Type t;
            t = o.GetType();
            FieldInfo[] fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (FieldInfo fi in fields)
            {
                if(fi.Name==Name)
                    return fi.GetValue(o);
            }
            return null;
        }

        public static object getPrivateProperiesValue(string Name, object o)
        {
            Type t;
            t = o.GetType();
            PropertyInfo[] fields = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            foreach (PropertyInfo fi in fields)
            {
                if (fi.Name == Name)
                    return fi.GetValue(o, null);
            }
            return null;
        }
    }
    public class whckAsyncResult : IAsyncResult
    {
        #region IAsyncResult Members

        public object AsyncState
        {
            get { throw new NotImplementedException(); }
        }

        public System.Threading.WaitHandle AsyncWaitHandle
        {
            get { throw new NotImplementedException(); }
        }

        public bool CompletedSynchronously
        {
            get { throw new NotImplementedException(); }
        }

        public bool IsCompleted
        {
            get { throw new NotImplementedException(); }
        }

        #endregion
    }
}
