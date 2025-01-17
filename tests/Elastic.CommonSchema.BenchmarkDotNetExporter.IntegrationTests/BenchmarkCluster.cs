using Elastic.Elasticsearch.Xunit;
using Xunit;

[assembly: TestFramework("Elastic.Elasticsearch.Xunit.Sdk.ElasticTestFramework", "Elastic.Elasticsearch.Xunit")]

namespace Elastic.CommonSchema.BenchmarkDotNetExporter.IntegrationTests
{
	/// <summary> Declare our cluster that we want to inject into our test classes </summary>
	public class BenchmarkCluster : XunitClusterBase
	{
		public BenchmarkCluster() : base(new XunitClusterConfiguration("7.14.0") { StartingPortNumber = 9202 }) { }
	}
}
