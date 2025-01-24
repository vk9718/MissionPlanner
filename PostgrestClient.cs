using System.Net.Http;

internal class PostgrestClient
{
    private string supabaseUrl;
    private HttpClient httpClient;

    public PostgrestClient(string supabaseUrl, HttpClient httpClient)
    {
        this.supabaseUrl = supabaseUrl;
        this.httpClient = httpClient;
    }
}