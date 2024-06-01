namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Atom properties
    /// </summary>
    public static class StructurePropertiesEx
    {        
        const string DescCategory = "StructureDescriptors";
        
        static PropertyDescriptor<StructureDescriptors> DescriptorsProperty = PropertyHelper.OfType<StructureDescriptors>("Descriptors", category: DescCategory, autoClone: false);
        
        /// <summary>
        /// Get properties.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static StructureDescriptors Descriptors(this IStructure s)
        {
            var ret = s.GetProperty(DescriptorsProperty, null);
            if (ret == null)
            {
                ret = new StructureDescriptors();
                s.SetProperty(DescriptorsProperty, ret);
            }
            return ret;
        }
    }

    /// <summary>
    /// Atom properties base.
    /// </summary>
    public class StructureDescriptors
    {
        Dictionary<string, object> values = new Dictionary<string,object>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Indexor.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public object this[string name]
        {
            get
            {
                return TryGetDescriptor(name);
            }

            set
            {
                values[name] = value;
            }
        }
    
        
        /// <summary>
        /// Try to get the descriptor.
        /// Null if the property does not exist.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public object TryGetDescriptor(string name)
        {
            object ret = null;
            values.TryGetValue(name, out ret);
            return ret;            
        }

        /// <summary>
        /// Try get the descriptor ....
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public Maybe<T> TryGetDescriptor<T>(string name)
        {
            object ret = null;
            if (values.TryGetValue(name, out ret)) return Maybe.Just<T>((T)ret);
            return Maybe.Nothing<T>();
        }

        /// <summary>
        /// Remove...
        /// </summary>
        /// <param name="name"></param>
        public void RemoveDescriptor(string name)
        {
            values.Remove(name);
        }

        /// <summary>
        /// create the class.
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="name"></param>
        internal StructureDescriptors()
        {
        }
    }
}