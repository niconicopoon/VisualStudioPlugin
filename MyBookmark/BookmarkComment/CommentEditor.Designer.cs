namespace MyBookmark
{
    partial class CommentEditor
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
            this.editTextBox = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // editTextBox
            // 
            this.editTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
            this.editTextBox.Location = new System.Drawing.Point(0, 0);
            this.editTextBox.Multiline = true;
            this.editTextBox.Name = "editTextBox";
            this.editTextBox.Size = new System.Drawing.Size(800, 450);
            this.editTextBox.TabIndex = 0;
            this.editTextBox.TextChanged += new System.EventHandler(this.textBox1_TextChanged);
            // 
            // CommentEditor
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.editTextBox);
            this.Name = "CommentEditor";
            this.Text = "CommentEditor";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.TextBox editTextBox;
    }
}