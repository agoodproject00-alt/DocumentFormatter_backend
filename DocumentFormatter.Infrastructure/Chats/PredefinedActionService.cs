using DocumentFormatter.Application.Chats;
using DocumentFormatter.Domain.Chats;
using DocumentFormatter.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace DocumentFormatter.Infrastructure.Chats;

public sealed class PredefinedActionService(
    AuthDbContext database,
    OpenAiPredefinedActionGenerator generator) : IPredefinedActionService
{
    public async Task<PredefinedActionPageResponse> ListAsync(
        string ownerUserId,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        var actions = database.PredefinedActions
            .AsNoTracking()
            .Where(action => action.IsActive && (action.OwnerUserId == null || action.OwnerUserId == ownerUserId));

        var normalizedSearch = search?.Trim();
        IOrderedQueryable<PredefinedAction> orderedActions;
        if (!string.IsNullOrEmpty(normalizedSearch))
        {
            var terms = normalizedSearch
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Take(8)
                .ToArray();

            actions = actions.Where(BuildSearchPredicate(normalizedSearch, terms));

            orderedActions = actions
                .OrderByDescending(action => action.Label == normalizedSearch)
                .ThenByDescending(action => action.Label.StartsWith(normalizedSearch))
                .ThenByDescending(action => action.Label.Contains(normalizedSearch))
                .ThenByDescending(action => action.Prompt.Contains(normalizedSearch));
            orderedActions = orderedActions.ThenByDescending(BuildRelevanceScore(terms));
        }
        else
        {
            orderedActions = actions
                .OrderBy(action => action.OwnerUserId != null)
                .ThenBy(action => action.SortOrder);
        }

        var results = await orderedActions
            .ThenBy(action => action.OwnerUserId != null)
            .ThenBy(action => action.SortOrder)
            .ThenBy(action => action.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize + 1)
            .Select(action => new PredefinedActionResponse(
                action.Id,
                action.Label,
                action.Prompt,
                action.OwnerUserId == ownerUserId))
            .ToListAsync(cancellationToken);

        var hasMore = results.Count > pageSize;
        if (hasMore) results.RemoveAt(results.Count - 1);

        return new PredefinedActionPageResponse(results, hasMore);
    }

    private static Expression<Func<PredefinedAction, bool>> BuildSearchPredicate(
        string search,
        IReadOnlyList<string> terms)
    {
        var action = Expression.Parameter(typeof(PredefinedAction), "action");
        var label = Expression.Property(action, nameof(PredefinedAction.Label));
        var prompt = Expression.Property(action, nameof(PredefinedAction.Prompt));
        var contains = ContainsMethod;
        Expression matches = Expression.OrElse(
            Expression.Call(label, contains, Expression.Constant(search)),
            Expression.Call(prompt, contains, Expression.Constant(search)));

        foreach (var term in terms)
        {
            var termMatches = Expression.OrElse(
                Expression.Call(label, contains, Expression.Constant(term)),
                Expression.Call(prompt, contains, Expression.Constant(term)));
            matches = Expression.OrElse(matches, termMatches);
        }

        return Expression.Lambda<Func<PredefinedAction, bool>>(matches, action);
    }

    private static Expression<Func<PredefinedAction, int>> BuildRelevanceScore(IReadOnlyList<string> terms)
    {
        var action = Expression.Parameter(typeof(PredefinedAction), "action");
        var label = Expression.Property(action, nameof(PredefinedAction.Label));
        var prompt = Expression.Property(action, nameof(PredefinedAction.Prompt));
        Expression score = Expression.Constant(0);

        foreach (var term in terms)
        {
            var value = Expression.Constant(term);
            score = Expression.Add(score, Expression.Condition(
                Expression.Call(label, ContainsMethod, value),
                Expression.Constant(2),
                Expression.Constant(0)));
            score = Expression.Add(score, Expression.Condition(
                Expression.Call(prompt, ContainsMethod, value),
                Expression.Constant(1),
                Expression.Constant(0)));
        }

        return Expression.Lambda<Func<PredefinedAction, int>>(score, action);
    }

    private static readonly System.Reflection.MethodInfo ContainsMethod =
        typeof(string).GetMethod(nameof(string.Contains), [typeof(string)])!;

    public async Task<SavePredefinedActionResponse> GenerateAndCreateAsync(
        GeneratePredefinedActionRequest request,
        CancellationToken cancellationToken)
    {
        var (label, prompt) = await generator.GenerateAsync(request.Text.Trim(), cancellationToken);
        var normalizedLabel = label.ToUpperInvariant();
        var existingAction = await database.PredefinedActions
            .AsNoTracking()
            .Where(item => item.OwnerUserId == null && item.Label.ToUpper() == normalizedLabel)
            .Select(item => new PredefinedActionResponse(
                item.Id,
                item.Label,
                item.Prompt,
                false))
            .FirstOrDefaultAsync(cancellationToken);

        if (existingAction is not null)
        {
            return new SavePredefinedActionResponse(existingAction, false);
        }

        var maxSortOrder = await database.PredefinedActions
            .Where(item => item.OwnerUserId == null)
            .Select(item => (int?)item.SortOrder)
            .MaxAsync(cancellationToken);
        var action = new PredefinedAction
        {
            Label = label,
            Prompt = prompt,
            OwnerUserId = null,
            SortOrder = (maxSortOrder ?? 0) + 1
        };

        database.PredefinedActions.Add(action);
        await database.SaveChangesAsync(cancellationToken);
        return new SavePredefinedActionResponse(
            new PredefinedActionResponse(action.Id, action.Label, action.Prompt, false),
            true);
    }
}
