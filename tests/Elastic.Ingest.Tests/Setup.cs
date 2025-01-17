﻿using System;
using System.Linq;
using System.Threading;
using Elastic.CommonSchema;
using Elastic.Transport;
using Elastic.Transport.VirtualizedCluster;
using Elastic.Transport.VirtualizedCluster.Audit;
using Elastic.Transport.VirtualizedCluster.Components;
using Elastic.Transport.VirtualizedCluster.Rules;

namespace Elastic.Ingest.Tests
{
	public static class TestSetup
	{
		public static ITransport<ITransportConfiguration> CreateClient(Func<VirtualCluster, VirtualCluster> setup)
		{
			var cluster = Virtual.Elasticsearch.Bootstrap(numberOfNodes: 1).Ping(c=>c.SucceedAlways());
			var virtualSettings = setup(cluster)
				.StaticConnectionPool()
				.Settings(s=>s.DisablePing());

			//var audit = new Auditor(() => virtualSettings);
			//audit.VisualizeCalls(cluster.ClientCallRules.Count);

			var settings = new TransportConfiguration(virtualSettings.ConnectionPool, virtualSettings.Connection)
				.DisablePing()
				.EnableDebugMode();
			return new Transport<TransportConfiguration>(settings);
		}

		public static ClientCallRule BulkResponse(this ClientCallRule rule, params int[] statusCodes) =>
			rule.Succeeds(TimesHelper.Once).ReturnResponse(BulkResponseBuilder.CreateResponse(statusCodes));

		public class TestSession : IDisposable
		{
			private int _rejections;
			private int _requests;
			private int _responses;
			private int _retries;
			private int _maxRetriesExceeded;

			public TestSession(ITransport<ITransportConfiguration> transport)
			{
				Transport = transport;
				BufferOptions = new BufferOptions<EcsDocument>()
				{
					ConcurrentConsumers = 1,
					MaxConsumerBufferSize = 2,
					MaxConsumerBufferLifetime = TimeSpan.FromSeconds(10),
					WaitHandle = WaitHandle,
					MaxRetries = 3,
					BackoffPeriod = times => TimeSpan.FromMilliseconds(1),
					ServerRejectionCallback = (list) => Interlocked.Increment(ref _rejections),
					BulkAttemptCallback = (c, a) => Interlocked.Increment(ref _requests),
					ElasticsearchResponseCallback = (r, b) => Interlocked.Increment(ref _responses),
					MaxRetriesExceededCallback = (list) => Interlocked.Increment(ref _maxRetriesExceeded),
					RetryCallBack = (list) => Interlocked.Increment(ref _retries),
					ExceptionCallback= (e) => LastException = e
				};
				ChannelOptions = new ElasticsearchChannelOptions<EcsDocument>(transport) { BufferOptions = BufferOptions };
				Channel = new ElasticsearchChannel<EcsDocument>(ChannelOptions);
			}

			public ElasticsearchChannel<EcsDocument> Channel { get; }

			public ITransport<ITransportConfiguration> Transport { get; }

			public ElasticsearchChannelOptions<EcsDocument> ChannelOptions { get; }

			public BufferOptions<EcsDocument> BufferOptions { get; }

			public ManualResetEventSlim WaitHandle { get; } = new ManualResetEventSlim();

			public int Rejections => _rejections;
			public int TotalBulkRequests => _requests;
			public int TotalBulkResponses => _responses;
			public int TotalRetries => _retries;
			public int MaxRetriesExceeded => _maxRetriesExceeded;
			public Exception LastException { get; private set; }

			public void Wait()
			{
				WaitHandle.Wait(TimeSpan.FromSeconds(10));
				WaitHandle.Reset();
			}

			public void Dispose()
			{
				Channel?.Dispose();
				WaitHandle?.Dispose();
			}
		}

		public static TestSession CreateTestSession(ITransport<ITransportConfiguration> transport) =>
			new TestSession(transport);

		public static void WriteAndWait(this TestSession session, int events = 1)
		{
			foreach (var b in Enumerable.Range(0, events))
				session.Channel.TryWrite(new EcsDocument { Timestamp = DateTimeOffset.UtcNow });
			session.Wait();
		}
	}
}
