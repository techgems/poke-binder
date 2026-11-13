using Fluid;
using Fluid.Ast;

namespace PokeBinder.Emails.Rendering;

/// <summary>
/// Finds the variables a parsed template names that its model does not have.
/// </summary>
/// <remarks>
/// This is done against the model's type, not at render time, because Fluid cannot tell the two
/// apart while rendering: a property that is null and a name the model does not have both arrive
/// as "undefined". Fluid's own strict mode therefore rejects every optional value, and with it
/// off, a misspelt <c>{{ sigInLink }}</c> renders as nothing at all.
/// <para>
/// A path is checked one segment at a time for as long as it walks through the model's own
/// objects. It stops at a scalar or a list, where Liquid's built-ins (<c>size</c>, <c>first</c>)
/// take over, and at an indexer, whose key is not known until render time. A name the template
/// defines itself -- <c>assign</c>, <c>capture</c>, <c>increment</c> -- is not checked, and a
/// <c>for</c> variable is checked against the element type of the list it walks.
/// </para>
/// </remarks>
internal sealed class VariableCheck : AstVisitor
{
    // What Liquid provides whatever the model is.
    private static readonly HashSet<string> BuiltIns =
        new(["forloop", "tablerowloop", "empty", "blank", "nil", "null", "true", "false"], StringComparer.OrdinalIgnoreCase);

    private readonly Type _modelType;
    private readonly HashSet<string> _assigned;
    private readonly Dictionary<string, Type?> _loopVariables = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _unknown = [];

    private VariableCheck(Type modelType, HashSet<string> assigned)
    {
        _modelType = modelType;
        _assigned = assigned;
    }

    /// <summary>Every path the template names that the model has no member for, as written.</summary>
    public static IReadOnlyList<string> Unknown(IFluidTemplate template, Type modelType)
    {
        var assigned = new AssignedNames();
        assigned.VisitTemplate(template);

        var check = new VariableCheck(modelType, assigned.Names);
        check.VisitTemplate(template);
        return check._unknown.Distinct(StringComparer.OrdinalIgnoreCase).ToList();
    }

    protected override Statement VisitForStatement(ForStatement statement)
    {
        // Bound before the body is visited, which is where the variable is used.
        _loopVariables[statement.Identifier] = statement.Source is MemberExpression source
            ? ElementTypeOf(source)
            : null;
        return base.VisitForStatement(statement);
    }

    protected override Statement VisitTableRowStatement(TableRowStatement statement)
    {
        _loopVariables[statement.Identifier] = statement.Source is MemberExpression source
            ? ElementTypeOf(source)
            : null;
        return base.VisitTableRowStatement(statement);
    }

    protected override Expression VisitMemberExpression(MemberExpression expression)
    {
        var names = Names(expression);
        if (names.Count > 0 && Resolve(names) is { Known: false })
            _unknown.Add(string.Join('.', names));
        return base.VisitMemberExpression(expression);
    }

    private Type? ElementTypeOf(MemberExpression source)
    {
        var resolved = Resolve(Names(source));
        return resolved is { Known: true, Type: { } type } ? ModelTypes.ElementType(type) : null;
    }

    // Walks the path through the model. Type is null once the path leaves what can be checked.
    private (bool Known, Type? Type) Resolve(IReadOnlyList<string> names)
    {
        var root = names[0];
        if (BuiltIns.Contains(root) || _assigned.Contains(root)) return (true, null);

        Type? current;
        int next;
        if (_loopVariables.TryGetValue(root, out var element))
        {
            current = element;
            next = 1;
        }
        else
        {
            current = _modelType;
            next = 0;
        }

        for (var i = next; i < names.Count; i++)
        {
            if (current is null || ModelTypes.IsScalar(current) || ModelTypes.ElementType(current) is not null)
                return (true, null);

            var property = ModelTypes.Property(current, names[i]);
            if (property is null) return (false, null);
            current = property.PropertyType;
        }

        return (true, current);
    }

    // The leading run of plain names: `owner.name` from `owner.name`, `binders` from `binders[0].x`.
    private static List<string> Names(MemberExpression expression) =>
        expression.Segments.TakeWhile(s => s is IdentifierSegment).Select(s => ((IdentifierSegment)s).Identifier).ToList();

    /// <summary>The names a template defines for itself, wherever in the template it does so.</summary>
    private sealed class AssignedNames : AstVisitor
    {
        public HashSet<string> Names { get; } = new(StringComparer.OrdinalIgnoreCase);

        protected override Statement VisitAssignStatement(AssignStatement statement)
        {
            Names.Add(statement.Identifier);
            return base.VisitAssignStatement(statement);
        }

        protected override Statement VisitCaptureStatement(CaptureStatement statement)
        {
            Names.Add(statement.Identifier);
            return base.VisitCaptureStatement(statement);
        }

        protected override Statement VisitIncrementStatement(IncrementStatement statement)
        {
            Names.Add(statement.Identifier);
            return base.VisitIncrementStatement(statement);
        }

        protected override Statement VisitDecrementStatement(DecrementStatement statement)
        {
            Names.Add(statement.Identifier);
            return base.VisitDecrementStatement(statement);
        }
    }
}
