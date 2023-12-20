using System.Linq.Expressions;
using System.Reflection.Metadata;
using System.Globalization;
using System.Data;

namespace HULK
{
    //Declaracion de la creacion y comprobacion de variables
    public class Variable : Expression
    {
        public Types Type {get; private set;}
        public object Value {get=>ValueExpression?.GetValue(false); set {}}
        public Expression ValueExpression {get; private set;}
        public List<string> SavedDocs {get;}

        public Variable(List<string> saveddocs, string type, Expression ValExpression)
        {
            SavedDocs = saveddocs;
            TypeCast(type);
            bool checkingvalue = ValExpression == null || CheckedValue(ValExpression);
            if(!checkingvalue)
            {
                var value = ValExpression.GetValue(false);
                string catched;
                if(value == null && ValueExpression is Sum)
                {
                    catched = "number ' nor ' string";
                }
                else
                {
                    catched = value.TypeCast();
                }
                throw new SemanticError("Variable", catched);
            }
            ValueExpression = ValExpression;
        }

        public Variable (List<String> saveddocs, Expression ValExpression )
        {
            SavedDocs=saveddocs;
            TypeCast(ValExpression.GetValue(false));
            ValueExpression = ValExpression;
        }

        private bool CheckedValue(Expression ValueExpression)
        {
            var value = ValueExpression.GetValue(false);
            bool tempvar = false;
            if(value == null)
            {
                if(ValueExpression is Sum && (Type == Types.number) || (Type == Types.hstring))
                {
                    tempvar = true;
                }
                else if(ValueExpression is CastedVariablesTreatment)
                {
                    tempvar = true;
                }
            }
            bool checkedMathValue = (value is double) && (Type == Types.number);
            bool checkedBool = (value is bool) && (Type == Types.boolean);
            bool checkedString = (value is string) && (Type == Types.hstring);

            if(checkedMathValue || checkedBool || checkedString)
            {
                return true;
            }
            return false;
        }

        private void TypeCast (string type)
        {
            Type = type
            switch
            {
                "number" => Types.number, "string" => Types.hstring, "boolean" => Types.boolean, _=>throw new Exception(),
            };
        }

        private void TypeCast(object value)
        {
            if(value is double)
            {
                Type = Types.number;
            }
            else if(value is string)
            {
                Type=Types.hstring;
            }
            else if(value is bool)
            {
                Type=Types.boolean;
            }
            else if(value == null)
            {
                Type = Types.Dynamic;
            }
            else
            {
                throw new Exception();
            }
        }


        public override object GetValue(bool run)
        {
            return new VoidReturn();
        }
    }

    //Declaracion de la creacion y comprobacion de funciones
    public class FunctionDeclaration : Expression
    {
        public Dictionary<string, CastedVariablesTreatment> Index {get; private set;}
        public List<string> IndexId {get;}
        public Expression? Description {get; private set;}
        public string FunctionId {get; private set;}

        public FunctionDeclaration(string functionId, List<string> indexId)
        {
            Index = new Dictionary<string, CastedVariablesTreatment>();
            FunctionId = functionId;
            IndexId = indexId;
            CheckArgs(IndexId);
        }

        private void CheckArgs(List<string> indexids)
        {
            foreach(string ind in indexids)
            {
                object val = default;
                Index.Add(ind, new CastedVariablesTreatment(ind, val, Types.Dynamic, CastedVariablesTreatment.ValueTreatment.Args));
            }
        }

        int stackid=0;
        public object Evaluate(List<Expression> Args, bool run)
        {
            if(Args.Count != Index.Count)
            {
                throw new SemanticError($"Function '{FunctionId}'",$"{Args.Count} argument");
            }
            else
            {
                if(stackid > Internal.StackCup)
                {
                    throw new OverflowError();
                }
                else
                {
                    stackid++;
                }

                List<object> PreviousValue = new();

                for(int i = 0;i<Args.Count;i++)
                {
                    string key=IndexId[i];
                    var x = Index[key];
                    PreviousValue.Add(x.DeclaredValue);
                    x.DeclaredValue = Args[i].GetValue(false);
                }

                var result = Description.GetValue(run);
                stackid--;

                for(int i=0;i<PreviousValue.Count;i++)
                {
                    string key = IndexId[i];
                    var x = Index[key];
                    x.DeclaredValue = PreviousValue[i];
                }
                return result;
            }
        }

        public void Define(Expression des)
        {
            Description=des;
            CheckDes();
        }

        public void CheckDes()
        {
            try
            {
                Description.GetValue(false);
            }
            catch(SemanticError ex)
            {
                throw ex;
            }
            catch(OverflowError)
            {
                throw new TypicalError($"Function '{FunctionId}' call stack limit");

            }
        }
          
        //Guardar las funciones creadas y comprobarlas
        public void History(SavedHistory Saved )
        {
            Saved.AddNewFunction(this.FunctionId, this);
        }

        public override object GetValue(bool run)
        {
            return new VoidReturn();
        }

    }

    //Declaracion de todos los comandos de uso comun en el compilador
    public static class Internal
    {
        public static string[] MainUse = { "let", "in", "else", "if", "function", "+", "-", "*", "/", "&&", "&", "|", "||", "(", ")", "=", ",","=>"};

        public static bool CheckedName(string name)
        {
            foreach(char c in name)
            {
                if(!Char.IsLetterOrDigit(c))
                {
                    return false;
                }
                
            }
            if(Internal.MainUse.Contains(name))
            {
                return false;
            }
            if(double.TryParse(name, out _))
            {
                return false;
            }
            if(Char.IsDigit(name[0]))
            {
                return false;
            }
            return true;
        }
        public const int StackCup = 1000;
    }

    public class SavedHistory
    {
        public Dictionary<string, FunctionDeclaration> Saved {get;}
        public SavedHistory()
        {
            Saved = new Dictionary<string, FunctionDeclaration>();
        }

        public void AddNewFunction(string key, FunctionDeclaration value)
        {
            if(!Saved.TryAdd(key,value))
            {
                throw new TypicalError($"{key} already saved");
            }
        }
    }
}