// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information.

#if FEATURE_FULL_CONSOLE

using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;

using Microsoft.Scripting.Utils;
using Microsoft.Scripting.Runtime;

namespace Microsoft.Scripting.Hosting.Shell {

    public class ConsoleHostOptionsParser {
        public ConsoleHostOptions Options { get; }
        public ScriptRuntimeSetup RuntimeSetup { get; }

        public ConsoleHostOptionsParser(ConsoleHostOptions options, ScriptRuntimeSetup runtimeSetup) {
            ContractUtils.RequiresNotNull(options, nameof(options));
            ContractUtils.RequiresNotNull(runtimeSetup, nameof(runtimeSetup));

            Options = options;
            RuntimeSetup = runtimeSetup;
        }

        /// <exception cref="InvalidOptionException"></exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        public void Parse(string[] args) {
            ContractUtils.RequiresNotNull(args, nameof(args));

            int i = 0;
            while (i < args.Length) {
                string current = args[i++];
                ParseOption(current, out string name, out string value);

                switch (name) {
                    case "console":
                        Options.RunAction = ConsoleHostOptions.Action.RunConsole;
                        break;

                    case "run":
                        OptionValueRequired(name, value);

                        Options.RunAction = ConsoleHostOptions.Action.RunFile;
                        Options.RunFile = value;
                        break;

                    case "lang":
                        OptionValueRequired(name, value);

                        string provider = null;
                        foreach (var language in RuntimeSetup.LanguageSetups) {
                            if (language.Names.Any(n => DlrConfiguration.LanguageNameComparer.Equals(n, value))) {
                                provider = language.TypeName;
                                break;
                            }
                        }
                        if (provider == null) {
                            throw new InvalidOptionException($"Unknown language id '{value}'.");
                        }

                        Options.LanguageProvider = provider;
                        Options.HasLanguageProvider = true;
                        break;

                    case "path":
                    case "paths":
                        OptionValueRequired(name, value);
                        Options.SourceUnitSearchPaths = value.Split(';');
                        break;

                    case "setenv":
                        Options.EnvironmentVars.AddRange(value.Split(';'));
                        break;

                    // first unknown/non-option:
                    case null:
                    default:
                        Options.IgnoredArgs.Add(current);
                        goto case "";

                    // host/passthru argument separator
                    case "/":
                    case "":
                        // ignore all arguments starting with the next one (arguments are not parsed):
                        while (i < args.Length) {
                            Options.IgnoredArgs.Add(args[i++]);
                        }
                        break;
                }
            }
        }

        /// <summary>
        /// name == null means that the argument doesn't specify an option; the value contains the entire argument
        /// name == "" means that the option name is empty (argument separator); the value is null then
        /// </summary>
        private void ParseOption(string arg, out string name, out string value) {
            Debug.Assert(arg != null);

            int colon = arg.IndexOf(':');

            if (colon >= 0) {
                name = arg.Substring(0, colon);
                value = arg.Substring(colon + 1);
            } else {
                name = arg;
                value = null;
            }

            if (name.StartsWith("--", StringComparison.Ordinal)) name = name.Substring("--".Length);
            else if (name.StartsWith("-", StringComparison.Ordinal) && name.Length > 1) name = name.Substring("-".Length);
            else if (name.StartsWith("/", StringComparison.Ordinal) && name.Length > 1) name = name.Substring("/".Length);
            else {
                value = name;
                name = null;
            }

            name = name?.ToLower(CultureInfo.InvariantCulture);
        }

        protected void OptionValueRequired(string optionName, string value) {
            if (value == null) {
                throw new InvalidOptionException(string.Format(CultureInfo.CurrentCulture, "Argument expected for the {0} option.", optionName));
            }
        }
    }
}

#endif
