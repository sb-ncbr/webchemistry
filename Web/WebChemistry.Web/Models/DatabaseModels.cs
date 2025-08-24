using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using WebChemistry.Platform;
using WebChemistry.Platform.MoleculeDatabase;
using WebChemistry.Platform.MoleculeDatabase.Filtering;

namespace WebChemistry.Web.Models
{
    public class CreateOrUpdateDatabaseModel
    {
        [Required(ErrorMessage = "Please enter the database name.")]
        [Display(Name="Name")]
        [MaxLength(80)]
        public string Name { get; set; }

        [Display(Name = "Description")]
        [DataType(DataType.MultilineText)]
        [MaxLength(500)]
        public string Description { get; set; }
    }

    public class DatabaseListModel
    {
        public IList<Tuple<DatabaseInfo, DatabaseStatistics>> Databases { get; set; }
    }

    public class CreateDatabaseViewModel
    {
        public string ViewName { get; set; }
        public string Description { get; set; }
        public EntityId DatabaseId { get; set; }
        public EntryFilter[] Filters { get; set; }
    }

    public class DatabaseViewListModel
    {
        public class Entry
        {
            public bool IsDefault { get; set; }
            public DatabaseView View { get; set; }
            public DatabaseInfo Database { get; set; }
            public DatabaseViewStatistics Stats { get; set; }
            public int FilterCount { get; set; }
            public string FilterStringHtml { get; set; }
        }

        public IList<Entry> Entries { get; set; }
    }


    public class ViewListEntryModel
    {
        public string Id { get; set; }
        public bool IsPublic { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public string DatabaseName { get; set; }
        public string Description { get; set; }
        public string ShortDescription { get; set; }
        public int Size { get; set; }
    }
}