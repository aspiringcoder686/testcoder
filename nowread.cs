using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EfNavTest
{
    public class GlobalFilterDemo
    {
        public static async Task Demo()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite("Data Source=filters-demo-full.db")
                //.LogTo(Console.WriteLine, LogLevel.Information) // log SQL
                .EnableSensitiveDataLogging()
                .Options;

            using var db = new AppDbContext(options);

            // Reset database each run
            db.Database.EnsureDeleted();
            db.Database.EnsureCreated();

            // -------- Seed ----------
            var asset1 = new AmAsset { Id = Guid.NewGuid(), Name = "Asset Approved", IsAssetApproved = true };
            var asset2 = new AmAsset { Id = Guid.NewGuid(), Name = "Asset Blocked", IsAssetApproved = false };
            var asset3 = new AmAsset { Id = Guid.NewGuid(), Name = "Asset Approved", IsAssetApproved = false };

            var fg1 = new UserFundGroup { Id = 1, FundGroupShortName = "FG-A" };
            var fg2 = new UserFundGroup { Id = 2, FundGroupShortName = "FG-B" };
            var fg3 = new UserFundGroup { Id = 3, FundGroupShortName = "FG-C" };

            db.AmAssets.AddRange(asset1, asset2);
            db.UserFundGroups.AddRange(fg1, fg2, fg3);

            db.AmOrders.AddRange(
                new AmOrder { Id = 1, Description = "Order 1", Asset = asset1, FundGroup = fg1 },
                new AmOrder { Id = 2, Description = "Order 2", Asset = asset2, FundGroup = fg2 },
                new AmOrder { Id = 3, Description = "Order 3", Asset = asset1, FundGroup = fg3 },
                new AmOrder { Id = 4, Description = "Order 4", Asset = asset3, FundGroup = fg3 }
            );

            db.AuditLogs.AddRange(
    new AuditLog { Id = 1, Message = "System started", CreatedAt = DateTime.UtcNow },
    new AuditLog { Id = 2, Message = "User logged in", CreatedAt = DateTime.UtcNow },
    new AuditLog { Id = 3, Message = "Order created", CreatedAt = DateTime.UtcNow }
);


            await db.SaveChangesAsync();

            // ================= Queries =================

            Console.WriteLine("\nAll Orders (no filter):");
            var allOrders = await db.AmOrders
                .Include(o => o.Asset)
                .Include(o => o.FundGroup)
                .AsNoTracking()
                .ToListAsync();
            PrintOrders(allOrders);

            Console.WriteLine("\nOrders with AssetApproved filter ON:");
            using (db.UseAssetApproved())
            {
                var filtered = await db.AmOrders
                    .Include(o => o.Asset)
                    .Include(o => o.FundGroup)
                    .AsNoTracking()
                    .ToListAsync();
                PrintOrders(filtered);
            }

            Console.WriteLine("\nOrders with FundGroup filter (FG-A, FG-C):");
            using (db.UseFundGroups(new[] { "FG-A", "FG-C" }))
            {
                var filtered = await db.AmOrders
                    .Include(o => o.Asset)
                    .Include(o => o.FundGroup)
                    .AsNoTracking()
                    .ToListAsync();
                PrintOrders(filtered);
            }

            Console.WriteLine("\nOrders with BOTH filters (AssetApproved + FundGroups FG-A,FG-C):");
            using (db.UseAssetApproved())
            using (db.UseFundGroups(new[] { "FG-A", "FG-C" }))
            {
                var filtered = await db.AmOrders
                    .Include(o => o.Asset)
                    .Include(o => o.FundGroup)
                    .AsNoTracking()
                    .ToListAsync();
                PrintOrders(filtered);
            }

            Console.WriteLine("\nOrders bypassing filters (IgnoreQueryFilters):");
            using (db.UseAssetApproved())
            using (db.UseFundGroups(new[] { "FG-A", "FG-C" }))
            {
                var bypassed = await db.AmOrders
                    .Include(o => o.Asset)
                    .Include(o => o.FundGroup)
                    .IgnoreQueryFilters()
                    .AsNoTracking()
                    .ToListAsync();
                PrintOrders(bypassed);
            }

            Console.WriteLine("\nDone. Check console above for generated SQL queries.");

            Console.WriteLine("\nAuditLogs inside filter scope (should be unaffected):");
            using (db.UseAssetApproved())
            using (db.UseFundGroups(new[] { "FG-A", "FG-C" }))
            {
                var logs = await db.AuditLogs.AsNoTracking()
                    .OrderBy(l => l.Id)
                    .ToListAsync();

                foreach (var log in logs)
                {
                    Console.WriteLine($" - {log.Id}: {log.Message}");
                }
            }

        }

        private static void PrintOrders(IEnumerable<AmOrder> orders)
        {
            foreach (var o in orders)
            {
                Console.WriteLine(
                    $" - {o.Description} -> Asset={o.Asset?.Name} (approved={o.Asset?.IsAssetApproved}), FundGroup={o.FundGroup?.FundGroupShortName}");
            }
        }
    }

    // ======================= Entities =======================
    public class AuditLog
    {
        public int Id { get; set; }
        public string Message { get; set; } = "";
        public DateTime CreatedAt { get; set; }
    }

    public class AmAsset
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public bool IsAssetApproved { get; set; }

        public ICollection<AmOrder> Orders { get; set; } = new List<AmOrder>();
    }

    public class UserFundGroup
    {
        public int Id { get; set; }
        public string FundGroupShortName { get; set; } = "";

        public ICollection<AmOrder> Orders { get; set; } = new List<AmOrder>();
    }

    public class AmOrder
    {
        public int Id { get; set; }
        public string Description { get; set; } = "";

        public Guid AssetId { get; set; }
        public AmAsset Asset { get; set; } = null!;

        public int FundGroupId { get; set; }
        public UserFundGroup FundGroup { get; set; } = null!;
    }

    // ======================= DbContext =======================

    public class AppDbContext : DbContext
    {
        public DbSet<AmAsset> AmAssets => Set<AmAsset>();
        public DbSet<UserFundGroup> UserFundGroups => Set<UserFundGroup>();
        public DbSet<AmOrder> AmOrders => Set<AmOrder>();

        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        // Filter flags
        public bool ApplyAssetApprovedFilter { get; set; } = false;
        public bool ApplyFundGroupAccessFilter { get; set; } = false;
        public List<string> AllowedFundGroups { get; set; } = new();

        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Global filter for Asset
            modelBuilder.Entity<AmAsset>()
                .HasQueryFilter(e => !ApplyAssetApprovedFilter || e.IsAssetApproved);

            // Global filter for FundGroup
            modelBuilder.Entity<UserFundGroup>()
                .HasQueryFilter(e => !ApplyFundGroupAccessFilter ||
                                     AllowedFundGroups.Contains(e.FundGroupShortName));
        }
    }

    // ======================= Scope Helpers =======================

    public static class FilterScopes
    {
        public static IDisposable UseAssetApproved(this AppDbContext db, bool apply = true)
            => new BoolFlagScope(
                get: () => db.ApplyAssetApprovedFilter,
                set: v => db.ApplyAssetApprovedFilter = v,
                value: apply);

        public static IDisposable UseFundGroups(this AppDbContext db, IEnumerable<string> groups)
            => new FundGroupsScope(db, groups);

        private sealed class BoolFlagScope : IDisposable
        {
            private readonly Action<bool> _set;
            private readonly bool _prev;

            public BoolFlagScope(Func<bool> get, Action<bool> set, bool value)
            {
                _set = set;
                _prev = get();
                _set(value);
            }

            public void Dispose() => _set(_prev);
        }

        private sealed class FundGroupsScope : IDisposable
        {
            private readonly AppDbContext _db;
            private readonly bool _prevApply;
            private readonly List<string> _prevList;

            public FundGroupsScope(AppDbContext db, IEnumerable<string> groups)
            {
                _db = db;

                // Save previous state
                _prevApply = db.ApplyFundGroupAccessFilter;
                _prevList = db.AllowedFundGroups;

                // Set new
                db.AllowedFundGroups = (groups ?? Array.Empty<string>())
                    .Where(s => !string.IsNullOrWhiteSpace(s))
                    .Select(s => s.Trim())
                    .Distinct(StringComparer.Ordinal)
                    .ToList();

                db.ApplyFundGroupAccessFilter = db.AllowedFundGroups.Count > 0;
            }

            public void Dispose()
            {
                _db.AllowedFundGroups = _prevList;
                _db.ApplyFundGroupAccessFilter = _prevApply;
            }
        }
    }
}
