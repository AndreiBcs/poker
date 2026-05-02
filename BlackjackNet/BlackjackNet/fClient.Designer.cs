

using System.Windows.Forms;

namespace BlackjackNet
{
    partial class fClient
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

        private void InitializeComponent()
        {
            this.dealerFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblDealerScore = new System.Windows.Forms.Label();
            this.playerFlowPanel = new System.Windows.Forms.FlowLayoutPanel();
            this.lblPlayerScore = new System.Windows.Forms.Label();
            this.btnDeal = new System.Windows.Forms.Button();
            this.btnHit = new System.Windows.Forms.Button();
            this.btnStand = new System.Windows.Forms.Button();
            this.textBoxLog = new System.Windows.Forms.TextBox();
            this.btnConnect = new System.Windows.Forms.Button();
            this.tbAddress = new System.Windows.Forms.TextBox();
            this.lblAddress = new System.Windows.Forms.Label();
            this.btnRestart = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // dealerFlowPanel
            // 
            this.dealerFlowPanel.AutoScroll = true;
            this.dealerFlowPanel.BackColor = System.Drawing.Color.DarkGreen;
            this.dealerFlowPanel.Location = new System.Drawing.Point(22, 50);
            this.dealerFlowPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.dealerFlowPanel.Name = "dealerFlowPanel";
            this.dealerFlowPanel.Size = new System.Drawing.Size(562, 200);
            this.dealerFlowPanel.TabIndex = 0;
            // 
            // lblDealerScore
            // 
            this.lblDealerScore.AutoSize = true;
            this.lblDealerScore.Location = new System.Drawing.Point(22, 19);
            this.lblDealerScore.Name = "lblDealerScore";
            this.lblDealerScore.Size = new System.Drawing.Size(110, 20);
            this.lblDealerScore.TabIndex = 1;
            this.lblDealerScore.Text = "Dealer Scor: 0";
            // 
            // playerFlowPanel
            // 
            this.playerFlowPanel.AutoScroll = true;
            this.playerFlowPanel.BackColor = System.Drawing.Color.DarkGreen;
            this.playerFlowPanel.Location = new System.Drawing.Point(22, 284);
            this.playerFlowPanel.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.playerFlowPanel.Name = "playerFlowPanel";
            this.playerFlowPanel.Size = new System.Drawing.Size(562, 200);
            this.playerFlowPanel.TabIndex = 0;
            // 
            // lblPlayerScore
            // 
            this.lblPlayerScore.AutoSize = true;
            this.lblPlayerScore.Location = new System.Drawing.Point(22, 260);
            this.lblPlayerScore.Name = "lblPlayerScore";
            this.lblPlayerScore.Size = new System.Drawing.Size(106, 20);
            this.lblPlayerScore.TabIndex = 0;
            this.lblPlayerScore.Text = "Player Scor: 0";
            // 
            // btnDeal
            // 
            this.btnDeal.Location = new System.Drawing.Point(619, 238);
            this.btnDeal.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnDeal.Name = "btnDeal";
            this.btnDeal.Size = new System.Drawing.Size(112, 50);
            this.btnDeal.TabIndex = 3;
            this.btnDeal.Text = "Deal";
            this.btnDeal.UseVisualStyleBackColor = true;
            this.btnDeal.Click += new System.EventHandler(this.btnDeal_Click);
            // 
            // btnHit
            // 
            this.btnHit.Location = new System.Drawing.Point(619, 300);
            this.btnHit.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnHit.Name = "btnHit";
            this.btnHit.Size = new System.Drawing.Size(112, 50);
            this.btnHit.TabIndex = 4;
            this.btnHit.Text = "Hit";
            this.btnHit.UseVisualStyleBackColor = true;
            this.btnHit.Click += new System.EventHandler(this.btnHit_Click);
            // 
            // btnStand
            // 
            this.btnStand.Location = new System.Drawing.Point(619, 362);
            this.btnStand.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnStand.Name = "btnStand";
            this.btnStand.Size = new System.Drawing.Size(112, 50);
            this.btnStand.TabIndex = 5;
            this.btnStand.Text = "Stand";
            this.btnStand.UseVisualStyleBackColor = true;
            this.btnStand.Click += new System.EventHandler(this.btnStand_Click);
            // 
            // textBoxLog
            // 
            this.textBoxLog.Location = new System.Drawing.Point(22, 508);
            this.textBoxLog.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.textBoxLog.Multiline = true;
            this.textBoxLog.Name = "textBoxLog";
            this.textBoxLog.ReadOnly = true;
            this.textBoxLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxLog.Size = new System.Drawing.Size(708, 149);
            this.textBoxLog.TabIndex = 6;
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(619, 50);
            this.btnConnect.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(112, 50);
            this.btnConnect.TabIndex = 7;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // tbAddress
            // 
            this.tbAddress.Location = new System.Drawing.Point(619, 150);
            this.tbAddress.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.tbAddress.Name = "tbAddress";
            this.tbAddress.Size = new System.Drawing.Size(112, 26);
            this.tbAddress.TabIndex = 8;
            this.tbAddress.Text = "127.0.0.1";
            // 
            // lblAddress
            // 
            this.lblAddress.AutoSize = true;
            this.lblAddress.Location = new System.Drawing.Point(615, 125);
            this.lblAddress.Name = "lblAddress";
            this.lblAddress.Size = new System.Drawing.Size(78, 20);
            this.lblAddress.TabIndex = 9;
            this.lblAddress.Text = "Server IP:";
            // 
            // btnRestart
            // 
            this.btnRestart.Enabled = false;
            this.btnRestart.Location = new System.Drawing.Point(619, 425);
            this.btnRestart.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.btnRestart.Name = "btnRestart";
            this.btnRestart.Size = new System.Drawing.Size(112, 50);
            this.btnRestart.TabIndex = 10;
            this.btnRestart.Text = "Restart";
            this.btnRestart.UseVisualStyleBackColor = true;
            this.btnRestart.Click += new System.EventHandler(this.btnRestart_Click);
            // 
            // fClient
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 20F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(770, 664);
            this.Controls.Add(this.lblAddress);
            this.Controls.Add(this.tbAddress);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.btnStand);
            this.Controls.Add(this.btnHit);
            this.Controls.Add(this.btnDeal);
            this.Controls.Add(this.lblPlayerScore);
            this.Controls.Add(this.playerFlowPanel);
            this.Controls.Add(this.textBoxLog);
            this.Controls.Add(this.lblDealerScore);
            this.Controls.Add(this.dealerFlowPanel);
            this.Controls.Add(this.btnRestart);
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "fClient";
            this.Text = "Blackjack Client";
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.fClient_FormClosed);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.FlowLayoutPanel dealerFlowPanel;
        private System.Windows.Forms.Label lblDealerScore;
        private System.Windows.Forms.FlowLayoutPanel playerFlowPanel;
        private System.Windows.Forms.Label lblPlayerScore;
        private System.Windows.Forms.Button btnDeal;
        private System.Windows.Forms.Button btnHit;
        private System.Windows.Forms.Button btnStand;
        private System.Windows.Forms.TextBox textBoxLog;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.TextBox tbAddress;
        private System.Windows.Forms.Label lblAddress;
        private System.Windows.Forms.Button btnRestart;
    }
}