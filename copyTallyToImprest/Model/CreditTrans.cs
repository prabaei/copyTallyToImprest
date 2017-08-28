using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace copyTallyToImprest.Model
{
    public class CreditTrans
    {
        public int Tyear { get; set; }
        public int Tmonth { get; set; }
        public decimal CAmount { get; set; }
    }

    public class DebitTrans
    {
        public int Tyear { get; set; }
        public int Tmonth { get; set; }
        public decimal DAmount { get; set; }
    }

    public class YearMon
    {
        public int Tyear { get; set; }
        public int Tmonth { get; set; }
    }

    public class StartYrMo
    {
        public int Yr { get; set; }
        public int Mo { get; set; }
    }

    public class openbalance
    {
        public decimal openbal { get; set; }
    }
}
