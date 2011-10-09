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
// File: MultiPropertyWrapper.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;

    public class MultiPropertyWrapper : ComplexPropertyWrapper
    {
        public bool Add(Type type, string propertyName)
        {
            IPropertyWrapper pw = this.GetPropertyWrapper(type);

            if (pw != null)
                return false;

            IClassWrapper cw = ClassWrappers.GetForType(type);

            if (cw == null)
                return false;

            ISimplePropertyWrapper spw = cw.GetProperty(propertyName) as ISimplePropertyWrapper;

            if (spw == null)
                return false;

            this.propertyWrappers.Add(new Pair(type, spw));

            this.IsComparable = this.IsComparable && spw.IsComparable;

            return true;
        }

        public override Result GetValue(object data, params Object[] parameters)
        {
            IPropertyWrapper wrapper = GetPropertyWrapperFromData(data);

            return wrapper != null ? wrapper.GetValue(data) : Result.InvalidData;
        }

        public override object GetValueUnsafe(object data, params Object[] parameters)
        {
            return GetPropertyWrapperFromData(data).GetValueUnsafe(data);
        }

        public override State SetValue(object data, object value)
        {
            IPropertyWrapper wrapper = GetPropertyWrapperFromData(data);

            return wrapper != null ? wrapper.SetValue(data, value) : State.InvalidData;
        }

        public override bool IsIndexed
        {
            get { throw new NotImplementedException(); }
        }
    }
}
