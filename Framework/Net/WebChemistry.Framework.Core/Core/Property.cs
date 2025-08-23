namespace WebChemistry.Framework.Core
{
    public struct Property
    {
        public readonly object Value;
        public readonly PropertyDescriptor Descriptor;

        internal Property(object value, PropertyDescriptor descriptor)
        {
            this.Value = value;
            this.Descriptor = descriptor;
        }
    }
}
