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
// File: ComplexPropertyWrapper.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;
    using System.Collections.Generic;

    public abstract class ComplexPropertyWrapper : IComplexPropertyWrapper
    {
        protected class Pair
        {
            public readonly Type Type;
            public readonly IPropertyWrapper Wrapper;

            public Pair(Type type, IPropertyWrapper wrapper)
            {
                this.Type = type;
                this.Wrapper = wrapper;
            }
        }

        protected List<Pair> propertyWrappers = new List<Pair>();

        public ComplexPropertyWrapper()
        {
            this.IsComparable = true;
        }

        public abstract Result GetValue(object data, params Object[] parameters);

        public abstract object GetValueUnsafe(object data, params Object[] parameters);

        public abstract State SetValue(object data, object value);

        public IPropertyWrapper GetPropertyWrapper(Type type)
        {
            Pair pair = GetPairFromType(type);
            return pair != null ? pair.Wrapper : null;
        }

        public Type GetPropertyType(Type type)
        {
            ISimplePropertyWrapper wrapper = (ISimplePropertyWrapper)this.GetPropertyWrapper(type);

            return wrapper != null ? wrapper.PropertyType : null;
        }
                
        protected IPropertyWrapper GetPropertyWrapperFromData(object data)
        {
            if (data == null)
                return null;

            Pair pair = GetPairFromType(data.GetType());

            return pair != null ? pair.Wrapper : null;
        }

        protected Pair GetPairFromData(object data)
        {
            if (data == null)
                return null;

            return GetPairFromType(data.GetType());
        }

        protected int GetPairIndexFromData(object data)
        {
            if (data == null)
                return -1;

            return GetPairIndexFromType(data.GetType());
        }

        protected int GetPairIndexFromType(Type type)
        {
            if (type == null)
                return -1;

            for (int i = 0; i < this.propertyWrappers.Count; i++)
                if (this.propertyWrappers[i].Type == type)
                    return i;

            return -1;
        }

        protected Pair GetPairFromType(Type type)
        {
            if (type == null)
                return null;

            foreach (Pair pair in this.propertyWrappers)
                if (pair.Type == type) return pair;

            return null;
        }

        public string GetPropertyName(Type type)
        {
            throw new NotImplementedException();
        }

        public bool IsReadable(Type type)
        {
            throw new NotImplementedException();
        }

        public bool IsWriteable(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual bool IsIndexed
        {
            get
            {
                throw new NotImplementedException();
            }
        }

        public bool IsComparable
        {
            get;
            protected set;
        }

        public bool CanWorkWithInstance(object data)
        {
            if (data == null)
                return false;

            return this.CanWorkWithType(data.GetType());
        }

        public bool CanWorkWithType(Type type)
        {
            return this.GetPairFromType(type) != null;
        }

        public virtual int Compare(object objA, object objB)
        {
            if (!this.IsComparable)
                return 0;

            int indexA = this.GetPairIndexFromData(objA);
            int indexB = this.GetPairIndexFromData(objB);
            int r = indexA != -1 ? -1 : 0;
            r += indexB != -1 ? 1 : 0;

            if (r != 0 || indexA == -1)
                return r;

            if (indexA == indexB)
                return this.propertyWrappers[indexA].Wrapper.Compare(objA, objB);

            ISimplePropertyWrapper wA = (ISimplePropertyWrapper)this.propertyWrappers[indexA].Wrapper;
            ISimplePropertyWrapper wB = (ISimplePropertyWrapper)this.propertyWrappers[indexB].Wrapper;

            r = wA != null ? -1 : 0;
            r += wB != null ? 1 : 0;

            if (r != 0 || wA == null)
                return r;

            return wA.PropertyType == wB.PropertyType ? 
                ((IComparable)wA.GetValueUnsafe(objA)).CompareTo((IComparable)wB.GetValueUnsafe(objB)) :
                indexA.CompareTo(indexB);
        }
    }
}