using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.X509Certificates;

namespace HULK
{
    //Declaracion de los tipos de variables
    public enum Types { Void, Dynamic, number, boolean, hstring }

    //Comprobando que se le asignen los tipos de elementos correctos a las variables
    public class Property : Expression
    {
        public List<CastedVariablesTreatment> Variables { get; protected set; }
        public Expression ValueDeclaration { get; protected set; }
        public Property(List<CastedVariablesTreatment> Var, Expression valueDeclaration)
        {
            Variables = Var;
            SecurityCheck(valueDeclaration);
            ValueDeclaration = valueDeclaration;
        }

        private void SecurityCheck(Expression valueDeclaration)
        {
            Types type = default;
            var value = valueDeclaration.GetValue(false);
            if (value is double)
            {
                type = Types.number;
            }
            else if (value is string)
            {
                type = Types.hstring;
            }
            else if (value is null)
            {
                type = Types.Dynamic;
            }
            else if (value is bool)
            {
                type = Types.boolean;
            }
            else if (value is VoidReturn)
            {
                type = Types.Void;
            }

            foreach (CastedVariablesTreatment finalval in Variables)
            {
                if (finalval.Type != type)
                {
                    throw new TypicalError($"You can't assign a value of one type to a variable of a different type");
                }
            }
        }

        public override object GetValue(bool run)
        {
            if (run)
            {
                AssignValue();
            }

            return ValueDeclaration.GetValue(false);
        }

        public void AssignValue()
        {
            var value = ValueDeclaration.GetValue(false);
            foreach (CastedVariablesTreatment finalval in Variables)
            {
                finalval.DeclaredValue = value;
                finalval.Treatment = CastedVariablesTreatment.ValueTreatment.TreatedVariable;
            }
        }

    }

    //Llamado de las funciones
    public class MainCall : Expression
    {
        public string Id { get; protected set; }
        public FunctionDeclaration Description { get; protected set; }
        public List<Expression> Essentials { get; protected set; }

        public MainCall(string id, List<Expression> essentials, FunctionDeclaration description)
        {
            Id = id;
            Description = description;
            EssentialsCheck(essentials);
            Essentials = essentials;

            foreach (var ess in essentials)
            {
                if (ess.Required)
                {
                    Required = true;
                }
            }
        }


        private void EssentialsCheck(List<Expression> essentials)
        {
            foreach (var ess in essentials)
            {
                ess.GetValue(false);
            }
        }

        public override object GetValue(bool run)
        {
            try
            {
                return Description.Evaluate(Essentials, run);
            }
            catch (SemanticError ex)
            {
                throw new SemanticError($"'{Id}'cant recieve", ex.GivedInput);
            }
        }


    }

    public abstract class Expression
    {
        public abstract object GetValue(bool run);
        public bool Required { get; protected set; }
    }

    //Clasificacion de variables introducidas
    public class CastedVariablesTreatment : Expression
    {
        public object DeclaredValue { get; set; }
        public enum ValueTreatment { DeclaredValue, TreatedVariable, NonTreatedVariable, Args, Needs }
        public ValueTreatment Treatment { get; set; }
        public Types Type { get; protected set; }
        public string? Id { get; protected set; }

        //Strings, booleanos y numeros
        public CastedVariablesTreatment(object declaredValue)
        {
            DeclaredValue = declaredValue;
            Treatment = ValueTreatment.DeclaredValue;
            TypeCast();
        }

        //Variables sin tipo definido
        public CastedVariablesTreatment(string id, object declaredValue)
        {
            Id = id;
            DeclaredValue = declaredValue;
            Treatment = ValueTreatment.TreatedVariable;
            TypeCast();
        }

        //Variables con tipo definido
        public CastedVariablesTreatment(string id, object declaredValue, Types type, ValueTreatment treatment)
        {
            Id = id;
            Treatment = treatment;
            if (treatment == ValueTreatment.Needs || treatment == ValueTreatment.Args)
            {
                Required = true;
            }
            object checkvalue = declaredValue;
            bool TempVal = false;

            if (declaredValue is Expression expression)
            {
                checkvalue = expression.GetValue(false);
                if (checkvalue == null)
                {
                    if (declaredValue is Sum && (type == Types.number || type == Types.hstring))
                    {
                        TempVal = true;
                    }
                    else if (declaredValue is CastedVariablesTreatment)
                    {
                        TempVal = true;
                    }
                }
            }
            else if (checkvalue == null)
            {
                TempVal = true;
            }

            bool checkMathType = checkvalue is double && type == Types.number;
            bool checkboolean = checkvalue is bool && type == Types.boolean;
            bool checkstring = checkvalue is string && type == Types.hstring;

            if (checkMathType || checkboolean || checkstring || TempVal || type == Types.Dynamic)
            {
                DeclaredValue = declaredValue;
                Type = type;
            }
            else
            {
                throw new SemanticError($"{Id} cant recieve", (string)declaredValue);
            }
        }
        private void TypeCast()
        {
            if (DeclaredValue is double)
            {
                Type = Types.number;
            }
            else if (DeclaredValue is string)
            {
                Type = Types.hstring;
            }
            else if (DeclaredValue is bool)
            {
                Type = Types.boolean;
            }
        }

        public override object GetValue(bool run)
        {
            switch (Treatment)
            {
                case ValueTreatment.NonTreatedVariable:
                    {
                        throw new TypicalError("Variable not asigned");
                    }
                case ValueTreatment.Args:
                    {
                        return DeclaredValue;
                    }
            }

            if (Required)
            {
                return ((Expression)DeclaredValue).GetValue(run);
            }
            return DeclaredValue;
        }
    }

    public static class DefiningObjects
    {
        public static string TypeCast(this Object arg)
        {
            var type = arg.GetType();
            if (type == typeof(double))
            {
                return "number";
            }
            else if (type == typeof(bool))
            {
                return "boolean";
            }
            else if (type == typeof(string))
            {
                return "string";
            }
            else if (type == typeof(VoidReturn))
            {
                return "Void";
            }
            else return "type";
        }
    }

    public class VoidReturn
    {
        public VoidReturn()
        { }
    }

    //Retorno final
    public delegate void Print(object input);
    public class FinalReturn : Expression
    {
        public Expression Final { get; }
        Print PrintFunc;
        public FinalReturn(Expression final, Print printfunc)
        {
            Final = final;
            PrintFunc = printfunc;
        }

        public override object GetValue(bool run)
        {
            if (run)
            {
                PrintFunc(Final.GetValue(run));
            }
            return Final.GetValue(false);
        }
    }



}