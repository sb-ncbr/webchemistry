////using GalaSoft.MvvmLight.Command;
////using System;
////using System.Collections.Generic;
////using System.Net;
////using System.Windows;
////using System.Windows.Controls;
////using System.Windows.Documents;
////using System.Windows.Ink;
////using System.Windows.Input;
////using System.Linq;
////using System.Windows.Media;
////using System.Windows.Media.Animation;
////using System.Windows.Shapes;
////using WebChemistry.Charges.Core;
////using WebChemistry.Framework.Core;
////using WebChemistry.Framework.Visualization;
////using System.Text;
////using WebChemistry.Silverlight.Common.Services;

////namespace WebChemistry.Charges.Silverlight.DataModel
////{
////    public class ParameterSetWrap : InteractiveObject
////    {
////        private ICommand _setCurrentCommand;
////        public ICommand SetCurrentCommand
////        {
////            get
////            {
////                _setCurrentCommand = _setCurrentCommand ?? new RelayCommand(() => Session.CurrentSet = this);
////                return _setCurrentCommand;
////            }
////        }

////        private ICommand _copyToClipboardCommand;
////        public ICommand CopyToClipboardCommand
////        {
////            get
////            {
////                _copyToClipboardCommand = _copyToClipboardCommand ?? new RelayCommand(() => CopyToClipboard());
////                return _copyToClipboardCommand;
////            }
////        }


////        public Session Session { get; set; }
////        public EemParameterSet Set { get; set; }

////        public class ParameterRow
////        {
////            public string ElementSymbol { get; set; }
////            public int Multiplicity { get; set; }
////            public double A { get; set; }
////            public double B { get; set; }
////        }

////        public IEnumerable<ParameterRow> FormattedParamsList
////        {
////            get
////            {
////                var ret = Set.Parameters
////                    .GroupBy(row => row.Key.ElementSymbol)
////                    .OrderBy(group => group.Key)
////                    .Select(group => group.OrderBy(p => p.Key.Multiplicity))
////                    .SelectMany(p =>
////                        EnumerableEx.Return(new ParameterRow() { ElementSymbol = p.First().Key.ElementSymbol.ToString(), Multiplicity = p.First().Key.Multiplicity, A = p.First().Value.A, B = p.First().Value.B })
////                        .Concat(p.Skip(1).Select(e => new ParameterRow() { ElementSymbol = "", Multiplicity = e.Key.Multiplicity, A = e.Value.A, B = e.Value.B }))
////                    );

////                return ret;
////            }
////        }


////        private Color _color;
////        public Color Color
////        {
////            get
////            {
////                return _color;
////            }

////            set
////            {
////                if (_color == value) return;

////                _color = value;
////                NotifyPropertyChanged("Color");
////            }
////        }

////        void CopyToClipboard()
////        {
////            try
////            {
////                StringBuilder ret = new StringBuilder();

////                ret.AppendLine("Name: " + Set.Name);
////                Set.Properties.ForEach(p => ret.AppendLine(p.Key + ": " + p.Value));
////                ret.AppendLine("Kappa: " + Set.Kappa.ToStringInvariant("0.000000000"));
////                ret.AppendLine("Parameters:");
////                FormattedParamsList.ForEach(p => ret.AppendLine(p.ElementSymbol + "\t" + p.Multiplicity + "\t" + p.A.ToStringInvariant("0.000000000") + "\t" + p.B.ToStringInvariant("0.000000000")));

////                Clipboard.SetText(ret.ToString());

////                LogService.Default.AddEntry("Copy successful.");
////            }
////            catch (Exception e)
////            {
////                LogService.Default.AddEntry("Copy failed: {0}", e.Message);
////            }

            
////        }

////        public ParameterSetWrap()
////        {
////            Color = Pallete.GetRandomColor();
////        }
////    }
////}
