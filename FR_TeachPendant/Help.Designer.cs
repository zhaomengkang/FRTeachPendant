namespace FR_TeachPendant
{
    partial class Help
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Help));
            this.lb_text2 = new System.Windows.Forms.Label();
            this.bt_ok = new System.Windows.Forms.Button();
            this.lb_text1 = new System.Windows.Forms.Label();
            this.richTextBox1 = new System.Windows.Forms.RichTextBox();
            this.SuspendLayout();
            // 
            // lb_text2
            // 
            this.lb_text2.AutoSize = true;
            this.lb_text2.Location = new System.Drawing.Point(14, 28);
            this.lb_text2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lb_text2.Name = "lb_text2";
            this.lb_text2.Size = new System.Drawing.Size(0, 13);
            this.lb_text2.TabIndex = 0;
            // 
            // bt_ok
            // 
            this.bt_ok.Location = new System.Drawing.Point(805, 503);
            this.bt_ok.Margin = new System.Windows.Forms.Padding(2);
            this.bt_ok.Name = "bt_ok";
            this.bt_ok.Size = new System.Drawing.Size(78, 48);
            this.bt_ok.TabIndex = 1;
            this.bt_ok.Text = "OK";
            this.bt_ok.UseVisualStyleBackColor = true;
            this.bt_ok.Click += new System.EventHandler(this.bt_ok_Click);
            // 
            // lb_text1
            // 
            this.lb_text1.AutoSize = true;
            this.lb_text1.Font = new System.Drawing.Font("宋体", 16.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(134)));
            this.lb_text1.ForeColor = System.Drawing.Color.Red;
            this.lb_text1.Location = new System.Drawing.Point(151, 15);
            this.lb_text1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.lb_text1.Name = "lb_text1";
            this.lb_text1.Size = new System.Drawing.Size(327, 28);
            this.lb_text1.TabIndex = 2;
            this.lb_text1.Text = "FR iPendant V26.02.19";
            // 
            // richTextBox1
            // 
            this.richTextBox1.Location = new System.Drawing.Point(17, 68);
            this.richTextBox1.Name = "richTextBox1";
            this.richTextBox1.ReadOnly = true;
            this.richTextBox1.Size = new System.Drawing.Size(905, 399);
            this.richTextBox1.TabIndex = 3;
            this.richTextBox1.Text = resources.GetString("richTextBox1.Text");
            this.richTextBox1.Click += new System.EventHandler(this.richTextBox1_Click);
            // 
            // Help
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(954, 562);
            this.ControlBox = false;
            this.Controls.Add(this.richTextBox1);
            this.Controls.Add(this.lb_text1);
            this.Controls.Add(this.bt_ok);
            this.Controls.Add(this.lb_text2);
            this.Margin = new System.Windows.Forms.Padding(2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Help";
            this.Text = "Help";
            this.TopMost = true;
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lb_text2;
        private System.Windows.Forms.Button bt_ok;
        private System.Windows.Forms.Label lb_text1;
        private System.Windows.Forms.RichTextBox richTextBox1;
    }
}