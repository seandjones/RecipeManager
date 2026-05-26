using System.Text.RegularExpressions;

namespace RecipeManager.Web.Services;

/// <summary>
/// Parses ingredient lines to extract quantity, unit, and ingredient name.
/// </summary>
public static class IngredientParser
{
    /// <summary>
    /// Common cooking units recognized by the parser.
    /// </summary>
    private static readonly HashSet<string> CommonUnits = new(StringComparer.OrdinalIgnoreCase)
    {
        // Volume
        "cup", "cups", "c",
        "tablespoon", "tablespoons", "tbsp", "tbs", "t",
        "teaspoon", "teaspoons", "tsp",
        "fl oz", "fl. oz", "fluid ounce", "fluid ounces",
        "oz", "ounce", "ounces",
        "ml", "milliliter", "milliliters",
        "l", "liter", "liters",
        "gallon", "gallons", "gal",
        "pint", "pints", "pt",
        "quart", "quarts", "qt",
        
        // Weight
        "lb", "lbs", "pound", "pounds",
        "g", "gram", "grams",
        "kg", "kilogram", "kilograms",
        "mg", "milligram", "milligrams",
        
        // Count
        "whole", "piece", "pieces",
        "clove", "cloves",
        "stalk", "stalks",
        "bunch", "bunches",
        "slice", "slices",
        "pinch", "pinches",
        "dash", "dashes",
        "handful", "handfuls"
    };

    /// <summary>
    /// Parses an ingredient line into quantity, unit, and name components.
    /// </summary>
    /// <param name="line">The ingredient line to parse (e.g., "2 cups flour" or "1-2 tbsp olive oil")</param>
    /// <returns>A tuple of (Quantity, Unit, Name)</returns>
    public static (string? Quantity, string? Unit, string Name) Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return (null, null, string.Empty);
        }

        line = line.Trim();

        // Pattern: optional quantity (number, fraction, or range) + optional unit + rest is name
        // Examples:
        // "2 cups flour" -> (2, cups, flour)
        // "1/2 cup sugar" -> (1/2, cup, sugar)
        // "1-2 tbsp oil" -> (1-2, tbsp, oil)
        // "flour" -> (null, null, flour)
        // "a pinch of salt" -> (a, pinch, salt)

        // Match: optional quantity + optional whitespace + optional unit + optional whitespace + rest
        var quantityPattern = @"^([\d\-\.\/\s]+(?:\s*(?:and|or)\s*[\d\-\.\/\s]+)?)\s+";
        var quantityMatch = Regex.Match(line, quantityPattern);

        string? quantity = null;
        string? unit = null;
        string name = line;

        if (quantityMatch.Success)
        {
            quantity = quantityMatch.Groups[1].Value.Trim();
            var remainder = line.Substring(quantityMatch.Length).Trim();

            // Try to match a unit at the beginning of the remainder
            var unitTokens = remainder.Split(' ');
            if (unitTokens.Length > 0 && CommonUnits.Contains(unitTokens[0]))
            {
                unit = unitTokens[0];
                name = string.Join(" ", unitTokens.Skip(1)).Trim();
            }
            else
            {
                name = remainder;
            }
        }
        else
        {
            // No quantity found; try to find a unit at the start
            var tokens = line.Split(' ');
            if (tokens.Length > 1 && CommonUnits.Contains(tokens[0]))
            {
                unit = tokens[0];
                name = string.Join(" ", tokens.Skip(1)).Trim();
            }
        }

        // Fallback: if we parsed everything as quantity/unit but found no name, treat it all as name
        if (string.IsNullOrWhiteSpace(name))
        {
            quantity = null;
            unit = null;
            name = line;
        }

        return (quantity, unit, name);
    }
}
