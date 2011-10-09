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
// File: StaticIndexPropertyWrapper.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;

    public class StaticIndexPropertyWrapper : ISimplePropertyWrapper
    {
        private ISimplePropertyWrapper wrapper;
        private object[] parameters;
        
        public StaticIndexPropertyWrapper(ISimplePropertyWrapper propertyWrapper, params object[] parameters)
        {
            this.wrapper = propertyWrapper;
            this.parameters = parameters;
        }

        public Result GetValue(object data, params object[] parameters)
        {
           return this.wrapper.GetValue(data, this.parameters);
        }

        public object GetValueUnsafe(object data, params object[] parameters)
        {
           return this.wrapper.GetValueUnsafe(data, this.parameters);
        }

        public State SetValue(object data, object value)
        {
            throw new System.NotImplementedException();
        }

        public Type DeclaringType
        {
            get { return this.wrapper.DeclaringType; }
        }

        public IClassWrapper ParentWrapper
        {
            get { return this.wrapper.ParentWrapper; }
        }

        public Type PropertyType
        {
            get { return this.wrapper.PropertyType; }
        }

        public string PropertyName
        {
            get { return this.wrapper.PropertyName; }
        }

        public bool IsReadable
        {
            get { return this.wrapper.IsReadable; }
        }

        public bool IsWriteable
        {
            get { return this.wrapper.IsReadable; }
        }

        public bool IsComparable
        {
            get { return this.wrapper.IsComparable; }
        }

        public bool IsIndexed
        {
            get { return this.wrapper.IsIndexed; }
        }

        public bool CanWorkWithInstance(object data)
        {
            return this.CanWorkWithInstance(data);
        }

        public bool CanWorkWithType(Type type)
        {
            return this.wrapper.CanWorkWithType(type);
        }

        public int Compare(object objA, object objB)
        {
            throw new NotImplementedException();
        }

    }
}
