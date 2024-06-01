using System.Linq;
using System.Collections.Generic;
using WebChemistry.Framework.Core;

namespace WebChemistry.Framework.Visualization
{
    public class StructureModel3D : Model3D
    {
        IStructure _structure;

        public IStructure Structure
        {
            get { return _structure; }
        }

        AtomModel3D[] _atoms;
        BondModel3D[] _bonds;

        public AtomModel3D[] Atoms { get { return _atoms; } }
        public BondModel3D[] Bonds { get { return _bonds; } }

        public StructureModel3D(IStructure structure)
        {
            _structure = structure;

            var models = structure.Atoms.Select(a => new AtomModel3D(a)).ToArray();

            Dictionary<int, AtomModel3D> atomModels = models.ToDictionary(a => a.Atom.Id, a => a);
            
            _atoms = models;
            _bonds = structure.Bonds.Select(b => new BondModel3D(atomModels[b.A.Id], atomModels[b.B.Id], b)).ToArray();
        }

        public override void Dispose()
        {
            
        }

        public override void Update(UpdateContext context)
        {
            foreach (var a in _atoms) a.Update(context);
            foreach (var b in _bonds) b.Update(context);
        }

        public override void UpdateAsync(UpdateAsyncArgs args)
        {
            UpdateContext context = args.Context;

            int workerCount = args.WorkerCount;
            int index = args.WorkerIndex;

            int d = _atoms.Length / workerCount;
            int startIndex = index * d;
            int endIndex = (index + 1) * d;

            if (index == workerCount - 1)
            {
                endIndex = _atoms.Length;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                if (args.Worker != null && args.Worker.CancellationPending)
                {
                    args.WorkerArgs.Cancel = true;
                    return;
                }

                _atoms[i].Update(context);
            }

            d = _bonds.Length / workerCount;
            startIndex = index * d;
            endIndex = (index + 1) * d;

            if (index == workerCount - 1)
            {
                endIndex = _bonds.Length;
            }

            for (int i = startIndex; i < endIndex; i++)
            {
                if (args.Worker != null && args.Worker.CancellationPending)
                {
                    args.WorkerArgs.Cancel = true;
                    return;
                }

                _bonds[i].Update(context);
            }
        }
    }
}