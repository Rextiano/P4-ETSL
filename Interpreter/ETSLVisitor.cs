using Antlr4.Runtime.Misc;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ETSL_Project 
{
    public class EtslVisitor : ETSLBaseVisitor<object?>
    {
        private readonly Stack<Dictionary<string, object>> _scopes = new();
        private readonly Dictionary<string, ETSLParser.FuncDeclContext> _functions = new();

        public EtslVisitor()
        {
            _scopes.Push(new Dictionary<string, object>());
        }

        private Dictionary<string, object> CurrentScope => _scopes.Peek();

        public override object? VisitProgram(ETSLParser.ProgramContext context)
        {
            // Hoist functions first
            foreach (var decl in context.declaration())
            {
                if (decl.funcDecl() != null)
                {
                    _functions[decl.funcDecl().ID().GetText()] = decl.funcDecl();
                }
            }

            // Execute declarations and actions
            foreach (var decl in context.declaration())
            {
                Visit(decl);
            }

            return null;
        }

        public override object? VisitDeclaration(ETSLParser.DeclarationContext context)
        {
            if (context.varDecl() != null)
                return Visit(context.varDecl());
            if (context.funcDecl() != null)
                return null; // Functions already hoisted
            if (context.action() != null)
                return Visit(context.action());
            
            return base.VisitDeclaration(context);
        }

        public override object? VisitVarDecl(ETSLParser.VarDeclContext context)
        {
            string name = context.ID().GetText();
            object? value = Visit(context.expression());
            
            if (value == null)
            {
                string type = context.type().GetText();
                value = type switch
                {
                    "int" => 0,
                    "float" => 0.0,
                    "bool" => false,
                    "string" => "",
                    _ => 0
                };
            }
            
            CurrentScope[name] = value;
            return null;
        }

        public override object? VisitBlockAction(ETSLParser.BlockActionContext context)
        {
            return Visit(context.block());
        }

        public override object? VisitVarDeclAction(ETSLParser.VarDeclActionContext context)
        {
            return Visit(context.varDecl());
        }

        public override object? VisitLogAction(ETSLParser.LogActionContext context)
        {
            var value = Visit(context.expression());
            Console.WriteLine(value?.ToString() ?? "null");
            return null;
        }

        public override object? VisitIfAction(ETSLParser.IfActionContext context)
        {
            var cond = Visit(context.expression());
            if (cond is bool b && b)
            {
                Visit(context.action());
            }
            return null;
        }

        public override object? VisitReturnAction(ETSLParser.ReturnActionContext context)
        {
            throw new ReturnSignal(Visit(context.expression()));
        }

        public override object? VisitExprAction(ETSLParser.ExprActionContext context)
        {
            return Visit(context.expression());
        }

        public override object? VisitSkipAction(ETSLParser.SkipActionContext context)
        {
            return null;
        }

        public override object? VisitBlock(ETSLParser.BlockContext context)
        {
            _scopes.Push(new Dictionary<string, object>());
            try
            {
                foreach (var action in context.action())
                {
                    Visit(action);
                }
            }
            finally
            {
                _scopes.Pop();
            }
            return null;
        }

        public override object? VisitIdExpr(ETSLParser.IdExprContext context)
        {
            string name = context.ID().GetText();
            
            foreach (var scope in _scopes)
            {
                if (scope.TryGetValue(name, out var val))
                {
                    return val;
                }
            }
            
            throw new Exception($"Undefined variable: {name}");
        }

        public override object? VisitAssignExpr(ETSLParser.AssignExprContext context)
        {
            string name = context.ID().GetText();
            object? value = Visit(context.expression());
            
            foreach (var scope in _scopes)
            {
                if (scope.ContainsKey(name))
                {
                    scope[name] = value;
                    return value;
                }
            }
            
            CurrentScope[name] = value;
            return value;
        }

        public override object? VisitAddSubExpr(ETSLParser.AddSubExprContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));
            
            if (context.op.Text == "+" && (left is string || right is string))
            {
                return (left?.ToString() ?? "") + (right?.ToString() ?? "");
            }
            
            double leftNum = Convert.ToDouble(left ?? 0);
            double rightNum = Convert.ToDouble(right ?? 0);
            
            return context.op.Text == "+" ? leftNum + rightNum : leftNum - rightNum;
        }

        public override object? VisitMulDivExpr(ETSLParser.MulDivExprContext context)
        {
            double left = Convert.ToDouble(Visit(context.expression(0)) ?? 0);
            double right = Convert.ToDouble(Visit(context.expression(1)) ?? 0);
            
            if (context.op.Text == "*")
                return left * right;
            
            if (right == 0)
                throw new DivideByZeroException("Division by zero");
            
            return left / right;
        }

        public override object? VisitCompareExpr(ETSLParser.CompareExprContext context)
        {
            var left = Visit(context.expression(0));
            var right = Visit(context.expression(1));
            string op = context.op.Text;

            if (left is string || right is string)
            {
                string leftStr = left?.ToString() ?? "";
                string rightStr = right?.ToString() ?? "";
                return op switch
                {
                    "==" => leftStr == rightStr,
                    "!=" => leftStr != rightStr,
                    _ => false
                };
            }
            
            try
            {
                double leftNum = Convert.ToDouble(left ?? 0);
                double rightNum = Convert.ToDouble(right ?? 0);
                
                return op switch
                {
                    "==" => leftNum == rightNum,
                    "!=" => leftNum != rightNum,
                    ">" => leftNum > rightNum,
                    "<" => leftNum < rightNum,
                    ">=" => leftNum >= rightNum,
                    "<=" => leftNum <= rightNum,
                    _ => false
                };
            }
            catch
            {
                return false;
            }
        }

        public override object? VisitCallExpr(ETSLParser.CallExprContext context) 
        {
            string name = context.ID().GetText();
            
            if (!_functions.TryGetValue(name, out var funcCtx))
            {
                throw new Exception($"Function not defined: {name}");
            }

            var args = new List<object?>();
            if (context.argList() != null)
            {
                foreach (var expr in context.argList().expression())
                {
                    args.Add(Visit(expr));
                }
            }

            var funcScope = new Dictionary<string, object>();
            var parameters = funcCtx.paramList()?.param();

            if (parameters != null)
            {
                for (int i = 0; i < parameters.Length && i < args.Count; i++)
                {
                    string paramName = parameters[i].ID().GetText();
                    funcScope[paramName] = args[i];
                }
            }

            _scopes.Push(funcScope);
            object? result = null;
            try
            {
                Visit(funcCtx.block());
            }
            catch (ReturnSignal sig)
            {
                result = sig.Value;
            }
            finally
            {
                _scopes.Pop();
            }
            return result;
        }

        public override object? VisitIntExpr(ETSLParser.IntExprContext context) 
            => int.Parse(context.INT().GetText());
        
        public override object? VisitBooleanExpr(ETSLParser.BooleanExprContext context) 
            => bool.Parse(context.BOOLEAN().GetText());
        
        public override object? VisitStringExpr(ETSLParser.StringExprContext context) 
            => context.STRING().GetText().Trim('"');
        
        public override object? VisitParenExpr(ETSLParser.ParenExprContext context) 
            => Visit(context.expression());
        
        public override object? VisitFloatExpr(ETSLParser.FloatExprContext context) 
            => double.Parse(context.FLOAT().GetText());
    }

    public class ReturnSignal : Exception 
    {
        public object? Value { get; }
        public ReturnSignal(object? value) => Value = value;
    }
}