namespace WebChemistry.Platform
{
    using Newtonsoft.Json;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Xml;
    using System.Xml.Linq;
    
    /// <summary>
    /// Manager base class.
    /// </summary>
    public abstract class ManagerBase<TManager, TElement, TIndex, TUpdate> : PersistentObjectBase<TManager>
        where TElement : ManagedPersistentObjectBase<TElement, TIndex, TUpdate>, new()
        where TManager : ManagerBase<TManager, TElement, TIndex, TUpdate>, new()
    {
        protected class IndexEntry
        {
            XElement xml;

            public EntityId Id { get; private set; }

            string entryString;
            TIndex entry;
            public TIndex Entry
            {
                get
                {
                    if (entry == null) entry = JsonConvert.DeserializeObject<TIndex>(entryString);
                    return entry;
                }
                private set
                {
                    entry = value;
                }
            }

            public XElement ToXml()
            {
                if (xml != null) return xml;

                xml = new XElement("Entry", 
                    new XAttribute("Id", Id.ToString()),
                    new XAttribute("Entry", JsonConvert.SerializeObject(Entry)));
                return xml;
            }

            /// <summary>
            /// Read the Id attribute.
            /// </summary>
            /// <param name="e"></param>
            /// <returns></returns>
            public static EntityId GetEntityId(XElement e)
            {
                return EntityId.Parse(e.Attribute("Id").Value);
            }

            public static IndexEntry FromXml(XElement e)
            {
                return new IndexEntry
                {
                    Id = EntityId.Parse(e.Attribute("Id").Value),
                    entryString = e.Attribute("Entry").Value,
                    xml = e
                };
            }

            public IndexEntry(EntityId id, TIndex entry)
            {
                this.Id = id;
                this.entry = entry;
            }

            private IndexEntry()
            {
            }
        }
                
        string GetIndexFilename() { return Path.Combine(CurrentDirectory, "index.xml"); }

        /// <summary>
        /// Saves the index.
        /// </summary>
        /// <param name="index"></param>
        protected void SaveIndex(List<IndexEntry> index)
        {
            var fn = GetIndexFilename();
            var doc = new XElement("Index", index.Select(e => e.ToXml()));
            using (var w = XmlWriter.Create(fn, new XmlWriterSettings() { Indent = true }))
            {
                doc.WriteTo(w);
            }
        }
                
        /// <summary>
        /// Adds an entry to the index.
        /// </summary>
        /// <param name="entry"></param>
        protected void AddToIndex(TElement element)
        {            
            var doc = GetIndexElement();
            var idString = element.Id.ToString();

            if (!doc.Elements("Entry").Any(e => e.Attribute("Id").Value.Equals(idString, StringComparison.Ordinal)))
            {
                doc.Add(new IndexEntry(element.Id, element.IndexEntry).ToXml());
                using (var w = XmlWriter.Create(GetIndexFilename(), new XmlWriterSettings() { Indent = true }))
                {
                    doc.WriteTo(w);
                }
            }
        }

        /// <summary>
        /// Removes an entry from the index.
        /// </summary>
        /// <param name="id"></param>
        protected void RemoveFromIndex(EntityId id)
        {
            var ids = id.ToString();
            var doc = GetIndexElement();
            var node = doc.Elements()
                .FirstOrDefault(e => e.Attribute("Id").Value.Equals(ids, StringComparison.Ordinal));

            if (node != null)
            {
                node.Remove();

                using (var w = XmlWriter.Create(GetIndexFilename(), new XmlWriterSettings() { Indent = true }))
                {
                    doc.WriteTo(w);
                }
            }
        }

        private XElement GetIndexElement()
        {
            var fn = GetIndexFilename();
            if (!File.Exists(fn)) return new XElement("Index");
            return XElement.Load(fn);
        }

        /// <summary>
        /// Finds element with specific child (short) Id.
        /// </summary>
        /// <param name="id"></param>
        /// <returns>Null if object is not present.</returns>
        public TElement TryGetByShortId(string id)
        {
            var e = GetIndexElement()
                .Elements()
                .FirstOrDefault(t => IndexEntry.GetEntityId(t).GetChildId().Equals(id, StringComparison.Ordinal));
            if (e == null)
            {
                try
                {
                    return LoadElement(Id.GetChildId(id));
                }
                catch
                {
                    return null;
                }
            }
            return LoadElement(IndexEntry.GetEntityId(e));
        }

        /// <summary>
        /// Reds the index.
        /// </summary>
        /// <returns></returns>
        protected List<IndexEntry> ReadIndex()
        {
            return GetIndexElement()
                .Elements()
                .Select(e => IndexEntry.FromXml(e))
                .ToList();
        }

        /// <summary>
        /// Get all databases.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<TElement> GetAll()
        {
            return GetIndexElement()
                .Elements()
                .Select(e => TryLoadElement(IndexEntry.GetEntityId(e)))
                .Where(e => e != null)
                .ToList();
        }

        /// <summary>
        /// Get all by predicate.
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public IEnumerable<TElement> GetAll(Func<TIndex, bool> predicate)
        {
            return ReadIndex()
                .Where(e => predicate(e.Entry))
                .Select(e => TryLoadElement(e.Id))
                .Where(e => e != null)
                .ToList();
        }

        /// <summary>
        /// Reads the element.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected abstract TElement LoadElement(EntityId id);

        /// <summary>
        /// Attempts to lead the element, if failed, returns null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public TElement TryLoadElement(EntityId id)
        {
            try
            {
                return LoadElement(id);
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// What to do on load .. like ensure consistency.
        /// </summary>
        protected virtual void OnManagerLoaded()
        {

        }
        
        /// <summary>
        /// Load the manager.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static new TManager Load(EntityId id)
        {
            var obj = PersistentObjectBase<TManager>.TryLoad(id);
            if (obj == null)
            {
                obj = CreateAndSave(id);
            }
            obj.OnManagerLoaded();
            return obj;
        }
                          
        /// <summary>
        /// Check if an ID is available.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        protected bool CheckIdAvailable(EntityId id)
        {
            return ReadIndex().Count(e => e.Id.Equals(id)) == 0;
        }
        
        /// <summary>
        /// Updates the element and its index entry.
        /// </summary>
        /// <param name="element"></param>
        /// <param name="model"></param>
        public void Update(TElement element, TUpdate model)
        {
            var id = element.Id.ToString();
            var doc = GetIndexElement();
            var node = doc.Elements().FirstOrDefault(e => e.Attribute("Id").Value.Equals(id, StringComparison.Ordinal));

            if (node != null)
            {
                element.UpdateAndSave(model);
                node.Attribute("Entry").SetValue(JsonConvert.SerializeObject(element.IndexEntry));

                using (var w = XmlWriter.Create(GetIndexFilename(), new XmlWriterSettings() { Indent = true }))
                {
                    doc.WriteTo(w);
                }
            }
        }
    }
}
