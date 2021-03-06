﻿// // --------------------------------------------------------------------------------------------------------------------
// // <copyright file="LambdaDurationTests.cs" company="">
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

using System;
using NFluent.ApiChecks;

namespace NFluent.Tests
{
    using System.Diagnostics;
    using System.Threading;
    using NFluent.Helpers;
    using NUnit.Framework;

    [TestFixture]
    public class LambdaDurationTests
    {
        private const int EnoughMillisecondsForMutualizedSoftwareFactorySlaveToSucceed = 15 * 1000;

        [Test]
        public void DurationTest()
        {
            Check.ThatCode(() => Thread.Sleep(1)).LastsLessThan(EnoughMillisecondsForMutualizedSoftwareFactorySlaveToSucceed, TimeUnit.Milliseconds);
        }

        [Test]
        public void FailDurationTest()
        {
            Check.ThatCode(() =>
                {
                    Check.ThatCode(() => Thread.Sleep(0)).LastsLessThan(0, TimeUnit.Milliseconds);
                })
                .ThrowsAny()
                .AndWhichMessage().StartsWith(
                    Environment.NewLine +
                    "The checked code took too much time to execute." + Environment.NewLine +
                    "The checked execution time:");
        }

        [Test]
        public void ConsumedTest()
        {
            Check.ThatCode(
                () =>
                {
                    Thread.Sleep(20);
                }).ConsumesLessThan(100, TimeUnit.Milliseconds);
        }   

        [Test]
        public void ConsumedTestFailsProperly()
        {
            Check.ThatCode(() =>
            {
                Check.ThatCode(
                () =>
                {
                    var timer = new Stopwatch();
                    timer.Start();
                    while (timer.ElapsedMilliseconds < 40)
                    {
                        for (var i = 0; i < 100000; i++)
                        {
                            var unused = i * 2;
                        }
                    }
                }).ConsumesLessThan(5, TimeUnit.Milliseconds);
            })
            .IsAFaillingCheckWithMessage("",
                    "The checked code consumed too much CPU time.",
                    "The checked cpu time:", 
                    "*",
                    "The expected cpu time: less than",
                    "*"); 
        }
    }
}