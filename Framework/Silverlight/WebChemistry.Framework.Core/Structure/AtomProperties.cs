namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Atom properties
    /// </summary>
    public static class AtomPropertiesEx
    {
        /// <summary>
        /// Default extension (with dot).
        /// </summary>
        public static readonly string DefaultExtension = ".wprop";

        const string PropCategory = "AtomProperties";

        /// <summary>
        /// Get a descriptor for a property with a given name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        static PropertyDescriptor<AtomPropertiesBase> GetAtomPropertyDescriptor(string name)
        {
            return PropertyHelper.OfType<AtomPropertiesBase>("AP_" + name.ToLowerInvariant(), category: PropCategory);
        }
        
        /// <summary>
        /// Read properties.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static AtomPropertiesBase Read(TextReader reader)
        {
            return AtomPropertiesBase.Read(reader);
        }

        /// <summary>
        /// Write the properties to a string.
        /// </summary>
        /// <param name="props"></param>
        /// <returns></returns>
        public static string WriteToString(this AtomPropertiesBase props)
        {
            using (var w = new StringWriter())
            {
                props.Write(w);
                w.Flush();
                return w.ToString();
            }
        }

        /// <summary>
        /// Get properties.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AtomPropertiesBase GetAtomProperties(this IStructure s, string name)
        {
            var ret = s.GetProperty(GetAtomPropertyDescriptor(name), null);
            if (ret == null) throw new ArgumentException(string.Format("There is no property called {0}.", name));
            return ret;
        }

        /// <summary>
        /// Try get atom properties.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public static AtomPropertiesBase TryGetAtomProperties(this IStructure s, string name)
        {
            var ret = s.GetProperty(GetAtomPropertyDescriptor(name), null);
            return ret;
        }

        /// <summary>
        /// Attach properties.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="props"></param>
        public static void AttachAtomProperties(this IStructure s, AtomPropertiesBase props)
        {
            if (!s.Id.EqualOrdinalIgnoreCase(props.ParentId))
            {
                throw new ArgumentException(string.Format("Cannot attach properties {0}: The structure IDs do not match (got {1}, expected {2}).", props.Name, props.ParentId, s.Id));
            }

            var desc = GetAtomPropertyDescriptor(props.Name);
            var ret = s.GetProperty(desc, null);
            if (ret != null)
            {
                throw new ArgumentException(string.Format("Cannot attach properties {0} to {1}: The structure already contains properties with this name.", props.Name, s.Id));
            }

            s.SetProperty(desc, props);
            props.Parent = s;
        }

        /// <summary>
        /// Steal properties from a different structure.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="from"></param>
        public static void StealAtomProperties(this IStructure s, IStructure from)
        {
            from.Properties
                .Where(p => p.Descriptor.Category.Equals(PropCategory, StringComparison.Ordinal))
                .Select(p => p.Value as AtomPropertiesBase)
                .ForEach(p =>
                {
                    s.RemoveAtomProperties(p.Name);
                    s.AttachAtomProperties(p.Steal(s));
                });
        }
        
        /// <summary>
        /// Remove properties.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="name"></param>
        public static void RemoveAtomProperties(this IStructure s, string name)
        {
            var desc = GetAtomPropertyDescriptor(name);
            s.RemoveProperty(desc);
        }
    }

    /// <summary>
    /// Atom properties base.
    /// </summary>
    public abstract class AtomPropertiesBase
    {
        internal IStructure Parent { get; set; }

        /// <summary>
        /// Id of the parent.
        /// </summary>
        public string ParentId { get; private set; }

        /// <summary>
        /// Name of the property.
        /// </summary>
        public string Name { get; private set; }

        /// <summary>
        /// Property tag.
        /// </summary>
        public string Tag { get; private set; }
        
        /// <summary>
        /// Get real value.
        /// </summary>
        /// <param name="atom"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public virtual double GetRealValue(IAtom atom, double defaultValue = 0.0)
        {
            throw new NotSupportedException();
        }
        
        /// <summary>
        /// Try to get the value.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public abstract object TryGetValue(IAtom atom);

        /// <summary>
        /// Write the properties.
        /// </summary>
        /// <param name="w"></param>
        public void Write(TextWriter w)
        {
            if (Parent == null)
            {
                throw new InvalidOperationException("Cannot write parentless properties.");
            }

            w.WriteLine(ParentId);
            w.WriteLine(Name);
            w.WriteLine(Tag);
            w.WriteLine(this.GetType().Name);
            WriteValues(w);
        }

        /// <summary>
        /// read the properties.
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static AtomPropertiesBase Read(TextReader reader)
        {
            var parent = reader.ReadLine();
            var name = reader.ReadLine();
            var tag = reader.ReadLine();
            var type = reader.ReadLine();

            if (type.EqualOrdinalIgnoreCase(typeof(RealAtomProperties).Name))
            {
                var ret = new RealAtomProperties(parent, name, tag);
                ret.ReadValues(reader);
                return ret;
            }

            throw new NotSupportedException("The property format is not supported.");
        }

        /// <summary>
        /// Write the values.
        /// </summary>
        /// <param name="w"></param>
        protected abstract void WriteValues(TextWriter w);

        /// <summary>
        /// Read the values.
        /// </summary>
        /// <param name="r"></param>
        protected abstract void ReadValues(TextReader r);


        /// <summary>
        /// Steal the properties.
        /// </summary>
        /// <param name="culprit"></param>
        /// <returns></returns>
        public abstract AtomPropertiesBase Steal(IStructure culprit);

        /// <summary>
        /// Renamed.
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        public abstract AtomPropertiesBase Renamed(string newName);

        /// <summary>
        /// create the class.
        /// </summary>
        /// <param name="parentId"></param>
        /// <param name="name"></param>
        protected AtomPropertiesBase(string parentId, string name, string tag)
        {
            this.ParentId = parentId;
            this.Name = name;
            this.Tag = tag ?? "";
        }
    }

    /// <summary>
    /// Real (double properties).
    /// </summary>
    public class RealAtomProperties : AtomPropertiesBase
    {
        /// <summary>
        /// The actual values.
        /// </summary>
        private Dictionary<int, double> Values { get; set; }

        /// <summary>
        /// Get the real value.
        /// </summary>
        /// <param name="atom"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public override double GetRealValue(IAtom atom, double defaultValue = 0.0)
        {
            double value;
            if (Values.TryGetValue(atom.Id, out value)) return value;
            return defaultValue;
        }
        
        /// <summary>
        /// Try to get the value.
        /// </summary>
        /// <param name="atom"></param>
        /// <returns></returns>
        public override object TryGetValue(IAtom atom)
        {
            double value;
            if (Values.TryGetValue(atom.Id, out value)) return value;
            return null;
        }

        /// <summary>
        /// Steal it..
        /// </summary>
        /// <param name="culprit"></param>
        /// <returns></returns>
        public override AtomPropertiesBase Steal(IStructure culprit)
        {
            return new RealAtomProperties(culprit.Id, Name, Tag) { Values = Values };
        }

        /// <summary>
        /// Renamed.
        /// </summary>
        /// <param name="newName"></param>
        /// <returns></returns>
        public override AtomPropertiesBase Renamed(string newName)
        {
            return new RealAtomProperties(ParentId, newName, Tag) { Values = Values };
        }

        /// <summary>
        /// Write values
        /// </summary>
        /// <param name="w"></param>
        protected override void WriteValues(TextWriter w)
        {
            int count = Parent.Atoms.Count(a => Values.ContainsKey(a.Id));
            
            w.WriteLine(count);
            foreach (var a in Parent.Atoms)
            {
                double v;
                if (Values.TryGetValue(a.Id, out v))
                {
                    w.WriteLine(a.Id + " " + v.ToStringInvariant());
                }
            }
        }

        /// <summary>
        /// Read the values.
        /// </summary>
        /// <param name="r"></param>
        protected override void ReadValues(TextReader r)
        {
            var count = int.Parse(r.ReadLine());
            Values = new Dictionary<int, double>(count);

            for (int i = 0; i < count; i++)
            {
                var line = r.ReadLine();
                var split = line.IndexOf(' ');
                Values.Add(NumberParser.ParseIntFast(line, 0, split), NumberParser.ParseDoubleFast(line, split + 1, line.Length - split - 1));
            }
        }

        /// <summary>
        /// Create the property set.
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="name"></param>
        /// <param name="tag"></param>
        /// <param name="values"></param>
        /// <returns></returns>
        public static RealAtomProperties Create(IStructure parent, string name, string tag, Func<IAtom, double?> values)
        {
            var ret = new RealAtomProperties(parent.Id, name, tag);
            ret.Values = new Dictionary<int, double>(parent.Atoms.Count);
            foreach (var a in parent.Atoms)
            {
                var v = values(a);
                if (v.HasValue) ret.Values.Add(a.Id, v.Value);
            }
            return ret;
        }

        internal RealAtomProperties(string parentId, string name, string tag)
            : base(parentId, name, tag)
        {

        }
    }
}