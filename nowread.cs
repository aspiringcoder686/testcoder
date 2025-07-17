
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq.Expressions;
using NHibernate;
using NHibernate.Hql.Ast;
using NHibernate.SqlCommand;
using NHibernate.Util;
using log4net;
using Microsoft.Data.SqlClient;


namespace SampleProjWithLimitedFn
{
    public class AmsEntityManager : EfEntityManager
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(AmsEntityManager));
        private readonly HibernateEntityManagerFactory factory;

        public AmsEntityManager(MyAppDbContext dbContext)
            : base(dbContext)
        {
        }

        public virtual IList<T> Query<T>(Expression<Func<T, bool>> where = null,
                                         Expression<Func<T, T>> select = null,
                                         bool approvedOnly = true)
            where T : class
        {
            throw new NotImplementedException();
        }

        public virtual IList<TResult> Query<T, TResult>(Expression<Func<T, bool>> where,
                                                         Expression<Func<T, TResult>> select,
                                                         bool approvedOnly = true)
            where T : class
            where TResult : class
        {
            throw new NotImplementedException();
        }

        public virtual IList<TResult> QueryProjection<T, TResult>(
            Func<IQueryable<T>, IQueryable<T>> queryFilter = null,
            Expression<Func<T, TResult>> select = null,
            bool approvedOnly = true)
            where T : class
            where TResult : class
        {
            throw new NotImplementedException();
        }

        //public virtual IEnumerable<T> SqlQuery<T>(string query,
        //                                          Func<IDataReader, T> projection,
        //                                          IEnumerable<SqlParmeter> parameters = null)
        //{
        //    throw new NotImplementedException();
        //}

        public override T Find<T>(object key)
        {
            throw new NotImplementedException();
        }

        public override IList<T> FindAll<T>()
        {
            throw new NotImplementedException();
        }

        private const string DIMUPLOADStatement = "DELETE FROM REP_DIM_AP_UPLOAD_RELATIONSHIP WHERE UPLOAD_KEY='$0'";
        private const string AMASSETStatement = "DELETE FROM REP_DIM_AP_UPLOAD_RELATIONSHIP WHERE ASSET_ID='$0'";
        private const string AMHPCStatement = "DELETE FROM REP_DIM_AP_UPLOAD_RELATIONSHIP WHERE ASSET_PORTFOLIO_ID='$0'";
        private const string AMPERIODStatement = "DELETE FROM REP_DIM_AP_UPLOAD_RELATIONSHIP WHERE REPORTING_PERIOD='$0'";

        public override void CreateOrUpdate(object o)
        {
            throw new NotImplementedException();
        }

        public override void Create(object o)
        {
            throw new NotImplementedException();
        }

        public override void Remove(object o)
        {
            throw new NotImplementedException();
        }

        private void UpdateDependentTables(object o)
        {
            throw new NotImplementedException();
        }

        private bool IsInterceptedType(Type type)
        {
            throw new NotImplementedException();
        }

        public virtual void SqlStoredProcedure(string sProcedureName,
                                               IEnumerable<SqlParameter> sqlparameters = null,
                                               int? commandTimeout = null,
                                               bool useReportingDb = false,
                                               bool throwException = false)
        {
            throw new NotImplementedException();
        }

        public virtual DataTable SqlStoredProcedureWithDataTable(string sProcedureName,
                                                                  IEnumerable<SqlParameter> sqlparameters = null,
                                                                  int? commandTimeout = null,
                                                                  bool useReportingDb = false,
                                                                  bool throwException = false)
        {
            throw new NotImplementedException();
        }

        public int ExecuteSQLQueryInt(string sql, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public override void ExecuteSQLQuery(string sql)
        {
            throw new NotImplementedException();
        }

        public override void ExecuteSQLQuery(string sql, IDictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> FindAllByNamedQuery<T>(string queryName,
                                                       Dictionary<string, object> parameters,
                                                       bool approvedOnly = true)
        {
            throw new NotImplementedException();
        }

        public override IList<T> FindAllByNamedQuery<T>(string queryName, object[] parameters)
        {
            throw new NotImplementedException();
        }

        public override IList<T> FindAllByNamedQuery<T>(string queryName, object[] parameters, int? start, int? max)
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> FindAllByNamedQuery<T>(string queryName,
                                                       object[] parameters = null,
                                                       int? start = null,
                                                       int? max = null,
                                                       bool approvedOnly = true)
        {
            throw new NotImplementedException();
        }

        public virtual T FindByNamedQuery<T>(string queryName)
        {
            throw new NotImplementedException();
        }

        public virtual T FindByNamedQuery<T>(string queryName, object[] parameters)
        {
            throw new NotImplementedException();
        }

        public virtual T FindByNamedQuery<T>(string queryName, object[] parameters, int? start, int? max)
        {
            throw new NotImplementedException();
        }

        private T FindSingle<T>(IList<T> list)
        {
            throw new NotImplementedException();
        }

        protected IList<T> FilterListByApproval<T>(bool doFilter, IList<T> originalList)
        {
            throw new NotImplementedException();
        }

        private Guid AssetIdSelector<T>(T item)
        {
            throw new NotImplementedException();
        }

        public IDictionary<Guid, bool> GetAssetApprovement(HashSet<Guid> assetIds)
        {
            throw new NotImplementedException();
        }
    }
}



using log4net;
using Microsoft.EntityFrameworkCore;
using System.Collections;
using System.Data;

namespace SampleProjWithLimitedFn
{
    /// <summary>
    /// Sample DbContext for EF Core
    /// </summary>
    public class MyAppDbContext : DbContext
    {
        public MyAppDbContext(DbContextOptions<MyAppDbContext> options)
            : base(options)
        {
        }

        // Register your entities here:
        // public DbSet<Asset> Assets { get; set; }
        // public DbSet<Fund> Funds { get; set; }
        // public DbSet<User> Users { get; set; }
    }

    /// <summary>
    /// EF Core–based entity manager implementing the full contract
    /// </summary>
    public class EfEntityManager : IDisposable
    {
        private static readonly ILog log = LogManager.GetLogger(typeof(EfEntityManager));
        private readonly MyAppDbContext _db;
        private bool _disposed;

        public EfEntityManager(MyAppDbContext dbContext)
        {
            _db = dbContext;
        }

        public virtual void Create(object entity)
        {
            try
            {
                _db.Add(entity);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public void Update(object entity)
        {
            try
            {
                _db.Update(entity);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public void Merge(object entity)
        {
            // EF Core Update acts as merge
            Update(entity);
        }

        public virtual void CreateOrUpdate(object entity)
        {
            try
            {
                var entry = _db.Entry(entity);
                if (entry.IsKeySet)
                    Update(entity);
                else
                    Create(entity);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public virtual void CreateOrUpdateAll<T>(IList<T> entities)
        {
            foreach (var e in entities)
                CreateOrUpdate(e);
        }

        public virtual void Remove(object entity)
        {
            try
            {
                _db.Remove(entity);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public virtual void Remove<T>(object key) where T : class
        {
            try
            {
                var entity = Find<T>(key);
                if (entity != null)
                    Remove(entity);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public virtual void RemoveAll(Type type)
        {
            try
            {
                var set = (IQueryable)_db.GetType().GetMethod("Set").MakeGenericMethod(type).Invoke(_db, null);
                var list = ((IEnumerable)set).Cast<object>().ToList();
                _db.RemoveRange(list);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public void RemoveAll<T>(IList<T> entities)
        {
            try
            {
                _db.RemoveRange(entities);
                _db.SaveChanges();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public void RemoveAllByNamedQuery(Type type, string queryName)
        {
            throw new NotImplementedException();
        }

        public void RemoveAllByNamedQuery(Type type, string queryName, Dictionary<string, object> parameters)
        {
            throw new NotImplementedException();
        }

        public virtual T Find<T>(object key) where T : class
        {
            try
            {
                return _db.Find<T>(key);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public virtual IList<T> FindAll<T>() where T : class
        {
            try
            {
                return _db.Set<T>().ToList();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public int Count<T>() where T : class
        {
            try
            {
                return _db.Set<T>().Count();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public virtual IList<T> FindAllByNamedQuery<T>(string queryName) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> FindAllByNamedQuery<T>(string queryName, object[] parameters) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual IList<T> FindAllByNamedQuery<T>(string queryName, object[] parameters, int? start, int? max) where T : class
        {
            throw new NotImplementedException();
        }

        public T FindByNamedQuery<T>(string queryName) where T : class
        {
            throw new NotImplementedException();
        }

        public T FindByNamedQuery<T>(string queryName, object[] parameters) where T : class
        {
            throw new NotImplementedException();
        }

        public T FindByNamedQuery<T>(string queryName, object[] parameters, int? start, int? max) where T : class
        {
            throw new NotImplementedException();
        }

        public void Flush()
        {
            _db.SaveChanges();
        }

        public virtual void ExecuteSQLQuery(string sql)
        {
            try
            {
                _db.Database.ExecuteSqlRaw(sql);
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public virtual void ExecuteSQLQuery(string sql, IDictionary<string, object> parameters)
        {
            try
            {
                _db.Database.ExecuteSqlRaw(sql, parameters.Values.ToArray());
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public void ExecuteNonSQLQuery(string name)
        {
            ExecuteSQLQuery(name);
        }

        public void ExecuteNonSQLQuery(string name, IDictionary<string, object> parameters)
        {
            ExecuteSQLQuery(name, parameters);
        }

        public void ExecuteStoredProcedure(string name)
        {
            ExecuteSQLQuery($"EXEC {name}");
        }

        public IList<T> ExecuteStoredProcedure<T>(string name) where T : class
        {
            try
            {
                return _db.Set<T>().FromSqlRaw($"EXEC {name}").ToList();
            }
            catch (Exception ex)
            {
                log.Error(ex.Message, ex);
                throw new PersistenceException(ex.Message, ex);
            }
        }

        public void ExecuteStoredProcedure(string name, IDictionary<string, object> paramDictionary)
        {
            throw new NotImplementedException();
        }

        public IList<T> ExecuteStoredProcedure<T>(string name, IDictionary<string, object> paramDictionary) where T : class
        {
            throw new NotImplementedException();
        }

        public virtual object Create<T>(Dictionary<string, object> dictNewRecordValues) where T : class
        {
            throw new NotImplementedException();
        }

        public void Detach(object entity)
        {
            _db.Entry(entity).State = EntityState.Detached;
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _db.Dispose();
                _disposed = true;
            }
        }
    }
}


using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace SampleProjWithLimitedFn
{
    /// <summary>
    /// Secure entity manager with access filtering, stubbed
    /// </summary>
    public class SecureEntityManager : AmsEntityManager
    {
        public SecureEntityManager(MyAppDbContext dbContext)
            : base(dbContext)
        {
        }

        public override IList<T> FindAll<T>()
        {
            throw new NotImplementedException();
        }

        public override IList<T> FindAllByNamedQuery<T>(string queryName)
        {
            throw new NotImplementedException();
        }

        public override IList<T> FindAllByNamedQuery<T>(string queryName, object[] parameters, int? start, int? max)
        {
            throw new NotImplementedException();
        }

        public override IList<T> Query<T>(Expression<Func<T, bool>> where = null,
                                           Expression<Func<T, T>> select = null,
                                           bool approvedOnly = true)
        {
            throw new NotImplementedException();
        }

        public override IList<TResult> Query<T, TResult>(Expression<Func<T, bool>> where,
                                                         Expression<Func<T, TResult>> select,
                                                         bool approvedOnly = true)
        {
            throw new NotImplementedException();
        }

        public override void CreateOrUpdate(object o)
        {
            throw new NotImplementedException();
        }

        protected IList<T> FilterListByAccess<T>(IList<T> originalList)
        {
            throw new NotImplementedException();
        }

        public virtual IList<AssetAndFunds> FetchAssetAndFunds()
        {
            throw new NotImplementedException();
        }
    }// TODO: Add AMS-specific overrides or custom methods here.
}
