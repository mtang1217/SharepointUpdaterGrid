using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using System.Globalization;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Windows.Forms;

class SharePointIndexer
{
    private static HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    private static IConfiguration config;
    private static string clientId;
    private static string clientSecret;
    private static string realm;
    private static string principal;
    private static string host;
    private static string tokenEndpoint;
    private static string baseFolderPath;
    private static string sitePrefix;

    private static string cachedAccessToken = null;
    private static DateTime tokenExpiry = DateTime.MinValue;

    private static SQLiteConnection connection;

    public static int FileCount = 0;
    private static string rootFolderPath = baseFolderPath;
    public static async Task IndexAllAsync(string dbPath, ProgressBar progressBar, Label statusLabel)
    {
        FileCount = 0;
        config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
            .Build();

        clientId = ConfigLoader.Config["SharepointApiConfig:Configuration:appReg_clientId"];
        clientSecret = ConfigLoader.Config["SharepointApiConfig:Configuration:appReg_clientSecret"];
        realm = ConfigLoader.Config["SharepointApiConfig:Configuration:realm"];
        principal = ConfigLoader.Config["SharepointApiConfig:Configuration:principal"];
        host = ConfigLoader.Config["SharepointApiConfig:Configuration:targetHost"];
        baseFolderPath = ConfigLoader.Config["SharepointApiConfig:Configuration:baseFolderPath"];

        tokenEndpoint = string.Format(
            ConfigLoader.Config["SharepointApiConfig:APIEndPoints:GetAccessTokenEndPoint"], realm
        );
        sitePrefix = ConfigLoader.Config["SharepointApiConfig:Configuration:sitePrefix"];

        var stopwatch = Stopwatch.StartNew();

        // Setup database
        dbPath = "indexed_metadata.db";
        if (System.IO.File.Exists(dbPath)) System.IO.File.Delete(dbPath);
        SQLiteConnection.CreateFile(dbPath);
        connection = new SQLiteConnection($"Data Source={dbPath};Version=3;");
        connection.Open();

        string createTableSql = @"
        CREATE TABLE Files (
            Id INTEGER PRIMARY KEY AUTOINCREMENT,
            FirstName TEXT,
            LastName TEXT,
            DOB TEXT,
            EI_ID TEXT,
            Student_ID TEXT,
            DocID TEXT,
            Agency TEXT,
            District TEXT,
            Email TEXT,
            Phone TEXT,
            Source TEXT,
            fileUrl TEXT
        );
        CREATE INDEX idx_name_dob ON Files (FirstName, LastName, DOB);
        CREATE INDEX idx_eiid ON Files (EI_ID);
        CREATE INDEX idx_studentid ON Files (Student_ID);

        ";
        using (var command = new SQLiteCommand(createTableSql, connection))
        {
            command.ExecuteNonQuery();
        }

        string accessToken = await GetAccessToken();
        if (!string.IsNullOrEmpty(accessToken))
        {
            await TraverseFolderRecursive(baseFolderPath, accessToken, progressBar, statusLabel);
        }

        stopwatch.Stop();
        Console.WriteLine();
        Console.WriteLine("==================================");
        Console.WriteLine(" Indexing complete.");
        Console.WriteLine($" Root folder: {rootFolderPath}");
        Console.WriteLine($" Total files processed: {FileCount}");
        Console.WriteLine($" Time taken: {stopwatch.Elapsed}");
        Console.WriteLine("==================================");


        // Lookups
        //RunCustomQuery("Select * from Files where firstname like 'three'");
        //RunCustomQuery("Select * from Files Where LastName like '%tap%'");
        //RunCustomQuery("Select * from Files Where DOB like '2017-09-11%'");
        connection.Close();
    }

    private static async Task<string> GetAccessToken()
    {
        if (!string.IsNullOrEmpty(cachedAccessToken) && DateTime.UtcNow < tokenExpiry)
        {
            return cachedAccessToken;
        }

        var body = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "client_credentials"),
            new KeyValuePair<string, string>("client_id", $"{clientId}@{realm}"),
            new KeyValuePair<string, string>("client_secret", clientSecret),
            new KeyValuePair<string, string>("resource", $"{principal}/{host}@{realm}")
        });

        HttpResponseMessage response = await httpClient.PostAsync(tokenEndpoint, body);
        if (response.IsSuccessStatusCode)
        {
            dynamic result = JObject.Parse(await response.Content.ReadAsStringAsync());
            cachedAccessToken = result.access_token;
            int expiresIn = int.Parse((string)result.expires_in);
            tokenExpiry = DateTime.UtcNow.AddSeconds(expiresIn - 60);

            return cachedAccessToken;
        }

        return null;
    }

    private static async Task TraverseFolderRecursive(string folderPath, string accessToken, ProgressBar progressBar, Label statusLabel)
    {
        using (var transaction = connection.BeginTransaction()) // start transaction
        {
            string urlTemplate = ConfigLoader.Config["SharepointApiConfig:APIEndPoints:GetFilesWithMetadata"];
            string filesUrl = string.Format(urlTemplate, host, folderPath);

            var filesResponse = await SendSharePointRequest(filesUrl, accessToken);
            if (filesResponse?["value"] is JArray files)
            {
                progressBar.Invoke((MethodInvoker)(() =>
                {
                    progressBar.Maximum = files.Count;
                    progressBar.Value = 0;
                }));

                // Update status label text
                statusLabel.Invoke((MethodInvoker)(() =>
                {
                    statusLabel.Text = $"📁 Folder: {folderPath} | Files in folder: {files.Count}";
                }));

                foreach (var file in files)
                {
                    progressBar.Invoke((MethodInvoker)(() =>
                    {
                        progressBar.Value++;
                    }));

                    var metadata = file["ListItemAllFields"];
                    if (metadata != null)
                    {
                        string rawDob = metadata.Value<string>("DOB") ?? "";
                        string dob = "";

                        if (DateTime.TryParse(rawDob, CultureInfo.InvariantCulture,
                            DateTimeStyles.AdjustToUniversal, out var dt))
                        {
                            dob = dt.ToString("yyyy-MM-dd"); // ISO format
                        }
                        else
                        {
                            dob = ""; // store empty if parsing fails
                        }

                        // Get the File URL from the file object
                        string fileUrl = file.Value<string>("ServerRelativeUrl") ?? "";

                        // Insert the record, now including the fileUrl after Source
                        InsertRecord(
                            metadata.Value<string>("ChildFirstName") ?? "",
                            metadata.Value<string>("ChildLastName") ?? "",
                            dob,
                            metadata.Value<string>("NYEIS_x0020_ID_x0020__x002d__x0020_NEW") ?? "",
                            metadata.Value<string>("Student_x0020_ID_x0020_MCM") ?? "",
                            metadata.Value<string>("Id") ?? "",
                            metadata.Value<string>("Foster_x0020_Care_x0020_Agency") ?? "",
                            metadata.Value<string>("District") ?? "",
                            metadata.Value<string>("Parent_x002d_Guardian_x0020_Email") ?? "",
                            metadata.Value<string>("Guardian_x0020_Phone_x0020__x0023_") ?? "",
                            metadata.Value<string>("Source") ?? "",
                            fileUrl,  // Add fileUrl here
                            transaction
                        );

                        FileCount++;
                    }
                }
            }

            transaction.Commit(); // commit once
        }

        // Recurse into subfolders
        string template = ConfigLoader.Config["SharepointApiConfig:APIEndPoints:GetFolders"];
        string foldersUrl = string.Format(template, host, folderPath);
        var foldersResponse = await SendSharePointRequest(foldersUrl, accessToken);
        if (foldersResponse?["value"] is JArray folders)
        {
            foreach (var folder in folders)
            {
                string name = folder.Value<string>("Name");
                if (name == "Forms") continue;
                string subfolderPath = folder.Value<string>("ServerRelativeUrl");
                await TraverseFolderRecursive(subfolderPath, accessToken, progressBar, statusLabel);
            }
        }
    }


    private static void InsertRecord(
    string firstName, string lastName, string dob, string eiId, string studentId,
    string docId, string agency, string district, string email, string phone,string source, string fileUrl,
    SQLiteTransaction transaction)
    {
        string insertSql = @"
    INSERT INTO Files 
    (FirstName, LastName, DOB, EI_ID, Student_ID, DocID, Agency, District, Email, Phone, Source, fileUrl)
    VALUES 
    (@FirstName, @LastName, @DOB, @EI_ID, @Student_ID, @DocID, @Agency, @District, @Email, @Phone, @Source, @fileUrl)";

        using (var command = new SQLiteCommand(insertSql, connection, transaction))
        {
            command.Parameters.AddWithValue("@FirstName", firstName);
            command.Parameters.AddWithValue("@LastName", lastName);
            command.Parameters.AddWithValue("@DOB", dob);
            command.Parameters.AddWithValue("@EI_ID", eiId);
            command.Parameters.AddWithValue("@Student_ID", studentId);
            command.Parameters.AddWithValue("@DocID", docId);
            command.Parameters.AddWithValue("@Agency", agency);
            command.Parameters.AddWithValue("@District", district);
            command.Parameters.AddWithValue("@Email", email);
            command.Parameters.AddWithValue("@Phone", phone);
            command.Parameters.AddWithValue("@Source", source);
            command.Parameters.AddWithValue("@fileUrl", fileUrl);

            command.ExecuteNonQuery();
        }

    }



    private static async Task<JObject> SendSharePointRequest(string url, string token)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var response = await httpClient.SendAsync(request);
        if (response.IsSuccessStatusCode)
        {
            string content = await response.Content.ReadAsStringAsync();
            return JObject.Parse(content);
        }
        return null;
    }
    public static void RunCustomQuery(string sql)
    {
        Console.WriteLine();
        Console.WriteLine($"Running custom query: {sql}");
        Console.WriteLine(new string('-', 60));

        using(var command = new SQLiteCommand(sql, connection))
        using (var reader = command.ExecuteReader())
        {
            bool foundAny = false;
            while (reader.Read())
            {
                foundAny = true;

                // Print all columns dynamically
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    Console.Write($"{reader.GetName(i)}: {reader.GetValue(i)}  ");
                }
                Console.WriteLine();
            }

            if (!foundAny)
                Console.WriteLine("No results.");

            Console.WriteLine(new string('-', 60));
        }

    }
    public static void UpdateLocalRow(string fileUrl, Dictionary<string, object> updates)
    {
        if (!File.Exists("indexed_metadata.db"))
            return;

        using (var conn = new SQLiteConnection("Data Source=indexed_metadata.db;Version=3;"))
        {
            conn.Open();
            using (var tx = conn.BeginTransaction())
            {
                foreach (var pair in updates)
                {
                    string field = null;
                    switch (pair.Key)
                    {
                        case "ChildFirstName":
                            field = "FirstName"; break;
                        case "ChildLastName":
                            field = "LastName"; break;
                        case "DOB":
                            field = "dob"; break;
                        case "NYEIS_x0020_ID_x0020__x002d__x0020_NEW":
                            field = "EI_ID"; break;
                        case "Student_x0020_ID_x0020_MCM":
                            field = "Student_ID"; break;
                        case "Foster_x0020_Care_x0020_Agency":
                            field = "Agency"; break;
                        case "District":
                            field = "District"; break;
                        case "Parent_x002d_Guardian_x0020_Email":
                            field = "Email"; break;
                        case "Guardian_x0020_Phone_x0020__x0023_":
                            field = "Phone"; break;
                    }

                    if (field == null) continue;

                    using (var cmd = new SQLiteCommand($"UPDATE Files SET {field} = @val WHERE fileUrl = @url", conn, tx))
                    {
                        cmd.Parameters.AddWithValue("@val", pair.Value ?? DBNull.Value);
                        cmd.Parameters.AddWithValue("@url", fileUrl);
                        cmd.ExecuteNonQuery();
                    }
                }

                tx.Commit();
            }
        }

    }

}
