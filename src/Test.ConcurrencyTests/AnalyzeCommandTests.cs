﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;

using FluentAssertions;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.IL;
using Microsoft.CodeAnalysis.IL.Rules;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.Coyote;
using Microsoft.Coyote.Specifications;
using Microsoft.Coyote.SystematicTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using System.Text;


using Microsoft.CodeAnalysis.IL.Sdk;

using Microsoft.CodeAnalysis.Sarif.Driver;
using Microsoft.CodeAnalysis.Sarif.Readers;
using Microsoft.CodeAnalysis.Sarif.VersionOne;
using Microsoft.CodeAnalysis.Sarif.Visitors;

using Moq;

//using Xunit;

namespace Test.CoyoteTests
{
    [TestClass]
    public class AnalyzeCommandTests
    {
        [TestMethod]
        public async Task AnalyzeCommand_TypicalPEFilesTest()
        {
            Task task = Task.Run(() =>
            {
                if (!PlatformSpecificHelpers.RunningOnWindows()) { return; }

            WindowsBinaryAndPdbSkimmerBase.s_PdbExceptions.Clear();
            string fileName = Path.Combine(Path.GetTempPath(), "AnalyzeCommand_TypicalPEFilesTest.sarif");
            string[] TypicalPEBaselineTestFiles = new string[] {
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "clangcl.pe.cpp.codeview.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Corrupted_Native_x86_VS2013_Default.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "gcc.pe.objectivec.dwarf.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Managed_x64_VS2015_FSharp.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Managed_x64_VS2019_CSharp_DebugType_Full.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Managed_x64_VS2019_CSharp_DebugType_None.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Managed_x64_VS2019_CSharp_DebugType_Portable.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Managed_x64_VS2019_VB_DebugType_Full.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Managed_x64_VS2019_VB_DebugType_None.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Managed_x64_VS2019_VB_DebugType_Portable.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Managed_x86_VS2013_Wpf.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "ManagedResourcesOnly.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "MixedMode_x64_VS2019_CPlusPlus_DEBUG_DEFAULT.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "MixedMode_x64_VS2019_CPlusPlus_DEBUG_FULL.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "MixedMode_x64_VS2019_CPlusPlus_DEBUG_NONE.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_ARM_VS2015_CvtresResourceOnly.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_RustC_Rust_debuginfo2_v1.58.1.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_VS2013_KernelModeDriver.sys"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_VS2015_CvtresResourceOnly.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_VS2019_CPlusPlus_DEBUG_DEFAULT.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_VS2019_CPlusPlus_DEBUG_FULL.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_VS2019_CPlusPlus_DEBUG_NONE.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_VSCode_Rust_DebugInfo_0.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_VSCode_Rust_DebugInfo_1.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x64_VSCode_Rust_DebugInfo_2.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x86_VS2013_KernelModeDriver.sys"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Native_x86_VS2017_15.5.4_PdbStripped.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Uwp_ARM_VS2017_VB.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Uwp_x64_VS2017_Cpp.dll"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Wix_3.11.1_VS2017_Bootstrapper.exe"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "Wix_3.11.1_VS2017_Msi.msi")
            };

            var options = new AnalyzeOptions
            {
                TargetFileSpecifiers = TypicalPEBaselineTestFiles,
                Level = new[] { FailureLevel.Error, FailureLevel.Warning, FailureLevel.Note, FailureLevel.None },
                Kind = new[] { ResultKind.Fail, ResultKind.Pass },
                OutputFilePath = fileName,
                OutputFileOptions = new[] { FilePersistenceOptions.ForceOverwrite },
                Recurse = true,
                Threads = 10,
                IgnorePdbLoadError = false,
                DataToInsert = new[] { OptionallyEmittedData.Hashes }
            };
            var command = new MultithreadedAnalyzeCommand();
            command.Run(options);
            var log = SarifLog.Load(fileName);
            log.Runs[0].Invocations[0].ToolConfigurationNotifications.Count.Should().Be(8);
            log.Runs[0].Results.Count.Should().Be(372);
            });
            await task;
        }

        [TestMethod]
        public async Task AnalyzeCommand_TypicalNonPEFilesTest()
        {
            Task task = Task.Run(() =>
            {
                string fileName = Path.Combine(Path.GetTempPath(), "AnalyzeCommand_TypicalNonPEFilesTest.sarif");
                string[] TypicalPEBaselineTestFiles = new string[] {
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "gcc.gsplitdwarf.5"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "gcc.helloworld.execstack.5.o"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "gcc.helloworld.nodwarf"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "gcc.object_file.dwarf.3.o"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "macho.clang-lib.fat.o"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "macho.clang_exe.pie"),
                Path.Combine(PEBinaryTests.BaselineTestDataDirectory, "macho.gcc-lib.nostackclashprotect.o")
            };

                var options = new AnalyzeOptions
                {
                    TargetFileSpecifiers = TypicalPEBaselineTestFiles,
                    Level = new[] { FailureLevel.Error, FailureLevel.Warning, FailureLevel.Note, FailureLevel.None },
                    Kind = new[] { ResultKind.Fail, ResultKind.Pass },
                    OutputFilePath = fileName,
                    OutputFileOptions = new[] { FilePersistenceOptions.ForceOverwrite },
                    Recurse = true,
                    Threads = 10,
                    IgnorePdbLoadError = false,
                    DataToInsert = new[] { OptionallyEmittedData.Hashes }
                };
                var command = new MultithreadedAnalyzeCommand();
                command.Run(options);
                var log = SarifLog.Load(fileName);
                log.Runs[0].Invocations[0].ToolConfigurationNotifications.Should().BeNull();
                log.Runs[0].Results.Count.Should().Be(31);
            });
            await task;
        }

        [TestMethod, TestCategory("NightlyTest")]
        public void SystematicTestScenario()
        {
            RunSystematicTest(AnalyzeCommand_TypicalPEFilesTest);
            RunSystematicTest(AnalyzeCommand_TypicalNonPEFilesTest);
        }


        private static void RunSystematicTest(Func<Task> test)
        {
            // Configuration for how to run a concurrency unit test with Coyote.
            Configuration config = Configuration
                .Create()
                .WithMaxSchedulingSteps(5000)
                .WithTestingIterations(1000);

            async Task TestActionAsync()
            {
                await test();
            };

            var testingEngine = TestingEngine.Create(config, TestActionAsync);

            try
            {
                testingEngine.Run();

                string assertionText = testingEngine.TestReport.GetText(config);
                assertionText +=
                    $"{Environment.NewLine} Random Generator Seed: " +
                    $"{testingEngine.TestReport.Configuration.RandomGeneratorSeed}{Environment.NewLine}";
                foreach (string bugReport in testingEngine.TestReport.BugReports)
                {
                    assertionText +=
                    $"{Environment.NewLine}" +
                    "Bug Report: " + bugReport.ToString(CultureInfo.InvariantCulture);
                }

                Assert.IsTrue(testingEngine.TestReport.NumOfFoundBugs == 0, assertionText);

                Console.WriteLine(testingEngine.TestReport.GetText(config));
            }
            finally
            {
                testingEngine.Stop();
                testingEngine.Dispose();
            }
        }

    }
}
