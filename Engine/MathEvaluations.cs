using System.Linq.Expressions;
using System.Numerics;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.InteropServices;
using System.Security.Cryptography.X509Certificates;
using Microsoft.CSharp.RuntimeBinder;

namespace HULK
{
    public abstract class SampleFunctions : Expression
    {
        //Evaluacion de operaciones Matematicas simples
        public abstract object Evaluate(object index);
        public Expression Argument {get; protected set;}
        public SampleFunctions(Expression Index)
        {
            if(Index.Required)
            {
                Required = true;                        
            }        
            Argument = Index;    
        }
        public override object GetValue(bool run)
        {
            return Evaluate (Argument.GetValue(run));
        }

    }

    //Funciones Matematicas

    //Evaluacion de la funcion seno
    public class Sine : SampleFunctions
    {
        public Sine(Expression index) : base(index)
        {}

        public override object Evaluate(object index)
        {
            if(index is double x)
            {
                return Math.Sin(x);
            }
            if(index==null)
            {
                return 5d;
            }
            throw new SemanticError("Sin" , index.TypeCast());
        }

    }

    //Evalucion de la funcion coseno
    public class Cosine : SampleFunctions
    {
        public Cosine(Expression index) : base(index)
        {}

        public override object Evaluate(object index)
        {
            if(index is double x)
            {
            return Math.Cos(x);
            }
            if(index==null)
            {
                return 5d;
            }
            throw new SemanticError("Cosine" , index.TypeCast());
        }
    }

    //Implementacion de la funcion raiz cuadrada
    public class SquareRoot : SampleFunctions
    {
    
        public SquareRoot(Expression index) : base(index)
        {}
        public override object Evaluate(object index)
        {
            if(index is double x)
            {
                return Math.Sqrt(x);
            }
            if(index==null)
            {
                return 5d;
            }
            throw new SemanticError("Squareroot", index.TypeCast());
        }
    }

    //Funcion booleana de negacion
    public class LogicNegation : SampleFunctions
    {
        public LogicNegation (Expression Index) : base(Index)
        {}
        public override object Evaluate(object index)
        {
            if(index is bool o)
            {
                return !o;
            }
            if(index==null)
            {
                return default(bool);
            }
            throw new SemanticError("!", index.TypeCast());
        }
    }

    //Funcion valor en positivo
    public class Positive : SampleFunctions
    {
        public Positive(Expression arg) : base(arg)
        {}
        public override object Evaluate(object index)
        {
            if(index is double x)
            {
                return x;
            }
            if(index==null)
            {
                return 5d;
            }
            throw new SemanticError("'+'",index.TypeCast());
        }
    }

    //Funcion valor en negativo
    public class Negative : SampleFunctions
    {
        public Negative(Expression arg) : base(arg)
        {}
        public override object Evaluate(object index)
        {
            if(index is double x)
            {
                return -x;
            }
            if(index==null)
            {
                return 5d;
            }
            throw new SemanticError("'-'",index.TypeCast());
        }
    }

    //Implementacion de una clase abstracta que procese las funciones binarias
    public abstract class CompoundFunction : Expression
    {
        public Expression LeftIndex {get; protected set;}
        public Expression RightIndex {get; protected set;}
        public abstract object Evaluate (object left, object right);
        public CompoundFunction(Expression leftIndex, Expression rightIndex)
        {
            if(leftIndex.Required || rightIndex.Required)
            {
                Required=true;
            }
                LeftIndex=leftIndex;
                RightIndex=rightIndex;           
        }
        public override object GetValue(bool run)
        {
            return Evaluate(LeftIndex.GetValue(run), RightIndex.GetValue(run));
        }

        protected bool IndexCheck(object left, object right, List<Type> PosibleTypes)
        {
            foreach (var type in PosibleTypes)
            {
                if(left ==null || right == null)
                {
                    if(left != null && right == null)
                    {
                        if(left.GetType() == type)
                        {
                            return true;
                        }
                    }
                    else if (left == null && right != null )
                    {
                        if(right.GetType() == type)
                        {
                            return true;
                        }
                    }

                return true;
                }

                if(left.GetType() == right.GetType() && left.GetType() == type)
                {
                    return true;
                }
            }
            return false;
        }

    }

    //Operacion suma
    public class Sum : CompoundFunction
    {
        public Sum (Expression leftIndex, Expression rightIndex) : base(leftIndex, rightIndex)
        {}

         public override object Evaluate(object left, object right)
        {
            if(left is null)
            {
                if((double)right != 0d)
                {
                    left=right;
                }
                {
                    left = 5d;
                }
            }
            if(right is null)
            {
                if((double)left != 0d)
                {
                    right = left;
                }
                else
                {
                    right=5d;
                }
            }

            if(left == null && right == null)
            {
                return default;
            }
            if((left is double && right is double) || (left is string && right is string))
            {
                return (dynamic)left + (dynamic)right;
            }
            throw new MathSemanticError("+", left.TypeCast(), right.TypeCast());
        }           
    }

    //Operacion resta
    public class Sub : CompoundFunction
    {
        public Sub (Expression leftIndex, Expression rightIndex) : base(leftIndex,rightIndex)
        {}
        public override object Evaluate(object left, object right)
        {
            if(left is null)
            {
                if((double)right != 0d)
                {
                    left=right;
                }
                else
                {
                    left=5d;
                }
            }

            if(right is null)
            {
                if((double)left != 0d)
                {
                    right=left;
                }
                else
                {
                    right=5d;
                }
            }
            if(left == null && right == null)
            {
                return 5d;
            }
            if(left is double && right is double)
            {
                return (dynamic)left - (dynamic)right;
            }
            var error = left is not double ? left.TypeCast() : right.TypeCast();
            throw new MathSemanticError("-", left.TypeCast(),right.TypeCast());
        }
    }
        
        //Operacion Multiplicacion
        public class Multiplication : CompoundFunction
        {
            public Multiplication(Expression leftIndex, Expression rightIndex) : base(leftIndex,rightIndex)
            {}
            public override object Evaluate(object left, object right)
            {
                left ??= right;
                right ??= left;
                if(left==null && right==null)
                {
                    return 5d;
                }
                if(left is double && right is double)
                {
                    return(dynamic)left*(dynamic)right;
                }
                var error = left is not double ? left.TypeCast() : right.TypeCast();
                throw new SemanticError("'+'", error);
            }
        }

        //Operacion division
        public class Division : CompoundFunction
        {
            public Division(Expression leftIndex,Expression rightIndex) : base(leftIndex,rightIndex)
            {}
        public override object Evaluate(object left, object right)
        {
            left ??= right;
            right ??= left;

            if(left==null && right==null)
            {
                return 5d;
            }
            if(left is double && right is double divisor)
            {
                if(divisor==0)
                {
                    throw new TypicalError("Cant divide by 0");
                }
                return (dynamic)left / (dynamic)right;
            }
            var error = left is not double ? left.TypeCast() : right.TypeCast();
            throw new SemanticError("'/'", error);
        }
        }

        //Operacion modulo
        public class Module : CompoundFunction
        {
            public Module(Expression leftIndex,Expression rightIndex) : base(leftIndex,rightIndex)
            {}
        public override object Evaluate(object left, object right)
        {
            left ??= right;
            right ??= left;

            if(left == null && right == null)
            {
                return 5d;
            }
            if(left is double && right is double divisor)
            {
                if(divisor==0)
                {
                    throw new TypicalError("Cant divide by 0");
                }
                return (dynamic)left % (dynamic)right;
            }
            var error = left is not double ? left.TypeCast() : right.TypeCast();
            throw new SemanticError("'%'",error);
        }
    }

    //Operacion potencia
    public class Power : CompoundFunction
    {
        public Power (Expression leftIndex, Expression rightIndex) : base(leftIndex,rightIndex)
        {}
        public override object Evaluate(object left, object right)
        {
            left ??= right;
            right ??= left;
            if(left==null && right==null)
            {
                return 5d;
            }
            if(left is double && right is double)
            {
                return Math.Pow((dynamic)left, (dynamic)right);
            }
            var error = left is not double ? left.TypeCast() : right.TypeCast();
            throw new SemanticError("'^'",error);
        }
    }

    //Operacion logaritmo
    public class Log : CompoundFunction
    {
        public Log (Expression leftIndex, Expression rightIndex) : base(leftIndex,rightIndex)
        {}
        public override object Evaluate(object left, object right)
        {
            left ??= right;
            right ??= left;
            if(left==null && right==null)
            {
                return 5d;
            }
            if(left is double && right is double)
            {
                return Math.Log((dynamic)left,(dynamic)right);
            }
            var error = left is not double ? left.TypeCast() : right.TypeCast();
            throw new SemanticError("'Log'", error);
        }
    }

    //Funcion menor que
    public class Lower : CompoundFunction
    {
        public Lower(Expression leftIndex, Expression rightIndex) : base(leftIndex,rightIndex)
        {}
        public override object Evaluate(object left, object right)
        {
            left ??= right;
            right ??= left;
            if(left==null && right == null)
            {
                return default(bool);
            }
            if(left is double && right is double)
            {
                return (dynamic)left < (dynamic)right;
            }
            var error = left is not double ? left.TypeCast() : right.TypeCast();
            throw new SemanticError("'<'",error);
        }
    }

    //Funcion menor igual
    public class LowerEqual : CompoundFunction
    {
        public LowerEqual(Expression leftIndex, Expression rightIndex) : base(leftIndex,rightIndex)
        {}
        public override object Evaluate(object left, object right)
        {
            left ??= right;
            right ??= left;
            if(left==null && right==null)
            {
                return default(bool);
            }
            if(left is double && right is double)
            {
                return (dynamic)left <= (dynamic)right;
            }
            var error = left is not double ? left.TypeCast() : right.TypeCast();
            throw new SemanticError("'<='",error);
        }
    }

    //Funcion mayor que
    public class Greater : CompoundFunction
    {
        public Greater(Expression leftIndex, Expression rightIndex) : base(leftIndex, rightIndex)
        {
        }
        public override object Evaluate(object left, object right)
        {
            left ??= right;
            right ??= left;
            if (left == null && right == null)
                return default(bool);
            if (left is double && right is double)
            {
                return (dynamic)left > (dynamic)right;
            }               
            var conflictiveType = left is not double ? left.TypeCast() : right.TypeCast();
            throw new SemanticError("'>'", conflictiveType);
        }
    }

    //Funcion mayor igual
    public class GreaterEqual : CompoundFunction
    {
        public GreaterEqual (Expression leftIndex,Expression rightIndex) : base(leftIndex,rightIndex)
        {}
        public override object Evaluate(object left, object right)
        {
            left ??= right;
            right ??= left;
            if(left==null && right==null)
            {
                return default(bool);
            }
            if(left is double && right is double)
            {
                return (dynamic)left >= (dynamic)right;
            }
            var error = left is not double ? left.TypeCast() : right.TypeCast();
            throw new SemanticError("'>='",error);
        }
    }

    //Funcion de igualdad
    public class Equal : CompoundFunction
    {
        public Equal(Expression leftIndex, Expression rightIndex) : base(leftIndex,rightIndex)
        {}
        public override object Evaluate(object left, object right)
        {
            try
            {
                return(dynamic) left == (dynamic)right;
            }
            catch(RuntimeBinderException ex)
            {
                string exe = ex.Message;
                throw new TypicalError(exe);
            }
        }
    }

    //Funcion de desigualdad
    public class Unequal : CompoundFunction
    {
        public Unequal(Expression leftIndex, Expression rightIndex) : base (leftIndex,rightIndex)
        {}
        public override object Evaluate(object left, object right)
        {
            try
            {
                return (dynamic) left != (dynamic)right;
            }
            catch (RuntimeBinderException ex)
            {
                string exe = ex.Message;
                throw new TypicalError(exe);
            }
        }
    }

    //Concatenar strings
    public class Conc : CompoundFunction
    {
        public Conc(Expression leftArgument, Expression rightArgument) : base(leftArgument, rightArgument)
        {
        }
        public override object Evaluate(object left, object right)
        {
            if (left == null )
            {
                left="";
            }
                
            
            if(right==null)
            {
                right=""; 
            }
                                           
            return left.ToString() + right.ToString();
        }
    }






}