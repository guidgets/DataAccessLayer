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
// File: ClassWrappers.ClassWrapperGenerator.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    public static partial class ClassWrappers
    {
        private static partial class ClassWrapperGenerator
        {
            public static IClassWrapper GenerateClassWrapper(Type objectType)
            {
                Type wrapperType = ClassWrapperGenerator.GenerateClassWrapperType(objectType);

                object t = Activator.CreateInstance(wrapperType);
                return wrapperType != null ? Activator.CreateInstance(wrapperType) as IClassWrapper : null;
            }

            private static Type GenerateClassWrapperType(Type dataType)
            {
                IClassWrapper baseWrapper = ClassWrappers.GetForType(dataType.BaseType);

                TypeBuilder newType = ClassWrappers.ModuleBuilder.DefineType(
                    string.Concat(dataType.Name, dataType.GetHashCode().ToString("X08")),
                    TypeAttributes.Public);
                
                newType.AddInterfaceImplementation(typeof(IClassWrapper));

                FieldBuilder propertyWrappersField = newType.DefineField("propertyWrappers", typeof(PropertyWrapperCollection), FieldAttributes.Public | FieldAttributes.Static);
                List<Type> propertyWrapperClasses = PropertyWrapperGenerator.GeneratePropertyWrappers(newType, baseWrapper, dataType.GetProperties(), dataType);
                object[] baseOTypes = baseWrapper != null ? new object[] { baseWrapper } : new object[0];
                GenerateWrapperConstructor(newType);
                GenerateStaticWrapperConstructor(newType, baseOTypes, propertyWrappersField, propertyWrapperClasses);

                GenerateCountProperty(newType, propertyWrappersField);
                GenerateGetPropertyWrapperByPropertyName(newType, propertyWrappersField);
                GenerateGetPropertyWrapperByPropertyIndex(newType, propertyWrappersField);
                GenerateWrappedClassType(newType, dataType);
                GenerateBaseClassWrapper(newType, dataType);
                GenerateGetPropertyIndex(newType, propertyWrappersField);
                GenerateGetPropertyValueByPropertyIndex(newType, propertyWrappersField);
                GenerateGetPropertyValueByPropertyName(newType, propertyWrappersField);
                GenerateSetPropertyValueByPropertyIndex(newType, propertyWrappersField);
                GenerateSetPropertyValueByPropertyName(newType, propertyWrappersField);
                GenerateIEnumerableIPropertyWrapper(newType, typeof(IEnumerable<IPropertyWrapper>), propertyWrappersField);
                GenerateIEnumerableIPropertyWrapper(newType, typeof(IEnumerable), propertyWrappersField);

                return newType.CreateType();
            }

            private static void GenerateIEnumerableIPropertyWrapper(TypeBuilder newType, Type interfaceType, FieldBuilder propertyWrappersField)
            {
                MethodInfo getEnumerator = typeof(IEnumerable<IPropertyWrapper>).GetMethod("GetEnumerator", Type.EmptyTypes);

                MethodBuilder builder = DefineMethod(
                    newType,
                    interfaceType, 
                    "GetEnumerator");
                ILGenerator g = builder.GetILGenerator();
                g.Emit(OpCodes.Ldsfld, propertyWrappersField);
                g.Emit(OpCodes.Callvirt, getEnumerator);
                g.Emit(OpCodes.Ret);
            }


            private static void GenerateSetPropertyValueByPropertyName(TypeBuilder newType, FieldBuilder propertyWrappersField)
            {
                MethodBuilder builder = DefineMethod(newType, "SetValue", new Type[] { typeof(string), typeof(object), typeof(object) });
                MethodInfo setPropertyValue = typeof(IClassWrapper).GetMethod("SetValue", new Type[] { typeof(int), typeof(object), typeof(object) });
                MethodInfo getPropertyIndex = typeof(IClassWrapper).GetMethod("GetIndex");

                ILGenerator g = builder.GetILGenerator();
                g.DeclareLocal(typeof(int));
                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, getPropertyIndex);
                g.Emit(OpCodes.Stloc_0);
                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Ldarg_2);
                g.Emit(OpCodes.Ldarg_3);
                g.Emit(OpCodes.Callvirt, setPropertyValue);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateSetPropertyValueByPropertyIndex(TypeBuilder newType, FieldBuilder propertyWrappersField)
            {
                MethodBuilder builder = DefineMethod(newType, "SetValue", new Type[] { typeof(int), typeof(object), typeof(object) });
                MethodInfo getPropertyWrapperMethodInfo = typeof(IClassWrapper).GetMethod("GetProperty", new Type[] { typeof(int) });
                MethodInfo setValue = typeof(IPropertyWrapper).GetMethod("SetValue", new Type[] { typeof(object), typeof(object) });

                ILGenerator g = builder.GetILGenerator();
                Label end = g.DefineLabel();
                Label invalidProperty = g.DefineLabel();
                g.DeclareLocal(typeof(IPropertyWrapper));
                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, getPropertyWrapperMethodInfo);
                g.Emit(OpCodes.Stloc_0);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Brfalse_S, invalidProperty);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Ldarg_2);
                g.Emit(OpCodes.Ldarg_3);
                g.Emit(OpCodes.Callvirt, setValue);
                g.Emit(OpCodes.Br_S, end);
                g.MarkLabel(invalidProperty);
                g.Emit(OpCodes.Ldc_I4, (int)State.InvalidProperty);
                g.MarkLabel(end);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateGetPropertyValueByPropertyName(TypeBuilder newType, FieldBuilder propertyWrappersField)
            {
                MethodBuilder builder = DefineMethod(newType, "GetValue", new Type[] { typeof(string), typeof(object), typeof(object[]) });
                MethodInfo getPropertyIndex = typeof(IClassWrapper).GetMethod("GetIndex");
                MethodInfo GetPropertyValueByID = typeof(IClassWrapper).GetMethod("GetValue", new Type[] { typeof(int), typeof(object), typeof(object[]) });
                ILGenerator g = builder.GetILGenerator();
                g.DeclareLocal(typeof(int));

                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, getPropertyIndex);
                g.Emit(OpCodes.Stloc_0);
                g.Emit(OpCodes.Ldarg_0);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Ldarg_2);
                g.Emit(OpCodes.Ldarg_3);
                g.Emit(OpCodes.Callvirt, GetPropertyValueByID);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateGetPropertyValueByPropertyIndex(TypeBuilder newType, FieldBuilder propertyWrappers)
            {
                MethodBuilder builder = DefineMethod(newType, "GetValue", new Type[] { typeof(int), typeof(object), typeof(object[])});
                ILGenerator g = builder.GetILGenerator();
                MethodInfo getGetAtIndex = typeof(PropertyWrapperCollection).GetMethod("GetAtIndex");
                ConstructorInfo resultConstructor = typeof(Result).GetConstructor(new Type[] { typeof(State), typeof(object) });
                MethodInfo getValue = typeof(IPropertyWrapper).GetMethod("GetValue");
                Label loadInvalidProperty = g.DefineLabel();
                Label end = g.DefineLabel();
                g.DeclareLocal(typeof(IPropertyWrapper));

                g.Emit(OpCodes.Ldsfld, propertyWrappers);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, getGetAtIndex);
                g.Emit(OpCodes.Stloc_0);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Brfalse_S, loadInvalidProperty);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Ldarg_2);
                g.Emit(OpCodes.Ldarg_3);
                g.Emit(OpCodes.Callvirt, getValue);
                g.Emit(OpCodes.Br_S, end);
                g.MarkLabel(loadInvalidProperty);
                g.Emit(OpCodes.Ldc_I4, (int)State.InvalidProperty);
                g.Emit(OpCodes.Ldnull);
                g.Emit(OpCodes.Newobj, resultConstructor);
                g.MarkLabel(end);
                g.Emit(OpCodes.Ret);

            }

            private static void GenerateGetPropertyIndex(TypeBuilder newType, FieldBuilder propertyWrappers)
            {
                MethodBuilder builder = DefineMethod(newType, "GetIndex");
                ILGenerator g = builder.GetILGenerator();
                MethodInfo getPropertyWrapperIndex = typeof(PropertyWrapperCollection).GetMethod("GetPropertyWrapperIndex");
                g.Emit(OpCodes.Ldsfld, propertyWrappers);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, getPropertyWrapperIndex);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateBaseClassWrapper(TypeBuilder newType, Type dataType)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "BaseWrapper");
                ILGenerator g = builder.GetILGenerator();
                MethodInfo getHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                MethodInfo getBaseType = typeof(Type).GetProperty("BaseType").GetGetMethod();
                MethodInfo getWrapperForType = typeof(ClassWrappers).GetMethod("GetForType");

                Label loadNull = g.DefineLabel();
                Label end = g.DefineLabel();
                g.DeclareLocal(typeof(Type));

                g.Emit(OpCodes.Ldtoken, dataType);
                g.Emit(OpCodes.Call, getHandle);
                g.Emit(OpCodes.Callvirt, getBaseType);
                g.Emit(OpCodes.Stloc_0);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Brfalse_S, loadNull);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Call, getWrapperForType);
                g.Emit(OpCodes.Br_S, end);
                g.MarkLabel(loadNull);
                g.Emit(OpCodes.Ldnull);
                g.MarkLabel(end);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateWrappedClassType(TypeBuilder newType, Type dataType)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "ClassType");

                MethodInfo getHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                ILGenerator g = builder.GetILGenerator();
                g.Emit(OpCodes.Ldtoken, dataType);
                g.Emit(OpCodes.Call, getHandle);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateGetPropertyWrapperByPropertyName(TypeBuilder newType, FieldBuilder propertyWrappersField)
            {
                MethodBuilder getWrapperBuilder = DefineMethod(newType, "GetProperty", new Type[] { typeof(string) });
                ILGenerator g = getWrapperBuilder.GetILGenerator();
                MethodInfo getWrapperMethod = typeof(PropertyWrapperCollection).GetMethod("GetPropertyWrapper", new Type[] { typeof(string) });
                g.Emit(OpCodes.Ldsfld, propertyWrappersField);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, getWrapperMethod);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateGetPropertyWrapperByPropertyIndex(TypeBuilder newType, FieldBuilder propertyWrappersField)
            {
                MethodBuilder getWrapperBuilder = DefineMethod(newType, "GetProperty", new Type[] { typeof(int) });
                ILGenerator g = getWrapperBuilder.GetILGenerator();
                MethodInfo getWrapperMethod = typeof(PropertyWrapperCollection).GetMethod("GetAtIndex", new Type[] { typeof(int) });
                g.Emit(OpCodes.Ldsfld, propertyWrappersField);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, getWrapperMethod);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateCountProperty(TypeBuilder newType, FieldBuilder propertyWrappersField)
            {
                PropertyInfo countInfo = typeof(IClassWrapper).GetProperty("Count");
                MethodInfo countGetInfo = countInfo.GetGetMethod();

                MethodBuilder countGetter = newType.DefineMethod(
                    string.Concat(typeof(IClassWrapper).Name, '.', countGetInfo.Name),
                        MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                        countGetInfo.ReturnType,
                        ClassWrappers.GetParameterTypes(countGetInfo.GetParameters())
                        );

                newType.DefineMethodOverride(countGetter, countGetInfo);
                ILGenerator g = countGetter.GetILGenerator();

                MethodInfo countMethod = typeof(PropertyWrapperCollection).GetProperty("Count").GetGetMethod();
                g.Emit(OpCodes.Ldsfld, propertyWrappersField);
                g.Emit(OpCodes.Callvirt, countMethod);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateWrapperConstructor(TypeBuilder newType)
            {
                ConstructorBuilder ctor = newType.DefineDefaultConstructor(MethodAttributes.Public);
            }

            private static void GenerateStaticWrapperConstructor(TypeBuilder newType, object[] baseTypes, FieldBuilder propertyWrappersField, List<Type> propertyWrapperClasses)
            {

                ConstructorBuilder staticConstructor = newType.DefineConstructor(MethodAttributes.Static, CallingConventions.Standard, Type.EmptyTypes);
                MethodInfo listAddMethod = typeof(List<PropertyWrapperCollection>).GetMethod("Add");
                ConstructorInfo createList = typeof(List<PropertyWrapperCollection>).GetConstructor(Type.EmptyTypes);
                ILGenerator g = staticConstructor.GetILGenerator();
                staticConstructor.InitLocals = true;
                g.DeclareLocal(typeof(List<PropertyWrapperCollection>)); // local 0
                g.DeclareLocal(typeof(IPropertyWrapper)); // local 1

                g.Emit(OpCodes.Newobj, createList);
                g.Emit(OpCodes.Stloc_0);
                foreach (object baseType in baseTypes)
                {
                    FieldInfo fi = baseType.GetType().GetField("propertyWrappers", BindingFlags.Public | BindingFlags.Static);
                    g.Emit(OpCodes.Ldloc_0);
                    g.Emit(OpCodes.Ldsfld, fi);
                    g.Emit(OpCodes.Call, listAddMethod);
                }

                ConstructorInfo pwc_ctor = typeof(PropertyWrapperCollection).GetConstructor(new Type[] { typeof(int), typeof(List<PropertyWrapperCollection>) });
                g.Emit(OpCodes.Ldc_I4, propertyWrapperClasses.Count);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Newobj, pwc_ctor);
                g.Emit(OpCodes.Stsfld, propertyWrappersField);

                MethodInfo addToList = typeof(PropertyWrapperCollection).GetMethod("Add");

                foreach (Type propertyWrapper in propertyWrapperClasses)
                {
                    ConstructorInfo ctor = propertyWrapper.GetConstructor(Type.EmptyTypes);
                    Label continueProgram = g.DefineLabel();
                    g.Emit(OpCodes.Newobj, ctor);
                    g.Emit(OpCodes.Isinst, typeof(IPropertyWrapper));
                    g.Emit(OpCodes.Stloc_1);
                    g.Emit(OpCodes.Ldloc_1);
                    g.Emit(OpCodes.Brfalse_S, continueProgram);
                    g.Emit(OpCodes.Ldsfld, propertyWrappersField);
                    g.Emit(OpCodes.Ldloc_1);
                    g.Emit(OpCodes.Callvirt, addToList);
                    g.MarkLabel(continueProgram);
                }

                g.Emit(OpCodes.Ret);
            }

            private static MethodBuilder DefinePropertyGetMethod(TypeBuilder newType, string propertyName)
            {
                Type interfaceType = typeof(IClassWrapper);
                MethodInfo getPropertyMethod = interfaceType.GetProperty(propertyName).GetGetMethod();

                if (getPropertyMethod == null)
                    return null;

                MethodBuilder builder = newType.DefineMethod(
                    string.Concat(interfaceType.Name, '.', getPropertyMethod.Name),
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    getPropertyMethod.ReturnType, Type.EmptyTypes);

                newType.DefineMethodOverride(builder, getPropertyMethod);

                return builder;
            }

            private static MethodBuilder DefineMethod(TypeBuilder newType, string methodName)
            {
                return DefineMethod(newType, typeof(IClassWrapper), methodName); 
            }

            private static MethodBuilder DefineMethod(TypeBuilder newType, string methodName, Type[] parameterTypes)
            {
                return DefineMethod(newType, typeof(IClassWrapper), methodName, parameterTypes);
            }

            private static MethodBuilder DefineMethod(TypeBuilder newType, Type interfaceType, string methodName)
            {
                MethodInfo getMethod = interfaceType.GetMethod(methodName);

                return DefineMethod(
                    newType,
                    interfaceType,
                    methodName,
                    ClassWrappers.GetParameterTypes(getMethod.GetParameters()));
            }

            private static MethodBuilder DefineMethod(TypeBuilder newType, Type interfaceType, string methodName, Type[] parameterTypes)
            {
                MethodInfo getMethod = interfaceType.GetMethod(methodName, parameterTypes);

                if (getMethod == null)
                    return null;

                MethodBuilder builder = newType.DefineMethod(
                    string.Concat(interfaceType.Name, '.', getMethod.Name),
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    getMethod.ReturnType, parameterTypes);

                newType.DefineMethodOverride(builder, getMethod);

                return builder;
            }
        }
    }
}
