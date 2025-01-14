﻿// Licensed to Elasticsearch B.V under one or more agreements.
// Elasticsearch B.V licenses this file to you under the Apache 2.0 License.
// See the LICENSE file in the project root for more information

using Serilog.Core;
using Serilog.Events;

namespace Elastic.Apm.SerilogEnricher
{
	/// <summary>
	/// This enricher adds trace id and transaction id for every log that is created during a
	/// transaction in case the Elastic APM Agent is active in your application. If there is
	/// an active span, the span id is also added.
	/// </summary>
	public sealed class ElasticApmEnricher : ILogEventEnricher
	{
		public void Enrich(LogEvent logEvent, ILogEventPropertyFactory propertyFactory)
		{
			if (!Agent.IsConfigured) return;
			if (Agent.Tracer is null) return;
			if (Agent.Tracer.CurrentTransaction is null) return;

			logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
				"ElasticApmTransactionId", Agent.Tracer.CurrentTransaction.Id));
			logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
				"ElasticApmTraceId", Agent.Tracer.CurrentTransaction.TraceId));

			if (Agent.Tracer.CurrentSpan != null)
			{
				logEvent.AddPropertyIfAbsent(propertyFactory.CreateProperty(
					"ElasticApmSpanId", Agent.Tracer.CurrentSpan.Id));
			}
		}
	}
}
