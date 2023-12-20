namespace HULK
{
    //Clase para lanzar el programa
    public class Load
    {
        public SavedHistory Save{get; private set;}
        Print PrintFunc;
        HulkParser Parser;

        public Load(Print ret)
        {
            Save=new();
            Parser=new(Save, ret);
            PrintFunc=ret;
        }

        public void RunProgram(string userinput)
        {
            string[] savedInput = Tokenizer.MakeTokens(userinput);
            List<string[]> Steps;
            if(savedInput.Length==0)
            {
                return;
            }
            try
            {
                Steps=Tokenizer.GetAndSave(savedInput);
            }
            catch(Exception err)
            {
                PrintFunc(err.Message);
                return;
            }

            for(int i = 0; i<Steps.Count; i++)
            {
                string[] step = Steps[i];
                if(step.Length == 0)
                {
                    continue;
                }            
                try
                {
                    try
                    {
                        Expression expression = Parser.Parse(step);
                        if(expression is FunctionDeclaration dec)
                        {
                            dec.History(Save);
                        }
                        else if(expression is FinalReturn ret)
                        {
                            ret.GetValue(false);
                            ret.GetValue(true);
                        }
                        else
                        {
                            PrintFunc(expression.GetValue(false));
                        }
                    }
                    catch(Exception ex)
                    {
                        throw ex;
                    }
                    
                }   
                catch(Exception ex)
                {
                   PrintFunc(ex.Message);
                }
            }
        }
    }
}