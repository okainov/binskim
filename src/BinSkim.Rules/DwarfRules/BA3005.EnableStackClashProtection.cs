﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;

using Microsoft.CodeAnalysis.BinaryParsers;
using Microsoft.CodeAnalysis.BinaryParsers.Dwarf;
using Microsoft.CodeAnalysis.IL.Sdk;
using Microsoft.CodeAnalysis.Sarif;
using Microsoft.CodeAnalysis.Sarif.Driver;

namespace Microsoft.CodeAnalysis.IL.Rules
{
    [Export(typeof(Skimmer<BinaryAnalyzerContext>)), Export(typeof(ReportingDescriptor))]
    public class EnableStackClashProtection : DwarfSkimmerBase
    {
        /// <summary>
        /// BA3005
        /// </summary>
        public override string Id => RuleIds.EnableStackClashProtection;

        /// <summary>
        /// This check ensures that stack clash protection is enabled. 
        /// Each program running on a computer uses a special memory region called the stack. 
        /// This memory region is special because it grows automatically when the program needs 
        /// more stack memory. But if it grows too much and gets too close to another memory region, 
        /// the program may confuse the stack with the other memory region. An attacker can exploit 
        /// this confusion to overwrite the stack with the other memory region, or the other way around. 
        /// Use the compiler flags '-fstack-clash-protection' to enable this.
        /// </summary>
        public override MultiformatMessageString FullDescription =>
            new MultiformatMessageString { Text = RuleResources.BA3005_EnableStackClashProtection_Description };

        protected override IEnumerable<string> MessageResourceNames => new string[] {
            nameof(RuleResources.BA3005_Pass),
            nameof(RuleResources.BA3005_Error),
            nameof(RuleResources.NotApplicable_InvalidMetadata)
        };

        public override AnalysisApplicability CanAnalyzeDwarf(IDwarfBinary target, Sarif.PropertiesDictionary policy, out string reasonForNotAnalyzing)
        {
            return CommandLineUtilities.CanAnalyzeDwarf(target, out reasonForNotAnalyzing);
        }

        public override void Analyze(BinaryAnalyzerContext context)
        {
            IDwarfBinary binary = context.DwarfBinary();
            List<DwarfCompileCommandLineInfo> failedList;

            static bool analyze(IDwarfBinary binary, out List<DwarfCompileCommandLineInfo> failedList)
            {
                failedList = new List<DwarfCompileCommandLineInfo>();

                foreach (DwarfCompileCommandLineInfo info in binary.CommandLineInfos)
                {
                    if (!info.ParametersIncluded)
                    {
                        continue;
                    }

                    if (!info.CommandLine.Contains("-fstack-clash-protection", StringComparison.OrdinalIgnoreCase)
                        || info.CommandLine.Contains("-fno-stack-clash-protection", StringComparison.OrdinalIgnoreCase))
                    {
                        failedList.Add(info);
                    }
                }

                return !failedList.Any();
            }

            if (binary is ElfBinary elf)
            {
                if (!analyze(elf, out failedList))
                {
                    // The Stack Clash Protection is missing from this binary,
                    // so the stack from '{0}' can clash/colide with another memory region.
                    // Ensure you are compiling with the compiler flags '-fstack-clash-protection' to address this.
                    // Modules did not meet the criteria: {1}
                    context.Logger.Log(this,
                        RuleUtilities.BuildResult(FailureLevel.Error, context, null,
                            nameof(RuleResources.BA3005_Error),
                            context.TargetUri.GetFileName(),
                            DwarfUtility.GetDistinctNames(failedList, context.TargetUri.GetFileName())));
                    return;
                }

                // The Stack Clash Protection was present, so '{0}' is protected.
                context.Logger.Log(this,
                    RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                        nameof(RuleResources.BA3005_Pass),
                        context.TargetUri.GetFileName()));
                return;
            }

            if (binary is MachOBinary mainBinary)
            {
                foreach (SingleMachOBinary subBinary in mainBinary.MachOs)
                {
                    if (!analyze(subBinary, out failedList))
                    {
                        // The Stack Clash Protection is missing from this binary,
                        // so the stack from '{0}' can clash/colide with another memory region.
                        // Ensure you are compiling with the compiler flags '-fstack-clash-protection' to address this.
                        // Modules did not meet the criteria: {1}
                        context.Logger.Log(this,
                            RuleUtilities.BuildResult(FailureLevel.Error, context, null,
                                nameof(RuleResources.BA3005_Error),
                                context.TargetUri.GetFileName(),
                                DwarfUtility.GetDistinctNames(failedList, context.TargetUri.GetFileName())));
                        return;
                    }
                }

                // The Stack Clash Protection was present, so '{0}' is protected.
                context.Logger.Log(this,
                    RuleUtilities.BuildResult(ResultKind.Pass, context, null,
                        nameof(RuleResources.BA3005_Pass),
                        context.TargetUri.GetFileName()));
            }
        }
    }
}
