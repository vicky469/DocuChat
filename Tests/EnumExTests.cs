using Common.Extensions;
using DocumentAPI.DTO.SEC;

namespace Tests;

public class EnumExTests
{
    [Theory]
    [InlineData("Form 10-K Summary", Sec10KFormSectionEnum.Item16)]
    public void TryGetEnumFromDescriptionTest1(string inputDescription, Sec10KFormSectionEnum expectedOutput)
    {
        // Act
        var result = EnumEx.TryGetEnumFromDescription<Sec10KFormSectionEnum>(inputDescription);

        // Assert
        Assert.Equal(expectedOutput, result);
    }
    
    [Theory]
    [InlineData("MICROSOFT CORP", SecCompanyEnum.MICROSOFTCORP)]
    public void TryGetEnumFromDescriptionTest2(string inputDescription, SecCompanyEnum expectedOutput)
    {
        // Act
        var result = EnumEx.TryGetEnumFromDescription<SecCompanyEnum>(inputDescription);

        // Assert
        Assert.Equal(expectedOutput, result);
    }
}