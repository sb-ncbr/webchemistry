namespace WebChemistry.Charges.Core
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WebChemistry.Framework.Core;

    /// <summary>
    /// Computation method.
    /// </summary>
    public enum ChargeComputationMethod
    {
        /// <summary>
        /// The full EEM matrix is used.
        /// </summary>
        Eem = 0,

        /// <summary>
        /// Cut off using a specified radius.
        /// </summary>
        EemCutoff,

        /// <summary>
        /// Cutoff method with coalescing H atoms to their neighbors.
        /// </summary>
        EemCutoffCoalesceH,

        /// <summary>
        /// Special covering method.
        /// </summary>
        EemCutoffCover,

        /// <summary>
        /// Reference charges.
        /// </summary>
        Reference
    }

    /// <summary>
    /// Determines the precision of the computation.
    /// </summary>
    public enum ChargeComputationPrecision
    {
        /// <summary>
        /// Use 64-bit floats.
        /// </summary>
        Double = 0,

        /// <summary>
        /// Use 32-bit floats.
        /// </summary>
        Single
    }

    /// <summary>
    /// COmputation parameters
    /// </summary>
    public class EemChargeComputationParameters
    {
        /// <summary>
        /// The structure.
        /// </summary>
        public IStructure Structure { get; set; }

        /// <summary>
        /// The method to use.
        /// </summary>
        public ChargeComputationMethod Method { get; set; }

        /// <summary>
        /// The precision of the computation.
        /// </summary>
        public ChargeComputationPrecision Precision { get; set; }

        /// <summary>
        /// Cutoff radius.
        /// </summary>
        public double CutoffRadius { get; set; }

        /// <summary>
        /// do cutoff correction.
        /// </summary>
        public bool CorrectCutoffTotalCharge { get; set; }
        
        /// <summary>
        /// Remove water atoms?
        /// </summary>
        public bool IgnoreWaters { get; set; }

        /// <summary>
        /// Only on selection?
        /// </summary>
        public bool SelectionOnly { get; set; }

        /// <summary>
        /// Atom selection.
        /// </summary>
        public IList<IAtom> AtomSelection { get; set; }

        /// <summary>
        /// Total Charge.
        /// </summary>
        public double TotalCharge { get; set; }

        /// <summary>
        /// Parameter set.
        /// </summary>
        public EemParameterSet Set { get; set; }

        string _id;
        /// <summary>
        /// Id string.
        /// </summary>
        public string Id
        {
            get
            {
                if (_id != null) return _id;
                _id = string.Format("{0}_{1}", Set.Name, MethodId);
                return _id;
            }
        }

        string GetMethodIdString()
        {
            switch (Method)
            {
                case ChargeComputationMethod.Eem: return "eem";
                case ChargeComputationMethod.EemCutoff: return string.Format("cut_{0}{1}", CutoffRadius.ToStringInvariant("0"), CorrectCutoffTotalCharge ? "_corr" : "");
                case ChargeComputationMethod.EemCutoffCoalesceH: return string.Format("cutch_{0}{1}", CutoffRadius.ToStringInvariant("0"), CorrectCutoffTotalCharge ? "_corr" : "");
                case ChargeComputationMethod.EemCutoffCover: return string.Format("cover_{0}{1}", CutoffRadius.ToStringInvariant("0"), CorrectCutoffTotalCharge ? "_corr" : "");                
            }
            throw new InvalidOperationException("Unknown method.");
        }

        string methodId;
        /// <summary>
        /// Method ID.
        /// </summary>
        public string MethodId
        {
            get
            {
                if (methodId != null) return methodId;

                if (Method == ChargeComputationMethod.Reference)
                {
                    methodId = "ref";
                    return methodId;
                }

                methodId = string.Format("{0}_{1}{2}{3}chrg_{4}",
                    GetMethodIdString(),
                    Precision == ChargeComputationPrecision.Single ? "sngl_" : "",
                    IgnoreWaters ? "nowtr_" : "",
                    SelectionOnly ? "sel_" : "",
                    TotalCharge.ToStringInvariant("0"));

                return methodId;
            }
        }

        /// <summary>
        /// String...
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Id;
        }
        
        /// <summary>
        /// Check if the parameters are equal.
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj)
        {
            var other = obj as EemChargeComputationParameters;
            if (other == null) return false;

            if (!this.Structure.Id.Equals(other.Structure.Id)) return false;
            if (this.IgnoreWaters != other.IgnoreWaters) return false;
            if (this.SelectionOnly != other.SelectionOnly) return false;
            if (this.TotalCharge != other.TotalCharge) return false;
            if (!this.Set.Equals(other.Set)) return false;
            if (!this.AtomSelection.SequenceEqual(other.AtomSelection)) return false;
            if (this.Method != other.Method) return false;
            if (this.Precision != other.Precision) return false;


            if (this.Method == ChargeComputationMethod.Eem || this.Method == ChargeComputationMethod.Reference) return true;
            else if (other.Method == ChargeComputationMethod.EemCutoff 
                || other.Method == ChargeComputationMethod.EemCutoffCoalesceH
                || other.Method == ChargeComputationMethod.EemCutoffCover)
            {
                return CutoffRadius == other.CutoffRadius;
            }

            return false;
        }

        /// <summary>
        /// Hash code...
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            int hash = 31;
            hash = unchecked(hash * 23 + Structure.Id.GetHashCode());
            hash = unchecked(hash * 23 + Method.GetHashCode());
            hash = unchecked(hash * 23 + Precision.GetHashCode());
            hash  = unchecked(hash * 23 + IgnoreWaters.GetHashCode());
            hash = unchecked(hash * 23 + SelectionOnly.GetHashCode());
            hash = unchecked(hash * 23 + TotalCharge.GetHashCode());
            hash = unchecked(hash * 23 + Set.GetHashCode());
            hash = unchecked(hash * 23 + AtomSelection.GetHashCode());
            if (Method == ChargeComputationMethod.EemCutoff 
                || Method == ChargeComputationMethod.EemCutoffCoalesceH
                || Method == ChargeComputationMethod.EemCutoffCover)
            {
                hash = unchecked(hash * 23 + CutoffRadius.GetHashCode());
                hash = unchecked(hash * 23 + CorrectCutoffTotalCharge.GetHashCode());
            }

            return hash;
        }

        public EemChargeComputationParameters()
        {
            AtomSelection = new List<IAtom>();
            CorrectCutoffTotalCharge = true;
        }
    }
}
