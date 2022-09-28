using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CountCodeLine
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            DialogResult ret = dlg.ShowDialog();
            if (ret == DialogResult.OK)
            {
                this.textBox_dir.Text = dlg.SelectedPath;
            }
        }



        public string[] _ignoreList = null;

        public bool CheckIgnore(string name,bool isFile)
        {
            if (this._ignoreList == null)
                return false;

            foreach (string one in this._ignoreList)
            {
                if (one.Trim() == "")
                    continue;

                if (one.StartsWith("*.")  && isFile==true) // 比较扩展名
                {
                    string ex = one.Substring(1);
                    FileInfo fi = new FileInfo(name);
                    if (fi.Extension == ex)
                        return true;
                }

                if (isFile == false)  //目录的情况
                {
                    DirectoryInfo dirInfo = new DirectoryInfo(name);
                    if (dirInfo.Name==one)
                        return true;
                }
            }

            return false;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.ClearHtml();
            this._ignoreList = null ;
            string ignore = this.textBox_ignore.Text.Trim();
            if (string.IsNullOrEmpty(ignore) == false)
            {
                this._ignoreList = ignore.Split(',');
            }

            string dir = this.textBox_dir.Text.Trim();
            if (string.IsNullOrEmpty(dir) == true)
            {
                MessageBox.Show(this, "请先选择目录");
                return;
            }

            if (Directory.Exists(dir) == false)
            {
                MessageBox.Show(this, "目录[" + dir + "]不存在");
                return;
            }
            this.Print("统计开始",true);

            // 清空
            this._ht.Clear();
            this._totalCount = 0;
            string only = this.textBox_only.Text.Trim();
            if (string.IsNullOrEmpty(only) == true)
            {
                int nRet = this.circulationLine(dir, out string error);
                if (nRet == -1)
                {
                    Print("出错：" + error);
                    MessageBox.Show(this, error);
                    return;
                }
            }
            else
            {

                //DirectoryInfo dirInfo = new DirectoryInfo(dir);

                string[] children = only.Split(new char[] {','});
                foreach (string child in children)
                {
                    string one = child.Replace('~','\\');
                    string theDir = dir + "\\" + one;
                    int nRet = this.circulationLine(theDir, out string error);
                    if (nRet == -1)
                    {
                        Print("出错：" + error);
                        MessageBox.Show(this, error);
                        return;
                    }
                }

            }


            this.Print("统计完成，共计" + this._totalCount.ToString(), true);
        }

        Hashtable _ht = new Hashtable();
        long _totalCount = 0;

        public int circulationLine(string dir, out string error)
        {
            int nRet = 0;
            error = "";

            DirectoryInfo dirInfo = new DirectoryInfo(dir);

            // 列出所有文件
            FileInfo[] files = dirInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                long lineCount = 0;
                string filePath = file.FullName;

                // 检查是否为过滤的文件
                if (this.CheckIgnore(filePath,true)==true) 
                {
                    this.Print("忽略" + "\t" + filePath);
                    continue;
                }

                StreamReader streamReader = new StreamReader(file.FullName);
                try
                {
                    //判断文件中是否有字符
                    while (streamReader.Peek() != -1)
                    {
                        //读取文件中的一行字符
                        streamReader.ReadLine();
                        lineCount++;
                    }
                }
                finally
                {
                    streamReader.Close();
                }

                // 记录下来文件与对应的行数
                _ht[filePath] = lineCount;

                // 此时就应显示在界面上
                this.Print(lineCount+"\t"+filePath);
                this._totalCount += lineCount;
                Application.DoEvents();
            }

            // 列出所有目录，递归
            DirectoryInfo[] childrenDir = dirInfo.GetDirectories();

            foreach (DirectoryInfo one in childrenDir)
            {
                // 检查是否为过滤的目录
                if (this.CheckIgnore(one.FullName, false) == true)
                {
                    this.Print("忽略" + "\t" + one.FullName);
                    continue;
                }

                nRet = this.circulationLine(one.FullName, out error);
                if (nRet == -1)
                    return -1;
            }

            // 空目录
            if (files.Length == 0 && childrenDir.Length == 0)
            {
                this.Print("空目录" + "\t" + dir);
            }

            return nRet;
        }

        public void GetInfo()
        {
            // 连接参数
            this.textBox_dir.Text = Properties.Settings.Default.dir;
            this.textBox_ignore.Text = Properties.Settings.Default.ignore;
            this.textBox_only.Text = Properties.Settings.Default.only;
        }


        public void SaveInfo()
        {

            // 连接参数
            Properties.Settings.Default.dir = this.textBox_dir.Text.Trim();
            Properties.Settings.Default.ignore = this.textBox_ignore.Text.Trim();
            Properties.Settings.Default.only = this.textBox_only.Text.Trim();

            // 保存参数
            Properties.Settings.Default.Save();

        }

        private void Form1_Load(object sender, EventArgs e)
        {
            this.GetInfo();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.SaveInfo();
        }

        #region 输出信息浏览器通用函数

        private void Print(string strHtml)
        {
            this.Print(strHtml, false);
        }

        private void Print(string strHtml, bool containerTime)
        {
            if (containerTime)
                strHtml = String.Format("{0}  {1}<br />", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), strHtml);
            else
                strHtml = strHtml + "<br />";

            WriteHtml(this.webBrowser1,
                strHtml);
        }

        // 不支持异步调用
        public void WriteHtml(WebBrowser webBrowser,
            string strHtml)
        {
            this.Invoke(new Action(() =>
            {
                HtmlDocument doc = webBrowser.Document;

                if (doc == null)
                {
                    webBrowser.Navigate("about:blank");
                    doc = webBrowser.Document;
                    doc.Write("<pre>");
                }

                doc.Write(strHtml);

                // 保持末行可见
                ScrollToEnd(webBrowser);
            }));
        }

        public void ClearHtml()
        {
            HtmlDocument doc = webBrowser1.Document;

            if (doc == null)
            {
                webBrowser1.Navigate("about:blank");
                doc = webBrowser1.Document;
            }
            doc = doc.OpenNew(true);
            doc.Write("<pre>");
        }


        public static void ScrollToEnd(WebBrowser webBrowser1)
        {
            if (webBrowser1.Document != null
                && webBrowser1.Document.Window != null
                && webBrowser1.Document.Body != null)
                webBrowser1.Document.Window.ScrollTo(
                    0,
                    webBrowser1.Document.Body.ScrollRectangle.Height);
        }

        #endregion

        private void button3_Click(object sender, EventArgs e)
        {
            this.ClearHtml();
            this._ignoreList = null;
            string ignore = this.textBox_ignore.Text.Trim();
            if (string.IsNullOrEmpty(ignore) == false)
            {
                this._ignoreList = ignore.Split(',');
            }

            string dir = this.textBox_dir.Text.Trim();
            if (string.IsNullOrEmpty(dir) == true)
            {
                MessageBox.Show(this, "请先选择目录");
                return;
            }

            if (Directory.Exists(dir) == false)
            {
                MessageBox.Show(this, "目录[" + dir + "]不存在");
                return;
            }
            this.textBox_only.Text = "";

            string temp = "";

            DirectoryInfo dirInfo = new DirectoryInfo(dir);
            // 列出所有文件
            FileInfo[] files = dirInfo.GetFiles();
            foreach (FileInfo file in files)
            {
                string name = file.Name;
                if (name.LastIndexOf(".") != -1)
                    name = name.Substring(0,name.LastIndexOf("."));

                if (temp != "")
                    temp += ",";

                temp += name;
                

            }

            // 放在仅允许的输入框
            this.textBox_only.Text = temp;
        }
    }
}
