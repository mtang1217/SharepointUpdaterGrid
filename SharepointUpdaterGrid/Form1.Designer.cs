namespace SharepointUpdaterGrid
{
    partial class Form1
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
            this.firstNameLabel = new System.Windows.Forms.Label();
            this.firstName = new System.Windows.Forms.TextBox();
            this.lastName = new System.Windows.Forms.TextBox();
            this.lastNameLabel = new System.Windows.Forms.Label();
            this.DOB = new System.Windows.Forms.TextBox();
            this.DOBLabel = new System.Windows.Forms.Label();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.search = new System.Windows.Forms.Button();
            this.cancel = new System.Windows.Forms.Button();
            this.submit = new System.Windows.Forms.Button();
            this.statusLabel = new System.Windows.Forms.Label();
            this.filePathLabel = new System.Windows.Forms.Label();
            this.filePathText = new System.Windows.Forms.TextBox();
            this.indexerClick = new System.Windows.Forms.Button();
            this.timeLabel = new System.Windows.Forms.Label();
            this.progressBar1 = new System.Windows.Forms.ProgressBar();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // firstNameLabel
            // 
            this.firstNameLabel.AutoSize = true;
            this.firstNameLabel.Location = new System.Drawing.Point(131, 56);
            this.firstNameLabel.Name = "firstNameLabel";
            this.firstNameLabel.Size = new System.Drawing.Size(72, 16);
            this.firstNameLabel.TabIndex = 0;
            this.firstNameLabel.Text = "First Name";
            this.firstNameLabel.Click += new System.EventHandler(this.firstNameLabel_Click);
            // 
            // firstName
            // 
            this.firstName.Location = new System.Drawing.Point(209, 50);
            this.firstName.Name = "firstName";
            this.firstName.Size = new System.Drawing.Size(125, 22);
            this.firstName.TabIndex = 1;
            this.firstName.TextChanged += new System.EventHandler(this.firstName_TextChanged);
            // 
            // lastName
            // 
            this.lastName.Location = new System.Drawing.Point(431, 50);
            this.lastName.Name = "lastName";
            this.lastName.Size = new System.Drawing.Size(131, 22);
            this.lastName.TabIndex = 3;
            this.lastName.TextChanged += new System.EventHandler(this.lastName_TextChanged);
            // 
            // lastNameLabel
            // 
            this.lastNameLabel.AutoSize = true;
            this.lastNameLabel.Location = new System.Drawing.Point(353, 56);
            this.lastNameLabel.Name = "lastNameLabel";
            this.lastNameLabel.Size = new System.Drawing.Size(72, 16);
            this.lastNameLabel.TabIndex = 2;
            this.lastNameLabel.Text = "Last Name";
            this.lastNameLabel.Click += new System.EventHandler(this.lastNameLabel_Click);
            // 
            // DOB
            // 
            this.DOB.Location = new System.Drawing.Point(732, 50);
            this.DOB.Name = "DOB";
            this.DOB.Size = new System.Drawing.Size(159, 22);
            this.DOB.TabIndex = 5;
            this.DOB.TextChanged += new System.EventHandler(this.DOB_TextChanged);
            // 
            // DOBLabel
            // 
            this.DOBLabel.AutoSize = true;
            this.DOBLabel.Location = new System.Drawing.Point(593, 56);
            this.DOBLabel.Name = "DOBLabel";
            this.DOBLabel.Size = new System.Drawing.Size(133, 16);
            this.DOBLabel.TabIndex = 4;
            this.DOBLabel.Text = "DOB (YYYY-MM-DD)";
            this.DOBLabel.Click += new System.EventHandler(this.DOBLabel_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Location = new System.Drawing.Point(46, 80);
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.RowHeadersWidth = 51;
            this.dataGridView1.RowTemplate.Height = 24;
            this.dataGridView1.Size = new System.Drawing.Size(1008, 426);
            this.dataGridView1.TabIndex = 6;
            this.dataGridView1.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView1_CellContentClick);
            // 
            // search
            // 
            this.search.Location = new System.Drawing.Point(935, 50);
            this.search.Name = "search";
            this.search.Size = new System.Drawing.Size(75, 23);
            this.search.TabIndex = 7;
            this.search.Text = "Search";
            this.search.UseVisualStyleBackColor = true;
            this.search.Click += new System.EventHandler(this.search_Click);
            // 
            // cancel
            // 
            this.cancel.Location = new System.Drawing.Point(778, 523);
            this.cancel.Name = "cancel";
            this.cancel.Size = new System.Drawing.Size(75, 23);
            this.cancel.TabIndex = 8;
            this.cancel.Text = "Cancel";
            this.cancel.UseVisualStyleBackColor = true;
            this.cancel.Click += new System.EventHandler(this.cancel_Click);
            // 
            // submit
            // 
            this.submit.Location = new System.Drawing.Point(890, 523);
            this.submit.Name = "submit";
            this.submit.Size = new System.Drawing.Size(75, 23);
            this.submit.TabIndex = 9;
            this.submit.Text = "Submit";
            this.submit.UseVisualStyleBackColor = true;
            this.submit.Click += new System.EventHandler(this.submit_Click);
            // 
            // statusLabel
            // 
            this.statusLabel.AutoSize = true;
            this.statusLabel.Location = new System.Drawing.Point(514, 526);
            this.statusLabel.Name = "statusLabel";
            this.statusLabel.Size = new System.Drawing.Size(48, 16);
            this.statusLabel.TabIndex = 10;
            this.statusLabel.Text = "Ready";
            this.statusLabel.Click += new System.EventHandler(this.label1_Click);
            // 
            // filePathLabel
            // 
            this.filePathLabel.AutoSize = true;
            this.filePathLabel.Location = new System.Drawing.Point(232, 19);
            this.filePathLabel.Name = "filePathLabel";
            this.filePathLabel.Size = new System.Drawing.Size(59, 16);
            this.filePathLabel.TabIndex = 11;
            this.filePathLabel.Text = "File Path";
            // 
            // filePathText
            // 
            this.filePathText.Location = new System.Drawing.Point(297, 12);
            this.filePathText.Name = "filePathText";
            this.filePathText.Size = new System.Drawing.Size(549, 22);
            this.filePathText.TabIndex = 12;
            // 
            // indexerClick
            // 
            this.indexerClick.Location = new System.Drawing.Point(46, 523);
            this.indexerClick.Name = "indexerClick";
            this.indexerClick.Size = new System.Drawing.Size(75, 23);
            this.indexerClick.TabIndex = 13;
            this.indexerClick.Text = "Indexer";
            this.indexerClick.UseVisualStyleBackColor = true;
            this.indexerClick.Click += new System.EventHandler(this.indexerClick_Click);
            // 
            // timeLabel
            // 
            this.timeLabel.AutoSize = true;
            this.timeLabel.Location = new System.Drawing.Point(290, 568);
            this.timeLabel.Name = "timeLabel";
            this.timeLabel.Size = new System.Drawing.Size(38, 16);
            this.timeLabel.TabIndex = 14;
            this.timeLabel.Text = "Time";
            this.timeLabel.Click += new System.EventHandler(this.timeLabel_Click);
            // 
            // progressBar1
            // 
            this.progressBar1.Location = new System.Drawing.Point(293, 587);
            this.progressBar1.Name = "progressBar1";
            this.progressBar1.Size = new System.Drawing.Size(553, 23);
            this.progressBar1.TabIndex = 15;
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1103, 622);
            this.Controls.Add(this.progressBar1);
            this.Controls.Add(this.timeLabel);
            this.Controls.Add(this.indexerClick);
            this.Controls.Add(this.filePathText);
            this.Controls.Add(this.filePathLabel);
            this.Controls.Add(this.statusLabel);
            this.Controls.Add(this.submit);
            this.Controls.Add(this.cancel);
            this.Controls.Add(this.search);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.DOB);
            this.Controls.Add(this.DOBLabel);
            this.Controls.Add(this.lastName);
            this.Controls.Add(this.lastNameLabel);
            this.Controls.Add(this.firstName);
            this.Controls.Add(this.firstNameLabel);
            this.Name = "Form1";
            this.Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label firstNameLabel;
        private System.Windows.Forms.TextBox firstName;
        private System.Windows.Forms.TextBox lastName;
        private System.Windows.Forms.Label lastNameLabel;
        private System.Windows.Forms.TextBox DOB;
        private System.Windows.Forms.Label DOBLabel;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.Button search;
        private System.Windows.Forms.Button cancel;
        private System.Windows.Forms.Button submit;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.Label filePathLabel;
        private System.Windows.Forms.TextBox filePathText;
        private System.Windows.Forms.Button indexerClick;
        private System.Windows.Forms.Label timeLabel;
        private System.Windows.Forms.ProgressBar progressBar1;
    }
}

