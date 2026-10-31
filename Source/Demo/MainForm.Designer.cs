namespace PortfolioSamples.Demo
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components;
        private System.Windows.Forms.TableLayoutPanel tlpRoot;
        private System.Windows.Forms.FlowLayoutPanel flpButtons;
        private System.Windows.Forms.Label lbTitle;
        private System.Windows.Forms.Label lbStatus;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.TextBox txtOutputDirectory;
        private System.Windows.Forms.Button btnMes;
        private System.Windows.Forms.Button btnConversion;
        private System.Windows.Forms.Button btnCurrency;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnExport;

        protected override void Dispose(bool disposing)
        {
            if (disposing && components != null) components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tlpRoot = new System.Windows.Forms.TableLayoutPanel();
            this.flpButtons = new System.Windows.Forms.FlowLayoutPanel();
            this.lbTitle = new System.Windows.Forms.Label();
            this.lbStatus = new System.Windows.Forms.Label();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.txtOutputDirectory = new System.Windows.Forms.TextBox();
            this.btnMes = new System.Windows.Forms.Button();
            this.btnConversion = new System.Windows.Forms.Button();
            this.btnCurrency = new System.Windows.Forms.Button();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnExport = new System.Windows.Forms.Button();
            this.SuspendLayout();
            this.tlpRoot.SuspendLayout();
            this.flpButtons.SuspendLayout();
            this.tlpRoot.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tlpRoot.ColumnCount = 1;
            this.tlpRoot.RowCount = 4;
            this.tlpRoot.Padding = new System.Windows.Forms.Padding(20);
            this.tlpRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 52F));
            this.tlpRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 96F));
            this.tlpRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tlpRoot.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.lbTitle.Text = "운영의 실패·재시도·검증을 코드로 설명합니다";
            this.lbTitle.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbTitle.Font = new System.Drawing.Font("맑은 고딕", 14F, System.Drawing.FontStyle.Bold);
            this.flpButtons.Dock = System.Windows.Forms.DockStyle.Fill;
            this.flpButtons.WrapContents = true;
            this.btnMes.Text = "MES 상태·장애 분류";
            this.btnMes.Size = new System.Drawing.Size(175, 36);
            this.btnMes.Click += new System.EventHandler(this.btnMes_Click);
            this.btnConversion.Text = "변환 재시도 분류";
            this.btnConversion.Size = new System.Drawing.Size(165, 36);
            this.btnConversion.Click += new System.EventHandler(this.btnConversion_Click);
            this.btnCurrency.Text = "통화 매핑 검증";
            this.btnCurrency.Size = new System.Drawing.Size(150, 36);
            this.btnCurrency.Click += new System.EventHandler(this.btnCurrency_Click);
            this.txtOutputDirectory.Text = "Results";
            this.txtOutputDirectory.Size = new System.Drawing.Size(130, 28);
            this.txtOutputDirectory.AccessibleName = "로컬 출력 폴더 이름";
            this.btnSave.Text = "설정 저장";
            this.btnSave.Size = new System.Drawing.Size(105, 36);
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            this.btnExport.Text = "샘플 결과 저장";
            this.btnExport.Size = new System.Drawing.Size(140, 36);
            this.btnExport.Click += new System.EventHandler(this.btnExport_Click);
            this.flpButtons.Controls.AddRange(new System.Windows.Forms.Control[] { this.btnMes, this.btnConversion, this.btnCurrency, this.txtOutputDirectory, this.btnSave, this.btnExport });
            this.txtLog.Multiline = true;
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Dock = System.Windows.Forms.DockStyle.Fill;
            this.txtLog.AccessibleName = "샘플 실행 결과";
            this.lbStatus.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lbStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            this.tlpRoot.Controls.Add(this.lbTitle, 0, 0);
            this.tlpRoot.Controls.Add(this.flpButtons, 0, 1);
            this.tlpRoot.Controls.Add(this.txtLog, 0, 2);
            this.tlpRoot.Controls.Add(this.lbStatus, 0, 3);
            this.Controls.Add(this.tlpRoot);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(820, 520);
            this.MinimumSize = new System.Drawing.Size(640, 460);
            this.Font = new System.Drawing.Font("맑은 고딕", 10F);
            this.Text = "김근재 · 공개 코드 샘플 (모의 실행)";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.flpButtons.ResumeLayout(false);
            this.flpButtons.PerformLayout();
            this.tlpRoot.ResumeLayout(false);
            this.tlpRoot.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
