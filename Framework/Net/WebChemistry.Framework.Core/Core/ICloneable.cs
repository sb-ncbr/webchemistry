namespace WebChemistry.Framework.Core
{
    public interface ICloneable<out T>
    {
        /// <summary>
        /// Creates a deep copy of the object
        /// </summary>
        /// <returns>Deep copy of the object</returns>
        T Clone();
    }
}
