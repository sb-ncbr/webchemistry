using System;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Framework.Core;
using WebChemistry.Queries.Silverlight.Visuals;
using WebChemistry.Framework.Visualization;
using System.Xml.Linq;
using System.Collections.Generic;
using WebChemistry.Framework.Core.Pdb;
using System.Linq;

namespace WebChemistry.Queries.Silverlight.DataModel
{
    public class MotiveWrap : InteractiveObject, IVisual
    {

        public string Name { get { return Motive.Id; } }

        public IStructure Parent { get; private set; }
        public IStructure Motive { get; private set; }

        public string AtomCountString { get; private set; }

        //public Visual { get; set; }

        public int Index { get; set; }

        public string ResidueString { get; private set; }

        public string ExportResidueString { get; private set; }

        public string ResidueIdentifiers { get; private set; }

        public string AtomString { get; private set; }

        public static MotiveWrap Create(IStructure parent, IStructure motive, int index)
        {
            var ret = new MotiveWrap
            {
                Parent = parent,
                Motive = motive,
                Index = index,
                ResidueString = motive.PdbResidues().CountedShortAminoNamesString,
                ExportResidueString = PdbResidueCollection.GetExplicitlyCountedResidueString(motive.PdbResidues().ResidueCounts),
                ResidueIdentifiers = string.Join("-", motive
                                                .PdbResidues()
                                                .OrderBy(r => r.ChainIdentifier)
                                                .ThenBy(r => r.Number)
                                                .Select(r => !string.IsNullOrWhiteSpace(r.ChainIdentifier)
                                                    ? string.Format("{0} {1} {2}", r.Name, r.Number, r.ChainIdentifier)
                                                    : string.Format("{0} {1}", r.Name, r.Number))),
                AtomString = PdbResidueCollection.GetExplicitlyCountedResidueString(motive
                    .Atoms
                    .GroupBy(a => a.ElementSymbol)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count(), StringComparer.Ordinal)),
                AtomCountString = motive.Atoms.Count.ToString() + " atoms",
                model = new StructureModel3D(motive),
                CenterInfo = motive.GeometricalCenterAndRadius()
            };

            return ret;
        }

        public override string ToString()
        {
            //return string.Format("{0}, motive {1}", Parent.Id, Index);
            return Name;
        }

        MotiveVisual3D visual;
        StructureModel3D model;

        public MotiveVisual3D Visual
        {
            get
            {
                return visual = visual ?? new MotiveVisual3D(model, this.Motive, Color.FromArgb(255, 0x33, 0x33, 0x33));
            }
        }

        public void ClearVisual()
        {
            if (visual != null)
            {
                visual.Dispose();
                visual = null;
            }
        }

        public Framework.Geometry.GeometricalCenterInfo CenterInfo { get; private set; }
        
        public static ListExporter GetExporter(IEnumerable<MotiveWrap> motives, char csvSeparator = ',')
        {
            return motives.GetExporter(separator: csvSeparator.ToString(), xmlRootName: "Motives", xmlElementName: "Motive")
                .AddExportableColumn(x => x.Name, ColumnType.String, "Name")
                .AddExportableColumn(x => x.Parent.Id, ColumnType.String, "ParentId")
                .AddExportableColumn(x => x.Motive.Atoms[0].Id, ColumnType.Number, "FirstAtomId")
                .AddExportableColumn(x => x.Motive.Atoms.Count, ColumnType.Number, "AtomCount")
                .AddExportableColumn(x => x.AtomString, ColumnType.String, "Atoms")
                .AddExportableColumn(x => x.ExportResidueString, ColumnType.String, "Signature")
                .AddExportableColumn(x => x.ResidueIdentifiers, ColumnType.String, "Residues");
        }
    }
}
