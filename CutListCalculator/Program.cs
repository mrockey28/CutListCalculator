using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.ComponentModel;

namespace CutListCalculator
{

    public enum boardType
    {
        [Description("1x2")]
        ONE_BY_TWO = 0,
        [Description("1x3")]
        ONE_BY_THREE,
        [Description("1x4")]
        ONE_BY_FOUR,
        [Description("1x6")]
        ONE_BY_SIX,
        [Description("1x8")]
        ONE_BY_EIGHT,
        [Description("1x10")]
        ONE_BY_TEN,
        [Description("2x2")]
        TWO_BY_TWO,
        [Description("2x4")]
        TWO_BY_FOUR,
        [Description("2x6")]
        TWO_BY_SIX,
        [Description("2x8")]
        TWO_BY_EIGHT,
        [Description("2x10")]
        TWO_BY_TEN,
        [Description("4x4")]
        FOUR_BY_FOUR
    };

    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new Form1());
        }
    }
}
