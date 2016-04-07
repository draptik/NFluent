﻿#region File header

// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="ObjectFieldsCheckExtensions.cs" company="">
// //   Copyright 2014 Cyrille DUPUYDAUBY, Thomas PIERRAIN
// //   Licensed under the Apache License, Version 2.0 (the "License");
// //   you may not use this file except in compliance with the License.
// //   You may obtain a copy of the License at
// //       http://www.apache.org/licenses/LICENSE-2.0
// //   Unless required by applicable law or agreed to in writing, software
// //   distributed under the License is distributed on an "AS IS" BASIS,
// //   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// //   See the License for the specific language governing permissions and
// //   limitations under the License.
// // </copyright>
// // --------------------------------------------------------------------------------------------------------------------
#endregion

namespace NFluent
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;

    using NFluent.Extensibility;
    using NFluent.Extensions;
    using NFluent.Helpers;

    /// <summary>
    ///     Provides check methods to be executed on an object instance.
    /// </summary>
    public static class ObjectFieldsCheckExtensions
    {
        #region Static Fields

        /// <summary>
        ///     The anonymous type field mask.
        /// </summary>
        private static readonly Regex AnonymousTypeFieldMask;

        /// <summary>
        ///     The auto property mask.
        /// </summary>
        private static readonly Regex AutoPropertyMask;

        /// <summary>
        ///     The mono anonymous type field mask.
        /// </summary>
        private static readonly Regex MonoAnonymousTypeFieldMask;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        ///     Initializes static members of the <see cref="ObjectFieldsCheckExtensions" /> class.
        /// </summary>
        static ObjectFieldsCheckExtensions()
        {
            AutoPropertyMask = new Regex("^<(.*)>k_");
            AnonymousTypeFieldMask = new Regex("^<(.*)>i_");
            MonoAnonymousTypeFieldMask = new Regex("^<(.*)>\\z");
        }

        #endregion

        #region Enums

        /// <summary>
        ///     Kind of field (whether normal, generated by an auto-property, an anonymous class, etc.
        /// </summary>
        public enum FieldKind
        {
            /// <summary>
            ///     Normal field.
            /// </summary>
            Normal, 

            /// <summary>
            ///     Field generated by an auto-property.
            /// </summary>
            AutoProperty, 

            /// <summary>
            ///     Field generated by an anonymous class.
            /// </summary>
            AnonymousClass
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Checks that the actual value has fields equals to the expected value ones.
        /// </summary>
        /// <param name="check">
        /// The fluent check to be extended.
        /// </param>
        /// <param name="expected">
        /// The expected value.
        /// </param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">
        /// The actual value doesn't have all fields equal to the expected value ones.
        /// </exception>
        /// <remarks>
        /// The comparison is done field by field.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use HasFieldsWithSameValues instead.")]
        public static ICheckLink<ICheck<object>> HasFieldsEqualToThose(this ICheck<object> check, object expected)
        {
            return HasFieldsWithSameValues(check, expected);
        }

        /// <summary>
        /// Checks that the actual value doesn't have all fields equal to the expected value ones.
        /// </summary>
        /// <param name="check">
        /// The fluent check to be extended.
        /// </param>
        /// <param name="expected">
        /// The expected value.
        /// </param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">
        /// The actual value has all fields equal to the expected value ones.
        /// </exception>
        /// <remarks>
        /// The comparison is done field by field.
        /// </remarks>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("Use HasNotFieldsWithSameValues instead.")]
        public static ICheckLink<ICheck<object>> HasFieldsNotEqualToThose(this ICheck<object> check, object expected)
        {
            return HasNotFieldsWithSameValues(check, expected);
        }

        /// <summary>
        /// Checks that the actual value has fields equals to the expected value ones.
        /// </summary>
        /// <typeparam name="T">
        /// Type of the checked value.
        /// </typeparam>
        /// <param name="check">
        /// The fluent check to be extended.
        /// </param>
        /// <param name="expected">
        /// The expected value.
        /// </param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">
        /// The actual value doesn't have all fields equal to the expected value ones.
        /// </exception>
        /// <remarks>
        /// The comparison is done field by field.
        /// </remarks>
        public static ICheckLink<ICheck<T>> HasFieldsWithSameValues<T>(this ICheck<T> check, object expected)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);
            var message = CheckFieldEquality(checker, checker.Value, expected, checker.Negated);

            if (message != null)
            {
                throw new FluentCheckException(message);
            }

            return checker.BuildChainingObject();
        }

        /// <summary>
        /// Checks that the actual value doesn't have all fields equal to the expected value ones.
        /// </summary>
        /// <typeparam name="T">
        /// Type of the checked value.
        /// </typeparam>
        /// <param name="check">
        /// The fluent check to be extended.
        /// </param>
        /// <param name="expected">
        /// The expected value.
        /// </param>
        /// <returns>
        /// A check link.
        /// </returns>
        /// <exception cref="FluentCheckException">
        /// The actual value has all fields equal to the expected value ones.
        /// </exception>
        /// <remarks>
        /// The comparison is done field by field.
        /// </remarks>
        public static ICheckLink<ICheck<T>> HasNotFieldsWithSameValues<T>(this ICheck<T> check, object expected)
        {
            var checker = ExtensibilityHelper.ExtractChecker(check);
            var negated = !checker.Negated;

            var message = CheckFieldEquality(checker, checker.Value, expected, negated);

            if (message != null)
            {
                throw new FluentCheckException(message);
            }

            return checker.BuildChainingObject();
        }

        private static IEnumerable<FieldMatch> ScanFields(object value, object expected, IList<object> scanned, string prefix = null)
        {
            var result = new List<FieldMatch>();
            for (var expectedType = expected.GetType(); expectedType != null; expectedType = expectedType.BaseType)
            {
                foreach (var fieldInfo in
                    expectedType.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy))
                {
                    var expectedFieldDescription = new ExtendedFieldInfo(prefix, fieldInfo);
                    var actualFieldMatching = FindField(value.GetType(), expectedFieldDescription.NameInSource);

                    // field not found in SUT
                    if (actualFieldMatching == null)
                    {
                        result.Add(new FieldMatch(expectedFieldDescription, null));
                        continue;
                    }

                    var actualFieldDescription = new ExtendedFieldInfo(prefix, actualFieldMatching);

                    // now, let's get to the values
                    expectedFieldDescription.CaptureFieldValue(expected);
                    actualFieldDescription.CaptureFieldValue(value);

                    if (expectedFieldDescription.ChecksIfImplementsEqual())
                    {
                        result.Add(new FieldMatch(expectedFieldDescription, actualFieldDescription));
                    }
                    else if (!scanned.Contains(expectedFieldDescription.Value))
                    {
                       scanned.Add(expectedFieldDescription.Value);
                       
                        // we need to recurse the scan
                        result.AddRange(
                            ScanFields(
                                actualFieldDescription.Value,
                                expectedFieldDescription.Value,
                                scanned,
                                string.Format("{0}.", expectedFieldDescription.LongFieldName)));

                    }
                }
            }

            return result;
        }

        #endregion

        #region Methods

        internal static string ExtractFieldNameAsInSourceCode(string name, out FieldKind kind)
        {
            string result;
            if (EvaluateCriteria(AutoPropertyMask, name, out result))
            {
                kind = FieldKind.AutoProperty;
                return result;
            }

            if (EvaluateCriteria(AnonymousTypeFieldMask, name, out result))
            {
                kind = FieldKind.AnonymousClass;
                return result;
            }

            if (EvaluateCriteria(MonoAnonymousTypeFieldMask, name, out result))
            {
                kind = FieldKind.AnonymousClass;
                return result;
            }

            result = name;
            kind = FieldKind.Normal;
            return result;
        }

        private static string CheckFieldEquality<T>(IChecker<T, ICheck<T>> checker, object value, object expected, bool negated)
        {
            var analysis = ScanFields(value, expected, new List<object>());

            foreach (var fieldMatch in analysis)
            {
                var result = fieldMatch.BuildMessage(checker, negated);
                if (result != null)
                {
                    return result.ToString();
                }
            }

            return null;
        }

        private static bool EvaluateCriteria(Regex expression, string name, out string actualFieldName)
        {
            var regTest = expression.Match(name);
            if (regTest.Groups.Count == 2)
            {
                actualFieldName = name.Substring(regTest.Groups[1].Index, regTest.Groups[1].Length);
                return true;
            }

            actualFieldName = string.Empty;
            return false;
        }

        private static FieldInfo FindField(Type type, string name)
        {
            while (true)
            {
                const BindingFlags BindingFlags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;
                Debug.Assert(type != null, "Type must not be null");
                var result = type.GetField(name, BindingFlags);

                if (result != null)
                {
                    return result;
                }

                if (type.BaseType == null)
                {
                    return null;
                }

                // compensate any autogenerated name
                FieldKind fieldKind;
                var actualName = ExtractFieldNameAsInSourceCode(name, out fieldKind);

                foreach (var field in from field in type.GetFields(BindingFlags)
                                            let fieldName = ExtractFieldNameAsInSourceCode(field.Name, out fieldKind)
                                            where fieldName == actualName
                                            select field)
                {
                    return field;
                }

                type = type.BaseType;
            }
        }

        #endregion

        private class ExtendedFieldInfo
        {
            #region Fields

            private readonly FieldInfo info;

            private readonly FieldKind kind;

            private readonly string nameInSource;

            private readonly string prefix;

            #endregion

            #region Constructors and Destructors

            public ExtendedFieldInfo(string prefix, FieldInfo info)
            {
                this.prefix = prefix;
                this.info = info;
                if (EvaluateCriteria(AutoPropertyMask, info.Name, out this.nameInSource))
                {
                    this.kind = FieldKind.AutoProperty;
                }
                else if (EvaluateCriteria(AnonymousTypeFieldMask, info.Name, out this.nameInSource))
                {
                    this.kind = FieldKind.AnonymousClass;
                }
                else if (EvaluateCriteria(MonoAnonymousTypeFieldMask, info.Name, out this.nameInSource))
                {
                    this.kind = FieldKind.AnonymousClass;
                }
                else
                {
                    this.nameInSource = info.Name;
                    this.kind = FieldKind.Normal;
                }
            }

            #endregion

            #region Public Properties

            public string LongFieldName
            {
                get
                {
                    return this.prefix == null
                               ? this.nameInSource
                               : string.Format("{0}{1}", this.prefix, this.nameInSource);
                }
            }

            public string FieldLabel
            {
                get
                {
                    string fieldLabel;
                    switch (this.kind)
                    {
                        case FieldKind.AnonymousClass:
                            fieldLabel = string.Format("field '{0}'", this.LongFieldName);
                            break;
                        case FieldKind.AutoProperty:
                            fieldLabel = string.Format("autoproperty '{0}' (field '{1}')", this.LongFieldName, this.info.Name);
                            break;
                        default:
                            fieldLabel = string.Format("field '{0}'", this.LongFieldName);
                            break;
                    }

                    return fieldLabel;
                }
            }

            public string NameInSource
            {
                get
                {
                    return this.nameInSource;
                }
            }

            public object Value { get; private set; }

            #endregion

            #region Public Methods and Operators

            public void CaptureFieldValue(object obj)
            {
                this.Value = this.info.GetValue(obj);
            }

            public bool ChecksIfImplementsEqual()
            {
                return this.info.FieldType.ImplementsEquals();
            }

            #endregion
        }

        private class FieldMatch
        {
            #region Fields

            private readonly ExtendedFieldInfo actual;

            private readonly ExtendedFieldInfo expected;

            #endregion

            #region Constructors and Destructors

            public FieldMatch(ExtendedFieldInfo expected, ExtendedFieldInfo actual)
            {
                this.actual = actual;
                this.expected = expected;
            }

            #endregion

            #region Public Properties

            private bool DoValuesMatches
            {
                get
                {
                    if (!this.ExpectedFieldFound)
                    {
                        return false;
                    }

                    if (this.expected.Value == null)
                    {
                        return this.actual.Value == null;
                    }

                    return this.expected.Value.Equals(this.actual.Value);
                }
            }

            private ExtendedFieldInfo Expected
            {
                get
                {
                    return this.expected;
                }
            }

            /// <summary>
            ///     Gets a value indicating whether the expected field has been found.
            /// </summary>
            private bool ExpectedFieldFound
            {
                get
                {
                    return this.actual != null;
                }
            }

            public FluentMessage BuildMessage<T>(IChecker<T, ICheck<T>> checker, bool negated)
            {
                FluentMessage result = null;
                if (this.DoValuesMatches == negated)
                {
                    if (negated)
                    {
                        result =
                            checker.BuildShortMessage(
                                string.Format(
                                    "The {{0}}'s {0} has the same value in the comparand, whereas it must not.",
                                    this.Expected.FieldLabel.DoubleCurlyBraces())).For("value");
                        EqualityHelper.FillEqualityErrorMessage(result, this.actual.Value, this.expected.Value, true);
                    }
                    else
                    {
                        if (!this.ExpectedFieldFound)
                        {
                            result = checker.BuildShortMessage(
                                string.Format(
                                    "The {{0}}'s {0} is absent from the {{1}}.",
                                    this.Expected.FieldLabel.DoubleCurlyBraces())).For("value");
                            result.Expected(this.expected.Value);
                        }
                        else
                        {
                            result =
                                checker.BuildShortMessage(
                                    string.Format(
                                        "The {{0}}'s {0} does not have the expected value.",
                                        this.Expected.FieldLabel.DoubleCurlyBraces())).For("value");
                            EqualityHelper.FillEqualityErrorMessage(result, this.actual.Value, this.expected.Value, false);
                        }
                    }
                }

                return result;
            }

            #endregion
        }
    }
}