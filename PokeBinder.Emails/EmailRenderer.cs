using System.Collections.Concurrent;
using System.Text.Encodings.Web;
using Fluid;
using PokeBinder.Emails.Rendering;

namespace PokeBinder.Emails;

/// <summary>
/// Renders an <see cref="EmailTemplate"/> against a model with Fluid, a Liquid engine. Liquid
/// rather than Razor because templates are edited at runtime: Liquid is parsed, not compiled, and a
/// template can only reach what the model hands it.
/// </summary>
/// <remarks>
/// A template sees the model's public properties as top-level variables, matched without regard to
/// case: a model with <c>SignInLink</c> is <c>{{ signInLink }}</c> in the template. Nested objects
/// and the elements of lists are reachable too. Nothing else is, which is the point of registering
/// types rather than letting Fluid reflect over anything it is handed.
/// <para>
/// A template that names a variable its model does not have, or a filter Fluid does not know, is
/// refused rather than rendered with a blank where the value should be: a misspelt
/// <c>{{ sigInLink }}</c> is an exception, not a sign-in email with no link in it. A property that
/// is merely null renders empty, as Liquid always does, so optional values stay testable with
/// <c>{% if %}</c>.
/// </para>
/// </remarks>
public sealed class EmailRenderer
{
    // Thread-safe, so one parser serves every render. Templates are parsed on every render rather
    // than cached: they change at runtime, and a parse is cheap next to sending the email.
    private static readonly FluidParser Parser = new();

    // One TemplateOptions per model type, fully registered before anything renders with it, so
    // nothing mutates an options object that another render may be reading.
    private readonly ConcurrentDictionary<Type, TemplateOptions> _optionsByModel = new();

    /// <summary>Renders every part of the template.</summary>
    /// <exception cref="EmailTemplateException">
    /// A part does not parse, names something the model does not have, or fails while rendering.
    /// </exception>
    public async Task<RenderedEmail> RenderAsync<TModel>(EmailTemplate template, TModel model)
        where TModel : class
    {
        if (Problems(template, typeof(TModel)).FirstOrDefault() is { } problem) throw problem;

        var options = _optionsByModel.GetOrAdd(typeof(TModel), CreateOptions);

        var subject = await RenderPartAsync("subject", template.Subject, model, options, NullEncoder.Default);
        var html = await RenderPartAsync("html", template.Html, model, options, HtmlEncoder.Default);
        var text = template.Text is null
            ? null
            : await RenderPartAsync("text", template.Text, model, options, NullEncoder.Default);

        // A subject is one line; a template that breaks it over several still means one.
        return new RenderedEmail(string.Join(' ', subject.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)), html, text);
    }

    /// <summary>
    /// Everything wrong with a template for a model, without rendering it: parse errors, and
    /// variables the model does not have. For whatever saves an edited template, so a broken one
    /// is refused at the editor and not at send time. Empty when the template is good.
    /// </summary>
    public IReadOnlyList<EmailTemplateException> Validate<TModel>(EmailTemplate template)
        where TModel : class =>
        Problems(template, typeof(TModel)).ToList();

    private static IEnumerable<EmailTemplateException> Problems(EmailTemplate template, Type modelType)
    {
        var parts = new List<(string Part, string Source)> { ("subject", template.Subject), ("html", template.Html) };
        if (template.Text is not null) parts.Add(("text", template.Text));

        foreach (var (part, source) in parts)
        {
            if (!Parser.TryParse(source, out var parsed, out var error))
            {
                yield return new EmailTemplateException(part, error);
                continue;
            }

            foreach (var unknown in VariableCheck.Unknown(parsed, modelType))
                yield return new EmailTemplateException(part, $"'{unknown}' is not something this email's model has.");
        }
    }

    private static async Task<string> RenderPartAsync(
        string part, string source, object model, TemplateOptions options, TextEncoder encoder)
    {
        var parsed = Parser.Parse(source);

        try
        {
            return await parsed.RenderAsync(new TemplateContext(model, options), encoder);
        }
        // An unknown filter, or a template that runs past MaxSteps.
        catch (Exception ex) when (ex is InvalidOperationException or FluidException)
        {
            throw new EmailTemplateException(part, ex.Message, ex);
        }
    }

    private static TemplateOptions CreateOptions(Type modelType)
    {
        var options = new TemplateOptions
        {
            StrictFilters = true,
            // A template edited at runtime can loop; this bounds what one render can cost.
            MaxSteps = 100_000,
            ModelNamesComparer = StringComparer.OrdinalIgnoreCase,
        };
        options.MemberAccessStrategy.IgnoreCasing = true;

        RegisterGraph(options.MemberAccessStrategy, modelType, []);
        return options;
    }

    // The model, and every object type reachable from its properties, through lists as well.
    private static void RegisterGraph(MemberAccessStrategy strategy, Type type, HashSet<Type> seen)
    {
        type = ModelTypes.Unwrap(type);
        if (ModelTypes.IsScalar(type) || !seen.Add(type)) return;

        if (ModelTypes.ElementType(type) is { } element)
        {
            RegisterGraph(strategy, element, seen);
            return;
        }

        strategy.Register(type);
        foreach (var property in type.GetProperties())
            RegisterGraph(strategy, property.PropertyType, seen);
    }
}
