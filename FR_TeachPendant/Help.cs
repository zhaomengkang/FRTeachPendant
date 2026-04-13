using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace FR_TeachPendant
{
    public partial class Help : Form
    {
        public Help()
        {
            InitializeComponent();
        }

        private void bt_ok_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void richTextBox1_Click(object sender, EventArgs e)
        {
            // 获取鼠标点击的位置
            Point pt = richTextBox1.PointToClient(Control.MousePosition);
            int charIndex = richTextBox1.GetCharIndexFromPosition(pt);

            // 获取当前点击位置的字符索引
            int lineIndex = richTextBox1.GetLineFromCharIndex(charIndex);
            int lineStartIndex = richTextBox1.GetFirstCharIndexFromLine(lineIndex);
            int lineLength = richTextBox1.Lines[lineIndex].Length;

            // 获取当前行的文本
            string lineText = richTextBox1.Lines[lineIndex].Trim();

            // 使用正则表达式提取链接
            string pattern = @"(https?://[^\s]+)";
            System.Text.RegularExpressions.Match match = System.Text.RegularExpressions.Regex.Match(lineText, pattern);

            if (match.Success)
            {
                string link = match.Value;

                try
                {
                    // 将链接复制到剪贴板
                    Clipboard.SetText(link);
                    Clipboard.SetDataObject(link, true);

                    // 提示用户
                    MessageBox.Show("链接已复制到剪贴板！", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch 
                {
                  
                }
            }
            
        }
    }
}
