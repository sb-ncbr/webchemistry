
namespace WebChemistry.Framework.Math
{
    public interface IMatrix<T>
    {
        T this[int i, int j] { get; set; }
        T[,] Data { get; }
        int Rows { get; }
        int Cols { get; }
    }
}
