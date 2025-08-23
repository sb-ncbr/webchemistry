namespace WebChemistry.Framework.Geometry
{

    /// <summary>
    /// Represents a priority/value pair.
    /// </summary>
    public class PriorityValuePair<TPriority, TValue>
    {
        /// <summary>
        /// Priority.
        /// </summary>
        public readonly TPriority Priority;

        /// <summary>
        /// Value.
        /// </summary>
        public readonly TValue Value;

        /// <summary>
        /// Creates the pair.
        /// </summary>
        /// <param name="priority"></param>
        /// <param name="value"></param>
        public PriorityValuePair(TPriority priority, TValue value)
        {
            this.Priority = priority;
            this.Value = value;
        }
    }
}
