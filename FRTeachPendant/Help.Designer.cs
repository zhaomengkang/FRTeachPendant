namespace FRTeachPendant
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
            this.lb_text2 = new System.Windows.Forms.Label();
            this.wb_Helper = new Microsoft.Web.WebView2.WinForms.WebView2();
            ((System.ComponentModel.ISupportInitialize)(this.wb_Helper)).BeginInit();
            this.SuspendLayout();
            // 
            // lb_text2
            // 
            this.lb_text2.AutoSize = true;
            this.lb_text2.Location = new System.Drawing.Point(19, 35);
            this.lb_text2.Name = "lb_text2";
            this.lb_text2.Size = new System.Drawing.Size(0, 15);
            this.lb_text2.TabIndex = 0;
            // 
            // wb_Helper
            // 
            this.wb_Helper.AllowExternalDrop = true;
            this.wb_Helper.CreationProperties = null;
            this.wb_Helper.DefaultBackgroundColor = System.Drawing.Color.White;
            this.wb_Helper.Location = new System.Drawing.Point(2, 0);
            this.wb_Helper.Name = "wb_Helper";
            this.wb_Helper.Size = new System.Drawing.Size(875, 641);
            this.wb_Helper.TabIndex = 1;
            this.wb_Helper.ZoomFactor = 1D;
            // 
            // Help
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(882, 653);
            this.Controls.Add(this.wb_Helper);
            this.Controls.Add(this.lb_text2);
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Help";
            this.Text = "Help";
            this.TopMost = true;
            ((System.ComponentModel.ISupportInitialize)(this.wb_Helper)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lb_text2;
        private Microsoft.Web.WebView2.WinForms.WebView2 wb_Helper;
    }
}