namespace HOSKYSWAP.UI.WASM.Services;

public class BackendService
{
    private HttpClient HttpClient { get; set; }

    public BackendService()
    {
        HttpClient = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:5120")
        };
    }
}