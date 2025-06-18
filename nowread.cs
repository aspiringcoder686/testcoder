protected void Application_BeginRequest(object sender, EventArgs e)
{
    var request = HttpContext.Current.Request;
    var url = request.Url.AbsolutePath;
    var query = request.QueryString.ToString();
    var method = request.HttpMethod.ToUpper();  // GET, POST, etc.

    var fullUrl = !string.IsNullOrEmpty(query) ? $"{url}?{query}" : url;

    // Set it in the ThreadContext as: [GET: /path?qs]
    log4net.ThreadContext.Properties["RequestInfo"] = $"{method}: {fullUrl}";
}

protected void Application_EndRequest(object sender, EventArgs e)
{
    log4net.ThreadContext.Properties.Remove("RequestInfo");
}


<conversionPattern value="%date %-5level %logger - [%property{RequestInfo}] %message%newline" />
