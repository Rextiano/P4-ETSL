using Antlr4.Runtime.Misc;

namespace ETSL_Project.Interpreter;

// This links your logic to the code you just generated
public class ETSLVisitor : ETSLBaseVisitor<object?>
{
    public override object? VisitAction([NotNull] ETSLParser.ActionContext context)
{
    if (context.GetText().StartsWith("log"))
    {
        // Get the string inside the log command
        var message = context.STRING()?.GetText();
        Console.WriteLine($"[ETSL LOG]: {message}");
    }
    return null;
}
}