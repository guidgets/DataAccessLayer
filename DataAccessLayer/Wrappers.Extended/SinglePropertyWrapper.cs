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
// File: SinglePropertyWrapper.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;

    public class SinglePropertyWrapper : ComplexPropertyWrapper
    {
        private string propertyName = string.Empty;

        public SinglePropertyWrapper(string propertyName)
        {
            this.propertyName = propertyName;
            this.IsComparable = true;
        }

        public override Result GetValue(object data, params Object[] parameters)
        {
            if (data == null)
                return Result.InvalidData;

            Pair pair = GetPairFromType(data.GetType());

            if (pair == null)
            {
                pair = AddPropertyWrapper(data.GetType());

                if (pair == null)
                    return Result.InvalidData;
            }

            if (pair.Wrapper == null)
                return Result.InvalidProperty;

            return pair.Wrapper != null ? pair.Wrapper.GetValue(data) : Result.InvalidProperty;
        }

        public override object GetValueUnsafe(object data, params Object[] parameters)
        {
            Pair pair = GetPairFromType(data.GetType());

            if (pair == null)
                pair = AddPropertyWrapper(data.GetType());

            return pair.Wrapper.GetValueUnsafe(data);
        }

        public override State SetValue(object data, object value)
        {
            if (data == null)
                return State.InvalidData;

            Pair pair = GetPairFromType(data.GetType());

            if (pair == null)
            {
                pair = AddPropertyWrapper(data.GetType());

                if (pair == null)
                    return State.InvalidData;
            }

            if (pair.Wrapper == null)
                return State.InvalidProperty;

            return pair.Wrapper != null ? pair.Wrapper.SetValue(data, value) : State.InvalidProperty;
        }

        private Pair AddPropertyWrapper(Type type)
        {
            IClassWrapper cw = ClassWrappers.GetForType(type);

            if (cw == null)
                return null;

            ISimplePropertyWrapper pw = (ISimplePropertyWrapper)cw.GetProperty(this.propertyName);

            Pair res = new Pair(type, pw);

            this.propertyWrappers.Add(res);

            if (pw != null)            
                this.IsComparable = this.IsComparable && pw.IsComparable;

            return res;
        }

        public override bool IsIndexed
        {
            get { throw new NotImplementedException(); }
        }
    }
}
