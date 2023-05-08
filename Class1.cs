using System;

public static class OkObjectResultExtensions
{
    public static OkObjectResult WithCorsHeaders(this OkObjectResult result, string origin)
    {
        result.Value = result.Value ?? "";
        result.StatusCode = (int)HttpStatusCode.OK;
        result.ContentTypes = new MediaTypeCollection { "application/json" };
        result.Value = JsonConvert.SerializeObject(result.Value);

        result.Headers.Add("Access-Control-Allow-Origin", origin);
        result.Headers.Add("Access-Control-Allow-Credentials", "true");
        result.Headers.Add("Access-Control-Allow-Methods", "GET");
        result.Headers.Add("Access-Control-Allow-Headers", "Authorization, Origin, Content-Type, Accept");

        return result;
    }
}
