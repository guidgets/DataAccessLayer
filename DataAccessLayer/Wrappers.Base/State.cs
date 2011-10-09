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
// File: State.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;
    using System.ComponentModel;

    /// <summary>
    /// Represents the state of a Get or Set operation.
    /// </summary>
    [Flags]
    public enum State
    {
        /// <summary>
        /// Operation is successful.
        /// </summary>
        [Description("Operation is successful.")]
        OK = 0,

        /// <summary>
        /// Supplied data object is of invalid type.
        /// </summary>
        [Description("Supplied data object is of invalid type.")]
        InvalidData = 0x01,

        /// <summary>
        /// Supplied value is of incorrect type.
        /// </summary>
        [Description("Supplied value is of incorrect type.")]
        InvalidValue = 0x02,

        /// <summary>
        /// The operation is not applicable.
        /// </summary>
        [Description("The operation is not applicable for the data.")]
        InvalidOperation = 0x04,

        /// <summary>
        /// Supplied property name or index does not correspond to any property.
        /// </summary>
        [Description("Supplied property name or index does not correspond to any property.")]
        InvalidProperty = 0x08,
        
        /// <summary>
        /// Suppied property index is not valid
        /// </summary>
        [Description("Suppied property index is not valid")]
        InvalidPropertyIndex = 0x16
    }
}
