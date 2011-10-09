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
// File: ClassWrappers.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Reflection;
    using System.Reflection.Emit;
    
    public static partial class ClassWrappers
    {

        private static IDictionary<Type, IClassWrapper> generatedWrappersPool;
        private static AssemblyName assemblyName;
        private static AssemblyBuilder assemblyBuilder;
        private static ModuleBuilder moduleBuilder;

        static ClassWrappers()
        {
            ClassWrappers.generatedWrappersPool = new Dictionary<Type, IClassWrapper>();
            ClassWrappers.assemblyName = new AssemblyName("DataLayerExtender_" + typeof(ClassWrappers).GetHashCode());

            ClassWrappers.assemblyBuilder = AppDomain.CurrentDomain.DefineDynamicAssembly(
                ClassWrappers.AssemblyName, AssemblyBuilderAccess.Run
                );

            ClassWrappers.moduleBuilder = ClassWrappers.AssemblyBuilder.DefineDynamicModule(ClassWrappers.AssemblyName.Name);
        }

        private static partial class ClassWrapperGenerator { };

        private static partial class PropertyWrapperGenerator { };

        private static AssemblyName AssemblyName
        {
            get { return ClassWrappers.assemblyName; }
        }

        private static AssemblyBuilder AssemblyBuilder
        {
            get { return ClassWrappers.assemblyBuilder; }
        }

        private static ModuleBuilder ModuleBuilder
        {
            get { return ClassWrappers.moduleBuilder; }
        }

        public static IClassWrapper GetForInstance(object objectInstance)
        {
            return objectInstance != null ? ClassWrappers.GetForType(objectInstance.GetType()) : null;
        }

        public static IClassWrapper GetForType(Type objectType)
        {
            if (objectType == null)
                return null;

            IClassWrapper wrapper = null;
            
            if (!generatedWrappersPool.TryGetValue(objectType, out wrapper))
            {
                wrapper = ClassWrapperGenerator.GenerateClassWrapper(objectType);
                ClassWrappers.generatedWrappersPool.Add(objectType, wrapper);
            }

            return wrapper;
        }

        private static Type[] GetParameterTypes(ParameterInfo[] parameters)
        {
            Type[] paramTypes = new Type[parameters.Length];

            for (int i = 0; i < paramTypes.Length; i++)
                paramTypes[i] = parameters[i].ParameterType;

            return paramTypes;
        }
    }
}