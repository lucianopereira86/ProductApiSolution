using FluentValidation;

namespace ProductApi.Products.Commands.UpdateProduct
{
    public class UpdateProductCommandValidator: AbstractValidator<UpdateProductCommand>
    {
        public UpdateProductCommandValidator()
        {
            RuleFor(r => r.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(r => r.Price).GreaterThan(0).WithMessage("Price must be greater than zero.");
            RuleFor(r => r.Stock).GreaterThanOrEqualTo(0).WithMessage("Stock must not be negative.");
        }
    }
}
