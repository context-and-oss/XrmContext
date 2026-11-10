using DataverseProxyGenerator.Core.Generation.Utilities;

namespace DataverseProxyGenerator.Tests;

public class NameSanitizerTests
{
    [Fact]
    public void SanitizeEnumOptionName_WithPlusCharacter_RemovesCharacter()
    {
        // Arrange
        var label = "Løbende måned + 14 dage";
        var optionValue = 11;

        // Act
        var result = NameSanitizer.SanitizeEnumOptionName(label, optionValue);

        // Assert
        Assert.Equal("Løbendemåned14dage", result);
    }

    [Fact]
    public void SanitizeEnumOptionName_WithNewlineCharacters_RemovesCharacters()
    {
        // Arrange
        var label = "Line1\nLine2\r\nLine3";
        var optionValue = 1;

        // Act
        var result = NameSanitizer.SanitizeEnumOptionName(label, optionValue);

        // Assert
        Assert.Equal("Line1Line2Line3", result);
    }

    [Fact]
    public void SanitizeEnumOptionName_WithTabCharacter_RemovesCharacter()
    {
        // Arrange
        var label = "Field1\tField2";
        var optionValue = 2;

        // Act
        var result = NameSanitizer.SanitizeEnumOptionName(label, optionValue);

        // Assert
        Assert.Equal("Field1Field2", result);
    }

    [Fact]
    public void SanitizeEnumOptionName_WithMultipleSpecialCharacters_RemovesAllCharacters()
    {
        // Arrange
        var label = "Test+Value\nWith\tSpecial.Characters";
        var optionValue = 3;

        // Act
        var result = NameSanitizer.SanitizeEnumOptionName(label, optionValue);

        // Assert
        Assert.Equal("TestValueWithSpecialCharacters", result);
    }

    [Fact]
    public void SanitizeName_WithPlusCharacter_RemovesCharacter()
    {
        // Arrange
        var name = "Field+Name";

        // Act
        var result = NameSanitizer.SanitizeName(name);

        // Assert
        Assert.Equal("FieldName", result);
    }

    [Fact]
    public void SanitizeName_WithNewlineCharacters_RemovesCharacters()
    {
        // Arrange
        var name = "Multi\nLine\r\nText";

        // Act
        var result = NameSanitizer.SanitizeName(name);

        // Assert
        Assert.Equal("MultiLineText", result);
    }

    [Theory]
    [InlineData("+")]
    [InlineData("\n")]
    [InlineData("\r")]
    [InlineData("\t")]
    [InlineData("(")]
    [InlineData(")")]
    [InlineData("'")]
    [InlineData("-")]
    [InlineData("–")]
    [InlineData("%")]
    [InlineData(" ")]
    [InlineData(".")]
    [InlineData(",")]
    [InlineData(":")]
    [InlineData(";")]
    [InlineData("/")]
    [InlineData("\\")]
    [InlineData("&")]
    [InlineData("$")]
    [InlineData("{")]
    [InlineData("}")]
    [InlineData("@")]
    [InlineData("#")]
    [InlineData("!")]
    [InlineData("*")]
    [InlineData("~")]
    [InlineData("`")]
    [InlineData("=")]
    [InlineData("<")]
    [InlineData(">")]
    [InlineData("?")]
    [InlineData("|")]
    [InlineData("\"")]
    public void SanitizeName_WithSpecialCharacter_RemovesCharacter(string specialChar)
    {
        // Arrange
        var name = $"Test{specialChar}Value";

        // Act
        var result = NameSanitizer.SanitizeName(name);

        // Assert
        Assert.Equal($"TestValue", result);
    }

    [Fact]
    public void SanitizeString_WithQuotes_EscapesQuotes()
    {
        // Arrange
        var input = "She said \"Hello\"";

        // Act
        var result = NameSanitizer.SanitizeString(input);

        // Assert
        Assert.Equal("She said \\\"Hello\\\"", result);
    }

    [Fact]
    public void SanitizeString_WithBackslash_EscapesBackslash()
    {
        // Arrange
        var input = "C:\\Windows\\System32";

        // Act
        var result = NameSanitizer.SanitizeString(input);

        // Assert
        Assert.Equal("C:\\\\Windows\\\\System32", result);
    }

    [Fact]
    public void SanitizeString_WithBackslashAndQuotes_EscapesBoth()
    {
        // Arrange
        var input = "Path is \"C:\\Program Files\"";

        // Act
        var result = NameSanitizer.SanitizeString(input);

        // Assert
        Assert.Equal("Path is \\\"C:\\\\Program Files\\\"", result);
    }

    [Fact]
    public void SanitizeString_WithNewlines_RemovesNewlines()
    {
        // Arrange
        var input = "Line1\nLine2\r\nLine3";

        // Act
        var result = NameSanitizer.SanitizeString(input);

        // Assert
        Assert.Equal("Line1Line2Line3", result);
    }

    [Fact]
    public void SanitizeString_WithSpecialCharactersForOptionSetMetadata_EscapesCorrectly()
    {
        // Arrange
        var input = "Value with $ and { and } characters";

        // Act
        var result = NameSanitizer.SanitizeString(input);

        // Assert - These characters should NOT be escaped, only quotes and backslashes
        Assert.Equal("Value with $ and { and } characters", result);
    }

    [Fact]
    public void EscapeXmlDoc_WithAmpersandAndAngleBrackets_EscapesThem()
    {
        // Arrange
        var input = "Amount must be < 1000 & > 0";

        // Act
        var result = NameSanitizer.EscapeXmlDoc(input);

        // Assert
        Assert.Equal("Amount must be &lt; 1000 &amp; &gt; 0", result);
    }

    [Fact]
    public void EscapeXmlDoc_WithAlreadyEscapedEntity_EscapesAmpersand()
    {
        // Arrange
        var input = "Tom &amp; Jerry";

        // Act
        var result = NameSanitizer.EscapeXmlDoc(input);

        // Assert
        Assert.Equal("Tom &amp;amp; Jerry", result);
    }

    [Fact]
    public void EscapeXmlDoc_WithQuotes_KeepsThem()
    {
        // Arrange
        var input = "The \"main\" contact's name";

        // Act
        var result = NameSanitizer.EscapeXmlDoc(input);

        // Assert
        Assert.Equal("The \"main\" contact's name", result);
    }

    [Fact]
    public void EscapeXmlDoc_WithNull_ReturnsEmpty()
    {
        // Act
        var result = NameSanitizer.EscapeXmlDoc(null!);

        // Assert
        Assert.Equal(string.Empty, result);
    }
}