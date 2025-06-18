protected void Application_BeginRequest(object sender, EventArgs e)
{
    var url = HttpContext.Current.Request.Url.AbsolutePath;
    var method = HttpContext.Current.Request.HttpMethod;

    var queryString = HttpContext.Current.Request.QueryString.ToString();
    log4net.ThreadContext.Properties["Url"] = url;
    log4net.ThreadContext.Properties["HttpMethod"] = method;
    log4net.ThreadContext.Properties["Query"] = queryString;
}

protected void Application_EndRequest(object sender, EventArgs e)
{
    log4net.ThreadContext.Properties.Remove("Url");
    log4net.ThreadContext.Properties.Remove("HttpMethod");
    log4net.ThreadContext.Properties.Remove("Query");  // If you added query string logging
}

<conversionPattern value="--- Starts query ---%newline%date %-5level %logger - [Url: %property{Url}] [HttpMethod: %property{HttpMethod}] %message%newline--- Ends query ---%newline" />


    [Query: %property{Query}]
