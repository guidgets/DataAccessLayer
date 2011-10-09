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
// File: IPropertyWrapper.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;

    public interface IPropertyWrapper
    {
        bool IsComparable { get; }
        bool IsIndexed { get; }

        bool CanWorkWithInstance(object data);
        bool CanWorkWithType(Type type);
        
        Result GetValue(object data, params Object[] parameters);
        object GetValueUnsafe(object data, params Object[] parameters);
        State SetValue(object data, object value);

        int Compare(object objA, object objB);
    }

    public interface ISimplePropertyWrapper : IPropertyWrapper
    {
        Type DeclaringType { get; }
        IClassWrapper ParentWrapper { get; }
        Type PropertyType { get; }
        string PropertyName { get; }

        bool IsReadable { get; }
        bool IsWriteable { get; }
    }

    public interface IComplexPropertyWrapper : IPropertyWrapper
    {
        IPropertyWrapper GetPropertyWrapper(Type type);
        Type GetPropertyType(Type type);
        string GetPropertyName(Type type);

        bool IsReadable(Type type);
        bool IsWriteable(Type type);
    }
}
