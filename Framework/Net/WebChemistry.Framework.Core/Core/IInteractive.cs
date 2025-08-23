namespace WebChemistry.Framework.Core
{
    using System.ComponentModel;

    public interface ISelectable : INotifyPropertyChanged
    {
        bool IsSelected { get; set; }
    }

    public interface IHighlightable : INotifyPropertyChanged
    {
        bool IsHighlighted { get; set; }
    }

    public interface IInteractive : INotifyPropertyChanged, ISelectable, IHighlightable
    {
        //bool IsSelected { get; set; }
        //bool IsHighlighted { get; set; }
    }
}