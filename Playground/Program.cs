using System.Reflection;
using Microsoft.Agents.AI;

foreach (var typeName in new[] { "Microsoft.Agents.AI.AgentSession", "Microsoft.Agents.AI.AgentSessionStateBag" })
{
    var asm = typeof(AgentSession).Assembly;
    var t = asm.GetType(typeName)!;
    Console.WriteLine($"\n=== {t.Name} (base: {t.BaseType?.Name ?? "none"}) ===");
    Console.WriteLine("Properties:");
    foreach (var p in t.GetProperties(BindingFlags.Public | BindingFlags.Instance))
        Console.WriteLine($"  {p.PropertyType.Name} {p.Name}");
    Console.WriteLine("Fields:");
    foreach (var f in t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
        Console.WriteLine($"  [{(f.IsPublic ? "pub" : f.IsPrivate ? "prv" : "int")}] {f.FieldType.Name} {f.Name}");
}


