using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace WebChemistry.MotiveValidator.Database
{
    public class MotiveValidatorDatabaseInterface
    {
        // ZIP File
        // ...

        // Contains map of entries in a zip file for fast lookup.
        Dictionary<string, object /* ZipEntry */> EntryMap;
    }
}
