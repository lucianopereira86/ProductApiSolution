using FluentValidation;
using FluentValidation.Results;

public class Mediator(IServiceProvider serviceProvider) : IMediator
{
    private readonly IServiceProvider _serviceProvider = serviceProvider;

    public Task<TResult> SendAsync<TResult>(IRequest<TResult> request, CancellationToken cancellationToken)
    {
        var requestType = request.GetType();
        var resultType = typeof(TResult);

        var validatorType = typeof(IValidator<>).MakeGenericType(requestType);
        var validator = _serviceProvider.GetService(validatorType);
        if (validator is not null)
        {
            var validationMethod = validatorType.GetMethod("Validate", [requestType]);

            if (validationMethod is null)
                throw new InvalidOperationException("Validate method not found.");

            var validationInvoked = validationMethod.Invoke(validator, [request]);

            if (validationInvoked is ValidationResult validationResult && !validationResult.IsValid)
                throw new ValidationException(validationResult.Errors);
        }

        var handlerType = typeof(IRequestHandler<,>).MakeGenericType(requestType, resultType);
        var handleMethod = handlerType.GetMethod("HandleAsync");
        if (handleMethod is null)
            throw new InvalidOperationException($"HandleAsync was not found on {handlerType.Name}");
        var handler = _serviceProvider.GetRequiredService(handlerType);
        var result = handleMethod.Invoke(handler, [request, cancellationToken]);
        var task = (Task<TResult>)result!;
        return task;
    }
}