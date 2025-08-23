
namespace WebChemistry.Framework.Core
{
    public abstract class InteractiveObject : ObservableObject, IInteractive
    {
        protected virtual void OnHighlightedChanged()
        {

        }

        protected virtual void OnSelectedChanged()
        {

        }

        protected void SetHighlightedWithoutNotify(bool value)
        {
            this.isHighlighted = value;
        }

        protected void SetSelectedWithoutNotify(bool value)
        {
            this.isSelected = value;
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
