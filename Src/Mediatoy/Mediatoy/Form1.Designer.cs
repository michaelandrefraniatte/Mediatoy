namespace Mediatoy
{
    partial class Form1
    {
        /// <summary>
        /// Variable nécessaire au concepteur.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Nettoyage des ressources utilisées.
        /// </summary>
        /// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Code généré par le Concepteur Windows Form

        /// <summary>
        /// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
        /// le contenu de cette méthode avec l'éditeur de code.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Form1));
            this.label1 = new System.Windows.Forms.Label();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.pbcrunchyroll = new System.Windows.Forms.PictureBox();
            this.pbmolotov = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pbcrunchyroll)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbmolotov)).BeginInit();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.Color.Transparent;
            this.label1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 16.2F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.ForeColor = System.Drawing.Color.White;
            this.label1.Location = new System.Drawing.Point(0, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(1342, 721);
            this.label1.TabIndex = 3;
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // timer1
            // 
            this.timer1.Enabled = true;
            this.timer1.Interval = 50;
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // pbcrunchyroll
            // 
            this.pbcrunchyroll.BackgroundImage = global::Mediatoy.Properties.Resources.crunchyroll;
            this.pbcrunchyroll.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pbcrunchyroll.Location = new System.Drawing.Point(129, 12);
            this.pbcrunchyroll.Name = "pbcrunchyroll";
            this.pbcrunchyroll.Size = new System.Drawing.Size(100, 100);
            this.pbcrunchyroll.TabIndex = 6;
            this.pbcrunchyroll.TabStop = false;
            this.pbcrunchyroll.Click += new System.EventHandler(this.pbcrunchyroll_Click);
            // 
            // pbmolotov
            // 
            this.pbmolotov.BackgroundImage = global::Mediatoy.Properties.Resources.molotov;
            this.pbmolotov.BackgroundImageLayout = System.Windows.Forms.ImageLayout.Zoom;
            this.pbmolotov.Location = new System.Drawing.Point(12, 12);
            this.pbmolotov.Name = "pbmolotov";
            this.pbmolotov.Size = new System.Drawing.Size(100, 100);
            this.pbmolotov.TabIndex = 5;
            this.pbmolotov.TabStop = false;
            this.pbmolotov.Click += new System.EventHandler(this.pbmolotov_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.ClientSize = new System.Drawing.Size(1342, 721);
            this.Controls.Add(this.pbmolotov);
            this.Controls.Add(this.pbcrunchyroll);
            this.Controls.Add(this.label1);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.Name = "Form1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "Mediatoy";
            this.TopMost = true;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.Load += new System.EventHandler(this.Form1_Load);
            this.SizeChanged += new System.EventHandler(this.Form1_SizeChanged);
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.Form1_KeyDown);
            ((System.ComponentModel.ISupportInitialize)(this.pbcrunchyroll)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbmolotov)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Timer timer1;
        private System.Windows.Forms.PictureBox pbmolotov;
        private System.Windows.Forms.PictureBox pbcrunchyroll;
    }
}

