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
// File: SinglePropertyPathWrapper.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System.Text.RegularExpressions;
    using System;
    using System.Collections.Generic;

    public class SinglePropertyPathWrapper : ComplexPropertyPathWrapper
    {
        //protected readonly string[] properties;
        private readonly string propertyPath;

        public SinglePropertyPathWrapper(string propertyPath)
        {
            //this.properties = this.GetPropertiesFromPath(propertyPath);
            this.propertyPath = propertyPath;
        }

        public override Result GetValue(object data, params Object[] parameters)
        {
            PathReader pathReader = this.GetPathReaderFromType(data.GetType());
            if (pathReader == null)
                pathReader = this.CreatePathReader(data.GetType(), this.propertyPath);
            if (!pathReader.CanReadPath)
                return Result.InvalidProperty;

            Result result = new Result(State.OK, data);
            foreach (IPropertyWrapper property in pathReader.PropertyWrappers)
            {
                result = property.GetValue(result.Value);
                if (result.State != State.OK)
                    return result;
            }

            return result;
        }

        public override object GetValueUnsafe(object data, params Object[] parameters)
        {
            PathReader pathReader = this.GetPathReaderFromType(data.GetType());

            if (pathReader == null)
                pathReader = this.CreatePathReader(data.GetType(), this.propertyPath);
            if (!pathReader.CanReadPath)
                return Result.InvalidProperty;

            object result = data;
            foreach (IPropertyWrapper property in pathReader.PropertyWrappers)
            {
                result = property.GetValueUnsafe(result);
            }

            return result;
        }

        public override State SetValue(object data, object value)
        {
            PathReader pathReader = this.GetPathReaderFromType(data.GetType());
            if (pathReader == null)
                pathReader = this.CreatePathReader(data.GetType(), propertyPath);
            if (!pathReader.CanReadPath)
                return State.InvalidProperty;

            object result = data;
            IPropertyWrapper wrapper = pathReader.PropertyWrappers[pathReader.PropertyWrappers.Count - 1];

            for (int i = 0; i < pathReader.PropertyWrappers.Count - 1; i++)
            {
                result = pathReader.PropertyWrappers[i].GetValue(result).Value;
            }

            if (wrapper == null && result != null)
                return State.InvalidProperty;
            return wrapper.SetValue(result, value);
        }

        public override bool IsIndexed
        {
            get { throw new NotImplementedException(); }
        }
    }

    public class MultiPropertyPathWrapper : ComplexPropertyPathWrapper
    {
        public virtual bool Add(Type type, string propertyPath)
        {
            PathReader reader = this.GetPathReaderFromType(type);
            if (reader != null)
                return false;
            this.CreatePathReader(type, propertyPath);
            return true;
        }

        public override Result GetValue(object data, params Object[] parameters)
        {
            PathReader pathReader = this.GetPathReaderFromType(data.GetType());
            if (pathReader == null)
                return Result.InvalidData;
            if (!pathReader.CanReadPath)
                return Result.InvalidProperty;

            Result result = new Result(State.OK, data);
            foreach (IPropertyWrapper property in pathReader.PropertyWrappers)
            {
                if (result.State != State.OK)
                    break;

                result = property.GetValue(result.Value);
            }
            return result;
        }

        public override object GetValueUnsafe(object data, params Object[] parameters)
        {
            throw new NotImplementedException();
        }

        public override State SetValue(object data, object value)
        {
            throw new NotImplementedException();
        }

        public override bool IsIndexed
        {
            get { throw new NotImplementedException(); }
        }

        
    }

}
