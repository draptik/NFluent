﻿// --------------------------------------------------------------------------------------------------------------------
//  <copyright file="ObjectChecksNonGenericVariants.cs" company="NFluent">
//   Copyright 2018 Thomas PIERRAIN & Cyrille DUPUYDAUBY
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//       http://www.apache.org/licenses/LICENSE-2.0
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
// </copyright>
// --------------------------------------------------------------------------------------------------------------------

namespace NFluent
{
    using System;
    using Extensibility;
    using Extensions;
    using Helpers;

    /// <summary>
    /// Hosts non generic variants of some checks.
    /// </summary>
    public static class ObjectChecksNonGenericVariants
    {
        /// <summary>
        /// Checks if an object is an instance of a specified type.
        /// </summary>
        /// <param name="check">checker logic object</param>
        /// <param name="type">expected type</param>
        /// <typeparam name="T">type of checked object</typeparam>
        /// <returns>A check link</returns>
        public static ICheckLink<ICheck<T>> IsInstanceOfType<T>(this ICheck<T> check, Type type)
        {
            ExtensibilityHelper.BeginCheck(check).
                FailWhen(sut => sut== null && !typeof(T).IsNullable(), $"The {{checked}} is not an instance of [{type.ToStringProperlyFormatted()}].").
                Analyze((sut, test) =>
                {
                    var reflectionSut = sut as ReflectionWrapper;
                    if (reflectionSut != null)
                    {
                        var expectedWrapper = ReflectionWrapper.BuildFromType(type, reflectionSut.Criteria);
                        expectedWrapper.MapFields(reflectionSut, 1, (expected, actual, depth) =>
                        {
                            if (actual != null && expected != null && actual.ValueType != expected.ValueType)
                            {
                                if (!actual.ValueType.IsPrimitive() && !expected.ValueType.IsPrimitive())
                                {
                                    return true;
                                }

                                test.CheckSutAttributes(_ => actual.Value, actual.MemberLabel)
                                    .Fail("The {0} is of a different type than the {1}.")
                                    .DefineExpectedType(expected.ValueType);
                                return false;

                            }

                            if (actual != null && expected != null && expected.ValueType == actual.ValueType)
                            {
                                return false;
                            }

                            if (actual == null) 
                            {
                                test.CheckSutAttributes(_ => expectedWrapper.Value, expected.MemberLabel)
                                    .DefineExpectedValue(expected)
                                    .Fail("The {1} is absent from the {0}.", MessageOption.NoCheckedBlock);
                            }
                            else
                            {
                                test.CheckSutAttributes(_ => actual, actual.MemberLabel.DoubleCurlyBraces())
                                    .Fail("The {0} is absent from the {1}.");
                            }
                            return false;
                        });
                    }
                    else
                    {
                        test.FailWhen(sut2 => sut2.GetTypeWithoutThrowingException() != type,
                            $"The {{0}} is not an instance of [{type.ToStringProperlyFormatted()}].", 
                             MessageOption.WithType);
                    }
                })
                .DefineExpectedType(type)
                .OnNegate($"The {{0}} is an instance of [{type.ToStringProperlyFormatted()}] whereas it must not.", MessageOption.WithType)
                .EndCheck();
            return ExtensibilityHelper.BuildCheckLink(check);
        }

        /// <summary>
        /// Checks that the sut's type is not one of forbidden types types.
        /// </summary>
        /// <param name="context">check context</param>
        /// <param name="types">possible types</param>
        /// <typeparam name="T">type of the SUT</typeparam>
        /// <returns>check link</returns>
        public static ICheckLink<ICheck<T>> IsNotAnInstanceOfThese<T>(this ICheck<T> context, params Type[] types)
        {
            if (types.Length == 1)
            {
                // switch to single type check to provide error messages
                return IsInstanceOfType(context.Not, types[0]);
            }
                
            ExtensibilityHelper.BeginCheck(context)
                .OnNegateWhen(sut => sut== null, "The {checked} is null.")
                .CheckSutAttributes( sut => sut.GetTypeWithoutThrowingException(), "type")
                .Analyze((sut, test) =>
                {
                        foreach (var type in types)
                        {
                            if (sut == type)
                            {
                                test.Fail(
                                    $"The {{0}} is [{type.ToStringProperlyFormatted()}] where as it must not.");
                                break;
                            }
                        }
                })
                .DefineExpectedValue(types, "anything but", "one of those")
                .OnNegate($"The {{0}} is not one of the expected types.")
                .EndCheck();
                
            return ExtensibilityHelper.BuildCheckLink(context);
        }


        /// <summary>
        /// Checks that the sut's type is one of several possible types.
        /// </summary>
        /// <param name="context">check context</param>
        /// <param name="types">possible types</param>
        /// <typeparam name="T">type of the SUT</typeparam>
        /// <returns>check link</returns>
        public static ICheckLink<ICheck<T>> IsAnInstanceOfOneOf<T>(this ICheck<T> context, params Type[] types)
        {
            return context.Not.IsNotAnInstanceOfThese(types);
        }

        /// <summary>
        /// Checks if an object is not an instance of a specified type.
        /// </summary>
        /// <param name="check">checker logic object</param>
        /// <param name="type">expected type</param>
        /// <typeparam name="T">type of checked object</typeparam>
        /// <returns>A check link</returns>
        public static ICheckLink<ICheck<T>> IsNoInstanceOfType<T>(this ICheck<T> check, Type type)
        {
            return check.Not.IsInstanceOfType(type);
        }
    }
}