using System.ComponentModel.DataAnnotations;
using RecipeManager.ApiService.Data;

namespace RecipeManager.Tests;

[TestClass]
public class EntityValidationTests
{
    [TestMethod]
    public void User_WithValidEmail_PassesValidation()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.AreEqual(0, validationResults.Count, "Valid user should have no validation errors");
    }

    [TestMethod]
    public void User_WithInvalidEmail_FailsValidation()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "not-an-email",
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.IsTrue(validationResults.Count > 0, "Invalid email should fail validation");
        Assert.IsTrue(validationResults.Any(v => v.MemberNames.Contains("Email")));
    }

    [TestMethod]
    public void User_WithEmptyEmail_FailsValidation()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = string.Empty,
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(user);

        // Assert
        Assert.IsTrue(validationResults.Count > 0, "Empty email should fail validation");
    }

    [TestMethod]
    public void LoginCode_WithValid6DigitCode_PassesValidation()
    {
        // Arrange
        var loginCode = new LoginCode
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(loginCode);

        // Assert
        Assert.AreEqual(0, validationResults.Count, "Valid login code should have no validation errors");
    }

    [TestMethod]
    public void LoginCode_WithNon6DigitCode_FailsValidation()
    {
        // Arrange
        var loginCode = new LoginCode
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Code = "12345",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(loginCode);

        // Assert
        Assert.IsTrue(validationResults.Count > 0, "Code with less than 6 digits should fail validation");
    }

    [TestMethod]
    public void LoginCode_WithNonNumericCode_FailsValidation()
    {
        // Arrange
        var loginCode = new LoginCode
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Code = "ABC123",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        // Act
        var validationResults = ValidateModel(loginCode);

        // Assert
        Assert.IsTrue(validationResults.Count > 0, "Code with non-numeric characters should fail validation");
        Assert.IsTrue(validationResults.Any(v => v.ErrorMessage != null && v.ErrorMessage.Contains("6 digits")));
    }

    [TestMethod]
    public void LoginCode_InitiallyNotUsed()
    {
        // Arrange & Act
        var loginCode = new LoginCode
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid(),
            Code = "123456",
            ExpiresAt = DateTime.UtcNow.AddMinutes(15),
            CreatedAt = DateTime.UtcNow
        };

        // Assert
        Assert.IsFalse(loginCode.IsUsed, "New login code should not be marked as used");
    }

    private static IList<ValidationResult> ValidateModel(object model)
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(model, null, null);
        Validator.TryValidateObject(model, validationContext, validationResults, true);
        return validationResults;
    }
}
