using System;
using System.Windows.Forms;

namespace FRTeachPendant.UI
{
    public class InputDialog : Form
    {
        private TextBox textBoxInput;
        private Button btnOK;
        private Button btnCancel;
        private Label lblPrompt;

        public string InputText => textBoxInput.Text;

        public InputDialog(string prompt, string title)
        {
            this.Width = 400;
            this.Height = 150;
            this.Text = title;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.StartPosition = FormStartPosition.CenterParent;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.ShowInTaskbar = false;

            lblPrompt = new Label() { Left = 10, Top = 10, Width = 360, Text = prompt };
            textBoxInput = new TextBox() { Left = 10, Top = 35, Width = 360 };

            btnOK = new Button() { Text = "OK", Left = 200, Width = 80, Top = 70, DialogResult = DialogResult.OK };
            btnCancel = new Button() { Text = "Cancel", Left = 290, Width = 80, Top = 70, DialogResult = DialogResult.Cancel };

            this.Controls.Add(lblPrompt);
            this.Controls.Add(textBoxInput);
            this.Controls.Add(btnOK);
            this.Controls.Add(btnCancel);

            this.AcceptButton = btnOK;
            this.CancelButton = btnCancel;
        }

        public static string Show(string prompt, string title)
        {
            using (var dlg = new InputDialog(prompt, title))
            {
                var result = dlg.ShowDialog();
                return result == DialogResult.OK ? dlg.InputText : null;
            }
        }
    }
}