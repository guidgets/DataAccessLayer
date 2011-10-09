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
// File: ComplexPropertyPathWrapper.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;

    public abstract class ComplexPropertyPathWrapper : IPropertyWrapper
    {
        protected class PathReader
        {
            public readonly Type Type;
            public readonly List<IPropertyWrapper> PropertyWrappers;
            public readonly bool CanReadPath;

            public PathReader(Type dataType, List<IPropertyWrapper> propertyWrappers, bool canReadPath)
            {
                this.Type = dataType;
                this.PropertyWrappers = propertyWrappers;
                this.CanReadPath = canReadPath;
            }
        }
        protected List<PathReader> pathReaders;
        private readonly Regex propertyRegex = new Regex(@"(?<index>\[.*\])|(?<property>[^.[]+)");
        private readonly Regex indexRegex = new Regex(@"\[(\'(?<string>\w*)\')|(?<numerical>\w*)\]");

        public ComplexPropertyPathWrapper()
        {
            this.pathReaders = new List<PathReader>();
        }

        protected virtual PathReader GetPathReaderFromType(Type type)
        {
            foreach (PathReader pathReader in this.pathReaders)
            {
                if (type == pathReader.Type)
                    return pathReader;
            }
            return null;
        }

        protected PathReader CreatePathReader(Type type, string propertyPath)
        {
            List<IPropertyWrapper> wrappers = new List<IPropertyWrapper>();
            IClassWrapper cw = ClassWrappers.GetForType(type);
            bool canReadPath = true;

            foreach (Match match in propertyRegex.Matches(propertyPath))
            {
                IPropertyWrapper pw = null;
                string property;
                object[] parameters;
                this.GetWrapperData(match, out property, out parameters);
                if (parameters == null)
                    pw = cw.GetProperty(property);
                else
                {
                    ISimplePropertyWrapper sw = (ISimplePropertyWrapper)cw.GetProperty(property);
                    if (sw == null || !sw.IsIndexed)
                        pw = null;
                    else
                        pw = new StaticIndexPropertyWrapper(sw, parameters);
                }

                if (pw == null || !pw.CanWorkWithType(cw.ClassType))
                {
                    canReadPath = false;
                    break;
                }

                wrappers.Add(pw);
                cw = ClassWrappers.GetForType(((ISimplePropertyWrapper)pw).PropertyType);
            }

            PathReader reader = new PathReader(type, wrappers, canReadPath);
            this.pathReaders.Add(reader);
            return reader;
        }

        private void GetWrapperData(Match match, out string property, out object[] parameters)
        {
            parameters = null;
            property = string.Empty;
            if (match.Groups["property"].Success)
            {
                property = match.Groups["property"].Value;
                return;
            }

            if (match.Groups["index"].Success)
            {
                property = "Item";
                parameters = new object[1];
                Match indexMatch = indexRegex.Match(match.Groups["index"].Value);
                if (indexMatch.Groups["string"].Success)
                    parameters[0] = indexMatch.Groups["string"].Value;

                int index;
                if (indexMatch.Groups["numerical"].Success && int.TryParse(indexMatch.Groups["numerical"].Value, out index))
                    parameters[0] = index;
                else
                    parameters = new object[0];
                return;
            }
        }

        public bool IsComparable
        {
            get { throw new System.NotImplementedException(); }
        }

        public bool CanWorkWithInstance(object data)
        {
            if (data == null)
                return false;
            return this.CanWorkWithType(data.GetType());
        }

        public bool CanWorkWithType(Type type)
        {
            PathReader reader = this.GetPathReaderFromType(type);
            return reader != null;
        }

        public abstract Result GetValue(object data, params Object[] parameters);

        public abstract object GetValueUnsafe(object data, params Object[] parameters);

        public abstract State SetValue(object data, object value);

        public int Compare(object objA, object objB)
        {
            throw new System.NotImplementedException();
        }

        public abstract bool IsIndexed
        {
            get;
        }

    }
}
