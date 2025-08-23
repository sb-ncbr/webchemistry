namespace WebChemistry.Framework.Core
{
    using System;

    public class CannotWriteImmutablePropertyException : Exception
    {
        public PropertyDescriptor Property { get; private set; }
        public object Where { get; private set; }

        public override string Message
        {
            get
            {
                return string.Format("Cannot write the immutable property '{0}' on object of type {1}.", Property.Name, Where.GetType().ToString());
            }
        }

        public CannotWriteImmutablePropertyException(PropertyDescriptor descriptor, object where)
        {
            this.Property = descriptor;
            this.Where = where;
        }
    }
}
