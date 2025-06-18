using System;
using System.Collections.Concurrent;
using System.IO;
using System.Text;
using System.Web;
using log4net.Appender;
using log4net.Core;

public class NHibernateGroupedSqlAppender : AppenderSkeleton
{
    private static readonly ConcurrentDictionary<string, StringBuilder> _buffers = new();
    private static readonly ConcurrentDictionary<string, string> _requestInfo = new();

    public string FilePath { get; set; }

    protected override void Append(LoggingEvent loggingEvent)
    {
        if (loggingEvent.LoggerName != "NHibernate.SQL")
            return;

        var requestId = GetRequestId();
        var sb = _buffers.GetOrAdd(requestId, _ => new StringBuilder());

        sb.AppendLine(RenderLoggingEvent(loggingEvent));
    }

    public static void SetRequestInfo(string requestId, string info)
    {
        _requestInfo[requestId] = info;
    }

    public static void Flush(string requestId, NHibernateGroupedSqlAppender appender)
    {
        if (_buffers.TryRemove(requestId, out var sb))
        {
            _requestInfo.TryRemove(requestId, out var info);

            var output = new StringBuilder();
            output.AppendLine($"--- Starts request SQL [{info}] ---");
            output.Append(sb);
            output.AppendLine($"--- Ends request SQL [{info}] ---");

            File.AppendAllText(appender.FilePath, output.ToString());
        }
    }

    private string GetRequestId()
    {
        return HttpContext.Current?.TraceIdentifier 
            ?? System.Threading.Thread.CurrentThread.ManagedThreadId.ToString();
    }
}


protected void Application_BeginRequest(object sender, EventArgs e)
{
    var request = HttpContext.Current.Request;
    var method = request.HttpMethod.ToUpper();
    var url = request.Url.AbsolutePath;
    var query = request.QueryString.ToString();
    var fullUrl = !string.IsNullOrEmpty(query) ? $"{url}?{query}" : url;

    var requestId = HttpContext.Current.TraceIdentifier;
    NHibernateGroupedSqlAppender.SetRequestInfo(requestId, $"{method}: {fullUrl}");
}

protected void Application_EndRequest(object sender, EventArgs e)
{
    var repo = log4net.LogManager.GetRepository();
    foreach (var appender in repo.GetAppenders())
    {
        if (appender is NHibernateGroupedSqlAppender customAppender)
        {
            var requestId = HttpContext.Current.TraceIdentifier;
            NHibernateGroupedSqlAppender.Flush(requestId, customAppender);
        }
    }
}


<appender name="NHibernateGroupedSqlAppender" type="YourNamespace.NHibernateGroupedSqlAppender, YourAssemblyName">
    <param name="FilePath" value="C:\\Logs\\AMS_SQL.log" />
  </appender>
