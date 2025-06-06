namespace PS4Saves
{
    partial class Main
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
            components = new System.ComponentModel.Container();
            ipTextBox = new System.Windows.Forms.TextBox();
            connectButton = new System.Windows.Forms.Button();
            processesButton = new System.Windows.Forms.Button();
            setupButton = new System.Windows.Forms.Button();
            processesComboBox = new System.Windows.Forms.ComboBox();
            userComboBox = new System.Windows.Forms.ComboBox();
            dirsComboBox = new System.Windows.Forms.ComboBox();
            searchButton = new System.Windows.Forms.Button();
            mountButton = new System.Windows.Forms.Button();
            unmountButton = new System.Windows.Forms.Button();
            connectionGroupBox = new System.Windows.Forms.GroupBox();
            label2 = new System.Windows.Forms.Label();
            ipLabel = new System.Windows.Forms.Label();
            fwVersionComboBox = new System.Windows.Forms.ComboBox();
            createGroupBox = new System.Windows.Forms.GroupBox();
            sizeLabel = new System.Windows.Forms.Label();
            sizeTrackBar = new System.Windows.Forms.TrackBar();
            nameLabel = new System.Windows.Forms.Label();
            nameTextBox = new System.Windows.Forms.TextBox();
            createButton = new System.Windows.Forms.Button();
            mountGroupBox = new System.Windows.Forms.GroupBox();
            label1 = new System.Windows.Forms.Label();
            infoGroupBox = new System.Windows.Forms.GroupBox();
            dateTextBox = new System.Windows.Forms.TextBox();
            dateLabel = new System.Windows.Forms.Label();
            detailsTextBox = new System.Windows.Forms.TextBox();
            detailsLabel = new System.Windows.Forms.Label();
            subtitleTextBox = new System.Windows.Forms.TextBox();
            subtitleLabel = new System.Windows.Forms.Label();
            titleTextBox = new System.Windows.Forms.TextBox();
            titleLabel = new System.Windows.Forms.Label();
            sizeToolTip = new System.Windows.Forms.ToolTip(components);
            statusLabel = new System.Windows.Forms.Label();
            connectionGroupBox.SuspendLayout();
            createGroupBox.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)sizeTrackBar).BeginInit();
            mountGroupBox.SuspendLayout();
            infoGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // ipTextBox
            // 
            ipTextBox.Location = new System.Drawing.Point(93, 28);
            ipTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            ipTextBox.Name = "ipTextBox";
            ipTextBox.Size = new System.Drawing.Size(155, 26);
            ipTextBox.TabIndex = 0;
            // 
            // connectButton
            // 
            connectButton.Location = new System.Drawing.Point(256, 28);
            connectButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            connectButton.Name = "connectButton";
            connectButton.Size = new System.Drawing.Size(241, 29);
            connectButton.TabIndex = 1;
            connectButton.Text = "Connect";
            connectButton.UseVisualStyleBackColor = true;
            connectButton.Click += connectButton_Click;
            // 
            // processesButton
            // 
            processesButton.Location = new System.Drawing.Point(8, 103);
            processesButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            processesButton.Name = "processesButton";
            processesButton.Size = new System.Drawing.Size(241, 30);
            processesButton.TabIndex = 2;
            processesButton.Text = "Get Processes";
            processesButton.UseVisualStyleBackColor = true;
            processesButton.Click += processesButton_Click;
            // 
            // setupButton
            // 
            setupButton.Location = new System.Drawing.Point(8, 142);
            setupButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            setupButton.Name = "setupButton";
            setupButton.Size = new System.Drawing.Size(241, 30);
            setupButton.TabIndex = 3;
            setupButton.Text = "Setup";
            setupButton.UseVisualStyleBackColor = true;
            setupButton.Click += setupButton_Click;
            // 
            // processesComboBox
            // 
            processesComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            processesComboBox.FormattingEnabled = true;
            processesComboBox.Location = new System.Drawing.Point(256, 103);
            processesComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            processesComboBox.Name = "processesComboBox";
            processesComboBox.Size = new System.Drawing.Size(240, 27);
            processesComboBox.TabIndex = 4;
            processesComboBox.SelectedIndexChanged += processesComboBox_SelectedIndexChanged;
            // 
            // userComboBox
            // 
            userComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            userComboBox.FormattingEnabled = true;
            userComboBox.Location = new System.Drawing.Point(256, 142);
            userComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            userComboBox.Name = "userComboBox";
            userComboBox.Size = new System.Drawing.Size(240, 27);
            userComboBox.TabIndex = 5;
            userComboBox.SelectedIndexChanged += userComboBox_SelectedIndexChanged;
            // 
            // dirsComboBox
            // 
            dirsComboBox.FormattingEnabled = true;
            dirsComboBox.Location = new System.Drawing.Point(257, 28);
            dirsComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dirsComboBox.Name = "dirsComboBox";
            dirsComboBox.Size = new System.Drawing.Size(239, 27);
            dirsComboBox.TabIndex = 7;
            dirsComboBox.SelectedIndexChanged += dirsComboBox_SelectedIndexChanged;
            // 
            // searchButton
            // 
            searchButton.Location = new System.Drawing.Point(8, 28);
            searchButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            searchButton.Name = "searchButton";
            searchButton.Size = new System.Drawing.Size(241, 30);
            searchButton.TabIndex = 6;
            searchButton.Text = "Search";
            searchButton.UseVisualStyleBackColor = true;
            searchButton.Click += searchButton_Click;
            // 
            // mountButton
            // 
            mountButton.Location = new System.Drawing.Point(8, 70);
            mountButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            mountButton.Name = "mountButton";
            mountButton.Size = new System.Drawing.Size(241, 33);
            mountButton.TabIndex = 8;
            mountButton.Text = "Mount";
            mountButton.UseVisualStyleBackColor = true;
            mountButton.Click += mountButton_Click;
            // 
            // unmountButton
            // 
            unmountButton.Location = new System.Drawing.Point(256, 70);
            unmountButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            unmountButton.Name = "unmountButton";
            unmountButton.Size = new System.Drawing.Size(241, 33);
            unmountButton.TabIndex = 9;
            unmountButton.Text = "Unmount";
            unmountButton.UseVisualStyleBackColor = true;
            unmountButton.Click += unmountButton_Click;
            // 
            // connectionGroupBox
            // 
            connectionGroupBox.Controls.Add(label2);
            connectionGroupBox.Controls.Add(ipLabel);
            connectionGroupBox.Controls.Add(ipTextBox);
            connectionGroupBox.Controls.Add(connectButton);
            connectionGroupBox.Controls.Add(processesButton);
            connectionGroupBox.Controls.Add(setupButton);
            connectionGroupBox.Controls.Add(fwVersionComboBox);
            connectionGroupBox.Controls.Add(processesComboBox);
            connectionGroupBox.Controls.Add(userComboBox);
            connectionGroupBox.Location = new System.Drawing.Point(9, 17);
            connectionGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            connectionGroupBox.Name = "connectionGroupBox";
            connectionGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            connectionGroupBox.Size = new System.Drawing.Size(505, 185);
            connectionGroupBox.TabIndex = 10;
            connectionGroupBox.TabStop = false;
            connectionGroupBox.Text = "Connection";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new System.Drawing.Point(9, 69);
            label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label2.Name = "label2";
            label2.Size = new System.Drawing.Size(116, 19);
            label2.TabIndex = 6;
            label2.Text = "Firmware version:";
            // 
            // ipLabel
            // 
            ipLabel.AutoSize = true;
            ipLabel.Location = new System.Drawing.Point(8, 32);
            ipLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            ipLabel.Name = "ipLabel";
            ipLabel.Size = new System.Drawing.Size(77, 19);
            ipLabel.TabIndex = 6;
            ipLabel.Text = "IP Address:";
            // 
            // fwVersionComboBox
            // 
            fwVersionComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            fwVersionComboBox.FormattingEnabled = true;
            fwVersionComboBox.Location = new System.Drawing.Point(141, 66);
            fwVersionComboBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            fwVersionComboBox.Name = "fwVersionComboBox";
            fwVersionComboBox.Size = new System.Drawing.Size(356, 27);
            fwVersionComboBox.TabIndex = 4;
            // 
            // createGroupBox
            // 
            createGroupBox.Controls.Add(sizeLabel);
            createGroupBox.Controls.Add(sizeTrackBar);
            createGroupBox.Controls.Add(nameLabel);
            createGroupBox.Controls.Add(nameTextBox);
            createGroupBox.Controls.Add(createButton);
            createGroupBox.Location = new System.Drawing.Point(10, 376);
            createGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            createGroupBox.Name = "createGroupBox";
            createGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            createGroupBox.Size = new System.Drawing.Size(505, 188);
            createGroupBox.TabIndex = 11;
            createGroupBox.TabStop = false;
            createGroupBox.Text = "Create New Saves";
            // 
            // sizeLabel
            // 
            sizeLabel.AutoSize = true;
            sizeLabel.Location = new System.Drawing.Point(8, 70);
            sizeLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            sizeLabel.Name = "sizeLabel";
            sizeLabel.Size = new System.Drawing.Size(94, 19);
            sizeLabel.TabIndex = 9;
            sizeLabel.Text = "max save size:";
            // 
            // sizeTrackBar
            // 
            sizeTrackBar.Location = new System.Drawing.Point(156, 70);
            sizeTrackBar.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            sizeTrackBar.Maximum = 32768;
            sizeTrackBar.Minimum = 96;
            sizeTrackBar.Name = "sizeTrackBar";
            sizeTrackBar.Size = new System.Drawing.Size(343, 50);
            sizeTrackBar.TabIndex = 8;
            sizeTrackBar.Value = 96;
            sizeTrackBar.Scroll += sizeTrackBar_Scroll;
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Location = new System.Drawing.Point(8, 36);
            nameLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new System.Drawing.Size(135, 19);
            nameLabel.TabIndex = 7;
            nameLabel.Text = "save directory name:";
            // 
            // nameTextBox
            // 
            nameTextBox.Location = new System.Drawing.Point(156, 32);
            nameTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            nameTextBox.MaxLength = 31;
            nameTextBox.Name = "nameTextBox";
            nameTextBox.Size = new System.Drawing.Size(340, 26);
            nameTextBox.TabIndex = 6;
            // 
            // createButton
            // 
            createButton.Location = new System.Drawing.Point(7, 144);
            createButton.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            createButton.Name = "createButton";
            createButton.Size = new System.Drawing.Size(489, 33);
            createButton.TabIndex = 6;
            createButton.Text = "Create Save";
            createButton.UseVisualStyleBackColor = true;
            createButton.Click += createButton_Click;
            // 
            // mountGroupBox
            // 
            mountGroupBox.Controls.Add(label1);
            mountGroupBox.Controls.Add(searchButton);
            mountGroupBox.Controls.Add(dirsComboBox);
            mountGroupBox.Controls.Add(mountButton);
            mountGroupBox.Controls.Add(unmountButton);
            mountGroupBox.Location = new System.Drawing.Point(9, 212);
            mountGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            mountGroupBox.Name = "mountGroupBox";
            mountGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            mountGroupBox.Size = new System.Drawing.Size(505, 155);
            mountGroupBox.TabIndex = 12;
            mountGroupBox.TabStop = false;
            mountGroupBox.Text = "Mount Existing Saves";
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new System.Drawing.Point(8, 108);
            label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            label1.MaximumSize = new System.Drawing.Size(500, 475);
            label1.Name = "label1";
            label1.Size = new System.Drawing.Size(457, 38);
            label1.TabIndex = 6;
            label1.Text = "Make sure to unmount before closing game, otherwise the save may get corrupted!";
            // 
            // infoGroupBox
            // 
            infoGroupBox.Controls.Add(dateTextBox);
            infoGroupBox.Controls.Add(dateLabel);
            infoGroupBox.Controls.Add(detailsTextBox);
            infoGroupBox.Controls.Add(detailsLabel);
            infoGroupBox.Controls.Add(subtitleTextBox);
            infoGroupBox.Controls.Add(subtitleLabel);
            infoGroupBox.Controls.Add(titleTextBox);
            infoGroupBox.Controls.Add(titleLabel);
            infoGroupBox.Location = new System.Drawing.Point(523, 17);
            infoGroupBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            infoGroupBox.Name = "infoGroupBox";
            infoGroupBox.Padding = new System.Windows.Forms.Padding(4, 5, 4, 5);
            infoGroupBox.Size = new System.Drawing.Size(528, 547);
            infoGroupBox.TabIndex = 12;
            infoGroupBox.TabStop = false;
            infoGroupBox.Text = "Save Info";
            // 
            // dateTextBox
            // 
            dateTextBox.Location = new System.Drawing.Point(12, 429);
            dateTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            dateTextBox.Name = "dateTextBox";
            dateTextBox.ReadOnly = true;
            dateTextBox.Size = new System.Drawing.Size(507, 26);
            dateTextBox.TabIndex = 7;
            // 
            // dateLabel
            // 
            dateLabel.AutoSize = true;
            dateLabel.Location = new System.Drawing.Point(8, 407);
            dateLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            dateLabel.Name = "dateLabel";
            dateLabel.Size = new System.Drawing.Size(39, 19);
            dateLabel.TabIndex = 6;
            dateLabel.Text = "date:";
            // 
            // detailsTextBox
            // 
            detailsTextBox.Location = new System.Drawing.Point(12, 242);
            detailsTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            detailsTextBox.Multiline = true;
            detailsTextBox.Name = "detailsTextBox";
            detailsTextBox.ReadOnly = true;
            detailsTextBox.Size = new System.Drawing.Size(507, 158);
            detailsTextBox.TabIndex = 5;
            // 
            // detailsLabel
            // 
            detailsLabel.AutoSize = true;
            detailsLabel.Location = new System.Drawing.Point(8, 219);
            detailsLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            detailsLabel.Name = "detailsLabel";
            detailsLabel.Size = new System.Drawing.Size(51, 19);
            detailsLabel.TabIndex = 4;
            detailsLabel.Text = "details:";
            // 
            // subtitleTextBox
            // 
            subtitleTextBox.Location = new System.Drawing.Point(12, 147);
            subtitleTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            subtitleTextBox.Multiline = true;
            subtitleTextBox.Name = "subtitleTextBox";
            subtitleTextBox.ReadOnly = true;
            subtitleTextBox.Size = new System.Drawing.Size(507, 66);
            subtitleTextBox.TabIndex = 3;
            // 
            // subtitleLabel
            // 
            subtitleLabel.AutoSize = true;
            subtitleLabel.Location = new System.Drawing.Point(8, 124);
            subtitleLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            subtitleLabel.Name = "subtitleLabel";
            subtitleLabel.Size = new System.Drawing.Size(57, 19);
            subtitleLabel.TabIndex = 2;
            subtitleLabel.Text = "subtitle:";
            // 
            // titleTextBox
            // 
            titleTextBox.Location = new System.Drawing.Point(12, 51);
            titleTextBox.Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            titleTextBox.Multiline = true;
            titleTextBox.Name = "titleTextBox";
            titleTextBox.ReadOnly = true;
            titleTextBox.Size = new System.Drawing.Size(507, 67);
            titleTextBox.TabIndex = 1;
            // 
            // titleLabel
            // 
            titleLabel.AutoSize = true;
            titleLabel.Location = new System.Drawing.Point(8, 28);
            titleLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            titleLabel.Name = "titleLabel";
            titleLabel.Size = new System.Drawing.Size(35, 19);
            titleLabel.TabIndex = 0;
            titleLabel.Text = "title:";
            // 
            // statusLabel
            // 
            statusLabel.AutoSize = true;
            statusLabel.Location = new System.Drawing.Point(10, 569);
            statusLabel.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            statusLabel.Name = "statusLabel";
            statusLabel.Size = new System.Drawing.Size(50, 19);
            statusLabel.TabIndex = 13;
            statusLabel.Text = "Status:";
            // 
            // Main
            // 
            AutoScaleDimensions = new System.Drawing.SizeF(8F, 19F);
            AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            ClientSize = new System.Drawing.Size(1067, 597);
            Controls.Add(statusLabel);
            Controls.Add(infoGroupBox);
            Controls.Add(mountGroupBox);
            Controls.Add(createGroupBox);
            Controls.Add(connectionGroupBox);
            Margin = new System.Windows.Forms.Padding(4, 5, 4, 5);
            Name = "Main";
            Text = "Playstation 4 Save Mounter 1.3.1 [ps4debug]";
            connectionGroupBox.ResumeLayout(false);
            connectionGroupBox.PerformLayout();
            createGroupBox.ResumeLayout(false);
            createGroupBox.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)sizeTrackBar).EndInit();
            mountGroupBox.ResumeLayout(false);
            mountGroupBox.PerformLayout();
            infoGroupBox.ResumeLayout(false);
            infoGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private System.Windows.Forms.TextBox ipTextBox;
        private System.Windows.Forms.Button connectButton;
        private System.Windows.Forms.Button processesButton;
        private System.Windows.Forms.Button setupButton;
        private System.Windows.Forms.ComboBox processesComboBox;
        private System.Windows.Forms.ComboBox userComboBox;
        private System.Windows.Forms.ComboBox dirsComboBox;
        private System.Windows.Forms.Button searchButton;
        private System.Windows.Forms.Button mountButton;
        private System.Windows.Forms.Button unmountButton;
        private System.Windows.Forms.GroupBox connectionGroupBox;
        private System.Windows.Forms.Label ipLabel;
        private System.Windows.Forms.GroupBox createGroupBox;
        private System.Windows.Forms.TextBox nameTextBox;
        private System.Windows.Forms.Button createButton;
        private System.Windows.Forms.GroupBox mountGroupBox;
        private System.Windows.Forms.GroupBox infoGroupBox;
        private System.Windows.Forms.Label nameLabel;
        private System.Windows.Forms.TrackBar sizeTrackBar;
        private System.Windows.Forms.Label sizeLabel;
        private System.Windows.Forms.ToolTip sizeToolTip;
        private System.Windows.Forms.Label statusLabel;
        private System.Windows.Forms.TextBox dateTextBox;
        private System.Windows.Forms.Label dateLabel;
        private System.Windows.Forms.TextBox detailsTextBox;
        private System.Windows.Forms.Label detailsLabel;
        private System.Windows.Forms.TextBox subtitleTextBox;
        private System.Windows.Forms.Label subtitleLabel;
        private System.Windows.Forms.TextBox titleTextBox;
        private System.Windows.Forms.Label titleLabel;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.ComboBox fwVersionComboBox;
    }
}

