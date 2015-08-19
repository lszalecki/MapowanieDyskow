using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
//using System.Threading.Tasks;
using System.Windows.Forms;
using aejw.Network;
using IniParser;
using IniParser.Model;
using System.Threading;

namespace MapowanieDyskow
{
    public partial class Form1 : Form
    {

        private FileIniDataParser parser = null;
        private IniData parsedData;
        private KeyDataCollection keyCol;
        private KeyDataCollection defaultCol;
        private KeyDataCollection version;
        private KeyDataCollection domain;
        private string configVersion;
        private string domainName;

        public Form1()
        {
            InitializeComponent();

            listView1.View = View.Details;
            listView1.Columns.Add("Dyski sieciowe:", 100);

            //Create an instance of a ini file parser
            parser = new FileIniDataParser();
            parser.Parser.Configuration.CommentString = "#";

            String configINI = Environment.CurrentDirectory + "\\config.ini";
            if (configINI == null)
            {
                
                richTextBoxLog.SelectionFont = new Font(richTextBoxLog.SelectionFont, FontStyle.Bold);
                richTextBoxLog.AppendText(DateTime.Now+": "+"Brak pliku konfiguracyjnego" + Environment.NewLine);
                return;
            }
            else
            {
                richTextBoxLog.SelectionFont = new Font(richTextBoxLog.SelectionFont, FontStyle.Bold);
                richTextBoxLog.AppendText(DateTime.Now + ": " + "Plik konfiguracyjny załadowany" + Environment.NewLine);
            }

            parsedData = parser.ReadFile(configINI);

            defaultCol = parsedData["default"];

            version = parsedData["wersja"];
            configVersion = version["Ver"];
            this.Text = this.Text + ": " + configVersion;

            domain = parsedData["domena"];
            domainName = domain["nazwa_domeny"];

            //This line gets the SectionData from the section "global"
            keyCol = parsedData["dyski_sieciowe"];

            foreach(KeyData keyData in keyCol){
                          
                if (defaultCol[keyData.KeyName] != null && defaultCol[keyData.KeyName].Equals("true"))
                {
                    ListViewItem item = new ListViewItem(keyData.KeyName.ToUpper());
                    item.Checked = true;
                    listView1.Items.Add(item);
                }
                else
                {
                    listView1.Items.Add(keyData.KeyName.ToUpper());
                }
               
            }

            

        }

        private void buttonMapuj_Click(object sender, EventArgs e)
        {

            mapujDyski();

        }


        public void UpdateProgressBar(int value)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<int>(UpdateProgressBar), new object[] { value });
                return;
            }
            toolStripProgressBar1.Value = value;
        }


        #region DisplayData
        /// <summary>
        /// method to display the data to & from the port
        /// on the screen
        /// </summary>
        /// <param name="type">MessageType of the message</param>
        /// <param name="msg">Message to display</param>
        [STAThread]
        private void DisplayData(string msg)
        {
            try
            {
                richTextBoxLog.Invoke(new EventHandler(delegate
                {
                    richTextBoxLog.SelectedText = string.Empty;
                    richTextBoxLog.SelectionFont = new Font(richTextBoxLog.SelectionFont, FontStyle.Bold);

                    richTextBoxLog.AppendText(msg + Environment.NewLine);
                    richTextBoxLog.ScrollToCaret();
                }));

            }
            catch (InvalidOperationException ie)
            {
                Console.WriteLine(ie);
            }

        }
        #endregion

        private void toolStripMenuItemClose_Click(object sender, EventArgs e)
        {
            this.Close();
            this.Dispose();
        }


        private void mapujDyski()
        {
            NetworkDrive drive;
            if (!String.IsNullOrEmpty(textBoxUsername.Text) && !String.IsNullOrEmpty(textBoxPassword.Text))
            {

                toolStripProgressBar1.Maximum = listView1.Items.Count;

                int count = 0;
                foreach (ListViewItem item in listView1.Items)
                {
                    if (item.Checked)
                    {
                        drive = new NetworkDrive();
                        drive.Force = true;
                        drive.LocalDrive = item.Text.ToLower() + ":";
                        string shareName = null;
                        if (keyCol[item.Text.ToLower()].Contains("*user*"))
                        {
                            shareName = keyCol[item.Text.ToLower()].Replace("*user*", textBoxUsername.Text);
                        }
                        else
                        {
                            shareName = keyCol[item.Text.ToLower()];
                        }
                        drive.ShareName = shareName;

                        try
                        {
                            drive.UnMapDrive();
                            DisplayData(DateTime.Now + ": " + "Dysk " + item.Text.ToUpper() + " został odmapowany poprawnie.");

                        }
                        catch (Exception ee)
                        {
                            Console.WriteLine(ee);
                        }


                        try
                        {
                            if (checkBoxDomena.Checked)
                            {
                                drive.MapDrive(domainName + "\\" + textBoxUsername.Text, textBoxPassword.Text);
                                DisplayData(DateTime.Now + ": " + "Dysk " + item.Text.ToUpper() + " został zmapowany poprawnie.");
                            }
                            else
                            {
                                drive.MapDrive(textBoxUsername.Text, textBoxPassword.Text);
                                DisplayData(DateTime.Now + ": " + "Dysk " + item.Text.ToUpper() + " został zmapowany poprawnie.");
                            }


                        }
                        catch (Exception ex)
                        {
                            // MessageBox.Show(this, "Error: " + ex.Message);                   
                            Console.WriteLine(ex);
                            DisplayData(DateTime.Now + ": " + ex.Message);
                        }

                    }

                    count++;
                    UpdateProgressBar(count);
                }

                DisplayData(DateTime.Now + ": " + " Zakończono.");

            }
            else
            {
                if (String.IsNullOrEmpty(textBoxUsername.Text) && String.IsNullOrEmpty(textBoxPassword.Text))
                {
                    MessageBox.Show("Wprowadź login i hasło.");

                }
                else if (!String.IsNullOrEmpty(textBoxUsername.Text) && String.IsNullOrEmpty(textBoxPassword.Text))
                {
                    MessageBox.Show("Wprowadź hasło.");
                }
                else
                {
                    MessageBox.Show("Wprowadź login.");
                }
            }
        }

        private void textBoxPassword_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (Char)Keys.Enter)
            {
                mapujDyski();
            }
        }

        private void textBoxUsername_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (Char)Keys.Enter)
            {
                mapujDyski();
            }
        }



    }
}
