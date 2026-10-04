using FluentValidation;
using Microsoft.EntityFrameworkCore;
using PokeBinder.Binders.DbContext;

namespace PokeBinder.Features.Binder.DeleteBinder;

/// <summary>
/// The one thing that can be wrong with a delete: the binder is not the caller's to delete. It
/// runs first and the handler runs only if it passes.
/// </summary>
/// <param name="context">The user's binders.</param>
/// <param name="userId">The signed-in user, from the principal rather than from the request.</param>
public class DeleteBinderValidator : AbstractValidator<DeleteBinder.Request>
{
    public DeleteBinderValidator(BinderDbContext context, int userId)
    {
        RuleFor(request => request.BinderId)
            // Someone else's binder answers the same way a missing one does, so a request cannot be
            // used to find out which binder ids exist.
            .MustAsync((binderId, ct) => context.Binders
                .AnyAsync(binder => binder.Id == binderId && binder.UserId == userId, ct))
            .WithMessage("That binder no longer exists.");
    }
}
