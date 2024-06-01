using System;
using System.Windows;
using System.Windows.Controls;
using System.Linq;
using System.Reactive.Linq;
using WebChemistry.Framework.Core;
using Microsoft.Practices.ServiceLocation;
using WebChemistry.Silverlight.Common;
using WebChemistry.Silverlight.Common.Services;
using System.Windows.Documents;
using System.Windows.Media;
using System.Collections.Generic;

namespace WebChemistry.Silverlight.Controls
{
    public partial class LogControl : UserControl
    {
        //object logChangedObserver;

        public LogControl()
        {
            InitializeComponent();

            logText.Blocks.Add(paragraph);

            LogService.Default.Entries
                .ObserveOnDispatcher()
                .Subscribe(e => AddEntry(e));

            LayoutRoot.SizeChanged += LayoutRoot_SizeChanged;
        }

        void LayoutRoot_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            this.logText.MaxHeight = this.LayoutRoot.ActualHeight;
        }

        int entryCount = 0;
        Paragraph paragraph = new Paragraph() { FontSize = 12 };

        Dictionary<LogEntryType, Brush> brushes = new Dictionary<LogEntryType, Brush>
        {
            { LogEntryType.Message, new SolidColorBrush(Colors.Black) },
            { LogEntryType.Info, new SolidColorBrush(Color.FromArgb(255, 0x11, 0x9E, 0xDA)) },
            { LogEntryType.Warning, new SolidColorBrush(Color.FromArgb(255, 0xFF, 0x85, 0x40)) },
            { LogEntryType.Error, new SolidColorBrush(Color.FromArgb(255, 0xED, 0x00, 0x2F)) },
        };
        
        void AddEntry(LogService.Entry entry)
        {
            if (entryCount > 255)
            {
                entryCount = 0;
                paragraph.Inlines.Clear();
            }

            if (entryCount > 0) paragraph.Inlines.Add(new LineBreak());            
            entryCount++;

            switch (entry.Type)
            {
                case LogEntryType.Message:
                    paragraph.Inlines.Add(new Run { Text = entry.Message, Foreground = brushes[LogEntryType.Message] });
                    break;
                case LogEntryType.Info:
                    paragraph.Inlines.Add(new Run { Text = entry.Message, Foreground = brushes[LogEntryType.Info] });
                    break;
                case LogEntryType.Warning:
                    paragraph.Inlines.Add(new Run { Text = "Warning ", FontWeight = FontWeights.Bold, Foreground = brushes[LogEntryType.Warning] });
                    paragraph.Inlines.Add(new Run { Text = entry.Message, Foreground = brushes[LogEntryType.Message] });
                    break;
                case LogEntryType.Error:
                    paragraph.Inlines.Add(new Run { Text = "Error ", FontWeight = FontWeights.Bold, Foreground = brushes[LogEntryType.Error] });
                    paragraph.Inlines.Add(new Run { Text = entry.Message, Foreground = brushes[LogEntryType.Error] });
                    break;
            }

            TextPointer pstart = logText.ContentEnd;
            logText.Selection.Select(pstart, pstart);
        }
    }
}
