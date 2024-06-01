
namespace WebChemistry.Queries.Core
{
    using WebChemistry.Framework.Core;
    using System;

    /// <summary>
    /// IStructure extensions for motives.
    /// </summary>
    public static class MotiveExtensions
    {
        const string MotivePropertyCathegory = "Motive";
        internal static readonly PropertyDescriptor<bool> IsMotiveProperty = PropertyHelper.Bool("IsMotive", category: MotivePropertyCathegory);
        internal static readonly PropertyDescriptor<string> MotiveParentIdProperty = PropertyHelper.String("MotiveParentId", category: MotivePropertyCathegory);
        internal static readonly PropertyDescriptor<MotiveContext> MotiveContextProperty = PropertyHelper.OfType<MotiveContext>("MotiveContext", category: MotivePropertyCathegory);

        /// <summary>
        /// Get id of the parent.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static string MotiveParentId(this IStructure structure)
        {
            return structure.GetProperty(MotiveParentIdProperty, "unknown");
        }

        /// <summary>
        /// Checks if a structure is a motive.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static bool IsMotive(this IStructure structure)
        {
            return structure.GetProperty(IsMotiveProperty, false);
        }

        /// <summary>
        /// Get the MotiveContext for this structure.
        /// </summary>
        /// <param name="structure"></param>
        /// <returns></returns>
        public static MotiveContext MotiveContext(this IStructure structure)
        {
            var context = structure.GetProperty(MotiveContextProperty, null);
            if (context != null) return context;
            context = new MotiveContext(structure);
            structure.SetProperty(MotiveContextProperty, context);
            return context;
        }
    }
}
