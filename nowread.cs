using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System.Linq.Expressions;

public static class EfIncludeExtensions
{
    public static IQueryable<TEntity> IncludeAllNavigations<TEntity>(
        this IQueryable<TEntity> query,
        DbContext context) where TEntity : class
    {
        var entityType = context.Model.FindEntityType(typeof(TEntity));
        var navigations = entityType.GetNavigations();

        foreach (var navigation in navigations)
        {
            var parameter = Expression.Parameter(typeof(TEntity), "e");
            var property = Expression.Property(parameter, navigation.Name);
            var lambda = Expression.Lambda(property, parameter);

            query = query.Include((dynamic)lambda);
        }

        return query;
    }
}

var allAssets = dbContext.AmAssets
    .IncludeAllNavigations(dbContext) // auto-include all navigation properties
    .ToList();



IncludeGenerator.GenerateIncludeFile(typeof(AMSDbContext), "AmAsset", maxDepth: 3, dtoTypeName: "AssetDto");


#nullable enable
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

public static class IncludeGenerator
{
    private const string OUTPUT_DIR = "./IncludeTxt";
    private const int DEFAULT_MAX_DEPTH = 3;

    public static void GenerateIncludeFile(
        Type dbContextType,
        string entityName,
        int maxDepth = DEFAULT_MAX_DEPTH,
        string? dtoTypeName = null,
        int rootColumnCount = 3,
        int relatedColumnCount = 1)
    {
        Directory.CreateDirectory(OUTPUT_DIR);

        try
        {
            var builderType = typeof(DbContextOptionsBuilder<>).MakeGenericType(dbContextType);
            var optionsBuilder = (DbContextOptionsBuilder)Activator.CreateInstance(builderType)!;

            // Needs Microsoft.EntityFrameworkCore.InMemory
            optionsBuilder.UseInMemoryDatabase("EfLambdaIncludeGen");

            var db = (DbContext)Activator.CreateInstance(dbContextType, optionsBuilder.Options)!;
            var model = db.Model;

            var et = model.GetEntityTypes()
                .FirstOrDefault(t =>
                    string.Equals(t.ClrType.Name, entityName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(t.ClrType.FullName, entityName, StringComparison.OrdinalIgnoreCase));

            if (et is null)
            {
                WriteErrorFile(entityName, $"Entity '{entityName}' not found in DbContext model.");
                return;
            }

            var navPaths = CollectNavPaths(et, maxDepth);
            var rootScalars = CollectScalarProps(et);

            var rootCols = rootScalars.Take(Math.Max(1, rootColumnCount)).ToList();

            var relatedNav = navPaths.FirstOrDefault(); // choose first relation for sample
            var relatedName = relatedNav?.FirstOrDefault()?.Name;
            var relatedType = relatedNav?.FirstOrDefault()?.TargetEntityType;
            var relatedScalars = relatedType != null ? CollectScalarProps(relatedType) : new List<IProperty>();
            var relCols = relatedScalars.Take(Math.Max(0, relatedColumnCount)).ToList();

            var content = BuildTxt(
                entityClr: et.ClrType,
                navPaths: navPaths,
                rootScalars: rootScalars,
                rootColsForProjections: rootCols,
                relatedNavName: relatedName,
                relatedColsForProjections: relCols,
                maxDepth: maxDepth,
                dtoTypeName: dtoTypeName ?? "YourDto");

            var outFile = Path.Combine(OUTPUT_DIR, $"{et.ClrType.Name}.includes.txt");
            File.WriteAllText(outFile, content, Encoding.UTF8);
            Console.WriteLine($"Generated: {Path.GetFullPath(outFile)}");
        }
        catch (Exception ex)
        {
            WriteErrorFile(entityName, $"Error generating for '{entityName}':{Environment.NewLine}{ex}");
        }
    }

    private static void WriteErrorFile(string entityName, string message)
    {
        var errorFile = Path.Combine(OUTPUT_DIR, $"error-{entityName}.includes.txt");
        File.WriteAllText(errorFile, message, Encoding.UTF8);
        Console.WriteLine($"[ERROR] Wrote {Path.GetFullPath(errorFile)}");
    }

    // ─────────────────────────── metadata helpers ───────────────────────────

    private static List<List<INavigationBase>> CollectNavPaths(IEntityType root, int maxDepth)
    {
        var results = new List<List<INavigationBase>>();
        var visitedEdges = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<INavigationBase>();

        void Dfs(IEntityType current, int depth)
        {
            if (depth > maxDepth) return;

            var navs = current.GetNavigations().Cast<INavigationBase>()
                        .Concat(current.GetSkipNavigations());

            foreach (var nav in navs)
            {
                var edgeKey = $"{current.Name}->{nav.DeclaringEntityType.Name}.{nav.Name}";
                if (!visitedEdges.Add(edgeKey)) continue;

                stack.Push(nav);
                results.Add(stack.Reverse().ToList());
                Dfs(nav.TargetEntityType, depth + 1);
                stack.Pop();
            }
        }

        Dfs(root, 1);

        return results
            .GroupBy(p => string.Join(".", p.Select(n => n.Name)))
            .Select(g => g.First())
            .OrderBy(p => p.Count) // shorter first
            .ThenBy(p => string.Join(".", p.Select(n => n.Name)), StringComparer.Ordinal)
            .ToList();
    }

    private static List<IProperty> CollectScalarProps(IEntityType et)
    {
        static bool IsScalar(Type t)
        {
            t = Nullable.GetUnderlyingType(t) ?? t;
            return t.IsPrimitive
                   || t.IsEnum
                   || t == typeof(string)
                   || t == typeof(Guid)
                   || t == typeof(decimal)
                   || t == typeof(DateTime)
                   || t == typeof(DateTimeOffset)
                   || t == typeof(TimeSpan)
                   || t == typeof(byte[]);
        }

        return et.GetProperties().Where(p => IsScalar(p.ClrType)).ToList();
    }

    // ─────────────────────────── txt builder ───────────────────────────

    private static string BuildTxt(
        Type entityClr,
        List<List<INavigationBase>> navPaths,
        List<IProperty> rootScalars,
        List<IProperty> rootColsForProjections,
        string? relatedNavName,
        List<IProperty> relatedColsForProjections,
        int maxDepth,
        string dtoTypeName)
    {
        var T = entityClr.Name;
        var sb = new StringBuilder();

        sb.AppendLine($"// Generated Include, Projection & Expression snippets for {T} (depth ≤ {maxDepth})");
        sb.AppendLine($"// Generated on {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine("using Microsoft.EntityFrameworkCore;");
        sb.AppendLine("using System.Linq;");
        sb.AppendLine("using System.Linq.Expressions;");
        sb.AppendLine("using System.Threading.Tasks;");
        sb.AppendLine();

        var whereCol = (rootScalars.FirstOrDefault(p => p.IsPrimaryKey()) ??
                        rootScalars.FirstOrDefault(p => p.Name.Equals("Id", StringComparison.OrdinalIgnoreCase)) ??
                        rootScalars.FirstOrDefault(p => p.Name.EndsWith("Id", StringComparison.OrdinalIgnoreCase)) ??
                        rootScalars.FirstOrDefault(p => p.Name.EndsWith("Guid", StringComparison.OrdinalIgnoreCase)) ??
                        rootScalars.FirstOrDefault())
                       ?.Name ?? "/* add-column */";

        // WHERE template
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine("// Quick WHERE template");
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine($"// var list = await db.Set<{T}>()");
        sb.AppendLine($"//     .Where(e => e.{whereCol} == /* value */)");
        sb.AppendLine("//     .ToListAsync();");
        sb.AppendLine();

        // Include lines
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine("// COPY the Include/ThenInclude lines you need");
        sb.AppendLine("// ─────────────────────────────────────────────");
        if (navPaths.Count == 0)
        {
            sb.AppendLine("// (No navigations detected)");
        }
        else
        {
            foreach (var p in navPaths)
                sb.AppendLine(BuildIncludeChainLine(p));
        }
        sb.AppendLine();

        // Example query
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine("// Example: WHERE + Includes + execution");
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine($"var result = await db.Set<{T}>()");
        sb.AppendLine($"    .Where(e => e.{whereCol} == /* value */)");
        foreach (var p in navPaths.Take(Math.Min(navPaths.Count, 6)))
            sb.AppendLine("    " + BuildIncludeChainInline(p));
        sb.AppendLine("    .AsNoTracking()");
        sb.AppendLine("    .AsSplitQuery()");
        sb.AppendLine("    .ToListAsync();");
        sb.AppendLine();

        // Projection: root columns (2–3)
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine("// Projection: anonymous (2–3 root columns)");
        sb.AppendLine("// ─────────────────────────────────────────────");
        var anonRootCols = rootColsForProjections.Any()
            ? string.Join(", ", rootColsForProjections.Select(p => $"e.{p.Name}"))
            : "/* add columns */";
        sb.AppendLine($"var projRoot = await db.Set<{T}>()");
        sb.AppendLine($"    .Where(e => e.{whereCol} == /* value */)");
        sb.AppendLine($"    .Select(e => new {{ {anonRootCols} }})");
        sb.AppendLine("    .ToListAsync();");
        sb.AppendLine();

        // Projection: root + related column(s)
        if (!string.IsNullOrWhiteSpace(relatedNavName) && relatedColsForProjections.Any())
        {
            sb.AppendLine("// ─────────────────────────────────────────────");
            sb.AppendLine("// Projection: anonymous (root + related column[s])");
            sb.AppendLine("// NOTE: Include is not required for projections; EF will generate joins.");
            sb.AppendLine("// ─────────────────────────────────────────────");

            var parts = new List<string>();
            parts.AddRange(rootColsForProjections.Select(p => $"e.{p.Name}"));
            parts.AddRange(relatedColsForProjections.Select(rp => $"{relatedNavName}_{rp.Name} = e.{relatedNavName}.{rp.Name}"));
            var mixedCols = string.Join(", ", parts);

            sb.AppendLine($"var projMixed = await db.Set<{T}>()");
            sb.AppendLine($"    .Where(e => e.{whereCol} == /* value */)");
            sb.AppendLine($"    .Select(e => new {{ {mixedCols} }})");
            sb.AppendLine("    .ToListAsync();");
            sb.AppendLine();
        }

        // Projection: DTO
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine("// Projection: DTO (2–3 root + related column[s])");
        sb.AppendLine("// ─────────────────────────────────────────────");
        var dtoLines = new List<string>();
        dtoLines.AddRange(rootColsForProjections.Select(p => $"{p.Name} = e.{p.Name},"));
        if (!string.IsNullOrWhiteSpace(relatedNavName) && relatedColsForProjections.Any())
            dtoLines.AddRange(relatedColsForProjections.Select(rp => $"{relatedNavName}_{rp.Name} = e.{relatedNavName}.{rp.Name},"));
        var dtoBlock = dtoLines.Any() ? string.Join(Environment.NewLine + "        ", dtoLines).TrimEnd(',') : "// map your fields here";

        sb.AppendLine($"var dtoList = await db.Set<{T}>()");
        sb.AppendLine($"    .Where(e => e.{whereCol} == /* value */)");
        sb.AppendLine($"    .Select(e => new {dtoTypeName}");
        sb.AppendLine("    {");
        sb.AppendLine("        " + dtoBlock);
        sb.AppendLine("    })");
        sb.AppendLine("    .ToListAsync();");
        sb.AppendLine();

        // Single-column IDs
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine("// Projection: single column (IDs)");
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine($"var ids = await db.Set<{T}>()");
        sb.AppendLine("    .Where(e => /* filter */)");
        sb.AppendLine($"    .Select(e => e.{whereCol})");
        sb.AppendLine("    .ToListAsync();");
        sb.AppendLine();

        // Reusable static Expressions — with example usage after each
        sb.AppendLine("// ─────────────────────────────────────────────");
        sb.AppendLine("// Reusable static Expression snippets (each followed by usage)");
        sb.AppendLine("// You can paste these static fields into a helper class or directly above your query.");
        sb.AppendLine("// Usage pattern: query = query.Include(<StaticExpressionName>);");
        sb.AppendLine("// ─────────────────────────────────────────────");
        if (navPaths.Count == 0)
        {
            sb.AppendLine("// (No expressions — no navigations)");
        }
        else
        {
            foreach (var p in navPaths)
                sb.AppendLine(BuildExpressionSnippetWithUsage(T, p));
        }

        return sb.ToString();
    }

    private static string BuildIncludeChainLine(List<INavigationBase> path)
    {
        var sb = new StringBuilder();
        sb.Append("query = query.Include(e => e.");
        sb.Append(path[0].Name);
        sb.Append(")");
        for (int i = 1; i < path.Count; i++)
        {
            sb.Append(".ThenInclude(x => x.");
            sb.Append(path[i].Name);
            sb.Append(")");
        }
        sb.Append(";");
        return sb.ToString();
    }

    private static string BuildIncludeChainInline(List<INavigationBase> path)
    {
        var sb = new StringBuilder();
        sb.Append(".Include(e => e.");
        sb.Append(path[0].Name);
        sb.Append(")");
        for (int i = 1; i < path.Count; i++)
        {
            sb.Append(".ThenInclude(x => x.");
            sb.Append(path[i].Name);
            sb.Append(")");
        }
        return sb.ToString();
    }

    private static string BuildExpressionSnippetWithUsage(string entityName, List<INavigationBase> path)
    {
        // Field name like: AmAsset_With_AmFundAssets_CarlyleFund
        var fieldName = entityName + "_With_" + string.Join("_", path.Select(n => n.Name));

        // Build the expression body: e => e.Nav or e => e.Nav.Select(x0 => x0.Sub).Select(x1 => x1.Sub2)...
        var expr = "e";
        for (int i = 0; i < path.Count; i++)
        {
            var nav = path[i];
            if (i == 0)
            {
                expr += $".{nav.Name}";
            }
            else
            {
                expr = $"({expr}).Select(x{i} => x{i}.{nav.Name})";
            }
        }

        var sb = new StringBuilder();

        // Static expression
        sb.AppendLine($"public static readonly System.Linq.Expressions.Expression<Func<{entityName}, object>> {fieldName} = e => {expr};");

        // Usage block
        sb.AppendLine("// Example usage:");
        sb.AppendLine($"// var query = db.Set<{entityName}>().AsQueryable();");
        sb.AppendLine($"// query = query.Include({fieldName});");
        sb.AppendLine("// var list = await query.ToListAsync();");
        sb.AppendLine();

        return sb.ToString();
    }
}
