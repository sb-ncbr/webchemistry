
namespace WebChemistry.Framework.Math
{
    public interface IVector<T>
    {
        T this[int i] { get; set; }
        T[] Data { get; }
        int Dimension { get; }
        T Norm { get; }
    }
}
