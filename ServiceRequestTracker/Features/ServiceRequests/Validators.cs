using FluentValidation;

namespace ServiceRequestTracker.Features.ServiceRequests;

public class CreateServiceRequestRequestValidator : AbstractValidator<CreateServiceRequestRequest>
{
    public CreateServiceRequestRequestValidator()
    {
        RuleFor(x => x.Title).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Priority).IsInEnum();
    }
}

public class ChangeStatusRequestValidator : AbstractValidator<ChangeStatusRequest>
{
    public ChangeStatusRequestValidator()
    {
        RuleFor(x => x.Status).NotNull().IsInEnum();
    }
}

public class ListServiceRequestsQueryValidator : AbstractValidator<ListServiceRequestsQuery>
{
    public const int MaxPageSize = 100;

    public ListServiceRequestsQueryValidator()
    {
        RuleFor(x => x.Page).InclusiveBetween(1, int.MaxValue / MaxPageSize);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.Status).IsInEnum();
    }
}