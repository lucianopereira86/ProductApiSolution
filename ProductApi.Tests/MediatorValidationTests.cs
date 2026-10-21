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
        public async Task SendAsync_WithInvalidCommand_ShouldNotExecuteHandler()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();

            var handlerMock = new Mock<IRequestHandler<UpdateProductCommand, bool>>();
            services.AddScoped(sp => handlerMock.Object);

            using var serviceProvider = services.BuildServiceProvider();

            var mediator = new Mediator(serviceProvider);

            var command = new UpdateProductCommand(1, "Product", -1, 1, null, Convert.ToBase64String(new byte[8]));

            // Act & Assert
            var exception = await Assert.ThrowsAsync<ValidationException>(() => mediator.SendAsync(command, CancellationToken.None));

            Assert.Contains(exception.Errors, error => error.PropertyName == nameof(UpdateProductCommand.Price));

            handlerMock.Verify(v => v.HandleAsync(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task SendAsync_WithValidCommand_ShouldExecuteHandlerOnce()
        {
            // Arrange
            var services = new ServiceCollection();
            services.AddScoped<IValidator<UpdateProductCommand>, UpdateProductCommandValidator>();

            var handlerMock = new Mock<IRequestHandler<UpdateProductCommand, bool>>();
            handlerMock.Setup(s => s.HandleAsync(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
            services.AddScoped(sp => handlerMock.Object);

            using var serviceProvider = services.BuildServiceProvider();

            var mediator = new Mediator(serviceProvider);

            var command = new UpdateProductCommand(1, "Product", 1, 1, null, Convert.ToBase64String(new byte[8]));

            // Act
            var result = await mediator.SendAsync(command, CancellationToken.None);

            // Assert
            Assert.True(result);
            handlerMock.Verify(v => v.HandleAsync(It.IsAny<UpdateProductCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task SendAsync_WithoutValidator_ShouldExecuteHandler()
        {
            // Arrange
            var services = new ServiceCollection();

            var product = new Product("user-123", "product", 1, 1);
            var handlerMock = new Mock<IRequestHandler<GetProductByIdQuery, Product?>>();
            handlerMock.Setup(s => s.HandleAsync(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(product);
            services.AddScoped(sp => handlerMock.Object);

            using var serviceProvider = services.BuildServiceProvider();

            var mediator = new Mediator(serviceProvider);

            var query = new GetProductByIdQuery(1);

            // Act
            var result = await mediator.SendAsync(query, CancellationToken.None);

            // Assert
            Assert.NotNull(result);
            Assert.Same(product, result);

            handlerMock.Verify(v => v.HandleAsync(It.IsAny<GetProductByIdQuery>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
