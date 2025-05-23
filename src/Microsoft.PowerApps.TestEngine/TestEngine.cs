﻿// Copyright (c) Microsoft Corporation.
// Licensed under the MIT license.

using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.PowerApps.TestEngine.Config;
using Microsoft.PowerApps.TestEngine.Reporting;
using Microsoft.PowerApps.TestEngine.System;

namespace Microsoft.PowerApps.TestEngine
{
    /// <summary>
    /// This class covers the core functionality of Test Engine, including the entry point for executing tests.
    /// </summary>
    public class TestEngine
    {
        private readonly ITestState _state;
        private readonly IServiceProvider _serviceProvider;
        private readonly ITestReporter _testReporter;
        private readonly IFileSystem _fileSystem;
        private readonly ILoggerFactory _loggerFactory;
        private readonly ITestEngineEvents _eventHandler;

        public ILogger Logger { get; set; }

        public Func<DateTime> Timestamper { get; set; }

        public TestEngine(ITestState state,
                          IServiceProvider serviceProvider,
                          ITestReporter testReporter,
                          IFileSystem fileSystem,
                          ILoggerFactory loggerFactory,
                          ITestEngineEvents eventHandler)
        {
            _state = state;
            _serviceProvider = serviceProvider;
            _testReporter = testReporter;
            _fileSystem = fileSystem;
            _loggerFactory = loggerFactory;
            _eventHandler = eventHandler;
            Timestamper = () => DateTime.UtcNow;
        }

        /// <summary>
        /// This is the entry point function for executing tests. This function expects the Test Plan file along with 
        /// environment and tenant IDs along with other optional parameters and executes the specified tests on the app
        /// in the provided tenant and environment.
        /// </summary>
        /// <param name="testConfigFile">The (absolute or relative) path to the test plan file.</param>
        /// <param name="environmentId">The environment ID where the Power App is published.</param>
        /// <param name="tenantId">The tenant ID where the Power App is published.</param>
        /// <param name="outputDirectory">The output directory where the test results and logs are to be saved.</param>
        /// <param name="domain">The domain of the Power Apps Canvas Designer application where the app is published (Example: "https://apps.powerapps.com").</param>
        /// <param name="queryParams">Optional query parameters that would be passed to the Player URL for optional features or parameters.</param>
        /// <returns>The full path where the test results are saved.</returns>
        /// <exception cref="ArgumentNullException">Throws ArgumentNullException if any of testConfigFile, environmentId, tenantId or domain are missing or empty.</exception>
        public async Task<string> RunTestAsync(FileInfo testConfigFile, string environmentId, Guid? tenantId, DirectoryInfo outputDirectory, string domain, string queryParams)
        {
            // Set up test reporting
            var testRunId = _testReporter.CreateTestRun("Power Fx Test Runner", "User"); // TODO: determine if there are more meaningful values we can put here
            _testReporter.StartTestRun(testRunId);

            Logger = _loggerFactory.CreateLogger(testRunId);

            string testRunDirectory = string.Empty;

            try
            {
                // Setup state
                if (testConfigFile == null)
                {
                    throw new ArgumentNullException(nameof(testConfigFile));
                }

                if (string.IsNullOrEmpty(environmentId))
                {
                    throw new ArgumentNullException(nameof(environmentId));
                }

                if (tenantId == null)
                {
                    throw new ArgumentNullException(nameof(tenantId));
                }

                if (!string.IsNullOrEmpty(domain))
                {
                    if (!IsValidHttpsUrl(domain))
                    {
                        throw new ArgumentException(string.Format("   [Critical Error]: Invalid uri: {0}", nameof(domain)));
                    }
                }

                if (outputDirectory == null)
                {
                    throw new ArgumentNullException(nameof(outputDirectory));
                }
                else
                {
                    var inputUri = new Uri(outputDirectory.FullName.StartsWith(@"\\?\") ? outputDirectory.FullName.Replace(@"\\?\", "") : outputDirectory.FullName);
                    if (!new Uri(_fileSystem.GetDefaultRootTestEngine()).IsBaseOf(inputUri))
                    {
                        var wrongLocationError = $"Please ensure '{nameof(outputDirectory)}' is set to a value resolving to a location inside the permitted output location.";
                        Logger.LogError(wrongLocationError);
                        _eventHandler.EncounteredException(new UserInputException(string.Format("   [Critical Error]: {0}", wrongLocationError)));
                        return "InvalidOutputDirectory";
                    }
                }

                if (string.IsNullOrEmpty(queryParams))
                {
                    Logger.LogDebug($"Using no additional query parameters.");
                }
                else
                {
                    Logger.LogDebug($"Using query: {queryParams}");
                }

                // Create the output directory as early as possible so that any exceptions can be logged.
                _state.SetOutputDirectory(outputDirectory.FullName);
                Logger.LogDebug($"Using output directory: {outputDirectory.FullName}");

                _state.SetTestConfigFile(testConfigFile);

                var now = Timestamper().ToString("o")
                    .Replace(":", "-")
                    .Replace(".", "-");

                testRunDirectory = Path.Combine(_state.GetOutputDirectory(), now + "-" + testRunId.Substring(0, 6));
                _fileSystem.CreateDirectory(testRunDirectory);
                Logger.LogInformation($"Test results will be stored in: {testRunDirectory}");

                _state.ParseAndSetTestState(testConfigFile.FullName, Logger);
                _state.SetEnvironment(environmentId);
                _state.SetTenant(tenantId.ToString());
                _state.LoadExtensionModules(Logger);

                _state.SetDomain(domain);
                Logger.LogDebug($"Using domain: {domain}");

                await RunTestByBrowserAsync(testRunId, testRunDirectory, domain, queryParams);
                _testReporter.EndTestRun(testRunId);
                return _testReporter.GenerateTestReport(testRunId, testRunDirectory);
            }
            catch (UserInputException e)
            {
                _eventHandler.EncounteredException(e);
                if (string.IsNullOrEmpty(testRunDirectory))
                {
                    return "InvalidOutputDirectory";
                }
                else
                {
                    return testRunDirectory;
                }
            }
            catch (DirectoryNotFoundException)
            {
                _eventHandler.EncounteredException(new UserInputException(UserInputException.ErrorMapping.UserInputExceptionInvalidOutputPath.ToString()));
                return "InvalidOutputDirectory";
            }
            catch (Exception e)
            {
#if RELEASE
                if(string.IsNullOrEmpty(testRunDirectory))
                {
                    _eventHandler.EncounteredException(e);
                    return "InvalidOutputDirectory";
                }
                else
                {
                    Logger.LogError(e.Message);
                    return testRunDirectory;
                }
#else
                Logger.LogError(e.Message);
                throw;
#endif
            }
            finally

            {
                if (!string.IsNullOrEmpty(testRunDirectory) && TestLoggerProvider.TestLoggers.ContainsKey(testRunId))
                {
                    var testLogger = TestLoggerProvider.TestLoggers[testRunId];
                    testLogger.WriteToLogsFile(testRunDirectory, null);
                }
            }
        }

        private async Task RunTestByBrowserAsync(string testRunId, string testRunDirectory, string domain, string queryParams)
        {
            var testSettings = _state.GetTestSettings();

            var locale = GetLocaleFromTestSettings(testSettings.Locale);

            var browserConfigurations = testSettings.BrowserConfigurations;

            // Run sequentially
            foreach (var browserConfig in browserConfigurations)
            {
                await RunOneTestAsync(testRunId, testRunDirectory, _state.GetTestSuiteDefinition(), browserConfig, domain, queryParams, locale);
            }
        }
        private async Task RunOneTestAsync(string testRunId, string testRunDirectory, TestSuiteDefinition testSuiteDefinition, BrowserConfiguration browserConfig, string domain, string queryParams, CultureInfo locale)
        {
            using (IServiceScope scope = _serviceProvider.CreateScope())
            {
                var singleTestRunner = scope.ServiceProvider.GetRequiredService<ISingleTestRunner>();

                await singleTestRunner.RunTestAsync(testRunId, testRunDirectory, testSuiteDefinition, browserConfig, domain, queryParams, locale);
            }
        }

        public CultureInfo GetLocaleFromTestSettings(string strLocale)
        {
            var locale = CultureInfo.CurrentCulture;
            try
            {
                if (string.IsNullOrEmpty(strLocale))
                {
                    Logger.LogDebug($"Locale property not specified in testSettings. Using current system locale: {locale.Name}");
                }
                else
                {
                    locale = new CultureInfo(strLocale);
                    Logger.LogDebug($"Locale: {locale.Name}");
                }
                return locale;
            }
            catch (CultureNotFoundException)
            {
                Logger.LogError($"Locale from test suite definition {strLocale} unrecognized.");
                throw new UserInputException(UserInputException.ErrorMapping.UserInputExceptionInvalidTestSettings.ToString());
            }
        }

        public bool IsValidHttpsUrl(string val)
        {
            return Uri.TryCreate(val, UriKind.Absolute, out var uriResult) && uriResult?.Scheme == Uri.UriSchemeHttps;
        }
    }
}
