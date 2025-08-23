namespace WebChemistry.Framework.Core
{
    using System.ComponentModel;

    public abstract class InteractivePropertyObject : PropertyObject, INotifyPropertyChanged, IInteractive, IPropertyObject
    {
        protected virtual void OnHighlightedChanged()
        {
            
        }

        protected virtual void OnSelectedChanged()
        {

        }

        private bool isHighlighted;
        public bool IsHighlighted
        {
            get
            {
                return isHighlighted;
            }

            set
            {
                if (isHighlighted == value) return;

                isHighlighted = value;
                OnHighlightedChanged();
                NotifyPropertyChanged("IsHighlighted");
            }
        }

        private bool isSelected;
        public bool IsSelected
        {
            get
            {
                return isSelected;
            }

            set
            {
                if (isSelected == value) return;

                isSelected = value;
                OnSelectedChanged();
                NotifyPropertyChanged("IsSelected");
            }
        }
    }
}