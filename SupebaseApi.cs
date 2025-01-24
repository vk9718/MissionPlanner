using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
public static class HttpClientExtensions
{
    public static async Task<HttpResponseMessage> PatchAsync(this HttpClient client, string requestUri, HttpContent content)
    {
        var method = new HttpMethod("PATCH");
        var request = new HttpRequestMessage(method, requestUri)
        {
            Content = content
        };

        return await client.SendAsync(request);
    }
}

public class SupabaseActivationKeyRepository
{
    private readonly string _supabaseUrl= "https://fsmaxdraodrbwdjkuglt.supabase.co";
    private readonly string _supabaseKey= "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImZzbWF4ZHJhb2RyYndkamt1Z2x0Iiwicm9sZSI6ImFub24iLCJpYXQiOjE3Mzc2MDg2ODcsImV4cCI6MjA1MzE4NDY4N30.C_VP5mXjXaoaXVUV6fxXNcWKxDBVkMQqXx859nlRLnQ";
    private readonly HttpClient _httpClient;

    //public SupabaseActivationKeyRepository(string supabaseUrl, string supabaseKey)
    public SupabaseActivationKeyRepository()
    {
        //_supabaseUrl = supabaseUrl;
        //_supabaseKey = supabaseKey;
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("apikey", _supabaseKey);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_supabaseKey}");
    }

    public async Task<ActivationKeyUser> CreateActivationKey(ActivationKeyUser activationKey)
    {
        var url = $"{_supabaseUrl}/rest/v1/activation_key_users";
        var json = JsonConvert.SerializeObject(activationKey);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync(url, content);
        response.EnsureSuccessStatusCode();

        return activationKey;
    }

    public async Task<List<ActivationKeyUser>> GetActivationKeys(string systemName = null)
    {
        var url = $"{_supabaseUrl}/rest/v1/activation_key_users";

        if (!string.IsNullOrEmpty(systemName))
        {
            url += $"?system_name=eq.{systemName}";
        }

        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();

        var responseContent = await response.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<List<ActivationKeyUser>>(responseContent);
    }

    public async Task<ActivationKeyUser> UpdateActivationKey(Guid userId, ActivationKeyUser updatedKey)
    {
        var url = $"{_supabaseUrl}/rest/v1/activation_key_users?user_id=eq.{userId}";
        var json = JsonConvert.SerializeObject(updatedKey);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _httpClient.PatchAsync(url, content);
        response.EnsureSuccessStatusCode();

        return updatedKey;
    }

    public async Task<bool> DeleteActivationKey(Guid userId)
    {
        var url = $"{_supabaseUrl}/rest/v1/activation_key_users?user_id=eq.{userId}";
        var response = await _httpClient.DeleteAsync(url);
        return response.IsSuccessStatusCode;
    }
}





[Table("activation_key_users", Schema = "public")]
public class ActivationKeyUser

{

    [Key]
    [Column("user_id")]
    public Guid UserId { get; set; } = Guid.NewGuid();
    [Column("created_at")]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    [Column("updated_at")]
    public DateTime? UpdatedAt { get; set; } = DateTime.UtcNow;
    [Column("system_name")]
    public string SystemName { get; set; }
    [Column("system_id")]
    public string SystemId { get; set; }
    [Column("organisation_name")]
    public string OrganisationName { get; set; }
    [Column("used_for")]
    public string UsedFor { get; set; }

    [Column("activation_key")]
    public string ActivationKey { get; set; }
    [Column("valid_days")]
    public long ValidDays { get; set; }
    [Column("active_status")]
    public bool? ActiveStatus { get; set; }

}

//public class Program
//{
//    public static async Task Main()
//    {
//        string supabaseUrl = "YOUR_SUPABASE_PROJECT_URL";
//        string supabaseKey = "YOUR_SUPABASE_ANON_KEY";

//        var repository = new SupabaseRepository(supabaseUrl, supabaseKey);

//        try
//        {
//            var newUser = new User { Name = "John Doe", Email = "john@example.com" };
//            var createdUser = await repository.CreateUser(newUser);
//            var users = await repository.GetUsers();
//        }
//        catch (Exception ex)
//        {
//            Console.WriteLine($"Error: {ex.Message}");
//        }
//    }
//}