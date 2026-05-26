using Microsoft.VisualStudio.TestTools.UnitTesting;
using RecipeManager.Web.Services;

namespace RecipeManager.Tests;

[TestClass]
public class IngredientParserTests
{
    [TestMethod]
    public void Parse_WithQuantityUnitAndName_ExtractsAllParts()
    {
        var (quantity, unit, name) = IngredientParser.Parse("2 cups flour");
        Assert.AreEqual("2", quantity);
        Assert.AreEqual("cups", unit);
        Assert.AreEqual("flour", name);
    }

    [TestMethod]
    public void Parse_WithFraction_ExtractsCorrectly()
    {
        var (quantity, unit, name) = IngredientParser.Parse("1/2 cup sugar");
        Assert.AreEqual("1/2", quantity);
        Assert.AreEqual("cup", unit);
        Assert.AreEqual("sugar", name);
    }

    [TestMethod]
    public void Parse_WithRange_ExtractsRange()
    {
        var (quantity, unit, name) = IngredientParser.Parse("1-2 tbsp olive oil");
        Assert.AreEqual("1-2", quantity);
        Assert.AreEqual("tbsp", unit);
        Assert.AreEqual("olive oil", name);
    }

    [TestMethod]
    public void Parse_WithoutQuantity_ExtractsUnitAndName()
    {
        var (quantity, unit, name) = IngredientParser.Parse("a pinch of salt");
        Assert.IsNull(quantity);
        Assert.AreEqual("pinch", unit);
        Assert.AreEqual("of salt", name);
    }

    [TestMethod]
    public void Parse_PlainIngredient_ReturnsNameOnly()
    {
        var (quantity, unit, name) = IngredientParser.Parse("flour");
        Assert.IsNull(quantity);
        Assert.IsNull(unit);
        Assert.AreEqual("flour", name);
    }

    [TestMethod]
    public void Parse_WithMultipleWordName_ExtractsCorrectly()
    {
        var (quantity, unit, name) = IngredientParser.Parse("3 cloves fresh garlic");
        Assert.AreEqual("3", quantity);
        Assert.AreEqual("cloves", unit);
        Assert.AreEqual("fresh garlic", name);
    }

    [TestMethod]
    public void Parse_WithWhitespace_TrimsAndNormalizes()
    {
        var (quantity, unit, name) = IngredientParser.Parse("  2   cups   flour  ");
        Assert.AreEqual("2", quantity);
        Assert.AreEqual("cups", unit);
        Assert.AreEqual("flour", name);
    }

    [TestMethod]
    public void Parse_WithWeightUnit_ExtractsCorrectly()
    {
        var (quantity, unit, name) = IngredientParser.Parse("250 g flour");
        Assert.AreEqual("250", quantity);
        Assert.AreEqual("g", unit);
        Assert.AreEqual("flour", name);
    }

    [TestMethod]
    public void Parse_EmptyString_ReturnsEmptyName()
    {
        var (quantity, unit, name) = IngredientParser.Parse("   ");
        Assert.IsNull(quantity);
        Assert.IsNull(unit);
        Assert.AreEqual(string.Empty, name);
    }

    [TestMethod]
    public void Parse_CaseInsensitiveUnit_NormalizesUnit()
    {
        var (quantity, unit, name) = IngredientParser.Parse("2 CUPS flour");
        Assert.AreEqual("2", quantity);
        Assert.AreEqual("CUPS", unit);
        Assert.AreEqual("flour", name);
    }
}
