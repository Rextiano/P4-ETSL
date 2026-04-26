using Antlr4.Runtime;
using ETSL_Project.Interpreter; // This assumes you made the folder from the previous step

try 
{
    // 1. Read the ETSL file
    string input = File.ReadAllText("Samples/test.etsl");
    Console.WriteLine("--- Testing ETSL Parser ---");

    // 2. Lexical Analysis (Characters -> Tokens)
    var inputStream = CharStreams.fromString(input);
    var lexer = new ETSLLexer(inputStream);
    var tokenStream = new CommonTokenStream(lexer);

    // 3. Syntactic Analysis (Tokens -> Parse Tree)
    var parser = new ETSLParser(tokenStream);
    var tree = parser.program(); 

    // 4. Visit the Tree (Run the logic)
    var visitor = new ETSLVisitor();
    visitor.Visit(tree);

    Console.WriteLine("--- Execution Finished Successfully ---");
}
catch (Exception ex)
{
    Console.WriteLine($"Error: {ex.Message}");
}