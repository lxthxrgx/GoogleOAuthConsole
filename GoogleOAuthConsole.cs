using System.Text;
using System.Diagnostics;
using System.Net;
using System.Net.Http.Headers;
using Newtonsoft.Json;

public class Test
{
    static string clientId = @"324242308665-sb2qab4bsfarn5h9kt9oq4kq4fvik45q.apps.googleusercontent.com";
    static string clientSecret = @"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";
    static string auth_url = @"https://accounts.google.com/o/oauth2/v2/auth";
    static string token_url = @"https://oauth2.googleapis.com/token";
    static string redirect_uri = @"http://localhost:8500";
    static string redirect_uriToken = @"http://localhost:8500";
    static string code;
    static string accessTokenGoogle;
    class TokenData
    {
        public string access_token { get; set; }
        public string expires_in { get; set; }
        public string token_type { get; set; }
        public string scope { get; set; }
        public string refresh_token { get; set; }
    }
    public static string GetRequestGoogle()
    {
        UriBuilder builder = new UriBuilder(auth_url);
        var query = new StringBuilder();
        query.Append($"client_id={Uri.EscapeDataString(clientId)}");
        query.Append($"&redirect_uri={Uri.EscapeDataString(redirect_uri)}");
        query.Append($"&response_type=code");
        query.Append($"&scope=https://www.googleapis.com/auth/userinfo.email https://www.googleapis.com/auth/userinfo.profile");

        builder.Query = query.ToString();
        string requestUrl = builder.ToString();
        Console.WriteLine("Google request:",requestUrl);
        return requestUrl;
    }
    static async Task<string> GetTokenGoogle(string code)
    {
        UriBuilder builder = new UriBuilder(token_url);
        var query = new StringBuilder();
        query.Append($"client_id={Uri.EscapeDataString(clientId)}");
        query.Append($"&client_secret={Uri.EscapeDataString(clientSecret)}");
        query.Append($"&redirect_uri={Uri.EscapeDataString(redirect_uriToken)}");
        query.Append($"&grant_type=authorization_code");
        query.Append($"&code={Uri.EscapeDataString(code)}");

        builder.Query = query.ToString();
        string requestUrlToken = builder.ToString();
        Console.WriteLine(requestUrlToken);

        using (var httpClient = new HttpClient())
        {
            var response = await httpClient.PostAsync(requestUrlToken, null);
            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                var tokenData = JsonConvert.DeserializeObject<TokenData>(responseContent);
                if (tokenData != null)
                {
                    Console.WriteLine("Access Token: " + tokenData.access_token);
                    Console.WriteLine("Token Type: " + tokenData.token_type);
                    Console.WriteLine("expires_in: " + tokenData.expires_in);
                    Console.WriteLine("scope: " + tokenData.scope);
                    Console.WriteLine("refresh_token: " + tokenData.refresh_token);
                    accessTokenGoogle = tokenData.access_token;
                   
                    return tokenData.access_token;
                }
            }
            else
            {
                Console.WriteLine($"Ошибка при запросе токена. Код статуса: {response.StatusCode}");
            }
            return null;
        }
    }
    static async Task StartListeningAsync(string url)
    {
        var listener = new HttpListener();
        listener.Prefixes.Add(url);
        listener.Start();
        Console.WriteLine($"Сервер начал прослушивание {url}");
        while (true)
        {
            try
            {
                HttpListenerContext context = await listener.GetContextAsync();
                HandleRequestAsync(context);
            }
            catch (HttpListenerException)
            {
                break;
            }
        }
        listener.Close();
    }
    static async Task HandleRequestAsync(HttpListenerContext context)
    {
        HttpListenerRequest request = context.Request;
        HttpListenerResponse response = context.Response;
        code = request.QueryString["code"];
        string decodedcode = System.Web.HttpUtility.UrlDecode(code);
        code = decodedcode;
        response.Close();
        await GetTokenGoogle(code);
        await GetUserInfo(accessTokenGoogle);
    }
    static async Task<string> GetUserInfo(string accessToken)
    {
        string userInfoUrl = "https://www.googleapis.com/oauth2/v3/userinfo";

        using (var httpClient = new HttpClient())
        {
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            var response = await httpClient.GetAsync(userInfoUrl);

            if (response.IsSuccessStatusCode)
            {
                var responseContent = await response.Content.ReadAsStringAsync();
                Console.WriteLine("User Info: " + responseContent);
                return responseContent;
            }
            else
            {
                Console.WriteLine($"Ошибка при запросе информации о пользователе. Код статуса: {response.StatusCode}");
                return null;
            }
        }
    }

    static async Task Main(string[] args)
    {
        var requrl = GetRequestGoogle();

        ProcessStartInfo processStartInfo = new ProcessStartInfo()
        {
            FileName = requrl,
            UseShellExecute = true
        };

        Process browserProcess = Process.Start(processStartInfo);

        string urlToListen = "http://localhost:8500/";
        await StartListeningAsync(urlToListen);
        var accessToken = await GetTokenGoogle(await GetTokenGoogle(code));
        await GetUserInfo(accessToken);
    }
}
