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
// File: IClassWrapper.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    public interface IClassWrapper : IEnumerable<IPropertyWrapper>
    {
        /// <summary>
        /// Gets the wrapped class <see cref="Type"/>.
        /// </summary>
        Type ClassType { get; }

        /// <summary>
        /// Gets the wrapper for the base class.
        /// </summary>
        IClassWrapper BaseWrapper { get; }

        /// <summary>
        /// Retrieves the index of the property wrapper, given the <paramref name="propertyName"/>.
        /// </summary>
        /// <param name="propertyName">The name of the property, whose wrapper index is to be retrieved.</param>
        /// <returns>The index of the property wrapper.</returns>
        int GetIndex(string propertyName);

        /// <summary>
        /// Gets property wrapper for a property, given the <paramref name="propertyName"/>
        /// </summary>
        /// <param name="propertyName">The property name.</param>
        /// <returns>The <see cref="IPropertyWrapper"/> operating on the property.</returns>
        IPropertyWrapper GetProperty(string propertyName);

        /// <summary>
        /// Gets property wrapper for a property, given the <paramref name="propertyIndex"/>
        /// </summary>
        /// <param name="propertyIndex">The property index.</param>
        /// <returns>The <see cref="IPropertyWrapper"/> operating on the property.</returns>
        IPropertyWrapper GetProperty(int propertyIndex);

        /// <summary>
        /// Extracts value from the <paramref name="data"/> object.
        /// </summary>
        /// <param name="propertyIndex">The property index. See <see cref="GetProperty(int)"/>.</param>
        /// <param name="data">The data object.</param>
        /// <param name="parameters">The parameters passed to the property.</param>
        /// <returns><see cref="Result"/> containing operation state and value.</returns>
        /// <seealso cref="GetProperty(int)"/>
        Result GetValue(int propertyIndex, object data, params Object[] parameters);

        /// <summary>
        /// Extracts value from the <paramref name="data"/> object.
        /// </summary>
        /// <param name="propertyName">The name of the property, whose value is to be extracted.</param>
        /// <param name="data">The data object.</param>
        /// <param name="parameters">The parameters passed to the property.</param>
        /// <returns><see cref="Result"/> containing operation state and value.</returns>
        /// <seealso cref="GetProperty(string)"/>
        Result GetValue(string propertyName, object data, params Object[] parameters);

        /// <summary>
        /// Sets value to the <paramref name="data"/> object.
        /// </summary>
        /// <param name="propertyIndex">The property index.</param>
        /// <param name="data">The data object.</param>
        /// <param name="value">The value to be set.</param>
        /// <returns><see cref="State"/> of the operation.</returns>
        State SetValue(int propertyIndex, object data, object value);

        /// <summary>
        /// Sets value to the <paramref name="data"/> object.
        /// </summary>
        /// <param name="propertyName">The name of the property, whose value is to be set.</param>
        /// <param name="data">The data object.</param>
        /// <param name="value">The value to be set.</param>
        /// <returns><see cref="State"/> of the operation.</returns>
        State SetValue(string propertyName, object data, object value);

        /// <summary>
        /// Gets the number of <see cref="IPropertyWrapper"/> objects of the wrapped class.
        /// </summary>
        int Count { get; }
    }
}
