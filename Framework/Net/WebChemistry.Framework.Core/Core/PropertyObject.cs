namespace WebChemistry.Framework.Core
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// A base class for objects that need dynamic properties.
    /// </summary>
    public abstract class PropertyObject : ObservableObject, IPropertyObject
    {
        Dictionary<string, Property> properties;
        
        /// <summary>
        /// Gets the property. If the property does not exist, an exception is thrown.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <returns></returns>
        public T GetProperty<T>(PropertyDescriptor<T> property)
        {
            if (properties == null) throw new ArgumentException(string.Format("There is no property with name '{0}'.", property.Name));

            Property v;
            if (properties.TryGetValue(property.Name, out v))
            {
                return (T)v.Value;
            }

            throw new ArgumentException(string.Format("There is no property with name '{0}'.", property.Name));
        }
                
        /// <summary>
        /// Gets the property. If the property does not exist, the default value is returned.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public T GetProperty<T>(PropertyDescriptor<T> property, T defaultValue)
        {
            if (properties == null) return defaultValue;

            Property v;
            if (properties.TryGetValue(property.Name, out v))
            {
                return (T)v.Value;
            }

            return defaultValue;
        }

        /// <summary>
        /// Sets the value of the given property.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="property"></param>
        /// <param name="value"></param>
        public void SetProperty<T>(PropertyDescriptor<T> property, T value)
        {
            SetProperty(new Property(value, property));
        }
              
        /// <summary>
        /// Get a read-only collection of the properties.
        /// </summary>
        public PropertyCollection Properties
        {
            get { return properties != null ? new PropertyCollection(properties) : PropertyCollection.Empty ; }
        }

        /// <summary>
        /// Sets the property.
        /// </summary>
        /// <param name="property"></param>
        public void SetProperty(Property property)
        {
            string name = property.Descriptor.Name;
            object value = property.Value;

            if (properties == null)
            {
                properties = new Dictionary<string, Property>(StringComparer.Ordinal) { { name, property } };
                if (value != null) NotifyPropertyChanged(name);
            }
            else
            {
                Property oldValueProperty;
                bool contains = properties.TryGetValue(name, out oldValueProperty);
                object oldValue = oldValueProperty.Value;

                //if (property.Descriptor.IsImmutable && contains && !oldValue.Equals(value))
                //{
                //    throw new CannotWriteImmutablePropertyException(property.Descriptor, this);
                //}

                if (contains)
                {
                    if (oldValue == null)
                    {
                        if (value != null)
                        {
                            properties[name] = property;
                            NotifyPropertyChanged(name);
                        }
                    }
                    else if (!oldValue.Equals(value))
                    {
                        properties[name] = property;
                        NotifyPropertyChanged(name);
                    }
                }
                else
                {
                    properties[name] = property;
                }
            }
        }

        /// <summary>
        /// Removes the property.
        /// </summary>
        /// <param name="property"></param>
        public void RemoveProperty(PropertyDescriptor property)
        {
            properties.Remove(property.Name);    
        }
    }
}
