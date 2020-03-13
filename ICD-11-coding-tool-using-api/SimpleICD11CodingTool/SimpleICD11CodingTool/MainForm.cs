using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Net.Http.Headers;
using System.IO;
using ICD11CodingTool.Controller;
using ICD11CodingTool.Models;
using Newtonsoft.Json.Linq;
using System.Configuration;

namespace ICD11CodingTool
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private string input_term;

        public MainForm(string term)
        {
            InitializeComponent();

            input_term = term;

          
           

        }

        public static string ReplaceHtmlTag(string html, int length = 0)
        {
            string strText = System.Text.RegularExpressions.Regex.Replace(html, "<[^>]+>", "");
            strText = System.Text.RegularExpressions.Regex.Replace(strText, "&[^;]+;", "");

            if (length > 0 && strText.Length > length)
                return strText.Substring(0, length);

            return strText;
        }

        ICD11ApiClient apiClient = null;

        private async void btnSearch_Click(object sender, EventArgs e)
        {

            Configuration config = System.Configuration.ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);

            if (string.IsNullOrEmpty(config.AppSettings.Settings["clientId"].Value))
            {
                MessageBox.Show("You did not set clientId and clientSecret key-value in AppSettings from App.Config file. Please set your clientId and clientSecret, which can be obtained from ICD-11 API Home Page!");
                return;
            }

            if (txtInput.Text.Trim()=="")
            {
             //   MessageBox.Show("Please input at least one term");
                return;
            }
            txtInput.Text = txtInput.Text.Trim();

            string q_chapters = "";
            for(int i = 0; i < clChapter.Items.Count; i++)
            {
                if (clChapter.GetItemChecked(i))
                {
                    q_chapters += chlist[i].ChapterNo + ";";
                }
            }

            tvResult.Nodes.Clear();
            btnSearch.Text = "Searching...";
            btnSearch.Enabled = false;
            // apiClient.Authorize();
            // apiClient.SearchTerm("liver diseases");
            apiClient = new ICD11ApiClient();

            List<ICD11Entity> icd11list = await apiClient.GetResult(txtInput.Text,q_chapters);

            // begin filter chapter

            FilterChapater(icd11list, q_chapters);

            ///end filter


            //begin sort
            icd11list.Sort((x, y) => { return y.Score.CompareTo(x.Score); });

            ///end sort

            for (int i = 0; i < icd11list.Count; i++)
            {
                Console.WriteLine(icd11list[i].Title);
                string[] row = { icd11list[i].Code, icd11list[i].Title };
                TreeNode tn = new TreeNode(" - " + icd11list[i].Code + " " + ReplaceHtmlTag(icd11list[i].Title));
                tn.ForeColor = Color.OrangeRed;
                tn.Tag = icd11list[i].Data;

                tvResult.Nodes.Add(tn);



                for (int j = 0; j < icd11list[i].PVList.Count; j++)
                {
                    PV subentity = icd11list[i].PVList[j];
                    TreeNode n1 = new TreeNode(ReplaceHtmlTag(subentity.Label));
                    n1.Tag = subentity.Data;
                    tn.Nodes.Add(n1);


                }

                for (int j = 0; j < icd11list[i].Children.Count; j++)
                {
                    ICD11Entity subentity = icd11list[i].Children[j];


                    TreeNode tn1 = new TreeNode(" - " + subentity.Code + " " + ReplaceHtmlTag(subentity.Title));
                    tn1.ForeColor = Color.OrangeRed;
                    tn1.Tag = subentity.Data;

                    tn.Nodes.Add(tn1);


                }


            }

            tvResult.ExpandAll();
            if (tvResult.Nodes.Count > 0)
            {
                tvResult.SelectedNode = tvResult.Nodes[0];
            }

            btnSearch.Text = "Search";
            btnSearch.Enabled = true;

            List<WordCandidate> wlist = await apiClient.GetWordList(txtInput.Text,q_chapters);
            lbWord.Items.Clear();
            for (int i = 0; i < wlist.Count; i++)
            {
                lbWord.Items.Add(wlist[i].Label);
            }
          
            for(int i = 0; i < chlist.Count; i++)
            {
                int count = GetChapterCount(icd11list, chlist[i].ChapterNo);

                chlist[i].Freq = count;

           
            }

            //begin sort
            chlist.Sort((x, y) => { return y.Freq.CompareTo(x.Freq); });

            clChapter.Items.Clear();
 
               for (int i = 0; i < chlist.Count; i++)
            {
                clChapter.Items.Add(chlist[i].ChapterName + " (" + chlist[i].Freq + ")");

                if (apiClient.ChapterList != null)
                {
                    if (apiClient.ChapterList.Contains(chlist[clChapter.Items.Count-1].ChapterNo))
                    {
                        clChapter.SetItemChecked(clChapter.Items.Count - 1, true);
                    }
                    else
                    {
                        clChapter.SetItemChecked(clChapter.Items.Count - 1, false);
                    }
                }
                else
                {
                    clChapter.SetItemChecked(clChapter.Items.Count - 1, false);
                }
            }

            ///end sort


        }

        private void  FilterChapater(List<ICD11Entity> icd11list, string q_chapter)
        {
            if (icd11list == null || icd11list.Count <= 0)
                return;

            string[] qs = q_chapter.Substring(0, q_chapter.Length - 1).Split(';');

         
            for (int i = 0; i < icd11list.Count; i++)
            {
                if (!qs.Contains(icd11list[i].Chapter))
                {
                    icd11list.RemoveAt(i);
                    i--;
                }
                else
                {
                    FilterChapater(icd11list[i].Children, q_chapter);
                }
            }
           
        }

        private int GetChapterCount(List<ICD11Entity> icd11list,string chapterNo)
        {
            int count = 0;
            if (icd11list == null)
                return 0;
           for(int i = 0; i < icd11list.Count; i++)
            {
                if (icd11list[i].Chapter == chapterNo)
                    count += GetChapterCount(icd11list[i].Children, chapterNo) + 1;
            }
            return  count;
        }

        private void tvResult_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            TreeNode stn = e.Node;
            dynamic data = (dynamic)stn.Tag;

            txtSelectedCode.Text = "";

            if (e.Node.Text.StartsWith(" - "))
            {
                // sub node

                lvEntity.Columns.Clear();
                lvEntity.Items.Clear();
                lvEntity.Columns.Add("Property", 100);
                lvEntity.Columns.Add("Value", 500);
                JObject jObject = JObject.Parse(data.ToString());
                foreach (JProperty p in jObject.Properties())
                {
                    string[] valid_rows = new string[] { "Value", "Title", "Id", "Chapter", "TheCode", "Score", "IsLeaf", "StermId", "Depth" };
                    if (valid_rows.Contains(p.Name))
                    {
                        string[] rows = new string[] { p.Name, ReplaceHtmlTag(p.Value.ToString()) };
                        ListViewItem lvitem = new ListViewItem(rows);
                        lvEntity.Items.Add(lvitem);
                        if (p.Name == "TheCode")
                        {
                            txtSelectedCode.Text = p.Value.ToString();
                            lbDetails.Text = "Selected ICD Entity: [" + txtSelectedCode.Text+"]";
                        }
                        if (p.Name == "Title")
                        {
                            txtSelectedName.Text = ReplaceHtmlTag( p.Value.ToString());
                        }
                    }

                }
                txtInfo.Text = txtInput.Text;
               
               
            }
            else
            {
                //alternate name

                lvEntity.Columns.Clear();
                lvEntity.Items.Clear();
                lvEntity.Columns.Add("Property", 100);
                lvEntity.Columns.Add("Value", 500);
                JObject jObject = JObject.Parse(data.ToString());
                foreach (JProperty p in jObject.Properties())
                {
                    string[] valid_rows = new string[] { "PropertyId", "Label", "Score" };
                    if (valid_rows.Contains(p.Name))
                    {
                        string[] rows = new string[] { p.Name, ReplaceHtmlTag(p.Value.ToString()) };
                        ListViewItem lvitem = new ListViewItem(rows);
                        lvEntity.Items.Add(lvitem);
                        
                    }

                   
                }

                //dynamic theEntity = data;


                JObject jObject1 = JObject.Parse(data.ToString());
                foreach (JProperty p1 in jObject1.Properties())
                {
                    string[] valid_rows1 = new string[] { "Value", "Depth", "Id", "PropertyName", "Title", "Code", "Chapter", "IsResidualOtehr", "IsResidualUnspecified" };
                    if (valid_rows1.Contains(p1.Name))
                    {
                        string[] rows = new string[] { p1.Name, ReplaceHtmlTag(p1.Value.ToString()) };
                        ListViewItem lvitem = new ListViewItem(rows);
                        lvEntity.Items.Add(lvitem);
                        if (p1.Name == "Code")
                        {
                            txtSelectedCode.Text = p1.Value.ToString();
                            lbDetails.Text = "Matching PVs: [" + txtSelectedCode.Text + "]";

                        }
                        if (p1.Name == "Title")
                        {
                            txtSelectedName.Text = ReplaceHtmlTag( p1.Value.ToString());
                        }
                    }
                }

                txtInfo.Text = txtInput.Text;


            }
        

  

        }

        private void txtInput_TextChanged(object sender, EventArgs e)
        {
            
            btnSearch_Click(null, null);
        }

        private void label5_Click(object sender, EventArgs e)
        {

        }

        private void btnAssign_Click(object sender, EventArgs e)
        {
            if (txtSelectedCode.Text == "")
            {
                MessageBox.Show("The selected code should not be empty!");
                return;
            }
            ListViewItem lvi = new ListViewItem(new string[] { txtSelectedCode.Text,txtSelectedName.Text,txtInfo.Text });
            lvCodeList.Items.Add(lvi);
        }

    

        public class Chapter
        {
            public string ChapterNo { get; set; }
            public string ChapterName { get; set; }
            public int Freq { get; set; }
        }
        List<Chapter> chlist = new List<Chapter>();
        private void MainForm_Load(object sender, EventArgs e)
        {
             
            lvCodeList.Columns.Add("Code",100);
            lvCodeList.Columns.Add("Code Name",100);
            lvCodeList.Columns.Add("Corresponding Info", 300);
            string[] c_code;
            string[] c_desc;
            //show chapter list
            c_desc = new string[]
         {
               "Infections",
             "Neoplasms",
             "Blood",
            "Immune system",
            "Endocrine, nutritional, metabolic",
            "Mental and behavioural",
            "Sleep-wake",
            "Nervous system",
             "Eye and adnexa",
             "Ear and mastoid",
            "Circulatory system",
           "Respiratory system",
            "Digestive system",
           "Skin",
            "Musculoskeletal system ...",
            "Genitourinary System",
            "Sexual health",
            "Pregnancy, childbirth ...",
            "Perinatal and neonatal",
           "Developmental anomalies",
            "Symptoms, signs, findings ...",
           "Injury, poisoning, ...",
         "External causes",
            "Factors influencing health ...",
          "Codes for special purposes",
            "Traditional Medicine",
           "Functioning",
            "Extension Codes"
        };
         c_code = new string[28];
            for (int i = 0; i < c_code.Length; i++)
            {
                string ii = (i+1) + "";
                if (ii.Length == 1)
                    ii = "0" + ii;

                if (i == 26)
                    ii = "V";
                if (i == 27)
                    ii = "X";

                c_code[i] = ii;
            }

         
            for(int i = 0; i < c_code.Length; i++)
            {
                Chapter c = new Chapter();
                c.ChapterName = c_desc[i];
                c.ChapterNo = c_code[i];
                c.Freq = 0;
                chlist.Add(c);
            }

            for(int i = 0; i < chlist.Count; i++)
            {
                clChapter.Items.Add(chlist[i].ChapterName);
            }

            for(int i = 0; i < chlist.Count;i++) 
            {
                clChapter.SetItemChecked(i, true);
                if (i >= chlist.Count - 3)
                {
                    clChapter.SetItemChecked(i, false);
                }
            }

            if (!string.IsNullOrEmpty(input_term))
            {
                txtInput.Text = input_term;
                btnSearch_Click(null, null);
            }
            

        }

        private void btnMinus_Click(object sender, EventArgs e)
        {
            if (lvCodeList.SelectedItems == null || lvCodeList.SelectedItems.Count <= 0)
            {
                MessageBox.Show("Please selected at least one item from the list");
                return;
            }
            lvCodeList.Items.Remove(lvCodeList.SelectedItems[0]);
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            string codes = "";
            for(int i = 0; i < lvCodeList.Items.Count; i++)
            {
                if (i != lvCodeList.Items.Count - 1)
                {
                    codes += lvCodeList.Items[i].SubItems[0].Text+",";
                }
                else
                {
                    codes += lvCodeList.Items[i].SubItems[0].Text ;
                }
            }
            MessageBox.Show("The assigned code for this record is: " + codes);
        }

        private void btnClearAll_Click(object sender, EventArgs e)
        {
            lvCodeList.Items.Clear();
        }

        private void lbWord_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            txtInput.Text += " "+lbWord.SelectedItem.ToString();
        }
    }

  

  

}
