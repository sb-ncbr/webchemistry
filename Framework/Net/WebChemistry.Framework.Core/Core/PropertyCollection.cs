namespace WebChemistry.Framework.Core
{
    using System.Collections.Generic;
    using System.Linq;

    public class PropertyCollection : IEnumerable<Property>
    {
        internal static readonly PropertyCollection Empty = new PropertyCollection(new Dictionary<string, Property>());

        Dictionary<string, Property> properties;

        internal PropertyCollection(Dictionary<string, Property> properties)
        {
            this.properties = properties;
        }

        public IEnumerator<Property> GetEnumerator()
        {
            return properties.Values.AsEnumerable().GetEnumerator();
        }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator()
        {
            return properties.GetEnumerator();
        }
    }
}