using NHibernate;
using NHibernate.Cfg;

public static class NHibernateInitializer
{
    private static ISessionFactory _sessionFactory;

    public static ISessionFactory Initialize()
    {
        if (_sessionFactory != null)
            return _sessionFactory;

        var cfg = new Configuration();

        // Your connection and dialect
        cfg.SetProperty(NHibernate.Cfg.Environment.ConnectionString, "Data Source=YOUR_SERVER;Initial Catalog=YOUR_DB;Integrated Security=True");
        cfg.SetProperty(NHibernate.Cfg.Environment.Dialect, "NHibernate.Dialect.MsSql2012Dialect");

        // 👇 ADD THESE TWO LINES
        cfg.SetProperty(NHibernate.Cfg.Environment.ShowSql, "true");     // This shows the SQL
        cfg.SetProperty(NHibernate.Cfg.Environment.FormatSql, "true");   // Nicely formats the SQL

        // Load mappings (XML or assembly)
        cfg.AddAssembly(typeof(YourEntityClass).Assembly);  // Replace with one of your mapped entities

        _sessionFactory = cfg.BuildSessionFactory();

        return _sessionFactory;
    }
}


using (var session = NHibernateInitializer.Initialize().OpenSession())
{
    // Your existing code
}
