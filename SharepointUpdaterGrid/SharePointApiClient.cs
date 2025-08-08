using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SharepointUpdaterGrid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Windows.Forms;

public class SharePointApiClient
{
    private static readonly HttpClient httpClient = new HttpClient();

    private readonly string clientId;
    private readonly string clientSecret;
    private readonly string realm;
    private readonly string host;
    private readonly string siteFolder;
    private readonly string principal = ConfigLoader.Config["SharepointApiConfig:Configuration:principal"];

    private readonly string expectedName;
    private readonly string expectedDob;

    private static readonly string logFilePath = "log.txt";
    private string cachedAccessToken = null;

    public SharePointApiClient(string clientId, string clientSecret, string realm, string host, string siteFolder, string expectedName, string expectedDob)
    {
        this.clientId = clientId;
        this.clientSecret = clientSecret;
        this.realm = realm;
        this.host = host;
        this.siteFolder = siteFolder;
        this.expectedName = expectedName;
        this.expectedDob = expectedDob;

        File.WriteAllText(logFilePath, "===== SharePoint API Log Start =====\n");
    }

    private void Log(string message)
    {
        File.AppendAllText(logFilePath, message + "\n");
        Debug.WriteLine(message);
    }

    public async Task<DataTable> GetFilteredMetadataTableAsync()
    {
        var table = new DataTable();
        table.Columns.Add("Doc ID");
        table.Columns.Add("Student ID");
        table.Columns.Add("EI ID");
        table.Columns.Add("Foster Care Agency");
        table.Columns.Add("District");
        table.Columns.Add("Guard Email");
        table.Columns.Add("Guard Phone");
        table.Columns.Add("Source");
        table.Columns.Add("FileUrl");

        Log("🔐 Getting access token...");
        string accessToken = await GetAccessTokenAsync();
        if (string.IsNullOrEmpty(accessToken))
        {
            Log("❌ Failed to get access token.");
            return table;
        }

        Log("✅ Access token received.");

        // Limit JSON payload to essential fields

        string template = ConfigLoader.Config["SharepointApiConfig:APIEndPoints:GetFilesExpanded"];
        string getFilesUrl = string.Format(template, host, siteFolder);
        /*
        string getFilesUrl =
            $"https://{host}/sites/eidtsuat/_api/web/GetFolderByServerRelativeUrl('{siteFolder}')/Files" +
            "?$expand=ListItemAllFields&" +
            "$select=Name,ServerRelativeUrl," +
            "ListItemAllFields/ChildFirstName," +
            "ListItemAllFields/ChildLastName," +
            "ListItemAllFields/DOB";
        */
        Log("📁 Requesting file list from: " + getFilesUrl);

        var request = new HttpRequestMessage(HttpMethod.Get, getFilesUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await httpClient.SendAsync(request);
        string fileListJson = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            Log($"❌ File list failed: {response.StatusCode}");
            return table;
        }

        dynamic fileList = JsonConvert.DeserializeObject(fileListJson);
        if (fileList?.value == null || fileList.value.Count == 0)
        {
            Log("⚠ No files returned.");
            return table;
        }

        Log($"📄 {fileList.value.Count} files found.");

        foreach (var file in fileList.value)
        {
            string first = file.ListItemAllFields?.ChildFirstName ?? "";
            string last = file.ListItemAllFields?.ChildLastName ?? "";
            string dobValue = file.ListItemAllFields?.DOB ?? "";
            string fullName = $"{first} {last}".Trim();

            if (!fullName.Equals(expectedName, StringComparison.OrdinalIgnoreCase))
                continue;

            string normalizedActualDob = DateTime.TryParse(dobValue, out var actualDob)
                ? actualDob.ToString("yyyy-MM-dd")
                : dobValue;

            string normalizedExpectedDob = DateTime.TryParse(expectedDob, out var userDob)
                ? userDob.ToString("yyyy-MM-dd")
                : expectedDob;

            if (!string.Equals(normalizedActualDob, normalizedExpectedDob, StringComparison.OrdinalIgnoreCase))
                continue;

            // ✅ Only now fetch full metadata for the match
            string fileUrl = file.ServerRelativeUrl;
            string metadataTemplate = ConfigLoader.Config["SharepointApiConfig:APIEndPoints:GetFileMetadata"];
            string metadataUrl = string.Format(metadataTemplate, host, fileUrl);
            var metadataRequest = new HttpRequestMessage(HttpMethod.Get, metadataUrl);
            metadataRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            metadataRequest.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            var metadataResponse = await httpClient.SendAsync(metadataRequest);
            if (!metadataResponse.IsSuccessStatusCode) continue;

            string metadataJson = await metadataResponse.Content.ReadAsStringAsync();
            JObject metadata = JObject.Parse(metadataJson);

            string docId = metadata.Value<string>("Id") ?? "";
            string studentId = metadata.Value<string>("Student_x0020_ID_x0020_MCM") ?? "";
            string eiId = metadata.Value<string>("NYEIS_x0020_ID_x0020__x002d__x0020_NEW") ?? "";
            string fosterAgency = metadata.Value<string>("Foster_x0020_Care_x0020_Agency") ?? "";
            string district = metadata.Value<string>("District") ?? "";
            string guardEmail = metadata.Value<string>("Parent_x002d_Guardian_x0020_Email") ?? "";
            string guardPhone = metadata.Value<string>("Guardian_x0020_Phone_x0020__x0023_") ?? "";
            string source = metadata.Value<string>("Source") ?? "";

            table.Rows.Add(docId, studentId, eiId, fosterAgency, district, guardEmail, guardPhone, source, fileUrl);
        }

        Log($"✅ Metadata processing complete. Rows added: {table.Rows.Count}");
        return table;
    }

    private async Task<string> GetAccessTokenAsync()
    {
        if (!string.IsNullOrEmpty(cachedAccessToken))
        {
            return cachedAccessToken;
        }
        var formData = new FormUrlEncodedContent(new[]
        {
        new KeyValuePair<string, string>("grant_type", "client_credentials"),
        new KeyValuePair<string, string>("client_id", $"{clientId}@{realm}"),
        new KeyValuePair<string, string>("client_secret", clientSecret),
        new KeyValuePair<string, string>("resource", $"{principal}/{host}@{realm}")
    });

        string urlTemplate = ConfigLoader.Config["SharepointApiConfig:APIEndPoints:GetAccessTokenEndPoint"];
        string accessTokenUrl = string.Format(urlTemplate, realm);

        HttpResponseMessage response = await httpClient.PostAsync(accessTokenUrl, formData);

        string content = await response.Content.ReadAsStringAsync();
        Log("🔓 Token response:\n" + content);

        if (!response.IsSuccessStatusCode)
            return null;

        dynamic result = JsonConvert.DeserializeObject(content);
        cachedAccessToken = result.access_token;
        return cachedAccessToken;
    }
    public async Task<bool> UpdateFileMetadataAsync(string fileUrl, Dictionary<string, object> updates)
    {
        if (updates == null || updates.Count == 0)
            return true;
        string metadataTemplate = ConfigLoader.Config["SharepointApiConfig:APIEndPoints:GetFileMetadata"];
        string metadataUrl = string.Format(metadataTemplate, host, fileUrl);

        // Need form digest
        string formDigest = await GetFormDigestAsync();
        if (string.IsNullOrEmpty(formDigest))
        {
            Log("❌ Failed to retrieve form digest.");
            return false;
        }

        var payload = new JObject();
        foreach (var kvp in updates)
            payload[kvp.Key] = kvp.Value?.ToString() ?? "";

        var request = new HttpRequestMessage(HttpMethod.Post, metadataUrl);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        request.Headers.Add("X-RequestDigest", formDigest);
        request.Headers.Add("IF-MATCH", "*");
        request.Headers.Add("X-HTTP-Method", "MERGE");

        request.Content = new StringContent(payload.ToString(), System.Text.Encoding.UTF8, "application/json");

        HttpResponseMessage response = await httpClient.SendAsync(request);
        string responseContent = await response.Content.ReadAsStringAsync();
        Log($"Update response for {fileUrl}: {response.StatusCode} - {responseContent}");

        return response.IsSuccessStatusCode;
    }
    private async Task<string> GetFormDigestAsync()
    {
        string getFormDigestUrl = ConfigLoader.Config["SharepointApiConfig:APIEndPoints:GetFormDigest"];

        var request = new HttpRequestMessage(HttpMethod.Post, getFormDigestUrl);

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", await GetAccessTokenAsync());
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        HttpResponseMessage response = await httpClient.SendAsync(request);
        string content = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return null;

        JObject json = JObject.Parse(content);
        return json?["FormDigestValue"]?.ToString();
    }

}