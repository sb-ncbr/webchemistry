using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WebChemistry.Framework.Core;

namespace WebChemistry.Framework.Core.Tests
{
    [TestClass]
    public class BasicCoreTests
    {
        [TestMethod]
        public void TestAtomProperties()
        {
            var atom = Atom.Create(10, ElementSymbols.C, new Math.Vector3D(0, 1, 0));
            Assert.AreEqual(10, atom.Id);
            Assert.AreEqual(ElementSymbols.C, atom.ElementSymbol);
            Assert.AreEqual(new Math.Vector3D(0, 1, 0), atom.Position);
        }

        int newValue = 0;

        [TestMethod]
        public void TestMutablePropertyObject()
        {
            //var atom = new Atom(10, ElementSymbols.C);
            //var tp = PropertyHelper.Int("TestProperty", false);
            //atom.SetProperty(tp, 123);
            //Assert.AreEqual(123, atom.GetProperty(tp));
            //atom.ObservePropertyChanged(this, (l, s, o) => l.newValue = (s as Atom).GetProperty(PropertyHelper.Int("TestProperty", false)));
            //atom.SetProperty(tp, 145);
            //Assert.AreEqual(145, atom.GetProperty(tp));
            //Assert.AreEqual(145, newValue);
        }

        [TestMethod]
        public void TestImmutablePropertyObject()
        {
            //var atom = new Property(10, ElementSymbols.C);
            //var tp = PropertyHelper.Int("TestProperty", true);
            //atom.SetProperty(tp, 123);

            //try
            //{
            //    atom.SetProperty(tp, 123);
            //}
            //catch (CannotWriteImmutablePropertyException)
            //{
            //}
            //catch
            //{
            //    Assert.Fail();
            //}
        }

        [TestMethod]
        public void TestStructure()
        {
            var atoms = AtomCollection.Create(new IAtom[] { Atom.Create(1, ElementSymbols.C), Atom.Create(2, ElementSymbols.H), Atom.Create(3, ElementSymbols.N) });
            var bonds = BondCollection.Create(new IBond[] 
            {
                Bond.Create(atoms[0], atoms[1], BondType.Aromatic),
                Bond.Create(atoms[1], atoms[2], BondType.Aromatic),
                Bond.Create(atoms[2], atoms[0], BondType.Aromatic)
            });

            var structure = Structure.Create("testid", atoms, bonds);

            Assert.AreEqual("testid", structure.Id);
            Assert.AreEqual(3, structure.Atoms.Count);
            Assert.AreEqual(3, structure.Bonds.Count);
            for (int i = 0; i < structure.Atoms.Count; i++)
            {
                Assert.AreEqual(i + 1, structure.Atoms[i].Id);   
            }

            Assert.AreEqual(1, structure.Atoms.GetById(1).Id);
            Assert.AreEqual(2, structure.Bonds[atoms[0]].Count());
        }

        bool selected = false;

        IStructure CreateStructure()
        {
            var atoms = AtomCollection.Create(new IAtom[] { Atom.Create(1, ElementSymbols.C), Atom.Create(2, ElementSymbols.H), Atom.Create(3, ElementSymbols.N) });
            var bonds = BondCollection.Create(new IBond[] 
            {
                Bond.Create(atoms[0], atoms[1], BondType.Aromatic),
                Bond.Create(atoms[1], atoms[2], BondType.Aromatic),
                Bond.Create(atoms[2], atoms[0], BondType.Aromatic)
            });

            var structure = Structure.Create("s", atoms, bonds);

            return structure;
        }

        [TestMethod]
        public void TestResdiue()
        {
            //var atoms = AtomCollection.Create(new IAtom[] { Atom.Create(1, ElementSymbols.C), Atom.Create(2, ElementSymbols.H), Atom.Create(3, ElementSymbols.N) });
            //var bonds = BondCollection.Create(new IBond[] 
            //{
            //    Bond.Create(atoms[0], atoms[1], BondType.Aromatic),
            //    Bond.Create(atoms[1], atoms[2], BondType.Aromatic),
            //    Bond.Create(atoms[2], atoms[0], BondType.Aromatic)
            //});

            //var structure = PdbStructure.Create(Structure.Create("id", atoms, bonds), new string[] { }, new string[] { });

            //var residue = new Residue(atoms, structure);

            //atoms[1].ObservePropertyChanged(this, (s, o, a) => s.selected = (o as IAtom).IsSelected);

            //residue.IsSelected = true;
            //Assert.IsTrue(atoms[0].IsSelected);

            //Assert.IsTrue(selected);
        }

        [TestMethod]
        public void TestClone()
        {
            var s = CreateStructure();
            var prop = PropertyHelper.String("test");
            s.SetProperty(prop, "value");
         //   s.Atoms[0].SetProperty(prop, "aval");
            IStructure clone = s.Clone();
            Assert.AreEqual(s.Id, clone.Id);
            Assert.IsFalse(object.ReferenceEquals(s, clone));
            Assert.AreEqual(s.GetProperty(prop), clone.GetProperty(prop));
         //   Assert.AreEqual(s.Atoms[0].GetProperty(prop), clone.Atoms[0].GetProperty(prop));
            Assert.IsFalse(object.ReferenceEquals(s.Atoms[0], clone.Atoms[0]));
        }
    }
}
