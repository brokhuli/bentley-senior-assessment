using FormsApi.Controllers;
using FormsApi.Repositories;
using Microsoft.Extensions.Logging;
using Moq;

namespace FormsApi.Tests.Controllers;

public class FormsControllerTests
{
    private readonly Mock<IFormDataRepository> _repository = new();
    private readonly Mock<ILogger<FormsController>> _logger = new();

    private FormsController CreateSut() => new(_repository.Object, _logger.Object);

    [Fact]
    public void Placeholder()
    {
        var sut = CreateSut();

        Assert.NotNull(sut);
    }
}
