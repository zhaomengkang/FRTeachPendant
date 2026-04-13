using System;
using System.Windows.Forms;

// 弹窗类
public class InputDialog : Form
{
    private TextBox textBox;
    private Button btnOk;
    private Button btnCancel;
    private string inputText = "";

    public InputDialog()
    {
        // 初始化控件
        textBox = new TextBox
        {
            Width = 200,
            Height = 20,
            Location = new System.Drawing.Point(10, 50),
            Margin = new Padding(10)
        };
        btnOk = new Button
        {
            Text = "Send",
            Width = 60,
            Height = 25,
            Location = new System.Drawing.Point(10, 100),
            Margin = new Padding(10)
        };
        btnCancel = new Button
        {
            Text = "Abort",
            Width = 60,
            Height = 25,
            Location = new System.Drawing.Point(230, 100),
            Margin = new Padding(10)
        };

        btnOk.Click += BtnOk_Click;
        btnCancel.Click += BtnCancel_Click;

        this.Controls.Add(textBox);
        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
        this.Text = "Input Data";
        this.FormBorderStyle = FormBorderStyle.FixedDialog;
        this.StartPosition = FormStartPosition.CenterParent;
        this.AcceptButton = btnOk;
        this.CancelButton = btnCancel;
        this.ResumeLayout(false);

        // 调整窗体大小以适应控件
        this.ClientSize = new System.Drawing.Size(300, 150);
    }

    private void BtnOk_Click(object sender, EventArgs e)
    {
        inputText = textBox.Text;
        this.DialogResult = DialogResult.OK;
        this.Close();
    }

    private void BtnCancel_Click(object sender, EventArgs e)
    {
        this.DialogResult = DialogResult.Cancel;
        this.Close();
    }

    public string InputText { get { return inputText; } }

    private void InitializeComponent()
    {
            this.SuspendLayout();
            // 
            // InputDialog
            // 
            this.ClientSize = new System.Drawing.Size(282, 253);
            this.Name = "InputDialog";
            this.TopMost = true;
            this.ResumeLayout(false);

    }
}