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
// File: Result.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System.ComponentModel;

    /// <summary>
    /// Contains the data returned from a GetValue operation and the state of the operation.
    /// </summary>
    [Description("Contains the data returned from a GetValue operation and the state of the operation.")]
    public struct Result
    {
        /// <summary>
        /// Represents a Result object, whose <see cref="State"/> is <see cref="State.InvalidData"/>
        /// and <see cref="Value"/> is null.
        /// </summary>
        public static readonly Result InvalidData = new Result(State.InvalidData, null);

        /// <summary>
        /// Represents a Result object, whose <see cref="State"/> is <see cref="State.InvalidProperty"/>
        /// and <see cref="Value"/> is null.
        /// </summary>
        public static readonly Result InvalidProperty = new Result(State.InvalidProperty, null);

        /// <summary>
        /// Represents a Result object, whose <see cref="State"/> is <see cref="State.InvalidOperation"/>
        /// and <see cref="Value"/> is null.
        /// </summary>
        public static readonly Result InvalidOperation = new Result(State.InvalidOperation, null);

        /// <summary>
        /// Holds the <see cref="DataAccessLayer.Wrappers.State"/> of the operation.
        /// </summary>
        public readonly State State;

        /// <summary>
        /// Holds the value of the operation.
        /// </summary>
        public readonly object Value;

        /// <summary>
        /// Initializes a new instance of the <see cref="Result"/> struct.
        /// </summary>
        /// <param name="state">The operation state.</param>
        /// <param name="value">The value.</param>
        public Result(State state, object value)
        {
            this.State = state;
            this.Value = value;
        }

        /// <summary>
        /// Returns a <see cref="System.String"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="System.String"/> that represents this instance.
        /// </returns>
        public override string ToString()
        {
            return string.Format("'{0}' [{1}]", this.Value ?? "<null>", this.State);
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            return this.State.GetHashCode() ^ (this.Value != null ? this.Value.GetHashCode() : 0);
        }
    }
}
