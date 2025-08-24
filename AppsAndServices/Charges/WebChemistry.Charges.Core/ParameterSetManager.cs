namespace WebChemistry.Charges.Core
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;
    using System.Xml.Linq;

    /// <summary>
    /// A manager class for the parameter sets.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ParameterSetManager
    {
        Dictionary<string, EemParameterSet> SetMap;

        public ObservableCollection<EemParameterSet> Sets { get; private set; }

        /// <summary>
        /// Get a set by name.
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public EemParameterSet GetByName(string name)
        {
            EemParameterSet set;
            if (SetMap.TryGetValue(name, out set)) return set;
            return null;
            //throw new ArgumentException(string.Format("Parameter set named '{0}' was not found.", name));
        }

        /// <summary>
        /// Update the sets and return their names.
        /// </summary>
        /// <param name="xml"></param>
        /// <returns></returns>
        public IEnumerable<string> Update(XElement xml)
        {
            if (xml.Name.LocalName.Equals("ParameterSet", StringComparison.OrdinalIgnoreCase))
            {
                var set = EemParameterSet.FromXml(xml);
                Update(set);
                return new string[] { set.Name };
            }
            else if (xml.Name.LocalName.Equals("Sets", StringComparison.OrdinalIgnoreCase))
            {
                var sets = xml.Elements().Select(e => EemParameterSet.FromXml(e)).ToArray();
                sets.ForEach(s => Update(s));
                return sets.Select(s => s.Name).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            }
            return new string[0];
        }

        public void Update(EemParameterSet set)
        {
            var index = Sets.IndexOf(set);
            if (index >= 0)
            {
                bool selected = Sets[index].IsSelected;
                set.IsSelected = selected;
                Sets[index] = set;
            }
            else
            {
                Sets.Add(set);
                SetMap.Add(set.Name, set);
            }
        }

        public void Remove(EemParameterSet set)
        {
            Sets.Remove(set);
        }

        public ParameterSetManager()
        {
            Sets = new ObservableCollection<EemParameterSet>();
            SetMap = new Dictionary<string, EemParameterSet>(StringComparer.OrdinalIgnoreCase);
        }
    }
}
