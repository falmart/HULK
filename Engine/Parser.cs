using System.Diagnostics.Contracts;
using System.Linq.Expressions;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using System.Diagnostics;
using System.Xml.XPath;
using System.Security.Cryptography.X509Certificates;
using System.Reflection.Metadata.Ecma335;

namespace HULK
{
    //Clase fundamental del compilador
    public class HulkParser
    {
        Stack<Expression> Parsing;
        Print Ret;
        public SavedHistory Save { get; }

        public HulkParser(SavedHistory save, Print ret)
        {
            Save = save;
            Parsing = new Stack<Expression>();
            Ret = ret;
        }

        public Expression Parse(string[] savedToken)
        {
            try
            {
                var val = MainParse(savedToken, 0, savedToken.Length - 1);
                return val;
            }
            catch (Exception)
            {
                Parsing.Clear();
                throw;
            }
        }

        //Ordenes generales de parsing
        private Expression MainParse(string[] savedToken, int start, int stop)
        {
            if (savedToken.Length == 0)
            {
                return null;
            }
            Expression expresion = null;
            if (savedToken[start] == "function")
            {
                expresion = ParseFunctionDeclaration(savedToken, start, stop);
            }
            else if (savedToken[start] == "number" || savedToken[start] == "boolean" || savedToken[start] == "string")
            {
                expresion = ParseVariableDeclaration(savedToken, start, stop);
            }
            if (expresion is Variable)
            {
                throw new TypicalError("Use let-in to declare variables");
            }
            expresion ??= InnerParse(savedToken, start, stop);
            if (expresion == null)
            {
                throw new TypicalError("Invalid Input");
            }
            return expresion;
        }

        //Llamdo interno de de parsing segun los token a procesar
        private Expression InnerParse(string[] savedToken, int start, int stop)
        {
            if (savedToken.Length == 0)
            {
                return null;
            }
            Expression expression = null;
            if (savedToken[start] == "let")
            {
                expression = ParseLetInExpression(savedToken, start, stop);
            }
            else if (savedToken[start] == "if")
            {
                expression = ParseIfElseState(savedToken, start, stop);
            }

            expression ??=TryEqual(savedToken,start,stop);
            expression ??= TrySumOrSub(savedToken, start, stop);
            expression ??= TryProdDivMod(savedToken, start, stop);
            expression ??= Unary(savedToken, start, stop);
            expression ??= ParsePower(savedToken, start, stop);
            expression ??= ParseRelation(savedToken, start, stop);
            expression ??=TryConc(savedToken,start,stop);
            expression ??= Inicialization(savedToken, start, stop);


            if (expression == null)
            {
                throw new TypicalError("Invalid Input");
            }
            return expression;
        }

        //Instanciar las funciones simples
        private Expression SampleFunctionCall(string[] savedToken, int start, int stop, int tempval, Type type)
        {
            Expression expression = start != stop ? InnerParse(savedToken, start + 1, stop) :
            throw new SyntaxError("leftIndex", $"{savedToken[tempval]}");

            object[] exp = new object[] { expression };
            return (Expression)Activator.CreateInstance(type, exp);
        }

        //Instanciar las funciones binarias
        private Expression CompoundFunctionCall(string[] savedToken, int start, int stop, int tempval, Type type)
        {
            Expression LeftIndex = tempval != start ? InnerParse(savedToken, start, tempval - 1) :
            throw new SyntaxError("LeftIndex", $"{savedToken[tempval]}");

            Expression RightIndex = tempval != stop ? InnerParse(savedToken, tempval + 1, stop) :
            throw new SyntaxError("RightIndex", $"{savedToken[tempval]}");

            object[] exp = new object[] { LeftIndex, RightIndex };
            return (Expression)Activator.CreateInstance(type, exp);
        }

        private Expression Unary(string[] savedToken, int start, int stop)
        {
            switch (savedToken[start])
            {
                case "(":
                    {
                        int i = Tokenizer.OpenBalance(start, stop, savedToken);
                        if (i != stop)
                        {
                            return null;
                        }
                        else
                        {
                            return start != stop - 1 ? InnerParse(savedToken, start + 1, stop - 1) :
                            throw new SyntaxError(")", "expression");
                        }
                    }
                case "!":
                    {
                        return SampleFunctionCall(savedToken, start, stop, stop, typeof(LogicNegation));
                    }
                case "+":
                    {
                        return SampleFunctionCall(savedToken, start, stop, start, typeof(Positive));
                    }
                case "-":
                    {
                        return SampleFunctionCall(savedToken, start, stop, start, typeof(Negative));
                    }
            }
            return null;
        }

        //Instrucciones para llamado correcto de funciones
        private Expression FunctionCall(string[] savedToken, int start, int stop, Type type)
        {
            if (savedToken[start + 1] != "(" || savedToken[stop] != ")")
            {
                throw new SyntaxError("parenthesis", "function arguments");
            }
            else
            {
                List<Expression> Arguments = CommaExpressions(savedToken, start + 2, stop - 1);
                if (type == typeof(FinalReturn))
                {
                    List<object> print = new List<object>(Arguments);
                    print.Add(Ret);
                    object[] printretun = print.ToArray();
                    return (Expression)Activator.CreateInstance(type, printretun);
                }

                object[] arguments = Arguments.ToArray();
                return (Expression)Activator.CreateInstance(type, arguments);
            }
        }

        private Expression TryFunctionCall(string[] savedTokens, int start, int stop)
        {
            Expression result = null;
            if (savedTokens[start + 1] == "(")
            {
                if (savedTokens[stop] != ")")
                {
                    throw new SyntaxError(")", "function call");
                }
                FunctionDeclaration Definition;
                if (Save.Saved.ContainsKey(savedTokens[start]))
                {
                    Definition = Save.Saved[savedTokens[start]];
                }
                else
                {
                    try
                    {
                        var SavedExpressions = Parsing.ToList();
                        FunctionDeclaration main = SavedExpressions[^1] as FunctionDeclaration;
                        if (savedTokens[start] == main.FunctionId)
                        {
                            Definition = main;
                        }
                        else
                        {
                            throw new TypicalError($"function not found");
                        }
                    }
                    catch
                    {
                        throw new TypicalError($"missing function");
                    }
                }

                string name = savedTokens[start];
                var Args = CommaExpressions(savedTokens, start + 2, stop - 1);
                result = new MainCall(name, Args, Definition);
            }
            return result;
        }

        //Parsear funciones separadas por coma
        private List<Expression> CommaExpressions(string[] savedToken, int start, int stop)
        {
            List<Expression> result = new();
            if (savedToken[start] == "," || savedToken[stop] == ",")
            {
                throw new TypicalError("Misuse of comma");
            }
            int startCount = start;
            for (int i = start; i <= stop; i++)
            {
                if (savedToken[i] == "(")
                {
                    i = Tokenizer.OpenBalance(i, stop, savedToken);
                }
                if (savedToken[i] == ",")
                {
                    var expresions = InnerParse(savedToken, startCount, i - 1);
                    result.Add(expresions);
                    startCount = i + 1;
                }
                else if (i == stop)
                {
                    var expresions = InnerParse(savedToken, startCount, i);
                    result.Add(expresions);
                }
            }
            return result;
        }

        private List<Expression> CommaDeclarations(string[] savedToken, int start, int stop)
        {
            List<Expression> result = new();
            if (savedToken[start] == "," || savedToken[stop] == ",")
            {
                throw new TypicalError("Misuse of comma");
            }

            int startCount = start;
            for (int i = start; i <= stop; i++)
            {
                if (savedToken[i] == "(")
                {
                    i = Tokenizer.OpenBalance(i, stop, savedToken);
                }

                if (savedToken[i] == ",")
                {
                    var expresion = ParseLetIn(savedToken, startCount, i - 1);
                    if (expresion == null)
                    {
                        throw new SemanticError("Let-in", expresion.GetType().Name);
                    }
                    result.Add(expresion);
                    startCount = i + 1;
                }

                else if (i == stop)
                {
                    var expresion = ParseLetIn(savedToken, startCount, i);
                    if (expresion == null || expresion is not Variable)
                    {
                        throw new SemanticError("Let-in", expresion.GetType().Name);
                    }
                    result.Add(expresion);
                }
            }
            return result;
        }

        private Expression ParseVariableDeclaration(string[] savedToken, int start, int stop)
        {
            Expression result;
            if (savedToken[start] != "number" && savedToken[start] != "boolean" && savedToken[start] != "string")
            {
                throw new LexicalError(savedToken[start], "var type");
            }
            string type = savedToken[start];
            int Dec = Tokenizer.GetName(savedToken, start + 1, stop, "=");
            if (savedToken[start + 1] == "=")
            {
                throw new SyntaxError("var name", "var declaration");
            }
            List<string> names;
            try
            {
                names = Tokenizer.CheckingComma(Dec, start + 1, savedToken);
            }
            catch
            {
                string invalid = "";
                for (int i = start + 1; i <= Dec; i++)
                {
                    invalid += savedToken[i] + ", ";
                }
                throw new LexicalError(invalid, "declaration");
            }
            foreach (string name in names)
            {
                bool check = Internal.CheckedName(name);
                if (!check)
                {
                    throw new LexicalError(name, "var name");
                }
            }

            Expression Value = null;
            if (Dec < stop - 1)
            {
                Value = InnerParse(savedToken, Dec + 2, stop);
            }
            else if (Dec == stop - 1 || Dec > stop - 1)
            {
                throw new SyntaxError("val expression", "var declaration");
            }
            result = new Variable(names, type, Value);
            return result;
        }

        private Expression ParseLetIn(string[] savedToken, int start, int stop)
        {
            Variable result;
            string type = null;
            string name;

            int Dec;
            name = savedToken[start];
            Dec = Tokenizer.GetName(savedToken, start, stop, "=");
            
            List<string> VarName = new();

            bool check = Internal.CheckedName(name);

            if (!check)
            {
                throw new LexicalError(name, "var name");
            }

            VarName.Add(name);

            Expression Exp = null;
            if (Dec < stop - 1)
            {
                Exp = InnerParse(savedToken, Dec + 2, stop);
            }
            else if (Dec >= stop - 1)
            {
                throw new SyntaxError("val expression", "var declaration");
            }
            if (type == null)
            {
                return new Variable(VarName, Exp);
            }
            return new Variable(VarName, type, Exp);

        }

        private Expression TryConc(string[] tokens, int start, int end)
        {
            for (int i = end; i >= start; i--)
            {
                switch (tokens[i])
                {
                    case ")":
                    {
                        i = Tokenizer.ClosedBalance(i, start, tokens);
                        break;
                    }
                    case "@":
                    {
                        return i == start ? null : CompoundFunctionCall(tokens, start, end, i, typeof(Conc));
                    }
                }
            }
            return null;
        }

        private Expression ParseLetInExpression(string[] savedToken, int start, int stop)
        {
            LetInState result;
            if (savedToken[start] != "let")
            {
                return null;
            }
            int Dec = Tokenizer.GetName(savedToken, start, stop, "in");
            if (Dec >= stop - 1)
            {
                throw new SyntaxError("structure", "let-in");
            }
            List<Expression> Args = CommaDeclarations(savedToken, start + 1, Dec);
            Dictionary<string, CastedVariablesTreatment> Variables = new();
            foreach (Expression arg in Args)
            {
                if (arg is not Variable)
                {
                    throw new SemanticError("let-in", arg.GetType().Name);
                }
                var Vars = arg as Variable;
                foreach (string name in Vars.SavedDocs)
                {
                    if (Vars.ValueExpression is null)
                    {
                        throw new SyntaxError("value", "let-in argument");
                    }
                    CastedVariablesTreatment LetVariable;
                    if (Vars.ValueExpression.Required)
                    {
                        LetVariable = new CastedVariablesTreatment(name, Vars.ValueExpression, Vars.Type, CastedVariablesTreatment.ValueTreatment.Needs);
                    }
                    else
                    {
                        LetVariable = new CastedVariablesTreatment(name, Vars.ValueExpression.GetValue(false));
                    }
                    Variables.Add(name, LetVariable);
                }
            }
            result = new LetInState(Variables);
            Parsing.Push(result);
            Expression Def = InnerParse(savedToken, Dec + 2, stop);
            Parsing.Pop();
            result.Define(Def);
            return result;
        }

        private string CleanStrings(string sampled)
        {
            sampled = sampled.Replace("\\a", "\a");
            sampled = sampled.Replace("\\b", "\b");
            sampled = sampled.Replace("\\f", "\f");
            sampled = sampled.Replace("\\n", "\n");
            sampled = sampled.Replace("\\r", "\r");
            sampled = sampled.Replace("\\t", "\t");
            sampled = sampled.Replace("\\v", "\v");
            sampled = sampled.Replace("\\", "");
            sampled = sampled[1..^1];
            return sampled;
        }

        public Expression ParseFunctionDeclaration(string[] savedToken, int start, int stop)
        {
            int Dec = Tokenizer.GetName(savedToken, start, stop, "=>");
            if (savedToken[start] != "function")
            {
                throw new LexicalError(savedToken[start], "function declaration");
            }
            if (savedToken[start + 1] == "=>")
            {
                throw new SyntaxError("function name", "function declaration");
            }
            FunctionDeclaration result;
            if (Dec >= stop - 1)
            {
                throw new SyntaxError("function structure", "function declaration");
            }
            else
            {
                string FunctionName = savedToken[start + 1];
                if (!Internal.CheckedName(FunctionName))
                {
                    throw new LexicalError(FunctionName, "function name");
                }
                if (savedToken[start + 2] != "(")
                {
                    throw new SyntaxError("(", "function declaration");
                }
                if (savedToken[Dec] != ")")
                {
                    throw new SyntaxError(")", "argument");
                }

                List<string> Arguments;
                try
                {
                    Arguments = Tokenizer.CheckingComma(Dec - 1, start + 3, savedToken);
                }
                catch
                {
                    string invalid = "";
                    for (int i = start + 3; i <= Dec - 1; i++)
                    {
                        invalid += savedToken[i] + ", ";
                    }
                    throw new LexicalError(invalid, "arguments");
                }

                foreach (string arg in Arguments)
                {
                    if (!Internal.CheckedName(arg))
                    {
                        throw new LexicalError(arg, "var name");
                    }
                }

                result = new FunctionDeclaration(FunctionName, Arguments);
                Parsing.Push(result);
                Expression Definition = InnerParse(savedToken, Dec + 2, stop);
                Parsing.Pop();
                result.Define(Definition);
            }
            return result;
        }

        private Expression Inicialization(string[] savedToken, int start, int stop)
        {
            if (start == stop)
            {

                if (savedToken[start] == "true" || savedToken[start] == "false")
                    return new CastedVariablesTreatment(bool.Parse(savedToken[start]));
                if (double.TryParse(savedToken[start], NumberStyles.Any, new CultureInfo("en-US"), out double maybeNum))
                    return new CastedVariablesTreatment(maybeNum);
                if (Regex.Match(savedToken[start], @"\u0022(.)*\u0022").Success)
                {
                    string arg = CleanStrings(savedToken[start]);
                    return new CastedVariablesTreatment(arg);
                }
                return TryVariable(savedToken[start]);
            }


            switch (savedToken[start])
            {

                case "(":
                    {
                        return start != stop - 1 ? InnerParse(savedToken, start + 1, stop - 1) :
                        throw new SyntaxError(")", "expression");
                    }
                case "print":
                    {
                        return FunctionCall(savedToken, start, stop, typeof(FinalReturn));
                    }
                case "sin":
                    {
                        return FunctionCall(savedToken, start, stop, typeof(Sine));
                    }
                case "cos":
                    {
                        return FunctionCall(savedToken, start, stop, typeof(Cosine));
                    }
                case "log":
                    {
                        return FunctionCall(savedToken, start, stop, typeof(Log));
                    }
                default:
                    {
                        return TryFunctionCall(savedToken, start, stop);
                    }
            }

        }

        private Expression TryVariable(string varid)
        {
            switch (varid)
            {
                case "PI":
                    {
                        return new CastedVariablesTreatment(Math.PI);
                    }
                case "E":
                    {
                        return new CastedVariablesTreatment(Math.E);
                    }
            }
            Stack<Expression> PosibleLocations = new(new Stack<Expression>(Parsing));
            Dictionary<string, CastedVariablesTreatment> Location = new();
            while (PosibleLocations.TryPop(out Expression exp))
            {
                if (exp is FunctionDeclaration Dec)
                    Location = Dec.Index;
                else if (exp is LetInState Let)
                    Location = Let.SavedVariables;

                if (Location.ContainsKey(varid))
                    return Location[varid];
            }
            if (Location.ContainsKey(varid))
                return Location[varid];
            else
                throw new TypicalError($"variable {varid} not found");
        }

        private Expression TrySumOrSub(string[] savedToken, int start, int stop)
        {
            for (int i = stop; i >= start; i--)
            {
                switch (savedToken[i])
                {
                    case ")":
                        {
                            i = Tokenizer.ClosedBalance(i, start, savedToken);
                            break;
                        }
                    case "+":
                        {
                            return i == start ? null : CompoundFunctionCall(savedToken, start, stop, i, typeof(Sum));
                        }
                    case "-":
                        {
                            return i == start ? null : CompoundFunctionCall(savedToken, start, stop, i, typeof(Sub));
                        }
                }
            }
            return null;
        }

        private Expression TryProdDivMod(string[] savedToken, int start, int stop)
        {
            for (int i = stop; i >= start; i--)
            {
                switch (savedToken[i])
                {
                    case ")":
                        {
                            i = Tokenizer.ClosedBalance(i, start, savedToken);
                            break;
                        }
                    case "*":
                        {
                            return CompoundFunctionCall(savedToken, start, stop, i, typeof(Multiplication));
                        }
                    case "/":
                        {
                            return CompoundFunctionCall(savedToken, start, stop, i, typeof(Division));
                        }
                    case "%":
                        {
                            return CompoundFunctionCall(savedToken, start, stop, i, typeof(Module));
                        }
                }
            }
            return null;
        }

        private Expression ParsePower(string[] savedTokem, int start, int stop)
        {
            for (int i = start; i <= stop; i++)
            {
                switch (savedTokem[i])
                {
                    case "(":
                        {
                            i = Tokenizer.OpenBalance(i, stop, savedTokem);
                            break;
                        }
                    case "^":
                        {
                            return CompoundFunctionCall(savedTokem, start, stop, i, typeof(Power));
                        }
                }
            }
            return null;
        }

        private Expression ParseRelation(string[] savedToken, int start, int stop)
        {
            for (int i = stop; i >= start; i--)
            {
                switch (savedToken[i])
                {
                    case ")":
                        {
                            i = Tokenizer.ClosedBalance(i, start, savedToken);
                            break;
                        }
                    case "<":
                        {
                            return CompoundFunctionCall(savedToken, start, stop, i, typeof(Lower));
                        }
                    case "<=":
                        {
                            return CompoundFunctionCall(savedToken, start, stop, i, typeof(LowerEqual));
                        }
                    case ">":
                        {
                            return CompoundFunctionCall(savedToken, start, stop, i, typeof(Greater));
                        }
                    case ">=":
                        {
                            return CompoundFunctionCall(savedToken, start, stop, i, typeof(GreaterEqual));
                        }
                }
            }
            return null;
        }

        private Expression TryEqual(string[] savedToken, int start, int stop)
        {
            for(int i = stop; i>=start; i--)
            {
                switch(savedToken[i])
                {
                    case ")":
                    {
                        i=Tokenizer.ClosedBalance(i,start,savedToken);
                        break;
                    }
                    case "==":
                    {
                        return CompoundFunctionCall(savedToken,start,stop,i,typeof(Equal));
                    }
                    case "!=":
                    {
                        return CompoundFunctionCall(savedToken,start,stop,i,typeof(Unequal));
                    }
                }
            }
            return null;
        }

        private Expression ParseIfElseState(string[] savedToken, int start, int stop)
        {
            Expression result;
            if (savedToken[start] != "if")
            {
                throw new LexicalError(savedToken[start]);
            }
            else
            {
                int End;
                if (savedToken[start + 1] != "(")
                {
                    throw new SyntaxError("(", "if-else condition");
                }
                else
                {
                    End = Tokenizer.OpenBalance(start + 1, stop, savedToken);
                    if (End == stop)
                    {
                        throw new SyntaxError("if order", "if-else expression");
                    }
                    Expression condition = InnerParse(savedToken, start + 1, End);
                    int ifcount = Tokenizer.GetName(savedToken, start, stop, "else");
                    if (ifcount == stop - 1)
                    {
                        throw new SyntaxError("else order", "if-else expression");
                    }

                    Expression Do = InnerParse(savedToken, End + 1, ifcount);
                    Expression ElseDo = null;
                    if (ifcount < stop - 1)
                    {
                        ElseDo = InnerParse(savedToken, ifcount + 2, stop);
                    }
                    else
                    {
                        throw new SyntaxError("else expression", "if-else state");
                    }
                    result = new IfElseState(condition, Do, ElseDo);
                }
            }
            return result;
        }

        //Declaracion del If-Else como funcion
        public class IfElseState : Expression
        {
            public Expression Condition { get; protected set; }
            public Expression IfExpression { get; protected set; }
            public Expression ElseExpression { get; protected set; }

            public IfElseState(Expression Cond, Expression IfExp, Expression ElseExp)
            {
                if (Cond.Required || IfExp.Required || ElseExp.Required)
                    Required = true;
                Condition = Cond;
                IfExpression = IfExp;
                ElseExpression = ElseExp;

            }

            public override object GetValue(bool execute)
            {
                if (!execute)
                {
                    return CheckValue();
                }
                return Result(Condition, IfExpression, ElseExpression, execute);
            }
            private object CheckValue()
            {
                if (Condition is Variable && Condition.GetValue(false) == null)
                {
                    var ifValue = IfExpression.GetValue(false);
                    var elseValue = ElseExpression.GetValue(false);
                    return ifValue;
                }
                else
                {
                    return Result(Condition, IfExpression, ElseExpression, false);
                }
            }

            private object Result(Expression Cond, Expression IfExp, Expression ElseExp, bool execute)
            {
                if (Cond.GetValue(execute) is not bool)
                    throw new SemanticError("if-else condition", "introduced type");
                else
                {
                    var condition = (bool)Cond.GetValue(execute);
                    if (condition)
                        return IfExp.GetValue(execute);
                    if (IfExp != null)
                        if (ElseExp == null)
                            return null;
                    return ElseExp.GetValue(execute);
                }
            }
        }
 
        //Declaracion del Let-in como funcion
        public class LetInState : Expression
        {
            public Dictionary<string, CastedVariablesTreatment> SavedVariables { get; private set; }
            public Expression Body { get; private set; }
            public LetInState(Dictionary<string, CastedVariablesTreatment> Variables)
            {
                SavedVariables = Variables;
            }

            public override object GetValue(bool execute)
            {
                CheckValues();
                return Body.GetValue(execute);
            }
            private void CheckValues()
            {
                foreach (var V in SavedVariables.Values)
                {
                    bool isOK = false;
                    object val = null;
                    if (V.Required)
                    {
                        var Exp = V.DeclaredValue as Expression;
                        val = Exp.GetValue(false);
                        if (val != null)
                        {
                            if (V.Type == Types.boolean && val is bool)
                                isOK = true;
                            else if (V.Type == Types.hstring && val is string)
                                isOK = true;
                            else if (V.Type == Types.number && val is double)
                                isOK = true;
                            else if (V.Type == Types.Dynamic)
                                isOK = true;
                        }
                        else
                            isOK = true;
                    }
                    else
                        isOK = true;
                    if (!isOK)
                    {
                        string expectedType = "";
                        switch (V.Type)
                        {
                            case Types.number:
                                expectedType = "number";
                                break;
                            case Types.boolean:
                                expectedType = "boolean";
                                break;
                            case Types.hstring:
                                expectedType = "string";
                                break;
                        }
                        throw new SemanticError("Let-in expression", "gived type");
                    }
                }
            }
            public void Define(Expression Definition)
            {
                Body = Definition;
            }

        }

    }
}