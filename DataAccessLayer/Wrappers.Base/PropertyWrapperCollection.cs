// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 3 of the License, or (at your option) any later version.
// 
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
// 
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the
// Free Software Foundation, Inc., 59 Temple Place - Suite 330,
// Boston, MA 02111-1307, USA.
// 
// File: PropertyWrapperCollection.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public class PropertyWrapperCollection : ICollection<IPropertyWrapper>
    {
        private int[] baseIndices;
        private int current = 0;

        protected PropertyWrapperCollection[] parentCollections = null;
        protected IPropertyWrapper[] propertyWrappers;

        private IEnumerator<PropertyWrapperCollection> RetrieveParentCollections()
        {
            foreach (PropertyWrapperCollection wrapperCollection in this.parentCollections)
                yield return wrapperCollection;
        }

        public PropertyWrapperCollection(int initialPropertiesCount, IList<PropertyWrapperCollection> parentCollections)
        {
            this.propertyWrappers = new IPropertyWrapper[initialPropertiesCount];

            List<PropertyWrapperCollection> parentCollectionsFilter = new List<PropertyWrapperCollection>();

            ProcessParentCollections(parentCollectionsFilter, parentCollections);
            foreach (PropertyWrapperCollection wrapper in parentCollections)
                ProcessParentCollections(parentCollectionsFilter, wrapper.parentCollections);

            this.parentCollections = new PropertyWrapperCollection[parentCollectionsFilter.Count];
            this.baseIndices = new int[parentCollectionsFilter.Count + 1];

            for (int i = 0; i < parentCollectionsFilter.Count; i++)
            {
                this.parentCollections[i] = parentCollectionsFilter[i];
                this.baseIndices[i + 1] = parentCollectionsFilter[i].propertyWrappers.Length;
            }

            for (int i = 2; i < baseIndices.Length; i++)
                this.baseIndices[i] += this.baseIndices[i - 1];
        }

        public IPropertyWrapper this[int index]
        {
            get { return this.GetAtIndex(index); }
        }

        private int BaseIndex
        {
            get { return baseIndices[baseIndices.Length - 1]; }
        }

        public int IndexOf(IPropertyWrapper item)
        {
            for (int i = 0; i < this.propertyWrappers.Length; i++)
                if (propertyWrappers[i] == item) return i + this.BaseIndex;

            for (int j = 0; j < this.parentCollections.Length; j++)
            {
                PropertyWrapperCollection wrapperCollection = this.parentCollections[j];
                for (int i = 0; i < wrapperCollection.propertyWrappers.Length; i++)
                    if (wrapperCollection.propertyWrappers[i] == item) return i + this.baseIndices[j];
            }

            return -1;
        }

        public void Add(IPropertyWrapper item)
        {
            this.propertyWrappers[this.current++] = item;
        }

        public void AddRange(ICollection<IPropertyWrapper> collection)
        {
            foreach (IPropertyWrapper wrapper in collection)
                this.Add(wrapper);
        }

        public void Clear()
        {
            for (int i = 0; i < this.propertyWrappers.Length; i++)
                this.propertyWrappers[i] = null;

            this.propertyWrappers = null;
        }

        public bool Contains(IPropertyWrapper item)
        {
            return this.IndexOf(item) != -1;
        }

        public void CopyTo(IPropertyWrapper[] array, int arrayIndex)
        {
            if (array.Length < arrayIndex + this.Count)
                throw new ArgumentOutOfRangeException();

            int baseIndex = arrayIndex;
            foreach (PropertyWrapperCollection collection in this.parentCollections)
            {
                collection.propertyWrappers.CopyTo(array, baseIndex);
                baseIndex += collection.propertyWrappers.Length;
            }

            this.propertyWrappers.CopyTo(array, this.BaseIndex + arrayIndex);
        }

        public int Count
        {
            get { return this.BaseIndex + this.propertyWrappers.Length; }
        }

        public IPropertyWrapper GetAtIndex(int index)
        {
            if (index < 0 || index >= this.Count)
                return null;

            if (index >= this.BaseIndex)
                return this.propertyWrappers[index - this.BaseIndex];

            int i = this.baseIndices.Length - 1;
            while (index < this.baseIndices[i]) i--;

            return this.parentCollections[i].propertyWrappers[index - this.baseIndices[i]];
        }

        public bool IsReadOnly
        {
            get { return false; }
        }

        public bool Remove(IPropertyWrapper item)
        {
            return false;
        }

        public IEnumerator<IPropertyWrapper> GetEnumerator()
        {
            foreach (PropertyWrapperCollection collection in this.parentCollections)
                foreach (IPropertyWrapper wrapper in collection.propertyWrappers)
                    yield return wrapper;


            foreach (IPropertyWrapper wrapper in this.propertyWrappers)
                yield return wrapper;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public IPropertyWrapper GetPropertyWrapper(string propertyName)
        {
            foreach (ISimplePropertyWrapper wrapper in this)
            {
                if (string.Compare(propertyName, wrapper.PropertyName) == 0)
                    return wrapper;
            }

            return null;
        }

        public int GetPropertyWrapperIndex(string propertyName)
        {
            IPropertyWrapper wrapper = this.GetPropertyWrapper(propertyName);

            return wrapper != null ? this.IndexOf(wrapper) : -1;
        }

        private static void ProcessParentCollections(IList<PropertyWrapperCollection> parentCollectionsFilter, IEnumerable<PropertyWrapperCollection> parentCollections)
        {
            foreach (PropertyWrapperCollection wrapper in parentCollections)
            {
                if (!parentCollectionsFilter.Contains(wrapper))
                    parentCollectionsFilter.Add(wrapper);
            }
        }
    }
}
