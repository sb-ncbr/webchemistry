namespace WebChemistry.Framework.Visualization.Visuals
{
    using System.Windows.Input;

    public interface IMovableVisual : IInteractiveVisual
    {
        Key[] MoveActivationKeys { get; }
        void Move(double by);
        void StartMove();
        void EndMove();
    }
}
