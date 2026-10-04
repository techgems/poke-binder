using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext.Entities;
using PokeBinder.Features.Binder.DeleteBinder;
using BinderEntity = PokeBinder.Binders.DbContext.Entities.Binder;

namespace PokeBinder.Features.Tests.Binder;

/// <summary>
/// What DeleteBinder removes, and what it must leave alone: a delete takes the binder, its placed
/// cards and its tray, and nothing that belongs to any other binder.
/// </summary>
public class DeleteBinderTests
{
    private const int OtherBinderId = 2;

    private static DeleteBinder.Request Delete(int binderId = BinderFixture.BinderId) =>
        new() { BinderId = binderId };

    private static Task HandleAsync(BinderFixture fixture, DeleteBinder.Request request) =>
        DeleteBinder.Handler(request, BinderFixture.UserId, fixture.Binders);

    private static Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        BinderFixture fixture,
        DeleteBinder.Request request,
        int userId = BinderFixture.UserId) =>
        new DeleteBinderValidator(fixture.Binders, userId).ValidateAsync(request);

    /// <summary>A second binder of the caller's, with a card placed and one in its tray.</summary>
    private static BinderFixture WithOtherBinder(BinderFixture fixture)
    {
        fixture.Binders.Binders.Add(new BinderEntity
        {
            Id = OtherBinderId,
            Name = "Another binder",
            CreatedAt = 1_700_000_000,
            UserId = BinderFixture.UserId,
            BinderSizeId = 1,
            Pages = 4,
        });

        fixture.Binders.BinderCards.Add(new BinderCard
        {
            BinderId = OtherBinderId,
            CardId = 30,
            IndexInBinder = 0,
            IsMissing = false,
        });

        fixture.Binders.BinderTray.Add(new BinderTray { BinderId = OtherBinderId, CardId = 31, Quantity = 2 });

        fixture.Binders.SaveChanges();

        return fixture;
    }

    // -------------------------------------------------------------------------------------------
    // What a delete removes
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Deletes_the_binder()
    {
        using var fixture = new BinderFixture().WithBinder();

        await HandleAsync(fixture, Delete());

        fixture.Binders.ChangeTracker.Clear();
        Assert.False(await fixture.Binders.Binders.AnyAsync(binder => binder.Id == BinderFixture.BinderId));
    }

    [Fact]
    public async Task Deletes_the_cards_placed_in_it()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithPlacements((0, 11), (5, 12), (35, 13));

        await HandleAsync(fixture, Delete());

        Assert.Empty(await fixture.StoredPlacementsAsync());
    }

    [Fact]
    public async Task Deletes_its_tray()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithTray((11, 1), (12, 3));

        await HandleAsync(fixture, Delete());

        Assert.Empty(await fixture.StoredTrayAsync());
    }

    [Fact]
    public async Task Leaves_the_users_other_binders_alone()
    {
        using var fixture = WithOtherBinder(new BinderFixture()
            .WithBinder()
            .WithPlacements((0, 11))
            .WithTray((12, 1)));

        await HandleAsync(fixture, Delete());

        fixture.Binders.ChangeTracker.Clear();

        Assert.True(await fixture.Binders.Binders.AnyAsync(binder => binder.Id == OtherBinderId));
        Assert.Equal(
            [(0, 30)],
            await fixture.Binders.BinderCards
                .Where(card => card.BinderId == OtherBinderId)
                .Select(card => ValueTuple.Create(card.IndexInBinder, card.CardId))
                .ToListAsync());
        Assert.Equal(
            [(31, 2)],
            await fixture.Binders.BinderTray
                .Where(entry => entry.BinderId == OtherBinderId)
                .Select(entry => ValueTuple.Create(entry.CardId, entry.Quantity))
                .ToListAsync());
    }

    [Fact]
    public async Task Leaves_the_binder_size_alone()
    {
        using var fixture = new BinderFixture().WithBinder();

        await HandleAsync(fixture, Delete());

        // Sizes are the seeded list every binder picks from, not something a binder owns.
        fixture.Binders.ChangeTracker.Clear();
        Assert.True(await fixture.Binders.BinderSizes.AnyAsync(size => size.Id == 1));
    }

    // -------------------------------------------------------------------------------------------
    // Who may delete what
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task The_owner_may_delete_it()
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Delete());

        Assert.True(result.IsValid);
    }

    [Fact]
    public async Task Someone_elses_binder_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder(userId: BinderFixture.OtherUserId);

        var result = await ValidateAsync(fixture, Delete());

        var failure = Assert.Single(result.Errors);
        Assert.Equal(nameof(DeleteBinder.Request.BinderId), failure.PropertyName);
    }

    [Fact]
    public async Task A_binder_that_does_not_exist_is_refused_the_same_way()
    {
        using var fixture = new BinderFixture().WithBinder(userId: BinderFixture.OtherUserId);

        var missing = await ValidateAsync(fixture, Delete(binderId: 99));
        var someoneElses = await ValidateAsync(fixture, Delete());

        // One message for both, so the endpoint cannot be used to learn which binder ids exist.
        Assert.Equal(
            Assert.Single(someoneElses.Errors).ErrorMessage,
            Assert.Single(missing.Errors).ErrorMessage);
    }

    [Fact]
    public async Task The_handler_will_not_delete_someone_elses_binder_without_its_validator()
    {
        using var fixture = new BinderFixture()
            .WithBinder(userId: BinderFixture.OtherUserId)
            .WithPlacements((0, 11));

        await Assert.ThrowsAsync<InvalidOperationException>(() => HandleAsync(fixture, Delete()));

        Assert.Single(await fixture.StoredPlacementsAsync());
    }
}
