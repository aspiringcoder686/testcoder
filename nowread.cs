using System.Web;
using NHibernate;
using NHibernate.Cfg;
using log4net;

public class MvcApplication : HttpApplication
{
    public static ISessionFactory SessionFactory;

    protected void Application_Start()
    {
        // Initialize log4net
        log4net.Config.XmlConfigurator.Configure();

        // Initialize NHibernate
        var cfg = new Configuration();

        cfg.SetProperty(NHibernate.Cfg.Environment.ConnectionString, "YOUR_CONNECTION_STRING_HERE");
        cfg.SetProperty(NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MsSql2012Dialect");
        cfg.SetProperty(NHibernate.Cfg.Environment.ShowSql, "true");    // For Output window
        cfg.SetProperty(NHibernate.Cfg.Environment.FormatSql, "true");  // Nicely formatted SQL
        cfg.SetProperty(NHibernate.Cfg.Environment.GenerateStatistics, "true"); // Optional: more metrics

        // Add your mappings here
        cfg.AddAssembly(typeof(YourEntity).Assembly);  // Example

        SessionFactory = cfg.BuildSessionFactory();
    }
}


<configSections>
  <section name="log4net" type="log4net.Config.Log4NetConfigurationSectionHandler, log4net" />
</configSections>

<log4net>
  <appender name="FileAppender" type="log4net.Appender.FileAppender">
    <file value="App_Data\\NHibernateSQL.log" />
    <appendToFile value="true" />
    <layout type="log4net.Layout.PatternLayout">
      <conversionPattern value="%date %-5level %logger - %message%newline" />
    </layout>
  </appender>

  <logger name="NHibernate.SQL">
    <level value="DEBUG" />
    <appender-ref ref="FileAppender" />
  </logger>

  <root>
    <level value="WARN" />
    <appender-ref ref="FileAppender" />
  </root>
</log4net>
