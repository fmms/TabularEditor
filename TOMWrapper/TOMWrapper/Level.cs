﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TOM = Microsoft.AnalysisServices.Tabular;

namespace TabularEditor.TOMWrapper
{
    public partial class Level: ITabularTableObject
    {
        internal override void AfterRemoval(ITabularObjectCollection collection)
        {
            (collection as LevelCollection).Hierarchy.CompactLevelOrdinals();
            base.AfterRemoval(collection);
        }


        internal override void ReapplyReferences()
        {
            base.ReapplyReferences();

            // Since the original column could have been deleted since the level was deleted, let's find the column by name:
            var c = (Collection as LevelCollection).Hierarchy.MetadataObject.Table.Columns[MetadataObject.Column.Name];
            MetadataObject.Column = c;

            Hierarchy.FixLevelOrder(this, this.Ordinal);
        }

        [Browsable(false)]
        public Table Table
        {
            get
            {
                return Hierarchy.Table;
            }
        }

        protected override void OnPropertyChanging(string propertyName, object newValue, ref bool undoable, ref bool cancel)
        {
            if (propertyName == Properties.ORDINAL)
            {
                // No automatic handling of Ordinal changes. We will handle it manually in the hierarchy's FixLevelOrder() method.
                cancel = true;
                this.MetadataObject.Ordinal = (int)newValue;
                Hierarchy.FixLevelOrder(this, (int)newValue);
                return;
            }
            if (propertyName == Properties.COLUMN)
            {
                if (newValue == null && !Handler.UndoManager.UndoInProgress) throw new ArgumentNullException("Column");
                if (Hierarchy.Levels.Where(l => l != this).Any(l => l.Column == newValue))
                    throw new ArgumentException(string.Format("Another level in this hierarchy is already based on column \"{0}\"", (newValue as Column).Name), "Column");
            }
            base.OnPropertyChanging(propertyName, newValue, ref undoable, ref cancel);
        }

        protected override void OnPropertyChanged(string propertyName, object oldValue, object newValue)
        {
            if (propertyName == Properties.COLUMN)
            {
                Handler.UpdateObject(this);
            }
            base.OnPropertyChanged(propertyName, oldValue, newValue);
        }
    }

    public partial class LevelCollection
    {
        internal override bool Remove(Level item)
        {
            var result = base.Remove(item);
            Handler.UpdateLevels(Hierarchy);
            return result;
        }

        internal override void Add(Level item)
        {
            base.Add(item);
            Handler.UpdateLevels(Hierarchy);
        }
    }
}
