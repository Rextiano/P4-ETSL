using Antlr4.Runtime;
using ETSL_Project;

/**
 * Entry point for the ETSL Interpreter.
 * This program handles the full translation pipeline: 
 * Lexing -> Parsing -> Tree Generation -> Execution via Visitor.
 */
try
{
    // 1. Load the source code from the specified ETSL script file
    string input = File.ReadAllText("Samples/test.etsl");

    // 2. Lexical Analysis: Convert the raw text into a stream of meaningful tokens
    ICharStream stream = CharStreams.fromString(input);
    ETSLLexer lexer = new ETSLLexer(stream);
    ITokenStream tokens = new CommonTokenStream(lexer);

    // 3. Syntax Analysis: Parse the tokens into a concrete syntax tree (CST) based on the G4 grammar
    ETSLParser parser = new ETSLParser(tokens);
    var tree = parser.program();

    // 4. Semantic Execution: Traverse the tree using the custom Visitor pattern.
    // The visitor maintains the runtime environment (Stack-based scoping) and executes the logic.
    var visitor = new EtslVisitor(); 
    visitor.Visit(tree);

    // Final output to signal successful execution of the script logic
    Console.WriteLine("\n[System]: Execution finished successfully.");
}
catch (Exception ex)
{
    // Global error handling for Lexer/Parser errors or Runtime exceptions (e.g., Undefined variables)
    Console.WriteLine($"\n[Runtime/Syntax Error]: {ex.Message}");
}