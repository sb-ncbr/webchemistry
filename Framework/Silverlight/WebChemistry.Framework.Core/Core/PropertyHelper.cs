namespace WebChemistry.Framework.Core
{
    public static class PropertyHelper
    {
        public const string DefaultCategory = "Default";

        public static PropertyDescriptor<T> OfType<T>(string name, bool isImmutable = true,
                                                      string category = DefaultCategory, bool autoClone = true)
        {
            return new PropertyDescriptor<T>(name, isImmutable, category, autoClone);
        }

        public static PropertyDescriptor<double> Double(string name, bool isImmutable = true,
                                                        string category = DefaultCategory, bool autoClone = true)
        {
            return OfType<double>(name, isImmutable, category, autoClone);
        }

        public static PropertyDescriptor<string> String(string name, bool isImmutable = true,
                                                        string category = DefaultCategory, bool autoClone = true)
        {
            return OfType<string>(name, isImmutable, category, autoClone);
        }

        public static PropertyDescriptor<int> Int(string name, bool isImmutable = true,
                                                  string category = DefaultCategory, bool autoClone = true)
        {
            return OfType<int>(name, isImmutable, category, autoClone);
        }

        public static PropertyDescriptor<bool> Bool(string name, bool isImmutable = true,
                                                    string category = DefaultCategory, bool autoClone = true)
        {
            return OfType<bool>(name, isImmutable, category, autoClone);
        }

        public static PropertyDescriptor<char> Char(string name, bool isImmutable = true,
                                                    string category = DefaultCategory, bool autoClone = true)
        {
            return OfType<char>(name, isImmutable, category, autoClone);
        }
    }
}