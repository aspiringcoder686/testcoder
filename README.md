public class NavigationLoadOptions<TParent, TChild, TKey>
{
    public string ParentEntity { get; set; }
    public string ParentQuery { get; set; }
    public string ChildEntity { get; set; }
    public string ChildQuery { get; set; }

    public Func<TParent, TKey> ParentKeySelector { get; set; }
    public Func<TChild, TKey> ChildKeySelector { get; set; }
    public Action<TParent, TChild> AssignChild { get; set; }
}

public class NavigationLoader
{
    private readonly IQueryLoader _queryLoader;
    private readonly BaseRepository _repository;

    public NavigationLoader(IQueryLoader queryLoader, BaseRepository repository)
    {
        _queryLoader = queryLoader;
        _repository = repository;
    }

    public async Task<List<TParent>> LoadWithChildAsync<TParent, TChild, TKey>(
        NavigationLoadOptions<TParent, TChild, TKey> options)
    {
        var parentQuery = _queryLoader.GetQuery(options.ParentEntity, options.ParentQuery);
        var parentList = [.. await _repository.QueryAsync<TParent>(parentQuery?.Sql)];

        var distinctKeys = parentList
            .Select(options.ParentKeySelector)
            .Where(k => k != null)
            .Distinct()
            .ToList();

        var childQuery = _queryLoader.GetQuery(options.ChildEntity, options.ChildQuery);
        var childList = [.. await _repository.QueryAsync<TChild>(childQuery?.Sql, new { ShortNames = distinctKeys })];

        SimpleNavigationHelper.AssignNavigation(parentList, childList, options.ParentKeySelector, options.ChildKeySelector, options.AssignChild);

        return parentList;
    }
}

public Task<List<HierarchyView>> FindAllViewsAsync()
{
    var options = new NavigationLoadOptions<HierarchyView, FundGroup, string>
    {
        ParentEntity = nameof(HierarchyView),
        ParentQuery = QueryMapConstants.ViewAdministrationConstants.FindAllViews,
        ChildEntity = nameof(FundGroup),
        ChildQuery = QueryMapConstants.ViewAdministrationConstants.FindFundGroupByNames,
        ParentKeySelector = v => v.FundGroupName,
        ChildKeySelector = f => f.ShortName,
        AssignChild = (v, f) => v.FundGroup = f
    };

    return _navigationLoader.LoadWithChildAsync(options);
}

