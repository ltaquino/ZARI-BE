using FluentValidation;

namespace ZARI.Api.Extensions;

/// <summary>
/// For endpoints that bind {id} from the route plus a separate *Request body type, then construct
/// the real command manually inside the handler — AddEndpointFilter&lt;ValidationFilter&lt;TCommand&gt;&gt;
/// silently no-ops there, because ValidationFilter looks for a TCommand-typed bound *argument*, and
/// the command in that shape is a local variable, never one of context.Arguments. Call this
/// explicitly on the constructed command instead wherever that shape is used.
/// </summary>
public static class ValidatorExtensions
{
    public static async Task<IResult?> ValidateOrProblemAsync<T>(this IValidator<T> validator, T instance)
    {
        var result = await validator.ValidateAsync(instance);
        if (result.IsValid) return null;

        var errors = result.Errors
            .GroupBy(e => e.PropertyName)
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());

        return Results.ValidationProblem(errors);
    }
}
