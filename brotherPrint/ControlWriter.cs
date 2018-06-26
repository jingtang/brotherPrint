using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Controls;
namespace brotherPrint
{
    /// <summary>
    /// 将TextBox变为Console
    /// </summary>
    public class ControlWriter : Core.Features.ControlWriter
    {
        public ControlWriter(TextBox textBox) : base(textBox) { }
    }
}
