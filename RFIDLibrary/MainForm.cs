using System;
using System.Data;
using Microsoft.Data.Sqlite;
using System.Windows.Forms;
using Impinj.OctaneSdk;

namespace RFIDLibrary
{
    public partial class MainForm : Form
    {
        private ImpinjReader reader;
        private string connectionString = "Data Source=library.db";
        private string scannedRfidTag;

        public MainForm()
        {
            InitializeComponent();
            InitializeDatabase();
        }

        private void InitializeDatabase()
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string sql = "CREATE TABLE IF NOT EXISTS Books (Id INTEGER PRIMARY KEY, Title TEXT, RfidTag TEXT)";
                var command = new SqliteCommand(sql, conn);
                command.ExecuteNonQuery();
            }
            LoadBooks();
        }

        private void LoadBooks()
        {
            using (var conn = new SqliteConnection(connectionString))
            {
                conn.Open();
                string sql = "SELECT * FROM Books";
                var command = new SqliteCommand(sql, conn);
                using (var reader = command.ExecuteReader())
                {
                    DataTable dt = new DataTable();
                    dt.Load(reader);
                    dataGridViewBooks.DataSource = dt;
                }
            }
        }


        private void btnScan_Click(object sender, EventArgs e)
        {
            reader = new ImpinjReader();
            try
            {
                reader.Connect("hostname"); // Zamijeni "hostname" s IP adresom ili imenom čitača
                Settings settings = reader.QueryDefaultSettings();
                settings.Report.IncludeAntennaPortNumber = true;
                settings.Report.IncludeFirstSeenTime = true;
                settings.Report.IncludePeakRssi = true;
                settings.Report.IncludeSeenCount = true;

                reader.ApplySettings(settings);
                reader.TagsReported += OnTagsReported;
                reader.Start();
            }
            catch (OctaneSdkException ex)
            {
                MessageBox.Show("Error connecting to reader: " + ex.Message);
            }
        }

        private void OnTagsReported(ImpinjReader sender, TagReport report)
        {
            foreach (Tag tag in report)
            {
                scannedRfidTag = tag.Epc.ToString();
                // Prikazivanje pronađenih tagova u MessageBox
                MessageBox.Show($"RFID Tag: {scannedRfidTag}");
            }
            reader.Stop();
            reader.Disconnect();
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtTitle.Text) && !string.IsNullOrEmpty(scannedRfidTag))
            {
                using (var conn = new SqliteConnection(connectionString))
                {
                    conn.Open();
                    string sql = "INSERT INTO Books (Title, RfidTag) VALUES (@Title, @RfidTag)";
                    var command = new SqliteCommand(sql, conn);
                    command.Parameters.AddWithValue("@Title", txtTitle.Text);
                    command.Parameters.AddWithValue("@RfidTag", scannedRfidTag);
                    command.ExecuteNonQuery();
                }
                LoadBooks();
            }
            else
            {
                MessageBox.Show("Unesi naslov knjige i skeniraj RFID tag.");
            }
        }
    }
}
