using System.Text.RegularExpressions;
using System.Xml.Schema;
namespace HULK
{
    public static class Tokenizer
    {
        //Crea una lista con con los elementos separados del input del usuario
        public static List<string[]> GetAndSave(string[] userInput)
        {
            List<string[]> SavedInput = new();

            if(userInput[^1] != ";")
            throw new TypicalError("missing ;");

            int tempCount=0;
            for(int i=0; i<userInput.Length; i++)
            {
                if(userInput[i] == ";")
                {
                    string[] Insert = new string[i-tempCount];
                    for(int j=0; j<i-tempCount;j++)
                    {
                        Insert[j]=userInput[j+tempCount];
                    }
                    SavedInput.Add(Insert);
                    tempCount=i+1;
                }
            }
            return SavedInput;
        }

        //Crea los Tokens y los Guarda
        public static string[] MakeTokens(string input)
        {
            Regex Guide = new(@";|(\u0022([^\u0022\\]|\\.)*\u0022)|\(|\+|-|\*|/|%|,|(=>)|(<=)|(>=)|(<)|(>)|:=|={2}|=|(!=)|\^|&|\||!|\)|[^\(\)\+\-\*/\^%<>:=!&\|,;\s]+");
            MatchCollection GuideMatches = Guide.Matches(input);
            string[] Tokens = new string[GuideMatches.Count];
            for(int i = 0; i<Tokens.Length;i++)
            {
                Tokens[i]=GuideMatches[i].Value;
            }                      
            return Tokens;
        }
        
        //Para buscar las estruccturas de los comandos (let-in, if else, etc)
        public static int GetName(string[] tokens, int start, int stop, string limit)
        {
            int result = start;
            for(int i = start; i<stop;i++)
            {
                if(tokens[i]=="(")
                {
                    i=OpenBalance(i,stop,tokens);
                    result=i;
                }
                if(tokens[i] == limit)
                {
                    result--;
                    break;
                }
                result++;
            }
            return result;
        }

        //Separa los tokens por parentesis
        public static int OpenBalance(int count, int firstelement, string[] Tokens)
        {
            int tempCount=0;
            for(int i=count;i<=firstelement;i++)
            {
                switch(Tokens[i])
                {
                case ")":
                {
                 tempCount--;
                if(tempCount==0)
                {
                    return i;
                }
                }   
                break;                 
                case "(":
                {
                    tempCount++;
                    break;
                }

                

                }
            }
            throw new SyntaxError("(" , "input declaration");
        }

        //Separa los tokens por parentesis
        public static int ClosedBalance(int count, int lastelement, string[] Tokens)
        {
            int tempCount=0;
            for(int i=count;i>=lastelement;i--)
            {
                switch(Tokens[i])
                {
                case "(":
                {
                tempCount--;
                if(tempCount==0)
                {
                    return i;
                }

                }
                break;
                    
                case ")":
                {
                    tempCount++;
                    break;
                }


                }
            }
            throw new SyntaxError(")" , "input declaration");
        }

        //Revisando el uso de las comas para separa los tokens
        public static List<string> CheckingComma(int firstelement, int lastelement, string[] tokens)
        {
            List<string> output = new();
            if(tokens[firstelement]=="," || tokens[lastelement]==",")
            {
                throw new TypicalError("error with comma usage");
            }
            for(int i=firstelement;i<=lastelement;i++)
            {
                if(tokens[i] != ",")
                {
                    if(i%2==1 || firstelement==lastelement)
                    {
                        output.Add(tokens[i]);
                    }
                    else
                    {
                        throw new SyntaxError("," , "input declaration");
                    }
                }
            }
            return output;
        }
    }
}
