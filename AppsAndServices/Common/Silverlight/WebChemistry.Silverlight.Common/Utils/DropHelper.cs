using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Linq;
using System.Windows.Ink;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using WebChemistry.Silverlight.Common.DataModel;
using WebChemistry.Framework.Core;

namespace WebChemistry.Silverlight.Common.Utils
{
    /// <summary>
    /// Drag and drop helper.
    /// </summary>
    public static class DropHelper
    {
        public async static void DoDrop<T>(SessionBase<T> session, FileInfo[] files, Action<FileInfo[]> extrasHandler = null)
            where T : StructureWrapBase<T>, new()
        {
            if (extrasHandler == null) extrasHandler = _ => { };

            if (files.Length == 1 && files[0].Extension.Equals(session.WorkspaceExtension, StringComparison.OrdinalIgnoreCase))
            {
                session.LoadWorkspace(files[0]);
                return;
            }

            var loadable = files.Where(f => SessionBase<T>.IsOpenable(f.Name)).ToArray();

            await session.Load(loadable);

            var extras = files.Where(f => !SessionBase<T>.IsOpenable(f.Name)).ToArray();

            extrasHandler(extras);
        }
    }
}
