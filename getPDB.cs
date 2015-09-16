using System.Net;


class getPDB
{
    static void Main(string[] args)
    {
        string pdb = args[0];
        string url = "http://www.pdb.org/pdb/files/" + pdb + ".pdb";
        WebClient webClient = new WebClient();
        webClient.DownloadFile(url , "pdb.pdb");
    }
}