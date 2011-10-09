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
// File: ClassWrappers.PropertyWrapperGenerator.cs
// Authors: Capasicum <capasicum@gmail.com>
//          ostoich   <ostoich@gmail.com>

namespace DataAccessLayer.Wrappers
{
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Reflection.Emit;

    public static partial class ClassWrappers
    {
        private static partial class PropertyWrapperGenerator
        {
            public static List<Type> GeneratePropertyWrappers(TypeBuilder newCWType, IClassWrapper baseWrapper, PropertyInfo[] propertiesInfo, Type dataType)
            {
                List<Type> propertyWrapperClasses = new List<Type>();

                foreach (PropertyInfo propertyInfo in propertiesInfo)
                {
                    if (propertyInfo.DeclaringType != dataType)
                        continue;

                    Type wrapper = PropertyWrapperGenerator.GeneratePropertyWrapper(newCWType, propertyInfo, dataType);
                    propertyWrapperClasses.Add(wrapper);
                }

                return propertyWrapperClasses;
            }

            private static Type GeneratePropertyWrapper(TypeBuilder newCWType, PropertyInfo propertyInfo, Type dataType)
            {
                TypeBuilder newType = ClassWrappers.ModuleBuilder.DefineType(
                    string.Concat(newCWType.Name, ".", propertyInfo.Name, "Prop"),
                     TypeAttributes.NotPublic);

                MethodInfo getMethodInfo = propertyInfo.GetGetMethod();
                MethodInfo setMethodInfo = propertyInfo.GetSetMethod();

                bool canRead = propertyInfo.CanRead && getMethodInfo != null;
                bool canWrite = propertyInfo.CanWrite && setMethodInfo != null;

                FieldInfo defaultValue = GenerateDefaultValueField(newType, propertyInfo.PropertyType);
                bool isIndexed = propertyInfo.GetIndexParameters().Length > 0;

                newType.AddInterfaceImplementation(typeof(ISimplePropertyWrapper));
                GenerateWrapperStaticConstructor(newType, defaultValue, propertyInfo);

                
                GenerateWrapperConstructor(newType, defaultValue);
                GenerateDeclaryngType(newType, propertyInfo.DeclaringType);
                GenerateParentClassWrapper(newType, propertyInfo.DeclaringType);
                GeneratePropertyNameGet(newType, propertyInfo.Name);
                GeneratePropertyType(newType, propertyInfo.PropertyType);
                
     
                GenerateIsReadableGet(newType, canRead);
                GenerateIsWriteableGet(newType, canWrite);

                GenerateCanWorkWithInstance(newType, dataType);
                GenerateCanWorkWithType(newType, dataType);

                GenerateGetValue(newType, propertyInfo, dataType, getMethodInfo, canRead, defaultValue, isIndexed);
                GenerateGetValueUnsafe(newType, propertyInfo, dataType, getMethodInfo, canRead, defaultValue, isIndexed);
                GenerateSetValue(newType, propertyInfo.PropertyType, dataType, setMethodInfo, canWrite, isIndexed);
                GenerateIComparerImplementation(newType, getMethodInfo, propertyInfo.PropertyType, dataType, canRead);
                GenerateIsComparable(newType, propertyInfo.PropertyType, dataType, canRead);
                GenerateIsIndexed(newType, propertyInfo.GetIndexParameters());
                GenerateCompare(newType, getMethodInfo, dataType, propertyInfo.PropertyType, canRead);

                return newType.CreateType();
            }

            private static void GenerateIComparerImplementation(TypeBuilder newType, MethodInfo getValue, Type propertyType, Type dataType, bool canRead)
            {
                Type interfaceType = typeof(IComparer<>).MakeGenericType(dataType);
                newType.AddInterfaceImplementation(interfaceType);

                MethodInfo compareMethodInfo = interfaceType.GetMethod("Compare");
                MethodBuilder compareMethod = newType.DefineMethod(
                    string.Concat(interfaceType.Name, '.', compareMethodInfo.Name),
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual | MethodAttributes.Final,
                     CallingConventions.Standard,
                     compareMethodInfo.ReturnType,
                     ClassWrappers.GetParameterTypes(compareMethodInfo.GetParameters()));

                newType.DefineMethodOverride(compareMethod, compareMethodInfo);

                ILGenerator g = compareMethod.GetILGenerator();

                Type comparer = GetIComparable(propertyType);

                if (comparer == null || !canRead)
                {
                    g.Emit(OpCodes.Ldc_I4_0);
                    g.Emit(OpCodes.Ret);

                    return;
                }

                compareMethod.InitLocals = true;

                g.DeclareLocal(typeof(int));
                g.DeclareLocal(propertyType);
                g.DeclareLocal(propertyType);
                Label secondParamCheck = g.DefineLabel();
                Label checkIntermediateResult = g.DefineLabel();
                Label returnResult = g.DefineLabel();

                MethodInfo compareMethodCall = propertyType.GetMethod("CompareTo", new Type[] { propertyType });
                bool virtCall = false;
                if (compareMethodCall == null)
                {
                    compareMethodCall = comparer.GetMethod("CompareTo", GetCompareToParameters(comparer, propertyType));
                    virtCall = true;
                }

                g.Emit(OpCodes.Nop);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, getValue);

                if (propertyType.IsValueType)
                {
                    if (virtCall)
                        g.Emit(OpCodes.Box);
                    else
                    {
                        g.Emit(OpCodes.Stloc_1);
                        g.Emit(OpCodes.Ldloca_S, 1);
                    }
                }

                g.Emit(OpCodes.Ldarg_2);
                g.Emit(OpCodes.Callvirt, getValue);

                if (virtCall)
                    g.Emit(OpCodes.Callvirt, compareMethodCall);
                else
                    g.Emit(OpCodes.Call, compareMethodCall);
      
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateCompare(TypeBuilder newType, MethodInfo getValue, Type dataType, Type propertyType, bool canRead)
            {
                MethodInfo compareMethodInfo = typeof(IPropertyWrapper).GetMethod("Compare");
                MethodBuilder compareMethod = newType.DefineMethod(
                    string.Concat(typeof(IPropertyWrapper).Name, '.', compareMethodInfo.Name),
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                     CallingConventions.Standard,
                     compareMethodInfo.ReturnType,
                     ClassWrappers.GetParameterTypes(compareMethodInfo.GetParameters()));

                newType.DefineMethodOverride(compareMethod, compareMethodInfo);

                ILGenerator g = compareMethod.GetILGenerator();

                Type comparer = GetIComparable(propertyType);

                if (comparer == null || !canRead)
                {
                    g.Emit(OpCodes.Ldc_I4_0);
                    g.Emit(OpCodes.Ret);

                    return;
                }

                g.DeclareLocal(typeof(int));
                g.DeclareLocal(comparer);
                g.DeclareLocal(propertyType);
                Label secondParamCheck = g.DefineLabel();
                Label checkIntermediateResult = g.DefineLabel();
                Label returnResult = g.DefineLabel();

                MethodInfo interfaceCompareMethod = comparer.GetMethod("CompareTo", GetCompareToParameters(comparer, propertyType));

                g.Emit(OpCodes.Ldc_I4_0);
                g.Emit(OpCodes.Stloc_0);

                g.Emit(OpCodes.Ldarg_1);
                if (dataType.IsValueType)
                    g.Emit(OpCodes.Unbox_Any);
                else
                    g.Emit(OpCodes.Isinst, dataType);
                g.Emit(OpCodes.Callvirt, getValue);
                if (propertyType.IsValueType)
                    g.Emit(OpCodes.Box, propertyType);
                g.Emit(OpCodes.Isinst, comparer);

                g.Emit(OpCodes.Ldarg_2);
                if (dataType.IsValueType)
                    g.Emit(OpCodes.Unbox_Any);
                else
                    g.Emit(OpCodes.Isinst, dataType);
                g.Emit(OpCodes.Callvirt, getValue);

                g.Emit(OpCodes.Callvirt, interfaceCompareMethod);
                g.Emit(OpCodes.Stloc_0);

                g.MarkLabel(returnResult);
                g.Emit(OpCodes.Ldloc_0);
                g.Emit(OpCodes.Ret);

            }

            private static Type[] GetCompareToParameters(Type comparer, Type propertyType)
            {
                if (comparer.IsGenericType)
                    return new Type[] { propertyType };

                return new Type[] { typeof(object) };
            }

            private static void GenerateIsComparable(TypeBuilder newType, Type propertyType, Type dataType, bool canRead)
            {

                MethodBuilder builder = DefinePropertyGetMethod(newType, "IsComparable", typeof(IPropertyWrapper));
                ILGenerator g = builder.GetILGenerator();

                Type comparer = GetIComparable(propertyType);
                MethodInfo compareMethod = dataType.GetMethod("CompareTo", new Type[] { propertyType });

                bool canCompare = comparer != null || compareMethod != null;

                g.Emit(OpCodes.Ldc_I4, canCompare && canRead ? 1 : 0);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateIsIndexed(TypeBuilder newType, ParameterInfo[] indexParameters)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "IsIndexed", typeof(IPropertyWrapper));
                ILGenerator g = builder.GetILGenerator();
                if (indexParameters.Length > 0)
                    g.Emit(OpCodes.Ldc_I4_1);
                else
                    g.Emit(OpCodes.Ldc_I4_0);

                g.Emit(OpCodes.Ret);
            }

            private static Type GetIComparable(Type type)
            {
                Type[] intfs = type.GetInterfaces();

                foreach (Type intf in intfs)
                {
                    if (!intf.IsGenericType || intf.Name != typeof(IComparable<>).Name) continue;
                    Type[] args = intf.GetGenericArguments();
                    if (args[0] == type)
                        return intf;
                }
                return type.GetInterface("IComparable", false);
            }

            private static void GeneratePropertyType(TypeBuilder newType, Type propertyType)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "PropertyType", typeof(ISimplePropertyWrapper));
                MethodInfo getHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                ILGenerator g = builder.GetILGenerator();
                g.Emit(OpCodes.Ldtoken, propertyType);
                g.Emit(OpCodes.Call, getHandle);
                g.Emit(OpCodes.Ret);
            }

            private static FieldInfo GenerateDefaultValueField(TypeBuilder newType, Type type)
            {
                return newType.DefineField("defaultValue", typeof(object), FieldAttributes.InitOnly | FieldAttributes.Private | FieldAttributes.Static);
            }

            private static FieldInfo GenerateIndexField(TypeBuilder newType, Type type)
            {
                return newType.DefineField("index", typeof(object[]), FieldAttributes.Private | FieldAttributes.Static);
            }

            private static void GenerateParentClassWrapper(TypeBuilder newType, Type declaringType)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "ParentWrapper", typeof(ISimplePropertyWrapper));
                MethodInfo getHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                MethodInfo getWrapperForType = typeof(ClassWrappers).GetMethod("GetForType");
                ILGenerator g = builder.GetILGenerator();
                g.Emit(OpCodes.Ldtoken, declaringType);
                g.Emit(OpCodes.Call, getHandle);
                g.Emit(OpCodes.Call, getWrapperForType);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateDeclaryngType(TypeBuilder newType, Type declaringType)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "DeclaringType", typeof(ISimplePropertyWrapper));
                MethodInfo getHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                ILGenerator g = builder.GetILGenerator();
                g.Emit(OpCodes.Ldtoken, declaringType);
                g.Emit(OpCodes.Call, getHandle);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateWrapperStaticConstructor(TypeBuilder newType, FieldInfo defaultValue, PropertyInfo propertyInfo)
            {
                ConstructorBuilder ctor = newType.DefineConstructor(MethodAttributes.Static | MethodAttributes.Public, CallingConventions.Standard, Type.EmptyTypes);
                ILGenerator g = ctor.GetILGenerator();
                ctor.InitLocals = true;

                if (propertyInfo.PropertyType.IsValueType)
                {
                    g.DeclareLocal(propertyInfo.PropertyType);
                    g.Emit(OpCodes.Ldloc_0);
                    g.Emit(OpCodes.Box, propertyInfo.PropertyType);
                }
                else
                    g.Emit(OpCodes.Ldnull);

                g.Emit(OpCodes.Stsfld, defaultValue);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateWrapperConstructor(TypeBuilder newType, FieldInfo defaultValue)
            {
                ConstructorBuilder ctor = newType.DefineDefaultConstructor(MethodAttributes.Public);

            }

            private static void GenerateSetValue(TypeBuilder newType, Type parameterType, Type dataType, MethodInfo setMethodInfo, bool canWrite, bool isIndexed)
            {
                MethodBuilder setMethod = DefineMethod(newType, typeof(IPropertyWrapper), "SetValue");
                MethodInfo getHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                MethodInfo getType = typeof(Object).GetMethod("GetType");
                ILGenerator g = setMethod.GetILGenerator();
                setMethod.InitLocals = true;
                if (isIndexed)
                {
                    g.Emit(OpCodes.Ldc_I4, (int)State.InvalidPropertyIndex);
                    g.Emit(OpCodes.Ret);
                    return;
                }
                if (canWrite)
                {
                    g.DeclareLocal(dataType);
                    g.DeclareLocal(typeof(State));
                    g.DeclareLocal(parameterType);
                    Label checkParameter = g.DefineLabel();
                    Label storeDataCheck = g.DefineLabel();
                    Label endLabel = g.DefineLabel();
                    Label invalidParameter = g.DefineLabel();
                    g.Emit(OpCodes.Ldc_I4, (int)State.OK);
                    g.Emit(OpCodes.Stloc_1);
                    g.Emit(OpCodes.Ldarg_1);
                    g.Emit(OpCodes.Isinst, dataType);
                    g.Emit(OpCodes.Stloc_0);
                    g.Emit(OpCodes.Ldloc_0);
                    g.Emit(OpCodes.Brtrue_S, checkParameter);
                    g.Emit(OpCodes.Ldc_I4, (int)State.InvalidData);
                    g.Emit(OpCodes.Stloc_1);
                    g.MarkLabel(checkParameter);
                    g.Emit(OpCodes.Ldarg_2);
                    g.Emit(OpCodes.Isinst, parameterType);
                    g.Emit(OpCodes.Brfalse_S, invalidParameter);
                    g.Emit(OpCodes.Ldarg_2);
                    if (parameterType.IsValueType)
                        g.Emit(OpCodes.Unbox_Any, parameterType);
                    else
                        g.Emit(OpCodes.Castclass, parameterType);
                    g.Emit(OpCodes.Stloc_2);
                    g.Emit(OpCodes.Br_S, storeDataCheck);
                    g.MarkLabel(invalidParameter);
                    g.Emit(OpCodes.Ldloc_1);
                    g.Emit(OpCodes.Ldc_I4, (int)State.InvalidValue);
                    g.Emit(OpCodes.Or);
                    g.Emit(OpCodes.Stloc_1);
                    g.Emit(OpCodes.Br_S, endLabel);
                    g.MarkLabel(storeDataCheck);
                    g.Emit(OpCodes.Ldloc_1);
                    g.Emit(OpCodes.Ldc_I4, (int)State.OK);
                    g.Emit(OpCodes.Ceq);
                    g.Emit(OpCodes.Brfalse_S, endLabel);
                    g.Emit(OpCodes.Ldloc_0);
                    g.Emit(OpCodes.Ldloc_2);
                    g.Emit(OpCodes.Call, setMethodInfo);
                    g.MarkLabel(endLabel);
                    g.Emit(OpCodes.Ldloc_1);
                }
                else
                {
                    g.Emit(OpCodes.Ldc_I4, (int)State.InvalidOperation);
                }

                g.Emit(OpCodes.Ret);
            }

            private static void GenerateGetValue(TypeBuilder newType, PropertyInfo propertyInfo, Type dataType, MethodInfo getMethodInfo, bool canRead, FieldInfo defaultValue, bool isIndexed)
            {
                MethodBuilder methodBuilder = DefineMethod(newType, typeof(IPropertyWrapper), "GetValue");
                ILGenerator g = methodBuilder.GetILGenerator();
                ConstructorInfo resultConstructor = typeof(Result).GetConstructor(new Type[] { typeof(State), typeof(object) });
                int indexParametersLength = propertyInfo.GetIndexParameters().Length;

                if (canRead)
                {
                    // Implement code for static properties
                    // getMethodInfo.IsStatic = true

                    g.DeclareLocal(dataType);
                    g.DeclareLocal(typeof(object));

                    Label cannotGet = g.DefineLabel();
                    Label endGet = g.DefineLabel();
                    Label invalidIndex = g.DefineLabel();

                    g.Emit(OpCodes.Ldarg_1);
                    g.Emit(OpCodes.Isinst, dataType);
                    g.Emit(OpCodes.Stloc_0);
                    g.Emit(OpCodes.Ldloc_0);
                    g.Emit(OpCodes.Brfalse_S, cannotGet);

                    g.Emit(OpCodes.Ldarg_2);
                    g.Emit(OpCodes.Ldlen);
                    g.Emit(OpCodes.Ldc_I4, indexParametersLength);
                    g.Emit(OpCodes.Ceq);
                    g.Emit(OpCodes.Brfalse_S, invalidIndex);
                    g.Emit(OpCodes.Ldloc_0);

                    if (isIndexed)
                    {
                        ParameterInfo[] parametersInfo = propertyInfo.GetIndexParameters();
                        for (int i = 0; i < indexParametersLength; i++)
                        {
                            Type paramType = parametersInfo[i].ParameterType;
                            Label continueFor = g.DefineLabel();
                            g.Emit(OpCodes.Ldarg_2);
                            g.Emit(OpCodes.Ldc_I4, i);
                            g.Emit(OpCodes.Ldelem_Ref);
                            g.Emit(OpCodes.Stloc_1);
                            g.Emit(OpCodes.Ldloc_1);
                            g.Emit(OpCodes.Isinst, paramType);
                            g.Emit(OpCodes.Brtrue_S, continueFor);
                            for (int m = 0; m < i + 1; m++)
                                g.Emit(OpCodes.Pop);
                            g.Emit(OpCodes.Br_S, invalidIndex);

                            g.MarkLabel(continueFor);
                            g.Emit(OpCodes.Ldloc_1);
                            if (paramType.IsValueType)
                                g.Emit(OpCodes.Unbox_Any, paramType);
                            else
                                g.Emit(OpCodes.Castclass, paramType);
                        }
                    }

                    g.Emit(OpCodes.Callvirt, getMethodInfo);
                    if (propertyInfo.PropertyType.IsValueType)
                        g.Emit(OpCodes.Box, propertyInfo.PropertyType);
                  
                    g.Emit(OpCodes.Stloc_1);
                    g.Emit(OpCodes.Ldc_I4, (int)State.OK);
                    g.Emit(OpCodes.Ldloc_1);
                    g.Emit(OpCodes.Br_S, endGet);
            
                    g.MarkLabel(invalidIndex);
                    g.Emit(OpCodes.Ldc_I4, (int)State.InvalidPropertyIndex);
                    g.Emit(OpCodes.Ldsfld, defaultValue);
                    g.Emit(OpCodes.Br_S, endGet);
 
                    g.MarkLabel(cannotGet);
                    g.Emit(OpCodes.Ldc_I4, (int)State.InvalidData);
                    g.Emit(OpCodes.Ldsfld, defaultValue);
                    g.MarkLabel(endGet);
                    g.Emit(OpCodes.Newobj, resultConstructor);
                    g.Emit(OpCodes.Ret);
                }
                else
                {
                    Label badData = g.DefineLabel();
                    Label endGet = g.DefineLabel();
                    g.Emit(OpCodes.Ldarg_1);
                    g.Emit(OpCodes.Isinst, dataType);
                    g.Emit(OpCodes.Brfalse_S, badData);
                    g.Emit(OpCodes.Ldc_I4, (int)State.InvalidOperation);
                    g.Emit(OpCodes.Br_S, endGet);
                    g.MarkLabel(badData);
                    g.Emit(OpCodes.Ldc_I4, (int)State.InvalidData);
                    g.MarkLabel(endGet);
                    g.Emit(OpCodes.Ldsfld, defaultValue);
                    g.Emit(OpCodes.Newobj, resultConstructor);
                    g.Emit(OpCodes.Ret);
                }
            }

            private static void GenerateGetValueUnsafe(TypeBuilder newType, PropertyInfo propertyInfo, Type dataType, MethodInfo getMethodInfo, bool canRead, FieldInfo defaultValue, bool isIndexed)
            {
                MethodBuilder methodBuilder = DefineMethod(newType, typeof(IPropertyWrapper), "GetValueUnsafe");
                ILGenerator g = methodBuilder.GetILGenerator();
                int indexParametersLength = propertyInfo.GetIndexParameters().Length;
                if (canRead)
                {
                    g.Emit(OpCodes.Ldarg_1);
                    if (dataType.IsValueType)
                        g.Emit(OpCodes.Unbox_Any, dataType);
                    else
                        g.Emit(OpCodes.Castclass, dataType);
                    
                    ParameterInfo[] propertiesInfo = propertyInfo.GetIndexParameters();
                    for (int i = 0; i < indexParametersLength; i++)
                    {
                        Type paramType = propertiesInfo[i].ParameterType;
                        g.Emit(OpCodes.Ldarg_2);
                        g.Emit(OpCodes.Ldc_I4, i);
                        g.Emit(OpCodes.Ldelem_Ref);

                        g.Emit(OpCodes.Ldloc_1);
                        if (paramType.IsValueType)
                            g.Emit(OpCodes.Unbox_Any, paramType);
                        else
                            g.Emit(OpCodes.Castclass, paramType);
                    }
                    g.Emit(OpCodes.Callvirt, getMethodInfo);
                    if (propertyInfo.PropertyType.IsValueType)
                        g.Emit(OpCodes.Box, propertyInfo.PropertyType);
                    g.Emit(OpCodes.Ret);
                }
                else
                {
                    g.Emit(OpCodes.Ldnull);
                    g.Emit(OpCodes.Ret);
                }
            }

            private static void GeneratePropertyNameGet(TypeBuilder newType, string propertyName)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "PropertyName", typeof(ISimplePropertyWrapper));
                ILGenerator g = builder.GetILGenerator();
                g.Emit(OpCodes.Ldstr, propertyName);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateIsReadableGet(TypeBuilder newType, bool isReadable)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "IsReadable", typeof(ISimplePropertyWrapper));

                if (builder == null)
                    return;

                ILGenerator g = builder.GetILGenerator();
                g.Emit(OpCodes.Ldc_I4, (int)(isReadable ? 1 : 0));
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateIsWriteableGet(TypeBuilder newType, bool isWriteable)
            {
                MethodBuilder builder = DefinePropertyGetMethod(newType, "IsWriteable", typeof(ISimplePropertyWrapper));
                ILGenerator g = builder.GetILGenerator();
                g.Emit(OpCodes.Ldc_I4, (int)(isWriteable ? 1 : 0));
                g.Emit(OpCodes.Ret);
            }

            private static MethodBuilder DefinePropertyGetMethod(TypeBuilder newType, string propertyName, Type interfaceType)
            {
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

            private static MethodBuilder DefinePropertySetMethod(TypeBuilder newType, string propertyName, Type interfaceType)
            {
                MethodInfo setPropertyMethod = interfaceType.GetProperty(propertyName).GetSetMethod();

                if (setPropertyMethod == null)
                    return null;
                ParameterInfo[] param = setPropertyMethod.GetParameters();
                Type[] paramiters = new Type[param.Length];
                for (int i = 0; i < param.Length; i++)
                {
                    paramiters[i] = param[i].ParameterType;
                }
                MethodBuilder builder = newType.DefineMethod(
                    string.Concat(interfaceType.Name, '.', setPropertyMethod.Name),
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    setPropertyMethod.ReturnType, paramiters);

                newType.DefineMethodOverride(builder, setPropertyMethod);

                return builder;
            }

            private static void GenerateCanWorkWithInstance(TypeBuilder newType, Type dataType)
            {
                MethodInfo iMethod = typeof(IPropertyWrapper).GetMethod("CanWorkWithInstance");

                MethodBuilder mBuilder = newType.DefineMethod(
                    string.Concat(typeof(IPropertyWrapper).Name, '.', iMethod.Name),
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    CallingConventions.Standard,
                    iMethod.ReturnType,
                    ClassWrappers.GetParameterTypes(iMethod.GetParameters()));

                newType.DefineMethodOverride(mBuilder, iMethod);

                ILGenerator g = mBuilder.GetILGenerator();
                Label nullLabel = g.DefineLabel();
                Label endLabel = g.DefineLabel();
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Isinst, dataType);
                g.Emit(OpCodes.Brfalse_S, nullLabel);
                g.Emit(OpCodes.Ldc_I4_1);
                g.Emit(OpCodes.Br_S, endLabel);
                g.MarkLabel(nullLabel);
                g.Emit(OpCodes.Ldc_I4_0);
                g.MarkLabel(endLabel);
                g.Emit(OpCodes.Ret);
            }

            private static void GenerateCanWorkWithType(TypeBuilder newType, Type dataType)
            {
                MethodInfo iMethod = typeof(IPropertyWrapper).GetMethod("CanWorkWithType");

                MethodBuilder mBuilder = newType.DefineMethod(
                    string.Concat(typeof(IPropertyWrapper).Name, '.', iMethod.Name),
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                    CallingConventions.Standard,
                    iMethod.ReturnType,
                    ClassWrappers.GetParameterTypes(iMethod.GetParameters()));

                newType.DefineMethodOverride(mBuilder, iMethod);

                MethodInfo getHandle = typeof(Type).GetMethod("GetTypeFromHandle");
                MethodInfo methodInfo = typeof(Type).GetMethod("IsAssignableFrom");
                ILGenerator g = mBuilder.GetILGenerator();
                g.Emit(OpCodes.Ldtoken, dataType);
                g.Emit(OpCodes.Call, getHandle);
                g.Emit(OpCodes.Ldarg_1);
                g.Emit(OpCodes.Callvirt, methodInfo);
                g.Emit(OpCodes.Ret);
            }

            private static MethodBuilder DefineMethod(TypeBuilder newType, Type interfaceType, string methodName)
            {
                MethodInfo getInterfaceMethodInfo = interfaceType.GetMethod(methodName);
                MethodBuilder getMethod = newType.DefineMethod(
                    string.Concat(interfaceType.Name, '.', getInterfaceMethodInfo.Name),
                    MethodAttributes.Private | MethodAttributes.HideBySig | MethodAttributes.Virtual,
                     CallingConventions.Standard,
                     getInterfaceMethodInfo.ReturnType,
                     ClassWrappers.GetParameterTypes(getInterfaceMethodInfo.GetParameters()));

                newType.DefineMethodOverride(getMethod, getInterfaceMethodInfo);
                return getMethod;
            }

        }
    }
}
