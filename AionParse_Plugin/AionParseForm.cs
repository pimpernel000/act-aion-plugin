using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace AionParse_Plugin
{
    public partial class AionParseForm : UserControl
	{
		public AionParseForm(AionParse plugin)
		{
			InitializeComponent();
		}

        public void AddText(string text)
        {
            textBox1.Text += text + Environment.NewLine;
        }
	}
}
