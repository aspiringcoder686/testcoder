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
            request.Headers.TryAddWithoutValidation("Authorization", authHeader.ToString());
        }

        return await base.SendAsync(request, cancellationToken);
    }
}
builder.Services.AddHttpContextAccessor();
builder.Services.AddTransient<ForwardAuthHeaderHandler>();

var baseUrl = builder.Configuration["Reporting:BaseAddress"] ?? "http://localhost:5080/";
builder.Services.AddHttpClient<IReportingCalendarClient, HttpReportingCalendarClient>(c =>
{
    c.BaseAddress = new Uri(baseUrl);
})
.AddHttpMessageHandler<ForwardAuthHeaderHandler>();
