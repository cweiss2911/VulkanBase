using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sample
{
    public partial class MainForm : Form
    {
        public MainForm(FormWindowState windowState)
        {
            InitializeComponent();
            //  FormBorderStyle = FormBorderStyle.None;

            WindowState = windowState;
            Width = 1024;
            Height = 768;


            KeyDown += (object sender, KeyEventArgs e) =>
            {
                if (e.KeyCode == Keys.Escape)
                {
                    Close();
                }
            };

            FormClosed += (object sender, FormClosedEventArgs e) =>
            {
                Environment.Exit(4919);
            };
        }
    }
}
