namespace WebChemistry.Framework.Core
{
    /// <summary>
    /// Bond types.
    /// </summary>
    public enum BondType
    {
        Unknown = 0,

        Single = 1,
        Double = 2,
        Triple = 3,
        Aromatic = 4,

        Metallic = 5,
        Ion = 6,
        Hydrogen = 7,
        DisulfideBridge = 8
    }
}