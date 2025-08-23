namespace WebChemistry.Framework.Geometry
{
    using System;

    interface IPriorityCollection<TPriority, TValue>
        where TPriority : IComparable<TPriority>
    {
        void Add(TPriority priority, TValue value);
        int Count { get; }
    }

    interface IPriorityArray<TPriority, TValue> 
        : IPriorityCollection<TPriority, TValue>
        where TPriority : IComparable<TPriority>
    {
        TPriority GetLargestPriority(TPriority maxIfNotFull);
    }
}
