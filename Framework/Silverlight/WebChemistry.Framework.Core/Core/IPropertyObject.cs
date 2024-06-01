namespace WebChemistry.Framework.Core
{
    using System.ComponentModel;

    public interface IPropertyObject : INotifyPropertyChanged
    {
        T GetProperty<T>(PropertyDescriptor<T> property);
        T GetProperty<T>(PropertyDescriptor<T> property, T defaultValue = default(T));
        void RemoveProperty(PropertyDescriptor property);
        void SetProperty<T>(PropertyDescriptor<T> property, T value);
        void SetProperty(Property property);
        PropertyCollection Properties { get; }
    }
}