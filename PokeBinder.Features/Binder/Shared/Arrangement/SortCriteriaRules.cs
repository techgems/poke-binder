using FluentValidation;
using PokeBinder.Features.Binder.ReorderBinder.Models;

namespace PokeBinder.Features.Binder.Shared.Arrangement;

/// <summary>
/// What makes a list of sort criteria one BinderArrangement can sort by. Shared by every validator
/// whose request carries a sort, so ReorderBinder and UpdateBinder refuse the same lists for the
/// same reasons.
/// </summary>
internal static class SortCriteriaRules
{
    /// <summary>One, two or three criteria: Set, Rarity and Card name, each at most once.</summary>
    internal const int MaxCriteria = 3;

    /// <summary>
    /// The list as a whole. Chain it after the caller's own <c>Cascade(CascadeMode.Stop)</c> and
    /// <c>NotNull()</c>: whether the list is required is the slice's to say, and without the cascade
    /// the rules here would count a null.
    /// </summary>
    /// <typeparam name="TList">
    /// The request's list type, nullable or not -- a list the request may leave out is still one
    /// this checks once it is there.
    /// </typeparam>
    internal static IRuleBuilderOptions<T, TList> MakeASort<T, TList>(
        this IRuleBuilderOptions<T, TList> rule)
        where TList : IReadOnlyList<SortCriterion>? =>
        rule
            .Must(criteria => criteria!.Count is >= 1 and <= MaxCriteria)
                .WithMessage($"A sort uses at least one criterion and at most {MaxCriteria}.")
            .Must(criteria => criteria!.All(criterion => criterion is not null))
                .WithMessage("A criterion is empty.")
            // A criterion a second time could only break ties it has already broken.
            .Must(criteria => criteria!.Select(criterion => criterion.Key).Distinct().Count() == criteria!.Count)
                .WithMessage("The same criterion is used twice.");

    /// <summary>Each criterion on its own, for <c>RuleForEach(...).ChildRules(...)</c>.</summary>
    internal static void Criterion(InlineValidator<SortCriterion> criterion)
    {
        criterion.RuleFor(c => c.Key).IsInEnum();
        criterion.RuleFor(c => c.Direction).IsInEnum();

        // One criterion with a choice of key: Rarity has to say which ordering it reads, and a key
        // on anything else is a client that has confused its criteria.
        criterion.RuleFor(c => c.RarityKey)
            .NotNull()
                .WithMessage("Rarity has to say which ordering it sorts by: pull rate or name order.")
            .IsInEnum()
            .When(c => c.Key == CriterionKey.Rarity);

        criterion.RuleFor(c => c.RarityKey)
            .Null()
            .When(c => c.Key != CriterionKey.Rarity)
            .WithMessage("Only Rarity sorts by a rarity ordering.");
    }
}
