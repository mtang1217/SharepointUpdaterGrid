using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Data.SQLite;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Drawing;
using System.Drawing.Text;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ListView;
using System.IO;

namespace SharepointUpdaterGrid
{
    public partial class Form1 : Form
    {
        private DataTable originalTable;
        private DataTable workingTable;
        public Form1()
        {
            InitializeComponent();
            filePathText.Text = "/sites/eidtsuat/Shared Documents/Email attachments/CPSE 01";
            firstName.Text = "Three";
            lastName.Text = "Test Child Three";
            DOB.Text = "2022-09-01";
            EIID.Text = "765432";
            StudentID.Text = "333444555";
            progressBar1.Minimum = 0;
            progressBar1.Value = 0;
            progressBar1.Step = 1;
        }

        public void ConfigureDataGridView()
        {
            //Customization
            dataGridView1.EnableHeadersVisualStyles = false;
            dataGridView1.ColumnHeadersDefaultCellStyle.BackColor = Color.Navy;
            dataGridView1.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            dataGridView1.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 10, FontStyle.Bold);

            /*
            //Column Labels
            dataGridView1.Columns.Add("dID", "Doc ID");
            dataGridView1.Columns.Add("sID", "Student ID");
            dataGridView1.Columns.Add("eiID", "EI ID");
            dataGridView1.Columns.Add("fca", "Foster Care Agency");
            dataGridView1.Columns.Add("district", "District");
            dataGridView1.Columns.Add("gEmail", "Guard Email");
            dataGridView1.Columns.Add("gPhone", "Guard Phone");

            dataGridView1.Rows.Add("1", "2", "3", "test", "test", "email", "phone");
            */
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }

        private void DOB_TextChanged(object sender, EventArgs e)
        {

        }

        private void DOBLabel_Click(object sender, EventArgs e)
        {

        }

        private void lastName_TextChanged(object sender, EventArgs e)
        {

        }

        private void lastNameLabel_Click(object sender, EventArgs e)
        {

        }

        private void firstName_TextChanged(object sender, EventArgs e)
        {

        }

        private void firstNameLabel_Click(object sender, EventArgs e)
        {

        }

        private async void search_Click(object sender, EventArgs e)
        {
            try
            {
                SetButtonsEnabled(false);

                // Your existing code here
                string filePath = filePathText.Text.Trim();
                string fName = firstName.Text.Trim();
                string lName = lastName.Text.Trim();
                string dob = DOB.Text.Trim();
                string eiID = EIID.Text.Trim();
                string studentID = StudentID.Text.Trim();
                if (File.Exists("indexed_metadata.db"))
                {
                    LoadFromDatabase(fName, lName, dob, eiID, studentID);
                }
                else
                {
                    if (string.IsNullOrWhiteSpace(fName) || string.IsNullOrWhiteSpace(lName) || string.IsNullOrWhiteSpace(dob) || string.IsNullOrWhiteSpace(filePath))
                    {
                        MessageBox.Show("Please fill out all fields.");
                        return;
                    }
                    else
                    {
                        if (!DateTime.TryParse(DOB.Text.Trim(), out var parsedDob))
                        {
                            MessageBox.Show("DOB format should be yyyy-MM-dd");
                            return;
                        }
                        dob = parsedDob.ToString("yyyy-MM-dd");

                    }
                    await LoadData(fName, lName, dob, filePath);
                }
                
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private async Task LoadData(string firstName, string lastName, string dob, string filePath)
        {
            DateTime dt = DateTime.Now;
            statusLabel.Text = "🔄 Searching SharePoint...";
            statusLabel.ForeColor = Color.DarkGoldenrod;
            
            dataGridView1.DataSource = null;

            string fullName = $"{firstName} {lastName}";
            var client = new SharePointApiClient(
                clientId: ConfigLoader.Config["SharepointApiConfig:Configuration:appReg_clientId"],
                clientSecret: ConfigLoader.Config["SharepointApiConfig:Configuration:appReg_clientSecret"],
                realm: ConfigLoader.Config["SharepointApiConfig:Configuration:realm"],
                host: ConfigLoader.Config["SharepointApiConfig:Configuration:targetHost"],
                //siteFolder: "/sites/eidtsuat/Shared Documents/Email attachments/CPSE 01",
                siteFolder: filePath,
                expectedName: fullName,
                expectedDob: dob
            );

            DataTable result;
            try
            {
                //DateTime dt = DateTime.Now;
                result = await client.GetFilteredMetadataTableAsync();
                //TimeSpan ts = DateTime.Now.Subtract(dt);
                //MessageBox.Show(ts.TotalSeconds.ToString());
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error contacting SharePoint: " + ex.Message);
                statusLabel.Text = "❌ Error fetching data.";
                statusLabel.ForeColor = Color.Red;
                return;
            }

            TimeSpan ts = DateTime.Now.Subtract(dt);
            timeLabel.Text = $"Fetched list in {ts.TotalSeconds:F2} sec";

            originalTable = result;
            workingTable = originalTable.Copy();

            ConfigureDataGridView();
            dataGridView1.DataSource = workingTable;
            if (dataGridView1.Columns.Count > 0)
            {
                dataGridView1.Columns[0].ReadOnly = true;
                dataGridView1.Columns[7].ReadOnly = true;
                dataGridView1.Columns[8].ReadOnly = true;
            }

            if (workingTable.Rows.Count == 0)
            {
                //MessageBox.Show("No matching records found.");
                statusLabel.Text = "⚠ No records found.";
                statusLabel.ForeColor = Color.DarkOrange;
            }
            else
            {
                //MessageBox.Show("Results loaded successfully.");
                statusLabel.Text = $"✅ {workingTable.Rows.Count} record(s) found.";
                statusLabel.ForeColor = Color.Green;
            }
        }

        private void cancel_Click(object sender, EventArgs e)
        {
            workingTable = originalTable.Copy();
            dataGridView1.DataSource = workingTable;
        }

        private async void submit_Click(object sender, EventArgs e)
        {
            try
            {
                if (originalTable == null || workingTable == null)
                {
                    MessageBox.Show("No data loaded.");
                    return;
                }

                if (originalTable.PrimaryKey.Length == 0 && originalTable.Columns.Contains("FileUrl"))
                    originalTable.PrimaryKey = new[] { originalTable.Columns["FileUrl"] };

                var client = new SharePointApiClient(
                    clientId: ConfigLoader.Config["SharepointApiConfig:Configuration:appReg_clientId"],
                    clientSecret: ConfigLoader.Config["SharepointApiConfig:Configuration:appReg_clientSecret"],
                    realm: ConfigLoader.Config["SharepointApiConfig:Configuration:realm"],
                    host: ConfigLoader.Config["SharepointApiConfig:Configuration:targetHost"],
                    siteFolder: filePathText.Text.Trim(),
                    expectedName: $"{firstName.Text} {lastName.Text}",
                    expectedDob: DOB.Text.Trim()
                );

                int successCount = 0;
                int failCount = 0;
                DataTable updatedCopy = originalTable.Copy();

                progressBar1.Minimum = 0;
                progressBar1.Value = 0;
                progressBar1.Maximum = workingTable.Rows.Count;

                //SetButtonsEnabled(false); // optional UI locking

                for (int i = 0; i < workingTable.Rows.Count; i++)
                {
                    DataRow newRow = workingTable.Rows[i];
                    string fileUrl = newRow["FileUrl"]?.ToString();
                    if (string.IsNullOrEmpty(fileUrl)) continue;

                    DataRow oldRow = originalTable.Rows.Find(fileUrl);
                    if (oldRow == null) continue;

                    var payload = new Dictionary<string, object>();
                    if (!Equals(oldRow["First Name"], newRow["First name"]))
                        payload["ChildFirstName"] = newRow["First Name"];

                    if (!Equals(oldRow["Last Name"], newRow["Last name"]))
                        payload["ChildLastName"] = newRow["Last Name"];

                    if (!Equals(oldRow["DOB"], newRow["DOB"]))
                        payload["DOB"] = newRow["DOB"];

                    if (!Equals(oldRow["Student ID"], newRow["Student ID"]))
                        payload["Student_x0020_ID_x0020_MCM"] = newRow["Student ID"];

                    if (!Equals(oldRow["EI ID"], newRow["EI ID"]))
                        payload["NYEIS_x0020_ID_x0020__x002d__x0020_NEW"] = newRow["EI ID"];

                    if (!Equals(oldRow["Foster Care Agency"], newRow["Foster Care Agency"]))
                        payload["Foster_x0020_Care_x0020_Agency"] = newRow["Foster Care Agency"];

                    if (!Equals(oldRow["District"], newRow["District"]))
                        payload["District"] = newRow["District"];

                    if (!Equals(oldRow["Guard Email"], newRow["Guard Email"]))
                        payload["Parent_x002d_Guardian_x0020_Email"] = newRow["Guard Email"];

                    if (!Equals(oldRow["Guard Phone"], newRow["Guard Phone"]))
                        payload["Guardian_x0020_Phone_x0020__x0023_"] = newRow["Guard Phone"];

                    if (payload.Count == 0) continue;

                    bool success = await client.UpdateFileMetadataAsync(fileUrl, payload);
                    DataGridViewRow gridRow = dataGridView1.Rows[i];

                    if (success)
                    {
                        successCount++;
                        gridRow.DefaultCellStyle.BackColor = Color.LightGreen;

                        DataRow updatedRow = updatedCopy.Rows.Find(fileUrl);
                        if (updatedRow != null)
                        {
                            foreach (DataColumn col in workingTable.Columns)
                                updatedRow[col.ColumnName] = newRow[col.ColumnName];
                        }

                        SharePointIndexer.UpdateLocalRow(fileUrl, payload);
                    }
                    else
                    {
                        failCount++;
                        gridRow.DefaultCellStyle.BackColor = Color.LightCoral;
                    }

                    progressBar1.Value = i + 1;
                }

                //SetButtonsEnabled(true);
                originalTable = updatedCopy.Copy();

                if (failCount == 0)
                    MessageBox.Show($"✅ {successCount} record(s) updated in SharePoint.");
                else
                    MessageBox.Show($"✅ {successCount} updated.\n❌ {failCount} failed.");
                progressBar1.Value = progressBar1.Maximum;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"❌ Error submitting changes: {ex.Message}");
                //SetButtonsEnabled(true);
            }

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void timeLabel_Click(object sender, EventArgs e)
        {

        }

        private async void indexerClick_Click(object sender, EventArgs e)
        {
            try
            {
                SetButtonsEnabled(false);

                DateTime dt = DateTime.Now;
                statusLabel.Text = "🔄 Creating local index...";
                statusLabel.ForeColor = Color.DarkSlateBlue;

                string dbPath = "indexed_metadata.db";
                await SharePointIndexer.IndexAllAsync(dbPath, progressBar1, timeLabel);

                statusLabel.Text = $"✅ {SharePointIndexer.FileCount} files processed.";
                statusLabel.ForeColor = Color.SeaGreen;
                TimeSpan ts = DateTime.Now - dt;
                timeLabel.Text = $"Created Indexer in {ts.TotalSeconds:F2} sec";
            }
            finally
            {
                SetButtonsEnabled(true);
            }
        }

        private void LoadFromDatabase(string firstName, string lastName, string dob, string eiID, string studentID)
        {
            DateTime dt = DateTime.Now;
            statusLabel.Text = "🔄 Searching local index...";
            statusLabel.ForeColor = Color.DarkSlateBlue;
            dataGridView1.DataSource = null;
            string dbPath = "indexed_metadata.db";
            if (!File.Exists(dbPath))
            {
                MessageBox.Show("Local metadata index not found.");
                statusLabel.Text = "❌ No local index found.";
                statusLabel.ForeColor = Color.Red;
                return;
            }
            try
            {
                using (var conn = new SQLiteConnection($"Data Source={dbPath};Version=3;"))
                {
                    conn.Open();
                    string sql = BuildSqlQuery(firstName, lastName, dob, eiID, studentID);
                    using (var command = new SQLiteCommand(sql, conn))
                    using (var reader = command.ExecuteReader())
                    {
                        DataTable result = new DataTable();

                        // Create the expected columns
                        result.Columns.Add("Doc ID");
                        result.Columns.Add("First Name");
                        result.Columns.Add("Last Name");
                        result.Columns.Add("DOB");
                        result.Columns.Add("Student ID");
                        result.Columns.Add("EI ID");
                        result.Columns.Add("Foster Care Agency");
                        result.Columns.Add("District");
                        result.Columns.Add("Guard Email");
                        result.Columns.Add("Guard Phone");
                        result.Columns.Add("Source");
                        result.Columns.Add("FileUrl");

                        // Populate the data
                        while (reader.Read())
                        {
                            var row = result.NewRow();
                            row["Doc ID"] = reader["DocID"] ?? "";
                            row["First Name"] = reader["FirstName"] ?? "";
                            row["Last Name"] = reader["LastName"] ?? "";
                            row["DOB"] = reader["dob"] ?? "";
                            row["Student ID"] = reader["Student_ID"] ?? "";
                            row["EI ID"] = reader["EI_ID"] ?? "";
                            row["Foster Care Agency"] = reader["Agency"] ?? "";
                            row["District"] = reader["District"] ?? "";
                            row["Guard Email"] = reader["Email"] ?? "";
                            row["Guard Phone"] = reader["Phone"] ?? "";
                            row["Source"] = reader["Source"] ?? "";
                            row["FileUrl"] = reader["fileUrl"] ?? "";
                            result.Rows.Add(row);
                        }

                        TimeSpan ts = DateTime.Now - dt;
                        timeLabel.Text = $"Fetched from DB in {ts.TotalSeconds:F2} sec";
                        originalTable = result;
                        workingTable = originalTable.Copy();
                        ConfigureDataGridView();
                        dataGridView1.DataSource = workingTable;
                        
                        if (workingTable.Rows.Count == 0)
                        {
                            statusLabel.Text = "⚠ No records found.";
                            statusLabel.ForeColor = Color.DarkOrange;
                        }
                        else
                        {
                            dataGridView1.Columns[0].ReadOnly = true;
                            dataGridView1.Columns[10].ReadOnly = true;
                            dataGridView1.Columns[11].ReadOnly = true;
                            statusLabel.Text = $"✅ {workingTable.Rows.Count} record(s) found.";
                            statusLabel.ForeColor = Color.SeaGreen;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error querying local database:\n{ex.Message}");
                statusLabel.Text = "❌ Local DB query failed.";
                statusLabel.ForeColor = Color.Red;
            }
        }
        private string BuildSqlQuery(string firstName, string lastName, string dob, string eiID, string studentID)
        {
            List<string> conditions = new List<string>();

            if (!string.IsNullOrWhiteSpace(firstName))
                conditions.Add($"\"FirstName\" LIKE '%{firstName.Replace("'", "''")}%'");

            if (!string.IsNullOrWhiteSpace(lastName))
                conditions.Add($"\"LastName\" LIKE '%{lastName.Replace("'", "''")}%'");

            if (!string.IsNullOrWhiteSpace(dob))
                conditions.Add($"\"DOB\" LIKE '%{dob.Replace("'", "''")}%'");

            if (!string.IsNullOrWhiteSpace(eiID))
                conditions.Add($"\"EI_ID\" LIKE '{eiID.Replace("'", "''")}'");

            if (!string.IsNullOrWhiteSpace(studentID))
                conditions.Add($"\"Student_ID\" LIKE '{studentID.Replace("'", "''")}'");

            string whereClause = conditions.Count > 0 ? "WHERE " + string.Join(" AND ", conditions) : "";
            //NAME OF TABLE IS "Files"
            return $"SELECT * FROM Files {whereClause}";
        }
        private void SetButtonsEnabled(bool enabled)
        {
            search.Enabled = enabled;
            indexerClick.Enabled = enabled;
            cancel.Enabled = enabled;
            submit.Enabled = enabled;
        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }
    }
}
