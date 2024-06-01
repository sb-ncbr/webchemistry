namespace WebChemistry.Framework.Core
{
    using System;

    /// <summary>
    /// Non-generic property descriptor.
    /// </summary>
    public class PropertyDescriptor
    {
        /// <summary>
        /// Determines if the property is immutable.
        /// </summary>
        public readonly bool IsImmutable;

        /// <summary>
        /// Name of the property.
        /// </summary>
        public readonly string Name;

        /// <summary>
        /// Property category.
        /// </summary>
        public readonly string Category;

        /// <summary>
        /// Auto clone
        /// </summary>
        public readonly bool AutoClone;

        /// <summary>
        /// Used to create a deep copy of the property.
        /// Args: newAtoms, newBonds, oldObject
        /// </summary>
        public readonly Func<AtomCollection, BondCollection, object, object> OnClone;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="name"></param>
        /// <param name="isImmutable"></param>
        /// <param name="category"></param>
        /// <param name="onClone"></param>
        internal PropertyDescriptor(string name, bool isImmutable = true, string category = PropertyHelper.DefaultCategory, bool autoClone = true, Func<AtomCollection, BondCollection, object, object> onClone = null)
        {
            this.Name = name;
            this.Category = category;
            this.IsImmutable = isImmutable;
            this.AutoClone = autoClone;

            if (onClone != null)
            {
                /*if (!onClone.Method.IsStatic)
                {
                    throw new ArgumentException("OnClone function must be static.");
                }*/

                this.OnClone = onClone;
            }
            else
            {
                onClone = (a, b, o) => o;
            }
        }
    }
    
    /// <summary>
    /// Property descriptor. Bu default, properties are immutable (can only be set once).
    /// </summary>
    /// <typeparam name="T">Type of the property.</typeparam>
    public class PropertyDescriptor<T> : PropertyDescriptor
    {
        /// <summary>
        /// Crates are property descriptor.
        /// </summary>
        /// <param name="name">Name of the property.</param>
        /// <param name="isImmutable">Is set to true, the property can only be set once.</param>
        /// <param name="category"></param>
        /// <param name="onClone"></param>
        public PropertyDescriptor(string name, bool isImmutable = true, string category = PropertyHelper.DefaultCategory, bool autoClone = true, Func<AtomCollection, BondCollection, object, object> onClone = null)
            : base(name, isImmutable, category, autoClone, onClone)
        {

        }
    }
}
