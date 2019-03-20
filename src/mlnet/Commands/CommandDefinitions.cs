// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Invocation;
using System.IO;
using System.Linq;

namespace Microsoft.ML.CLI.Commands
{
    internal static class CommandDefinitions
    {
        internal static System.CommandLine.Command New(ICommandHandler handler)
        {
            var newCommand = new System.CommandLine.Command("new", "Create a new .NET project using ML.NET to train and run a model", handler: handler)
            {
                Dataset(),
                ValidationDataset(),
                TestDataset(),
                MlTask(),
                LabelName(),
                MaxExplorationTime(),
                LabelColumnIndex(),
                Verbosity(),
                Name(),
                OutputPath(),
                HasHeader(),
                Cache(),
                IgnoreColumns(),
            };

            newCommand.Argument.AddValidator((sym) =>
            {
                if (!sym.Children.Contains("--dataset"))
                {
                    return "Option required : --dataset";
                }
                if (!sym.Children.Contains("--ml-task"))
                {
                    return "Option required : --ml-task";
                }
                if (!sym.Children.Contains("--label-column-name") && !sym.Children.Contains("--label-column-index"))
                {
                    return "Option required : --label-column-name or --label-column-index";
                }
                if (sym.Children.Contains("--label-column-name") && sym.Children.Contains("--label-column-index"))
                {
                    return "The following options are mutually exclusive please provide only one : --label-column-name, --label-column-index";
                }
                if (sym.Children.Contains("--label-column-index") && sym.Children["--ignore-columns"]?.Arguments.Count > 0)
                {
                    return "Currently we don't support specifying --ignore-columns in conjunction with --label-column-index";
                }

                return null;
            });

            return newCommand;

            Option Dataset() =>
                new Option("--dataset", "File path to either a single dataset or a training dataset for train/test split approaches.",
                new Argument<FileInfo>().ExistingOnly());

            Option ValidationDataset() =>
                new Option("--validation-dataset", "File path for the validation dataset in train/validation/test split approaches.",
                new Argument<FileInfo>(defaultValue: default(FileInfo)).ExistingOnly());

            Option TestDataset() =>
                new Option("--test-dataset", "File path for the test dataset in train/test approaches.",
                new Argument<FileInfo>(defaultValue: default(FileInfo)).ExistingOnly());

            Option MlTask() =>
                new Option("--ml-task", "Type of ML task to perform. Current supported tasks: regression and binary-classification",
                new Argument<string>().FromAmong(GetMlTaskSuggestions()));

            Option LabelName() =>
                new Option("--label-column-name", "Name of the label (target) column to predict.",
                new Argument<string>());

            Option LabelColumnIndex() =>
                new Option("--label-column-index", "Index of the label (target) column to predict.",
                new Argument<uint>());

            Option MaxExplorationTime() =>
                new Option("--max-exploration-time", "Maximum time in seconds for exploring models with best configuration.",
                new Argument<uint>(defaultValue: 10));

            Option Verbosity() =>
                new Option(new List<string>() { "--verbosity" }, "Output verbosity choices: q[uiet], m[inimal] (by default) and diag[nostic]",
                new Argument<string>(defaultValue: "m").FromAmong(GetVerbositySuggestions()));

            Option Name() =>
                new Option(new List<string>() { "--name" }, "Name for the output project or solution to create. ",
                new Argument<string>());

            Option OutputPath() =>
                new Option(new List<string>() { "--output-path" }, "Location folder to place the generated output. The default is the current directory.",
                new Argument<DirectoryInfo>(defaultValue: new DirectoryInfo(".")));

            Option HasHeader() =>
                new Option(new List<string>() { "--has-header" }, "Specify true/false depending if the dataset file(s) have a header row.",
                new Argument<bool>(defaultValue: true));

            Option Cache() =>
                new Option(new List<string>() { "--cache" }, "Specify on/off/auto if you want cache to be turned on, off or auto determined.",
                new Argument<string>(defaultValue: "auto").FromAmong(GetCacheSuggestions()));

            // This is a temporary hack to work around having comma separated values for argument. This feature needs to be enabled in the parser itself.
            Option IgnoreColumns() =>
                new Option(new List<string>() { "--ignore-columns" }, "Specify the columns that needs to be ignored in the given dataset.",
                new Argument<List<string>>(symbolResult =>
                {
                    try
                    {
                        List<string> valuesList = new List<string>();
                        foreach (var argument in symbolResult.Arguments)
                        {
                            if (!string.IsNullOrWhiteSpace(argument))
                            {
                                var values = argument.Split(",", StringSplitOptions.RemoveEmptyEntries);
                                valuesList.AddRange(values);
                            }
                        }
                        if (valuesList.Count > 0)
                            return ArgumentResult.Success(valuesList);

                    }
                    catch (Exception)
                    {
                        return ArgumentResult.Failure($"Unknown exception occured while parsing argument for --ignore-columns :{string.Join(' ', symbolResult.Arguments.ToArray())}");
                    }

                    //This shouldn't be hit.
                    return ArgumentResult.Failure($"Unknown error while parsing argument for --ignore-columns");
                })
                {
                    Arity = ArgumentArity.OneOrMore,
                });
        }

        private static string[] GetMlTaskSuggestions()
        {
            return new[] { "binary-classification", "multiclass-classification", "regression" };
        }

        private static string[] GetVerbositySuggestions()
        {
            return new[] { "q", "m", "diag" };
        }

        private static string[] GetCacheSuggestions()
        {
            return new[] { "on", "off", "auto" };
        }
    }
}
