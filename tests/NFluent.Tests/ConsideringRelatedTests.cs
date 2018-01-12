﻿//--------------------------------------------------------------------------------------------------------------------
//<copyright file="ConsideringRelatedTests.cs" company="">
// Copyright 2018 Cyrille DUPUYDAUBY
//  Licensed under the Apache License, Version 2.0 (the "License");
//  you may not use this file except in compliance with the License.
//  You may obtain a copy of the License at
//      http://www.apache.org/licenses/LICENSE-2.0
//  Unless required by applicable law or agreed to in writing, software
//  distributed under the License is distributed on an "AS IS" BASIS,
//  WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//  See the License for the specific language governing permissions and
//  limitations under the License.
//</copyright>
//--------------------------------------------------------------------------------------------------------------------

namespace NFluent.Tests
{
    using NUnit.Framework;

    [TestFixture]
    public class ConsideringRelatedTests
    {
        [Test]
        public void ShouldWorkForIdenticalPublicFields()
        {
            var sut = new SutClass(2, 42);

            Check.That(sut).Considering().Public.Fields.IsEqualTo(new SutClass(2, 42));
        }


        [Test]
        public void ShouldWorkForIdenticalPublicProperties()
        {
            var sut = new SutClass(2, 42);

            Check.That(sut).Considering().Public.Properties.IsEqualTo(new SutClass(1, 42));
        }

        [Test]
        public void ShouldWorkForIdenticalPublicFieldsAndDifferentProperties()
        {
            var sut = new SutClass(2, 42);

            Check.That(sut).Considering().Public.Fields.IsEqualTo(new SutClass(2, 43));
        }

        [Test]
        public void ShouldFailForDifferentPublicFields()
        {
            var sut = new SutClass(2, 42);

            Check.ThatCode(() =>
                    Check.That(sut).Considering().Public.Fields.IsEqualTo(new SutClass(3, 42)))
                .Throws<FluentCheckException>();
        }

        [Test]
        public void ShouldFailForDifferentPublicProperties()
        {
            var sut = new SutClass(2, 42);

            Check.ThatCode(() =>
                    Check.That(sut).Considering().Public.Properties.IsEqualTo(new SutClass(2, 43)))
                .Throws<FluentCheckException>();
        }

        [Test]
        public void ShouldWorkForPrivateFields()
        {
            var sut = new SutClass(2, 42, 4, null);

            Check.That(sut).Considering().NonPublic.Fields.IsEqualTo(new SutClass(2, 42, 4, null));
        }

        [Test]
        public void ShouldWorkForAllMembers()
        {
            var sut = new SutClass(2, 42, 4, null);

            Check.That(sut).Considering().All.Fields.And.All.Properties.IsEqualTo(new SutClass(2, 42, 4, null));
        }

        private class SutClass
        {
            private static int autoInc = 0;

            public int TheField;
            private int thePrivateField;

            private int theProperty;
            protected internal object ThePrivatePropery { get; }

            public int TheProperty
            {
                get { return this.theProperty; }
                set { this.theProperty = value; }
            }

            public SutClass(int theField, int theProperty)
            {
                TheField = theField;
                TheProperty = theProperty;
                this.thePrivateField = autoInc++;
            }

            public SutClass(int theField, int theProperty, int thePrivateField, object thePrivatePropery)
            {
                TheField = theField;
                TheProperty = theProperty;
                this.ThePrivatePropery = thePrivatePropery;
                this.thePrivateField = thePrivateField;
            }
        }
    }
}