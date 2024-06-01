using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using WebChemistry.Framework.Core;
using WebChemistry.Framework.Visualization;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.Charges.Silverlight.DataModel;
using WebChemistry.Framework.Core.Pdb;
using WebChemistry.Charges.Core;


namespace WebChemistry.Charges.Silverlight.Visuals
{
    public class ChargeStructureVisual3D : Visual3D
    {
        FrameworkElement tooltipVisual;
        
        AtomVisual[] atomVisuals;
        BondVisual[] bondVisuals;
        StructureModel3D model;
        
        StructureCharges charges;
        AtomPartitionCharges partitionCharges;
        IStructure structure;
        AtomPartition partition;

        bool disposed = false;

        public bool SuppressTooltip { get; set; }

        public AtomPartition Partition { get { return partition; } }
        public IStructure Structure { get { return structure; } }

        public class TooltipInfo
        {
            public string Label { get; set; }
            public string Charge { get; set; }
            public string Set { get; set; }
        }

        internal void ShowToolip(AtomModel3D atom)
        {
            if (SuppressTooltip || charges == null) return;

            var group = partition.Groups[atom.Atom.Id];
            double charge;
            bool hasCharge = partitionCharges.PartitionCharges.TryGetValue(group, out charge);

            tooltipVisual.DataContext =
                new TooltipInfo
                {
                    Label = group.Label,
                    Charge = hasCharge ? charge.ToStringInvariant("0.000") : "n/a",
                    Set = charges.Name
                };
            tooltipVisual.Visibility = System.Windows.Visibility.Visible;

            Canvas.SetZIndex(tooltipVisual, Int16.MaxValue - 1);
            Canvas.SetLeft(tooltipVisual, atom.BoundingBox.Right + 7);
            Canvas.SetTop(tooltipVisual, atom.BoundingBox.Top + atom.BoundingBox.Height / 2);
        }

        internal void HideTooltip()
        {
            tooltipVisual.Visibility = System.Windows.Visibility.Collapsed;
        }

        void SetCameraRadius()
        {
            double currentRadius = Viewport.Camera.Radius;

            var radius = structure.GeometricalCenterAndRadius().Radius;
            if (radius < 10) radius *= 4;
            else if (radius < 50) radius *= 3.2;
            else if (radius < 100) radius *= 2.7;
            else radius *= 2.5;

            if (radius < 1) radius = 1;
            else if (radius > 155) radius = 155;

            if (radius > currentRadius)
            {
                Viewport.Camera.Radius = radius;
            }
        }
        
        public override void Register(Viewport3DBase viewport)
        {
            base.Register(viewport);
            viewport.Canvas.Children.Add(tooltipVisual);
            atomVisuals.ForEach(v => v.Register(Viewport));
            bondVisuals.ForEach(v => v.Register(Viewport));
            SetCameraRadius();
        }

        public override void Render(RenderContext context)
        {
            atomVisuals.ForEach(v => v.Render(context));
            bondVisuals.ForEach(v => v.Render(context));
        }

        public override void UpdateAsync(UpdateAsyncArgs args)
        {
            model.UpdateAsync(args);
        }

        public void Render()
        {
            Viewport.CancelRender();
            Viewport.Render();
        }

        public override void Dispose()
        {
            if (disposed) return;

            for (int i = 0; i < atomVisuals.Length; i++)
            {
                atomVisuals[i].Dispose();
            }
            for (int i = 0; i < bondVisuals.Length; i++)
            {
                bondVisuals[i].Dispose();
            }

            atomVisuals = null;
            bondVisuals = null;

            model.Dispose();
            disposed = true;
        }
        
        /// <summary>
        /// If range is null, computed automatically.
        /// </summary>
        /// <param name="charges"></param>
        /// <param name="range"></param>
        public void SetCharges(StructureCharges charges, Tuple<double, double> range = null)
        {
            this.charges = charges;
            this.partitionCharges = charges.PartitionCharges[partition.Name];

            if (range == null)
            {
                range = Tuple.Create(partitionCharges.MinCharge, partitionCharges.MaxCharge);
            }

            foreach (var atom in atomVisuals)
            {
                var group = partition.Groups[atom.Atom.Id];
                double charge;
                partitionCharges.PartitionCharges.TryGetValue(group, out charge);
                atom.UpdateCharge(charge, range);
            }

            foreach (var bond in bondVisuals)
            {
                double chargeA, chargeB;
                partitionCharges.PartitionCharges.TryGetValue(partition.Groups[bond.AtomA.Id], out chargeA);
                partitionCharges.PartitionCharges.TryGetValue(partition.Groups[bond.AtomB.Id], out chargeB);
                bond.UpdateCharge(chargeA, chargeB, range);
            }

            Render();
        }
        
        public void RemoveCharges()
        {
            atomVisuals.ForEach(v => v.RemoveCharges());
            bondVisuals.ForEach(v => v.RemoveCharges());

            Render();
        }
        
        public void ShowWaters(bool show)
        {
            var visibility = show ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;

            foreach (var atom in atomVisuals)
            {
                var group = partition.Groups[atom.Atom.Id];
                if (group.ResidueCount == 1 && PdbResidue.IsWaterName(group.Residues))
                {
                    atom.Show(visibility);
                }
            }

            foreach (var bond in bondVisuals)
            {
                var groupA = partition.Groups[bond.AtomA.Id];
                var groupB = partition.Groups[bond.AtomB.Id];
                if ((groupA.ResidueCount == 1 && PdbResidue.IsWaterName(groupA.Residues))
                    || (groupB.ResidueCount == 1 && PdbResidue.IsWaterName(groupB.Residues)))
                {
                    bond.Show(visibility);
                }
            }
        }

        public ChargeStructureVisual3D(AtomPartition partition, IStructure structure)
        {
            SuppressTooltip = true;

            tooltipVisual = (Application.Current.Resources["VisualTooltipTemplate"] as DataTemplate).LoadContent() as FrameworkElement;
            tooltipVisual.Visibility = System.Windows.Visibility.Collapsed;

            this.partition = partition;
            this.structure = structure;
            this.model = new StructureModel3D(structure);
            this.atomVisuals = model.Atoms.Select(a => new AtomVisual(a, this)).ToArray();
            this.bondVisuals = model.Bonds.Select(b => new BondVisual(b)).ToArray();
        }
    }
}
