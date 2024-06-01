﻿
namespace WebChemistry.Silverlight.Common.Utils
{
    using System.Globalization;

    public static class CultureHelper
    {
        public static void SetDefaultCultureToEnUS()
        {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
        }
    }
}
