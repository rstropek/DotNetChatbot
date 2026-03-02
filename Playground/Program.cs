using System.Reflection;

// Check extension methods on OpenAIResponseClientExtensions
var asm = Assembly.Load("Microsoft.Agents.AI.OpenAI");
var type = asm.GetType("OpenAI.Responses.OpenAIResponseClientExtensions")!;
foreach (var m in type.GetMethods(BindingFlags.Public | BindingFlags.Static))
{
    var parms = string.Join(", ", m.GetParameters().Select(p => $"{p.ParameterType.Name} {p.Name}"));
    Console.WriteLine($"  {m.ReturnType.Name} {m.Name}({parms})");
}
