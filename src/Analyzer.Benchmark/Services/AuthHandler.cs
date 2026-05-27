using System.Net.Http.Headers;

namespace Analyzer.Benchmark.Services;

public class BenchmarkSession
{
    public string? Token { get; set; }
}

public class BenchmarkAuthHandler(BenchmarkSession session) : DelegatingHandler
{
    private readonly BenchmarkSession _session = session;

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(_session.Token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _session.Token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}