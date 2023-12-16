using System.Diagnostics.CodeAnalysis;
using System.Diagnostics.Contracts;
using System.Globalization;
namespace HULK
{
    //Declaracion de la clase HulkError que se encarga de gestionar los mensajes de error del compilador
    public abstract class HulkError : Exception
    {
        public override string Message { get => (ErrorMessageDeclaration + ErrorMessageDescription + ".").Replace("hstring", "string");}
        public string ErrorMessageDeclaration {get; protected set;}
        public string ErrorMessageDescription {get; protected set;}
    }

    //Declaracion de errores de tipo generico
    public class TypicalError : HulkError
    {
        public TypicalError(string ErrorMessage)
        {
            ErrorMessageDescription = ErrorMessage;
            ErrorMessageDeclaration = "-Error: ";
        }

    }
    
    //Declaracion de errores de tipo sintactico
    public class SyntaxError : HulkError
    {
        public string MissingInput {get;}
        public string Location{get;}
        public SyntaxError(string missingInput, string location)
        {
            ErrorMessageDeclaration="-Syntax Error: ";
            MissingInput=missingInput;
            Location=location; 
            ErrorMessageDeclaration= $"-Not found {MissingInput} in {Location}";
        }
    }

    //Declaracion de errores de tipo semantico
    public class SemanticError :HulkError
    {
        public string Input {get;}
        public string GivedInput{get;}
        public SemanticError(string input, string givedInput)
        {
            ErrorMessageDeclaration="-Semantic Error: ";
            Input=input;
            GivedInput=givedInput;
            ErrorMessageDescription=$" {Input} cant receive '{GivedInput}'";

        }
    }

    //Declaracion de errores de tipo semantico en operaciones matematicas
    public class MathSemanticError : HulkError
    {
        public string Operation {get;}
        public string FirstTerm {get;}
        public string SecondTerm{get;}
        public MathSemanticError(string operation, string firstTerm, string secondTerm)
        {
            ErrorMessageDeclaration = "-MathSemanticError: ";
            Operation=operation;
            FirstTerm=firstTerm;
            SecondTerm=secondTerm;
            ErrorMessageDescription = $"Cant operate '{operation}' with '{firstTerm}' and '{secondTerm}'";
        }
    }

    //Declaracion de errores de tipo lexico
    public class LexicalError: HulkError
    {
        public string Input {get;}
        public string ExpectedInput {get;}
        
        public LexicalError(string input)
        {
            ErrorMessageDeclaration = "-LexicalError : ";
            Input = input;
            ExpectedInput = " ";
            ErrorMessageDescription = $"'{input}' it's an invalid token ";
        }
        
        public LexicalError (string input, string expectedinput)
        {
            ErrorMessageDeclaration ="-LexicalError : ";
            Input=input;
            ExpectedInput=expectedinput;
            ErrorMessageDescription = $"'{input}' is not valid {expectedinput}";

        }

    }

    //Declaracion adicional para errores de Overflow
    public class OverflowError : HulkError
    {
        public OverflowError()
        {
            ErrorMessageDeclaration = "-Program Error : ";
            ErrorMessageDescription = $"Overflow Error";
        }
    }







    

}