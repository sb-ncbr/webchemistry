namespace WebChemistry.Framework.TypeSystem
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Linq;

    /// <summary>
    /// Type nariance for unification and inferrence.
    /// </summary>
    internal enum TypeVariance
    {
        /// <summary>
        /// b.IsDerivedFrom(a)
        /// </summary>
        Covariant,

        /// <summary>
        /// a.IsDerivedFrom(b)
        /// </summary>
        Contravariant
    }


    /// <summary>
    /// Result of the unification.
    /// </summary>
    public class UnificationResult
    {
        /// <summary>
        /// Was the unification successful?
        /// </summary>
        public bool Success { get; set; }

        /// <summary>
        /// Substitutions of the variables in a.
        /// </summary>
        public Dictionary<string, TypeExpression> ASubstitutions { get; internal set; }

        /// <summary>
        /// Substitutions of the variables in b.
        /// </summary>
        public Dictionary<string, TypeExpression> BSubstitutions { get; internal set; }

        /// <summary>
        /// Expression including substitutions.
        /// </summary>
        public TypeExpression AExpression { get; internal set; }

        /// <summary>
        /// Expression including substitutions.
        /// </summary>
        public TypeExpression BExpression { get; internal set; }

        /// <summary>
        /// Expression including substitutions.
        /// </summary>
        public TypeExpression InferedExpression { get; internal set; }

        /// <summary>
        /// Create instance...
        /// </summary>
        internal UnificationResult()
        {
            ASubstitutions = new Dictionary<string, TypeExpression>(StringComparer.Ordinal);
            BSubstitutions = new Dictionary<string, TypeExpression>(StringComparer.Ordinal);
        }
    }

    /// <summary>
    /// Type unification.
    /// </summary>
    internal static class TypeUnification
    {
        static TypeExpression UnifyTuple(int indexA, TypeTuple a, int indexB, TypeTuple b, List<TypeExpression> inferred, TypeVariance variance, UnificationResult result)
        {
            if (a.Elements.Count == indexA && b.Elements.Count == indexB) return TypeTuple.Create(inferred);

            // If empty, check if other is "optional"
            if (a.Elements.Count == indexA)
            {
                if (b.Elements.Count - indexB == 1 && b.Elements[indexB] is TypeMany && (b.Elements[indexB] as TypeMany).AllowEmpty) return TypeTuple.Create(inferred);
                return null;
            }
            if (b.Elements.Count == indexB)
            {
                if (a.Elements.Count - indexA == 1 && a.Elements[indexA] is TypeMany && (a.Elements[indexA] as TypeMany).AllowEmpty) return TypeTuple.Create(inferred);
                return null;
            }

            var x = a.Elements[indexA].Substitute(result.ASubstitutions);
            var y = b.Elements[indexB].Substitute(result.BSubstitutions);

            if (x is TypeMany && !(y is TypeMany))
            {
                var manyX = x as TypeMany;

                Dictionary<string, TypeExpression> snapshotA = null, snapshotB = null;
                if (manyX.AllowEmpty)
                {
                    snapshotA = result.ASubstitutions.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
                    snapshotB = result.BSubstitutions.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
                }

                var compatible = Unify(manyX.Inner, y, variance, result);

                // if not compatible and allow optional, rollback the snapshot and skip in a.Elements.
                if (compatible == null && manyX.AllowEmpty)
                {
                    result.ASubstitutions = snapshotA; result.BSubstitutions = snapshotB;
                    return UnifyTuple(indexA + 1, a, indexB, b, inferred, variance, result);
                }

                if (compatible == null) return null;

                inferred.Add(compatible);

                // If Option, match only one element and continue.
                if (manyX.IsOption)
                {
                    return UnifyTuple(indexA + 1, a, indexB + 1, b, inferred, variance, result);
                }

                // See how many can we match.
                int matchedIndex = indexB + 1;
                var inner = manyX.Inner.Substitute(result.ASubstitutions);
                while (matchedIndex < b.Elements.Count)
                {
                    var element = b.Elements[matchedIndex].Substitute(result.BSubstitutions);
                    snapshotA = result.ASubstitutions.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
                    snapshotB = result.BSubstitutions.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
                    compatible = Unify(inner, element, variance, result);
                    if (compatible != null)
                    {
                        matchedIndex++;
                        inferred.Add(compatible);
                        inner = inner.Substitute(result.ASubstitutions);
                    }
                    else
                    {
                        result.ASubstitutions = snapshotA;
                        result.BSubstitutions = snapshotB;
                        break;
                    }
                }

                return UnifyTuple(indexA + 1, a, matchedIndex, b, inferred, variance, result);
            }

            if (y is TypeMany && !(x is TypeMany))
            {
                var manyY = y as TypeMany;

                Dictionary<string, TypeExpression> snapshotA = null, snapshotB = null;
                if (manyY.AllowEmpty)
                {
                    snapshotA = result.ASubstitutions.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
                    snapshotB = result.BSubstitutions.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
                }

                var compatible = Unify(x, manyY.Inner, variance, result);

                // if not compatible and allow optional, rollback the snapshot and skip in b.Elements.
                if (compatible == null && manyY.AllowEmpty)
                {
                    result.ASubstitutions = snapshotA; result.BSubstitutions = snapshotB;
                    return UnifyTuple(indexA, a, indexB + 1, b, inferred, variance, result);
                }

                if (compatible == null) return null;

                inferred.Add(compatible);

                // If Option, match only one element and continue.
                if (manyY.IsOption)
                {
                    return UnifyTuple(indexA + 1, a, indexB + 1, b, inferred, variance, result);
                }

                // See how many can we match.
                int matchedIndex = indexA + 1;
                var inner = manyY.Inner.Substitute(result.BSubstitutions);
                while (matchedIndex < a.Elements.Count)
                {
                    var element = a.Elements[matchedIndex].Substitute(result.ASubstitutions);
                    snapshotA = result.ASubstitutions.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
                    snapshotB = result.BSubstitutions.ToDictionary(v => v.Key, v => v.Value, StringComparer.Ordinal);
                    compatible = Unify(element, inner, variance, result);
                    if (compatible != null)
                    {
                        matchedIndex++;
                        inferred.Add(compatible);
                        inner = inner.Substitute(result.BSubstitutions);
                    }
                    else
                    {
                        result.ASubstitutions = snapshotA;
                        result.BSubstitutions = snapshotB;
                        break;
                    }
                }

                return UnifyTuple(matchedIndex, a, indexB + 1, b, inferred, variance, result);
            }

            // Unify normal types.
            {
                var compatible = Unify(x, y, variance, result);
                if (compatible != null)
                {
                    inferred.Add(compatible);
                    return UnifyTuple(indexA + 1, a, indexB + 1, b, inferred, variance, result);
                }
            }

            return null;
        }

        static TypeExpression UnifyArrow(TypeArrow a, TypeArrow b, UnificationResult result)
        {
            var from = Unify(a.From, b.From, TypeVariance.Contravariant, result);
            if (from != null)
            {
                var to = Unify(a.To.Substitute(result.ASubstitutions), b.To.Substitute(result.BSubstitutions), TypeVariance.Covariant, result);
                if (to != null) return TypeArrow.Create(from, to);
            }

            return null;
        }

        static TypeExpression Unify(TypeExpression a, TypeExpression b, TypeVariance variance, UnificationResult result)
        {
            ///////////////////////////////////
            // Variables
            if (a is TypeVariable && b is TypeVariable)
            {
                return b;
            }
            else if (a is TypeVariable)
            {
                result.ASubstitutions[(a as TypeVariable).Name] = b;
                return b;
            }
            else if (b is TypeVariable)
            {
                result.BSubstitutions[(b as TypeVariable).Name] = a;
                return b;
            }
            ///////////////////////////////////
            // Wildcards
            else if (a is TypeWildcard)
            {
                return b;
            }
            else if (b is TypeWildcard)
            {
                return a;
            }
            ///////////////////////////////////
            // Stars
            else if (a is TypeMany && b is TypeMany)
            {
                var ma = a as TypeMany;
                var mb = b as TypeMany;

                if (ma.AllowEmpty == mb.AllowEmpty && ma.IsOption == mb.IsOption)
                {
                    var ret = Unify(ma.Inner, mb.Inner, variance, result);
                    if (ret != null) return TypeMany.Create(ret, allowEmpty: ma.AllowEmpty, isOption: ma.IsOption);
                }
                return null;
            }
            else if (a is TypeMany)
            {
                var ma = a as TypeMany;
                return Unify(ma.Inner, b, variance, result);
            }
            else if (b is TypeMany)
            {
                var mb = b as TypeMany;
                return Unify(a, mb.Inner, variance, result);
            }
            ///////////////////////////////////
            // Constants
            else if (a is TypeConstant)
            {
                var ac = a as TypeConstant;
                if (b is TypeConstant)
                {
                    var bc = b as TypeConstant;

                    bool bFromA = bc.Class.IsDerivedFrom(ac.Class);
                    bool aFromB = ac.Class.IsDerivedFrom(bc.Class);

                    if (bFromA || aFromB)
                    {
                        if (variance == TypeVariance.Covariant) return bFromA ? b : a;
                        else if (variance == TypeVariance.Contravariant) return bFromA ? a : b;
                    }
                }
                return null;
            }
            ///////////////////////////////////
            // Tuples
            else if (a is TypeTuple)
            {
                if (b is TypeTuple) return UnifyTuple(0, a as TypeTuple, 0, b as TypeTuple, new List<TypeExpression>(), variance, result);
                return null;
            }
            //////////////////////////////////
            // Arrows
            else if (a is TypeArrow)
            {
                if (b is TypeArrow) return UnifyArrow(a as TypeArrow, b as TypeArrow, result);
                return null;
            }

            return null;
        }

        /// <summary>
        /// Unify types 'a' and 'b'.
        /// 'a' is the "pivot" type. Ie. when matching two type constants b.IsDerivedFrom(a) is called.
        /// </summary>
        /// <param name="a">The "pivot" type (usually without variables)</param>
        /// <param name="b">The type to match.</param>
        /// <returns></returns>
        public static UnificationResult Unify(TypeExpression a, TypeExpression b)
        {
            var result = new UnificationResult();

            var infered = Unify(a, b, TypeVariance.Covariant, result);
            result.Success = infered != null;
            result.AExpression = a.Substitute(result.ASubstitutions);
            result.BExpression = b.Substitute(result.BSubstitutions);

            if (infered != null) infered = infered.Substitute(result.BSubstitutions);
            result.InferedExpression = infered;

            return result;
        }
    }
}
