using Antlr4.Runtime.Misc;

namespace ETSL_Project;

/// <summary>
/// Custom exception used to propagate return values up through the visitor's recursive calls.
/// This effectively breaks the execution flow of a block and returns control to the function caller.
/// </summary>
public class ReturnException : Exception 
{
    public object? Value { get; }
    public ReturnException(object? value) => Value = value;
}

/// <summary>
/// The EtslVisitor navigates the Parse Tree and executes the logic of the ETSL script.
/// It uses a Stack-based environment to manage lexical scoping and support recursive functions.
/// </summary>
public class EtslVisitor : ETSLBaseVisitor<object?>
{
    // A stack of dictionaries representing the environment (Γ).
    // Each dictionary is a local scope; the stack ensures correct variable lookup order.
    private readonly Stack<Dictionary<string, object?>> _scopes = new();
    
    // Stores function contexts during the declaration phase for later dispatching.
    private readonly Dictionary<string, ETSLParser.FuncDeclContext> _methodTable = new();

    public EtslVisitor() 
    { 
        // Initialize the global scope at the bottom of the stack.
        _scopes.Push(new Dictionary<string, object?>()); 
    }

    // Helper to access the current active scope.
    private Dictionary<string, object?> CurrentScope => _scopes.Peek();

    // ---------------------------------------------------------
    // DECLARATIONS
    // ---------------------------------------------------------

    /// <summary>
    /// Handles both new variable declarations and updates to existing ones.
    /// Implements variable shadowing check: if the variable exists in an outer scope, update it.
    /// Otherwise, define it in the local scope.
    /// </summary>
    public override object? VisitVarDecl([NotNull] ETSLParser.VarDeclContext context)
    {
        var name = context.ID().GetText();
        var value = Visit(context.expression());

        // Semantic check: Traverse scopes to find if variable was already declared.
        foreach (var scope in _scopes)
        {
            if (scope.ContainsKey(name))
            {
                scope[name] = value;
                return null;
            }
        }

        // New variable definition within the current stack frame.
        CurrentScope[name] = value;
        return null;
    }

    /// <summary>
    /// Registers function signatures in the method table without executing the body.
    /// </summary>
    public override object? VisitFuncDecl([NotNull] ETSLParser.FuncDeclContext context)
    {
        var name = context.ID().GetText();
        _methodTable[name] = context;
        return null;
    }

    // ---------------------------------------------------------
    // ACTIONS & CONTROL FLOW
    // ---------------------------------------------------------

    /// <summary>
    /// Dispatches actions based on keywords (if, log, return) or handles assignments and calls.
    /// </summary>
    public override object? VisitAction([NotNull] ETSLParser.ActionContext context)
    {
        string firstToken = context.GetChild(0).GetText();

        // Control Flow: Conditional execution
        if (firstToken == "if")
        {
            var condition = Visit(context.expression());
            
            // Handles truthiness for different types (bool, int, float).
            bool isTrue = condition switch {
                bool b => b,
                int i => i != 0,
                float f => f != 0.0f,
                _ => condition != null
            };

            if (isTrue) return Visit(context.action());
            return null;
        }

        // Standard Output: Logging results to console
        if (firstToken == "log")
        {
            var value = Visit(context.expression());
            Console.WriteLine($"[ETSL LOG]: {value ?? "null"}");
            return null;
        }

        // Function Return: Triggers the stack unwind via ReturnException
        if (firstToken == "return")
        {
            var value = Visit(context.expression());
            throw new ReturnException(value);
        }

        // Statement: Direct function call (void or ignored return)
        if (context.ChildCount >= 3 && context.GetChild(1).GetText() == "(")
        {
            var funcName = context.GetChild(0).GetText();
            return ExecuteFunctionCall(funcName, context.argList());
        }

        // Statement: Variable re-assignment
        if (context.ChildCount >= 3 && context.GetChild(1).GetText() == "=")
        {
            var name = context.GetChild(0).GetText();
            var value = Visit(context.expression());
            
            foreach (var scope in _scopes)
            {
                if (scope.ContainsKey(name))
                {
                    scope[name] = value;
                    return null;
                }
            }
            throw new Exception($"Runtime Error: Cannot assign to undefined variable '{name}'.");
        }

        return base.VisitAction(context);
    }

    /// <summary>
    /// Manages function execution by creating a new stack frame (local scope),
    /// mapping arguments to parameters, and handling return value propagation.
    /// </summary>
    private object? ExecuteFunctionCall(string name, ETSLParser.ArgListContext? argList)
    {
        if (!_methodTable.ContainsKey(name)) 
            throw new Exception($"Runtime Error: Function '{name}' not found.");

        var funcCtx = _methodTable[name];
        var localScope = new Dictionary<string, object?>();
        
        // Parameter Binding: Map call arguments to function parameters in the new scope.
        if (funcCtx.paramList() != null && argList != null) 
        {
            var pars = funcCtx.paramList().param();
            var args = argList.expression();
            for (int i = 0; i < pars.Length && i < args.Length; i++)
            {
                localScope[pars[i].ID().GetText()] = Visit(args[i]);
            }
        }
        
        // Context Switch: Push the new local environment onto the scope stack.
        _scopes.Push(localScope);
        
        object? result = null;
        try 
        {
            Visit(funcCtx.block());
        }
        catch (ReturnException ex) 
        {
            // Capture the value returned via a 'return' statement.
            result = ex.Value;
        }
        finally 
        {
            // Environment Cleanup: Always pop the stack frame, even if an error occurs.
            _scopes.Pop();
        }
        
        return result;
    }

    /// <summary>
    /// Executes a sequence of actions within a block.
    /// </summary>
    public override object? VisitBlock([NotNull] ETSLParser.BlockContext context) 
    {
        object? res = null;
        foreach (var a in context.action()) 
        {
            res = Visit(a);
        }
        return res;
    }

    // ---------------------------------------------------------
    // EXPRESSIONS & LITERALS
    // ---------------------------------------------------------

    /// <summary>
    /// Evaluates logical comparisons between numeric types.
    /// </summary>
    public override object? VisitCompareExpr([NotNull] ETSLParser.CompareExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));
        var op = context.op.Text;

        if (left is int l && right is int r)
        {
            return op switch {
                "==" => l == r, "!=" => l != r,
                ">" => l > r,   "<" => l < r,
                ">=" => l >= r, "<=" => l <= r,
                _ => false
            };
        }
        if (left is float lf && right is float rf)
        {
             return op switch {
                "==" => lf == rf, "!=" => lf != rf,
                ">" => lf > rf,   "<" => lf < rf,
                ">=" => lf >= rf, "<=" => lf <= rf,
                _ => false
            };
        }
        return left?.Equals(right) ?? false;
    }

    public override object? VisitParenExpr([NotNull] ETSLParser.ParenExprContext context) => Visit(context.expression());

    public override object? VisitAddSubExpr([NotNull] ETSLParser.AddSubExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));
        var op = context.op.Text;

        if (left is int l && right is int r) return op == "+" ? l + r : l - r;
        if (left is float lf && right is float rf) return op == "+" ? lf + rf : lf - rf;
        
        // Support for implicit string concatenation.
        if (op == "+" && (left is string || right is string)) return $"{left}{right}";
        return null;
    }

    public override object? VisitMulDivExpr([NotNull] ETSLParser.MulDivExprContext context)
    {
        var left = Visit(context.expression(0));
        var right = Visit(context.expression(1));
        var op = context.op.Text;

        if (left is int l && right is int r) return op == "*" ? l * r : l / r;
        if (left is float lf && right is float rf) return op == "*" ? lf * rf : lf / rf;
        return null;
    }

    /// <summary>
    /// Variable Lookup: Searches from the current local scope down to the global scope.
    /// </summary>
    public override object? VisitIdExpr([NotNull] ETSLParser.IdExprContext context) 
    {
        var name = context.ID().GetText();
        foreach (var scope in _scopes) 
        {
            if (scope.ContainsKey(name)) return scope[name];
        }
        throw new Exception($"Runtime Error: Variable '{name}' is undefined.");
    }

    public override object? VisitCallExpr([NotNull] ETSLParser.CallExprContext context)
    {
        return ExecuteFunctionCall(context.ID().GetText(), context.argList());
    }

    // Literals: Conversion from token text to native C# types.
    public override object? VisitIntExpr([NotNull] ETSLParser.IntExprContext context) => int.Parse(context.INT().GetText());
    public override object? VisitFloatExpr([NotNull] ETSLParser.FloatExprContext context) => float.Parse(context.FLOAT().GetText());
    public override object? VisitStringExpr([NotNull] ETSLParser.StringExprContext context) => context.STRING().GetText().Trim('"');
    
    /// <summary>
    /// Handles boolean literal tokens 'true' and 'false'.
    /// </summary>
    public override object? VisitBooleanExpr([NotNull] ETSLParser.BooleanExprContext context) 
    {
        return context.BOOLEAN().GetText().ToLower() == "true";
    }
}