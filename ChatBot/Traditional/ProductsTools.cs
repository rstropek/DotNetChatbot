using OpenAI.Responses;

namespace ChatBot.Traditional;

public static class ProductsTools
{
    public static readonly FunctionTool GetAvailableColorsForFlowerTool = ResponseTool.CreateFunctionTool(
        functionName: nameof(GetAvailableColorsForFlower),
        functionDescription: "Gets a list of available colors for a specific flower",
        functionParameters: FunctionHelpers.ToJsonSchema<GetAvailableColorsForFlowerRequest>(),
        strictModeEnabled: false
    );

    private static readonly Dictionary<string, List<string>> FlowerColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Rose"] = ["red", "yellow", "purple"],
        ["Lily"] = ["yellow", "pink", "white"],
        ["Gerbera"] = ["pink", "red", "yellow"],
        ["Freesia"] = ["white", "pink", "red", "yellow"],
        ["Tulip"] = ["red", "yellow", "purple"],
        ["Sunflower"] = ["yellow"]
    };

    public static IEnumerable<string> GetAvailableColorsForFlower(GetAvailableColorsForFlowerRequest request)
    {
        return FlowerColors.TryGetValue(request.FlowerName, out var colors) ? colors : [];
    }

    public record GetAvailableColorsForFlowerRequest(string FlowerName);
}