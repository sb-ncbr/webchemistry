using System.Collections.Generic;

namespace WebChemistry.MotiveAtlas.DataModel
{
    public abstract class DescriptorBase
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
    }

    /// <summary>
    /// Describes the Atlas -- a list of categories.
    /// </summary>
    public class AtlasDescriptor
    {
        public string DatabaseName { get; set; }
        public CategoryDescriptor[] Categories { get; set; }
    }

    /// <summary>
    /// A category -- a list of sub categories.
    /// </summary>
    public class CategoryDescriptor : DescriptorBase
    {
        public SubCategoryDescriptor[] SubCategories { get; set; }
    }

    /// <summary>
    /// A list of motives.
    /// </summary>
    public class SubCategoryDescriptor : DescriptorBase
    {
        public MotiveDescriptor[] Motives { get; set; }
    }

    /// <summary>
    /// Just a name, id, and description.
    /// </summary>
    public class MotiveDescriptor : DescriptorBase
    {
        public int MotiveCount { get; set; }
    }
}
