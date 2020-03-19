﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Xunit.Abstractions;

namespace DaprExtensionTests.Logging
{
    sealed class TestLogProvider : ILoggerProvider
    {
        readonly ITestOutputHelper output;
        readonly ConcurrentDictionary<string, TestLogger> loggers;

        public TestLogProvider(ITestOutputHelper output)
        {
            this.output = output ?? throw new ArgumentNullException(nameof(output));
            this.loggers = new ConcurrentDictionary<string, TestLogger>(StringComparer.OrdinalIgnoreCase);
        }

        public bool TryGetLogs(string category, out IEnumerable<LogEntry> logs)
        {
            if (this.loggers.TryGetValue(category, out TestLogger logger))
            {
                logs = logger.GetLogs();
                return true;
            }

            logs = Enumerable.Empty<LogEntry>();
            return false;
        }

        ILogger ILoggerProvider.CreateLogger(string categoryName)
        {
            return this.loggers.GetOrAdd(
                categoryName,
                name => new TestLogger(this.output, name));
        }

        void IDisposable.Dispose()
        {
            // no-op
        }

        class TestLogger : ILogger
        {
            readonly ITestOutputHelper output;
            readonly List<LogEntry> entries;

            public TestLogger(ITestOutputHelper output, string categoryName)
            {
                this.output = output;
                this.entries = new List<LogEntry>();
            }

            public IReadOnlyCollection<LogEntry> GetLogs() => this.entries.AsReadOnly();

            IDisposable ILogger.BeginScope<TState>(TState state) => null;

            bool ILogger.IsEnabled(LogLevel logLevel) => true;

            void ILogger.Log<TState>(
                LogLevel level,
                EventId eventId,
                TState state,
                Exception exception,
                Func<TState, Exception, string> formatter)
            {
                var entry = new LogEntry(level, formatter(state, exception));
                this.entries.Add(entry);
                this.output.WriteLine(entry.ToString());
            }
        }
    }
}