// -----------------------------------------------------------------------
// <copyright file="Permutations.cs" company="">
// TODO: Update copyright text.
// </copyright>
// -----------------------------------------------------------------------

namespace WebChemistry.SiteBinder.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Based for combinatorics stuff.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public abstract class CombinatoricsBase<T>
    {
        protected T[] buffer;
        protected int bufferSize;

        public int Size { get { return bufferSize; } }
        
        /// <summary>
        /// Non-threadsafe visit fuction. the array gets reused.
        /// </summary>
        /// <param name="onNext"></param>
        public abstract void Visit(Action<T[]> onNext);
    }

    /// <summary>
    /// A wrapper for visiting permutations.
    /// </summary>
    public class Permutations<T> : CombinatoricsBase<T>
    {
        int elementLevel = -1;
        int numberOfElements;
        int[] permutationValue;
        T[] inputSet;
        Action<T[]> onNext;
        bool running = false;

        private Permutations()
        {

        }

        public static Permutations<T> Create(IEnumerable<T> xs)
        {
            var ret = new Permutations<T>();
            ret.inputSet = xs.ToArray();
            ret.permutationValue = new int[ret.inputSet.Length];
            ret.numberOfElements = ret.inputSet.Length;
            ret.buffer = new T[ret.inputSet.Length];
            ret.bufferSize = ret.inputSet.Length;
            return ret;
        }

        /// <summary>
        /// Non-threadsafe visit fuction. the array gets reused.
        /// </summary>
        /// <param name="onNext"></param>
        public override void Visit(Action<T[]> onNext)
        {
            if (running) throw new InvalidOperationException("Another Visit operation is in progress.");
            running = true;
            elementLevel = -1;
            for (int i = 0; i < numberOfElements; i++) permutationValue[i] = 0;
            this.onNext = onNext;
            VisitPermutations(0);
            this.onNext = null;
            running = false;
        }

        void VisitPermutations(int k)
        {
            elementLevel++;
            permutationValue[k] = elementLevel;
            
            if (elementLevel > 0) buffer[k] = inputSet[elementLevel - 1];

            if (elementLevel == numberOfElements)
            {
                onNext(buffer);
            }
            else
            {
                for (int i = 0; i < numberOfElements; i++)
                {
                    if (permutationValue[i] == 0)
                    {
                        VisitPermutations(i);
                    }
                }
            }
            elementLevel--;
            permutationValue[k] = 0;
        }
    }

    /// <summary>
    /// A wrapper for visiting combinations.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class Combinations<T> : CombinatoricsBase<T>
    {
        int numberOfElements;
        T[] inputSet;
        Action<T[]> onNext;
        bool running = false;
        
        private Combinations()
        {

        }

        public static Combinations<T> Create(IEnumerable<T> xs, int k)
        {
            var ret = new Combinations<T>();
            ret.inputSet = xs.ToArray();

            if (k < 0 || k > ret.inputSet.Length)
            {
                throw new ArgumentException("Combinations: k out of range.");
            }

            ret.numberOfElements = ret.inputSet.Length;
            ret.buffer = new T[k];
            ret.bufferSize = k;
            return ret;
        }

        /// <summary>
        /// Non-threadsafe visit fuction. the array gets reused.
        /// </summary>
        /// <param name="onNext"></param>
        public override void Visit(Action<T[]> onNext)
        {
            if (running) throw new InvalidOperationException("Another Visit operation is in progress.");
            running = true;
            this.onNext = onNext;
            VisitCombinations(0, 0);
            this.onNext = null;
            running = false;
        }

        void VisitCombinations(int p, int low) 
        {
            int high = numberOfElements - bufferSize + p;

            for (int i = low; i <= high; i++) 
            {
                buffer[p] = inputSet[i];

                if (p >= bufferSize - 1)
                {
                    onNext(buffer);
                }
                else
                {
                    VisitCombinations(p + 1, i + 1);
                }
            }
        }        
    }

    /// <summary>
    /// A chain of combinatorics stuff.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class CombinatoricsChain<T> : CombinatoricsBase<T>
    {
        CombinatoricsBase<T>[] combinatorics;
        Action<T[]>[] visitors;
        Action<T[]> onNext;
        bool running = false;

        private CombinatoricsChain()
        {

        }

        public static CombinatoricsChain<T> Create(IEnumerable<CombinatoricsBase<T>> combinations)
        {
            var ret = new CombinatoricsChain<T>();
            ret.combinatorics = combinations.ToArray();
            ret.buffer = new T[ret.combinatorics.Sum(c => c.Size)];
            ret.bufferSize = ret.buffer.Length;
            ret.visitors = ret.combinatorics.Select((c, i) =>
                {
                    var cs = ret;
                    var index = i;
                    var offset = i == 0 ? 0 : ret.combinatorics.Take(i).Sum(t => t.Size);
                    return new Action<T[]>(xs => 
                    {
                        var buffer = ret.buffer;
                        for (int j = 0; j < xs.Length; j++)
                        {
                            buffer[offset + j] = xs[j];
                        }
                        if (index < cs.combinatorics.Length - 1)
                        {
                            ret.VisitCombinations(index + 1);
                        }
                        else ret.onNext(xs);
                    });
                })
                .ToArray();
            return ret;
        }

        /// <summary>
        /// Non-threadsafe visit fuction. the array gets reused.
        /// </summary>
        /// <param name="onNext"></param>
        public override void Visit(Action<T[]> onNext)
        {
            if (running) throw new InvalidOperationException("Another Visit operation is in progress.");
            running = true;
            this.onNext = onNext;
            VisitCombinations(0);
            this.onNext = null;
            running = false;
        }

        void VisitCombinations(int index)
        {
            combinatorics[index].Visit(visitors[index]);
        }
    }
}
