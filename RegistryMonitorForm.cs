using Microsoft.Win32;
using System;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using System.Collections.Generic;

namespace RegistryMonitor
{
    public partial class Form1 : Form
    {
        private static List<string> registryChanges = new List<string>();
        private static NotifyIcon trayIcon;
        private static ContextMenu trayMenu;
        private static RegistryKey key;

        public Form1()
        {
            InitializeComponent();

            // Create a tray icon and menu
            trayMenu = new ContextMenu();
            trayMenu.MenuItems.Add("Open", OnOpen);
            trayMenu.MenuItems.Add("Exit", OnExit);
            trayIcon = new NotifyIcon();
            trayIcon.Text = "Registry Monitor";
            trayIcon.Icon = new Icon(SystemIcons.Application, 40, 40);
            trayIcon.ContextMenu = trayMenu;
            trayIcon.Visible = true;

            // Open the registry key and register for change notifications
            key = Registry.LocalMachine.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);
            key.NotifyOfChanges();

            // Start a new thread to monitor the registry key for changes
            Thread registryMonitorThread = new Thread(RegistryMonitor);
            registryMonitorThread.Start();
        }

        private void RegistryMonitor()
        {
            while (true)
            {
                // Wait for a change notification
                key.WaitForChanged();

                // Get all the value names for the key
                string[] valueNames = key.GetValueNames();

                // Check each value for changes
                foreach (string valueName in valueNames)
                {
                    object newValue = key.GetValue(valueName);
                    registryChanges.Add(string.Format("[{0}] Changed key: {1}, value_name: {2}, new_value: {3}",
                        DateTime.Now.ToString(), key.ToString(), valueName, newValue.ToString()));

                    // Display a Windows notification
                    trayIcon.ShowBalloonTip(3000, "Registry Change",
                        string.Format("Changed key: {0}\nValue name: {1}\nNew value: {2}", key.ToString(), valueName, newValue.ToString()),
                        ToolTipIcon.Info);
                }
                dataGridView1.Invoke((MethodInvoker)delegate
                {
                    dataGridView1.DataSource = registryChanges.Select(x => new { Changes = x }).ToList();
                });

            }
        }

        private void OnOpen(object sender, EventArgs e)
        {
            this.Show();
        }

        private void OnExit(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.Hide();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
            }
        }

        private void dataGridView1_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex >= 0)
            {
                var row = dataGridView1.Rows[e.RowIndex];
                txtLabel.Text = row.Cells[0].Value.ToString();
            }
        }

        private void btnLabel_Click(object sender, EventArgs e)
        {
            if (!string.IsNullOrEmpty(txtLabel.Text))
            {
                string selectedChanges = txtLabel.Text;
                string label = txtLabel.Text;
                int selectedIndex = registryChanges.FindIndex(x => x.Equals(selectedChanges));
                registryChanges[selectedIndex] = label;
                dataGridView1.DataSource = registryChanges.Select(x => new { Changes = x }).ToList();
                txtLabel.Text = "";
            }
        }
    }
}
