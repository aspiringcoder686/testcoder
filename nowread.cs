using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Reporting.Client;

public class ForwardAuthHeaderHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public ForwardAuthHeaderHandler(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var ctx = _httpContextAccessor.HttpContext;

        if (ctx != null && ctx.Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            if (!request.Headers.Contains("Authorization"))
            {
                request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
            }
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

using System.Net.Http.Json;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Reporting.Contracts;

namespace Reporting.Client;

public sealed class HttpReportingCalendarClient : IReportingCalendarClient
{
    private readonly HttpClient _http;
    public HttpReportingCalendarClient(HttpClient http) => _http = http;

    public async Task<ReportPeriodId> GetCurrentAsync(CancellationToken ct = default)
    {
        var dto = await _http.GetFromJsonAsync<ReportPeriodDto>("api/v1/reportperiod/current", ct);
        if (dto is null) throw new InvalidOperationException("No current report period.");
        return new ReportPeriodId(dto.Id);
    }

    public async Task<ReportPeriodId?> TryGetByDateAsync(DateOnly date, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"api/v1/reportperiod/by-date?date={date:yyyy-MM-dd}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<ReportPeriodDto>(cancellationToken: ct);
        return dto is null ? null : new ReportPeriodId(dto.Id);
    }

    public async Task<ReportPeriodId?> TryGetPreviousAsync(ReportPeriodId id, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"api/v1/reportperiod/previous/{(Guid)id}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<ReportPeriodDto>(cancellationToken: ct);
        return dto is null ? null : new ReportPeriodId(dto.Id);
    }

    public async Task<ReportPeriodId?> TryGetNextAsync(ReportPeriodId id, CancellationToken ct = default)
    {
        var resp = await _http.GetAsync($"api/v1/reportperiod/next/{(Guid)id}", ct);
        if (resp.StatusCode == System.Net.HttpStatusCode.NotFound) return null;
        resp.EnsureSuccessStatusCode();
        var dto = await resp.Content.ReadFromJsonAsync<ReportPeriodDto>(cancellationToken: ct);
        return dto is null ? null : new ReportPeriodId(dto.Id);
    }
}

// ----- DI Helper with auth forwarding -----
public static class ReportingClientRegistration
{
    public static IHttpClientBuilder AddReportingCalendarHttpClient(this IServiceCollection services, Uri baseAddress)
    {
        // bring IHttpContextAccessor so handler can grab incoming headers
        services.AddHttpContextAccessor();
        services.AddTransient<ForwardAuthHeaderHandler>();

        return services.AddHttpClient<IReportingCalendarClient, HttpReportingCalendarClient>(c =>
        {
            c.BaseAddress = baseAddress;
            c.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        })
        .AddHttpMessageHandler<ForwardAuthHeaderHandler>();
    }
}



using Reporting.Client;
using Reporting.Contracts;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var baseUrl = builder.Configuration["Reporting:BaseAddress"] ?? "http://localhost:5080/";

// 👇 super simple now
builder.Services.AddReportingCalendarHttpClient(new Uri(baseUrl));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("v1/swagger.json", "ConsumerService v1");
        c.RoutePrefix = string.Empty;
    });
}

app.MapControllers();
app.Run();
