namespace Cloud.Models
{
    //*: esta classe e responsavel por ter todo o conteudo para criar os executaveis 
    public class Runtime
    {
    public string dotNetIdentifier;
        public string pathOutput => dotNetIdentifier;


        public Runtime(string dotnet)
        {
            this.dotNetIdentifier = dotnet;

        }

    }
}