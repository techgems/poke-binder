using PokeBinder.Features.Binder.CreateBinderCardLabel;

namespace PokeBinder.Features.Tests.Binder;

/// <summary>
/// What the one dedicated label slice does and refuses. A label created here has a real id before
/// anything else can reference it, so the Create-and-apply flow in the pocket's Label modal is
/// what calls this and lands the id in the next debounced SaveBinderChanges.
/// </summary>
public class CreateBinderCardLabelTests
{
    private static CreateBinderCardLabel.Request Create(string? name = "Trade", string? color = "blue") =>
        new()
        {
            BinderId = BinderFixture.BinderId,
            Name = name,
            Color = color,
        };

    private static Task<CreateBinderCardLabel.Response> HandleAsync(
        BinderFixture fixture,
        CreateBinderCardLabel.Request request,
        int userId = BinderFixture.UserId) =>
        CreateBinderCardLabel.Handler(request, userId, fixture.Binders);

    private static Task<FluentValidation.Results.ValidationResult> ValidateAsync(
        BinderFixture fixture,
        CreateBinderCardLabel.Request request,
        int userId = BinderFixture.UserId) =>
        new CreateBinderCardLabelValidator(fixture.Binders, userId).ValidateAsync(request);

    // -------------------------------------------------------------------------------------------
    // Happy path
    // -------------------------------------------------------------------------------------------

    [Fact]
    public async Task Creates_a_label_and_returns_its_id()
    {
        using var fixture = new BinderFixture().WithBinder();

        var response = await HandleAsync(fixture, Create());

        var stored = await fixture.StoredLabelsAsync();

        Assert.NotEqual(0, response.Id);
        Assert.Equal(("Trade", "blue"), stored[response.Id]);
    }

    [Fact]
    public async Task Trims_the_name()
    {
        using var fixture = new BinderFixture().WithBinder();

        var response = await HandleAsync(fixture, Create(name: "  Trade  "));

        var stored = await fixture.StoredLabelsAsync();

        Assert.Equal("Trade", stored[response.Id].Name);
    }

    // -------------------------------------------------------------------------------------------
    // What the validator refuses
    // -------------------------------------------------------------------------------------------

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task A_blank_name_is_refused(string? blank)
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Create(name: blank));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBinderCardLabel.Request.Name));
    }

    [Fact]
    public async Task A_name_past_the_cap_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder();

        var tooLong = new string('x', CreateBinderCardLabel.MaxNameLength + 1);

        var result = await ValidateAsync(fixture, Create(name: tooLong));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBinderCardLabel.Request.Name));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("mauve")]
    public async Task An_unknown_colour_is_refused(string? color)
    {
        using var fixture = new BinderFixture().WithBinder();

        var result = await ValidateAsync(fixture, Create(color: color));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBinderCardLabel.Request.Color));
    }

    [Fact]
    public async Task A_duplicate_name_on_the_same_binder_is_refused()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithLabel(1, "Trade");

        var result = await ValidateAsync(fixture, Create(name: "Trade"));

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBinderCardLabel.Request.Name));
    }

    [Fact]
    public async Task A_duplicate_name_is_detected_after_trimming()
    {
        using var fixture = new BinderFixture()
            .WithBinder()
            .WithLabel(1, "Trade");

        var result = await ValidateAsync(fixture, Create(name: "  Trade  "));

        Assert.False(result.IsValid);
    }

    [Fact]
    public async Task Someone_elses_binder_is_refused()
    {
        using var fixture = new BinderFixture().WithBinder(userId: BinderFixture.OtherUserId);

        var result = await ValidateAsync(fixture, Create(), userId: BinderFixture.UserId);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.PropertyName == nameof(CreateBinderCardLabel.Request.BinderId));
    }
}
