/// <summary>
/// Inspired by SQL MERGE
/// </summary>
public static class MergeUtil
{
    public static void Merge<TTargetItem, TSourceItem, TMatchKey>(
        IEnumerable<TTargetItem> target,
        IEnumerable<TSourceItem> source,
        Func<TTargetItem, TMatchKey> targetKeySelector,
        Func<TSourceItem, TMatchKey> sourceKeySelector,
        Action<TTargetItem>? whenNotInSource = null,
        Action<TTargetItem, TSourceItem>? whenMatched = null,
        Action<TSourceItem>? whenNotInTarget = null)
    {
        MergeAsync(target, source, targetKeySelector, sourceKeySelector,
            whenNotInSource?.AsAsync(), whenMatched?.AsAsync(), whenNotInTarget?.AsAsync()
        ).GetAwaiter().GetResult();
    }

    public static async Task MergeAsync<TTargetItem, TSourceItem, TMatchKey>(
        IEnumerable<TTargetItem> target,
        IEnumerable<TSourceItem> source,
        Func<TTargetItem, TMatchKey> targetKeySelector,
        Func<TSourceItem, TMatchKey> sourceKeySelector,
        Func<TTargetItem, Task>? whenNotInSource = null,
        Func<TTargetItem, TSourceItem, Task>? whenMatched = null,
        Func<TSourceItem, Task>? whenNotInTarget = null)
    {
        var targetArray = target.ToArray();
        var sourceArray = source.ToArray();

        var matches = sourceArray.Join(targetArray, sourceKeySelector, targetKeySelector, (s, t) => new
        {
            Source = s,
            Target = t
        }).ToArray();

        if (whenNotInSource != null)
        {
            foreach (var itemOnlyInTarget in targetArray.Except(matches.Select(x => x.Target)))
            {
                await whenNotInSource(itemOnlyInTarget);
            }
        }

        if (whenMatched != null)
        {
            foreach (var match in matches)
            {
                await whenMatched(match.Target, match.Source);
            }
        }

        if (whenNotInTarget != null)
        {
            foreach (var itemOnlyInSource in sourceArray.Except(matches.Select(x => x.Source)))
            {
                await whenNotInTarget(itemOnlyInSource);
            }
        }
    }
    public static IReadOnlyCollection<TResultItem> MergeCollecting<TTargetItem, TSourceItem, TMatchKey, TResultItem>(
        IEnumerable<TTargetItem> target,
        IEnumerable<TSourceItem> source,
        Func<TTargetItem, TMatchKey> targetKeySelector,
        Func<TSourceItem, TMatchKey> sourceKeySelector,
        Func<TTargetItem, TResultItem>? whenNotInSource = null,
        Func<TTargetItem, TSourceItem, TResultItem>? whenMatched = null,
        Func<TSourceItem, TResultItem>? whenNotInTarget = null)
    {
        return MergeCollectingAsync(target, source, targetKeySelector, sourceKeySelector,
            whenNotInSource?.AsAsync(), whenMatched?.AsAsync(), whenNotInTarget?.AsAsync()
        ).GetAwaiter().GetResult();
    }

    public static async Task<IReadOnlyCollection<TResultItem>> MergeCollectingAsync<TTargetItem, TSourceItem, TMatchKey, TResultItem>(
        IEnumerable<TTargetItem> target,
        IEnumerable<TSourceItem> source,
        Func<TTargetItem, TMatchKey> targetKeySelector,
        Func<TSourceItem, TMatchKey> sourceKeySelector,
        Func<TTargetItem, Task<TResultItem>>? whenNotInSource = null,
        Func<TTargetItem, TSourceItem, Task<TResultItem>>? whenMatched = null,
        Func<TSourceItem, Task<TResultItem>>? whenNotInTarget = null)
    {
        var result = new List<TResultItem>();

        var targetArray = target.ToArray();
        var sourceArray = source.ToArray();

        var matches = sourceArray.Join(targetArray, sourceKeySelector, targetKeySelector, (s, t) => new
        {
            Source = s,
            Target = t
        }).ToArray();

        if (whenNotInSource != null)
        {
            foreach (var itemOnlyInTarget in targetArray.Except(matches.Select(x => x.Target)))
            {
                result.Add(await whenNotInSource(itemOnlyInTarget));
            }
        }

        if (whenMatched != null)
        {
            foreach (var match in matches)
            {
                result.Add(await whenMatched(match.Target, match.Source));
            }
        }

        if (whenNotInTarget != null)
        {
            foreach (var itemOnlyInSource in sourceArray.Except(matches.Select(x => x.Source)))
            {
                result.Add(await whenNotInTarget(itemOnlyInSource));
            }
        }

        return result;
    }
}

file static class DelegateExtensions
{
    public static Func<T, Task> AsAsync<T>(this Action<T> action) =>
        arg =>
        {
            action(arg);
            return Task.CompletedTask;
        };

    public static Func<T1, T2, Task> AsAsync<T1, T2>(this Action<T1, T2> action) =>
        (arg1, arg2) =>
        {
            action(arg1, arg2);
            return Task.CompletedTask;
        };

    public static Func<T, Task<TResult>> AsAsync<T, TResult>(this Func<T, TResult> action) =>
        (arg) => Task.FromResult(action(arg));

    public static Func<T1, T2, Task<TResult>> AsAsync<T1, T2, TResult>(this Func<T1, T2, TResult> action) =>
        (arg1, arg2) => Task.FromResult(action(arg1, arg2));
}