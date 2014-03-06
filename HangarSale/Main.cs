using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using TheCodeKing.ActiveButtons.Controls;
using EveCom;
using EveComFramework.Core;
using EveComFramework.Data;

namespace HangarSale
{
    public partial class Main : Form
    {
        Cache Cache = Cache.Instance;
        Core Bot = Core.Instance;
        Settings Config = Core.Instance.Config;
        ActiveButton Shrink = new ActiveButton();
        ActiveButton Minimize = new ActiveButton();

        public Main()
        {
            InitializeComponent();

            LoggerHelper.Instance.Loggers.Where(a => !a.Name.StartsWith("State")).ForEach(a => { a.RichEvent += Console; });
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            IActiveMenu aMenu = ActiveMenu.GetInstance(this);
            Shrink.Text = "▲";
            Shrink.Click += new EventHandler(Shrink_Click);
            Minimize.Text = "▬";
            Minimize.Click += new EventHandler(Minimize_Click);
            aMenu.Items.Add(Minimize);
            aMenu.Items.Add(Shrink);

            if (Bot.SessionControl.characterName != null)
            {
                this.Text = "HangarSale: " + Bot.SessionControl.characterName;
            }
            else
            {
                this.Text = "HangarSale: <unknown>";
            }

            if (Bot.SessionControl.characterName != null)
            {
                if (Config.WindowSettings == null) Config.WindowSettings = new Settings.SerializableDictionary<string, WindowSettings>();
                if (Config.WindowSettings.ContainsKey(Bot.SessionControl.characterName))
                {
                    if (Config.WindowSettings[Bot.SessionControl.characterName].Shrunk)
                    {
                        this.Height = 78;
                        Shrink.Text = "▼";
                    }
                    else
                    {
                        this.Width = Config.WindowSettings[Bot.SessionControl.characterName].Width;
                        this.Height = Config.WindowSettings[Bot.SessionControl.characterName].Height;
                    }
                    this.Left = Config.WindowSettings[Bot.SessionControl.characterName].X;
                    this.Top = Config.WindowSettings[Bot.SessionControl.characterName].Y;
                }
            }

            comboPriceBase.Items.AddRange(SolarSystem.All.Select(a => a.Name).ToArray());
            comboPriceBase.Text = Config.PriceBase;
            checkMarkup.Checked = Config.Markup;

            numericPercentDiscount.Value = Config.PercentDiscount;
            numericPercentDiscount.ValueChanged += (s, a) => { Config.PercentDiscount = (int)numericPercentDiscount.Value; Config.Save(); };
            numericIskDiscount.Value = Config.IskDiscount;
            numericIskDiscount.ValueChanged += (s, a) => { Config.IskDiscount = (int)numericIskDiscount.Value; Config.Save(); };
            comboPriceMode.Text = Config.Method;
            comboPriceMode.TextChanged += (s, a) => { Config.Method = comboPriceMode.Text; Config.Save(); };

            checkAlwaysOnTop.Checked = Config.AlwaysOnTop;
            this.TopMost = Config.AlwaysOnTop;

        }

        delegate void SetConsole(string Module, string Message);

        void Console(string Module, string Message)
        {
            if (richConsole.InvokeRequired)
            {
                richConsole.BeginInvoke(new SetConsole(Console), Module, Message);
            }
            else
            {
                LoggerHelper.Instance.RichTextboxUpdater(richConsole, Module, Message);
            }
        }

        void Shrink_Click(object sender, EventArgs e)
        {
            if (this.Height == 340)
            {
                this.Height = 78;
                Shrink.Text = "▼";
            }
            else
            {
                this.Height = 340;
                Shrink.Text = "▲";
            }
            string Character = Bot.SessionControl.characterName;
            if (Character == null) Character = Cache.Name;
            if (Character != null)
            {
                if (Config.WindowSettings == null) Config.WindowSettings = new Settings.SerializableDictionary<string, WindowSettings>();
                WindowSettings newWindowSettings = new WindowSettings();
                if (this.Height == 78)
                {
                    newWindowSettings.Shrunk = true;
                }
                else
                {
                    newWindowSettings.Shrunk = false;
                }
                newWindowSettings.Width = this.Width;
                newWindowSettings.Height = this.Height;
                newWindowSettings.X = this.Left;
                newWindowSettings.Y = this.Top;
                Config.WindowSettings.AddOrUpdate(Character, newWindowSettings);
                Config.Save();
            }
        }
        void Minimize_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (Cache.Name != null)
            {
                this.Text = "HangarSale: " + Cache.Name;
            }
            else if (Bot.SessionControl.characterName != null)
            {
                this.Text = "HangarSale: " + Bot.SessionControl.characterName;
            }
            else
            {
                this.Text = "HangarSale: <unknown>";
            }
        }

        private void Main_ResizeEnd(object sender, EventArgs e)
        {
            string Character = Bot.SessionControl.characterName;
            if (Character == null) Character = Cache.Name;
            if (Character != null)
            {
                if (Config.WindowSettings == null) Config.WindowSettings = new Settings.SerializableDictionary<string, WindowSettings>();
                WindowSettings newWindowSettings = new WindowSettings();
                if (this.Height == 78)
                {
                    newWindowSettings.Shrunk = true;
                }
                else
                {
                    newWindowSettings.Shrunk = false;
                }
                newWindowSettings.Width = this.Width;
                newWindowSettings.Height = this.Height;
                newWindowSettings.X = this.Left;
                newWindowSettings.Y = this.Top;
                Config.WindowSettings.AddOrUpdate(Character, newWindowSettings);
                Config.Save();
            }
        }

        private void checkEnabled_CheckedChanged(object sender, EventArgs e)
        {
            Bot.Enabled(checkEnabled.Checked);
        }

        private void comboPriceBase_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (comboPriceBase.Text != "")
            {
                Config.PriceBase = comboPriceBase.Text;
                Config.Save();
            }
        }

        private void checkMarkup_CheckedChanged(object sender, EventArgs e)
        {
            Config.Markup = checkMarkup.Checked;
            Config.Save();
        }


    }
}
