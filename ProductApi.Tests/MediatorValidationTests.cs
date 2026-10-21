using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ProductApi.Products.Commands.UpdateProduct;
using System;
using System.Collections.Generic;
using System.Text;

namespace ProductApi.Tests
{
    public class MediatorValidationTests
    {
        [Fact]
        public async Task UpdateProductCommandValidator_WhenPriceIsNegative_ShouldThrowException()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();

            var updateProductHandlerMock = new Mock<IRequestHandler<UpdateProductCommand, bool>>();
            services.AddScoped(sp => updateProductHandlerMock.Object);

            using var serviceProvider = services.BuildServiceProvider();

            var mediator = new Mediator(serviceProvider);

            var command = new UpdateProductCommand(1, "Product", -1, 1, null, Convert.ToBase64String(new byte[8]));

            // Act & Assert
            await Assert.ThrowsAsync<ValidationException>(() => mediator.SendAsync(command, CancellationToken.None));

            updateProductHandlerMock.Verify(v => v.HandleAsync(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }
    }
}
