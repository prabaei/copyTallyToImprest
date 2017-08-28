using copyTallyToImprest.data;
using copyTallyToImprest.Model;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace copyTallyToImprest
{
    class Program
    {
        
        static void Main(string[] args)
        {
            ImprestEntities ef = new ImprestEntities();
            //ef.Database.ExecuteSqlCommand("update OpenCloseBalance set OpeningBalance += 200 ,closingBalance+=200 where TranDate > '2016-09-01' and AccountNo = '2722101010986'");
            //ef.SaveChanges();
            //CheckCloseBalance("2722101015984", ef);
           // ComputeForAccount("2722101015984", ef);
            var record = ef.Database.SqlQuery<string>(string.Format("SELECT AccountNo FROM [Imprest].[dbo].[TransactionReport] group by AccountNo"));
            List<string> accountnos = record.ToList();
            int Tcount = accountnos.Count();
            int ccount = 1;
            foreach (string acc in accountnos)
            {

                Console.WriteLine("computing ..{0} {1} of {2} ", acc,ccount,Tcount);
                ComputeForAccount(acc,ef);
                ccount++;
            }
            
        }

        public static void ComputeForAccount(string acc, ImprestEntities ef)
        {
  //          var creditTran = ef.Database.SqlQuery<CreditTrans>(string.Format(@"SELECT DATEPART(Year, VoucherDate) as Tyear, DATEPART(Month, VoucherDate)as Tmonth ,sum(Amount) CAmount
  //FROM [Imprest].[dbo].[TransactionReport]  group by DATEPART(Year, VoucherDate), DATEPART(Month, VoucherDate),AccountNo,credit having AccountNo='{0}'  and credit =1 order by Tyear asc,Tmonth asc", acc));
  //          List<CreditTrans> CT = creditTran.ToList();
  //          var debitTran = ef.Database.SqlQuery<DebitTrans>(string.Format(@"SELECT DATEPART(Year, VoucherDate) as Tyear, DATEPART(Month, VoucherDate)as Tmonth ,sum(Amount) DAmount
  //FROM [Imprest].[dbo].[TransactionReport]  group by DATEPART(Year, VoucherDate), DATEPART(Month, VoucherDate),AccountNo,credit having AccountNo='{0}'  and credit =0 order by Tyear asc,Tmonth asc", acc));
  //          List<DebitTrans> DT = debitTran.ToList();

            var startYMon = ef.Database.SqlQuery<StartYrMo>(string.Format(@"select min(DATEPART(Year, VoucherDate)) as [Yr] ,min(DATEPART(Month, VoucherDate)) as [Mo] 
  from [Imprest].[dbo].[TransactionReport] where AccountNo='{0}' 
  and DATEPART(Year, VoucherDate) = (select min(DATEPART(Year, VoucherDate)) from TransactionReport where AccountNo='{0}')", acc));
            var sMY = startYMon.SingleOrDefault();

            var openbalance = ef.Database.SqlQuery<openbalance>(string.Format(@"select max(Amount) openbal from TransactionReport where AccountNo = '{0}' and DATEPART(Year, VoucherDate) = (select min(DATEPART(Year, VoucherDate)) 
  from TransactionReport where AccountNo = '{0}')
  and DATEPART(Month, VoucherDate) = (select min(DATEPART(Month, VoucherDate)) 
  from TransactionReport where AccountNo = '{0}' and DATEPART(Year, VoucherDate) =(select min(DATEPART(Year, VoucherDate)) 
  from TransactionReport where AccountNo = '{0}') )", acc));
            var opBal = openbalance.SingleOrDefault();


            var endYrMo = ef.Database.SqlQuery<StartYrMo>(string.Format(@"select max(DATEPART(Year, VoucherDate)) as [Yr] ,max(DATEPART(Month, VoucherDate)) as [Mo] 
  from [Imprest].[dbo].[TransactionReport] where AccountNo='{0}' 
  and DATEPART(Year, VoucherDate) = (select max(DATEPART(Year, VoucherDate)) from TransactionReport where AccountNo='{0}')", acc));

            var enym = endYrMo.SingleOrDefault();

            

            var MOnthWiseTran = ef.Database.SqlQuery<YearMon>(string.Format(@"select Datepart(Month,VoucherDate) Tmonth,Datepart(Year,VoucherDate) Tyear from TransactionReport where AccountNo ='{0}' group by Datepart(Month,VoucherDate),Datepart(Year,VoucherDate) order by Tyear asc ,TMonth asc",acc));
            List<YearMon> totalMonwise = MOnthWiseTran.OrderBy(m=>m.Tyear).ThenBy(m=>m.Tmonth).ToList();
            bool isfirst = true;
            OpenCloseBalance openclbal = new OpenCloseBalance();
            decimal FinalOpenbal = 0;
            decimal FinalClosebal = 0;
            foreach (var rec in totalMonwise)
            {
                string TotalCredit = string.Format(@"select sum(Amount) as [Tamount]
  from [Imprest].[dbo].[TransactionReport] where AccountNo='{0}' 
  and DATEPART(Year, VoucherDate) = {1} and DATEPART(Month, VoucherDate) = {2} and credit =1", acc, rec.Tyear, rec.Tmonth);

                string TotalDebit = string.Format(@"select sum(Amount) as [Tamount]
  from [Imprest].[dbo].[TransactionReport] where AccountNo='{0}' 
  and DATEPART(Year, VoucherDate) = {1} and DATEPART(Month, VoucherDate) = {2} and credit =0", acc, rec.Tyear, rec.Tmonth);

                var tc = ef.Database.SqlQuery<decimal?>(TotalCredit);
                decimal? stc = tc.SingleOrDefault();
                stc=stc ?? 0;
                var td = ef.Database.SqlQuery<decimal?>(TotalDebit);
                decimal? std = td.SingleOrDefault();
                std = std ?? 0;
                if(isfirst)
                FinalClosebal = (decimal)stc - (decimal)std;
                else
                    FinalClosebal = (decimal)stc - (decimal)std+ FinalOpenbal;
                openclbal = new OpenCloseBalance();
                openclbal.AccountNo = acc;
                openclbal.closingBalance = FinalClosebal;
                if (isfirst)
                    openclbal.OpeningBalance = opBal.openbal;
                else
                    openclbal.OpeningBalance = FinalOpenbal;
                openclbal.TranDate = new DateTime(rec.Tyear, rec.Tmonth, 1);
                
                FinalOpenbal = FinalClosebal;
                ef.OpenCloseBalances.Add(openclbal);
                ef.SaveChanges();
                isfirst = false;
            }

            
        }


        public static void ComputeForAccount(string acc,DateTime period, ImprestEntities ef)
        {
            var startYMon = ef.Database.SqlQuery<StartYrMo>(string.Format(@"select min(DATEPART(Year, VoucherDate)) as [Yr] ,min(DATEPART(Month, VoucherDate)) as [Mo] 
  from [Imprest].[dbo].[TransactionReport] where AccountNo='{0}' 
  and DATEPART(Year, VoucherDate) = (select min(DATEPART(Year, VoucherDate)) from TransactionReport where AccountNo='{0}')", acc));
            var sMY = startYMon.SingleOrDefault();

            var openbalance = ef.Database.SqlQuery<openbalance>(string.Format(@"select max(Amount) openbal from TransactionReport where AccountNo = '{0}' and DATEPART(Year, VoucherDate) = (select min(DATEPART(Year, VoucherDate)) 
  from TransactionReport where AccountNo = '{0}')
  and DATEPART(Month, VoucherDate) = (select min(DATEPART(Month, VoucherDate)) 
  from TransactionReport where AccountNo = '{0}' and DATEPART(Year, VoucherDate) =(select min(DATEPART(Year, VoucherDate)) 
  from TransactionReport where AccountNo = '{0}') )", acc));
            var opBal = openbalance.SingleOrDefault();


            var endYrMo = ef.Database.SqlQuery<StartYrMo>(string.Format(@"select max(DATEPART(Year, VoucherDate)) as [Yr] ,max(DATEPART(Month, VoucherDate)) as [Mo] 
  from [Imprest].[dbo].[TransactionReport] where AccountNo='{0}' 
  and DATEPART(Year, VoucherDate) = (select max(DATEPART(Year, VoucherDate)) from TransactionReport where AccountNo='{0}')", acc));

            var enym = endYrMo.SingleOrDefault();

            var lastMonthDateTime = ef.Database.SqlQuery<YearMon>(string.Format(@"select top 1 Datepart(Month,VoucherDate) TMonth,Datepart(Year,VoucherDate) TYear
from TransactionReport where AccountNo ='{0}' and VoucherDate < '{1}'  group by Datepart(Month,VoucherDate),Datepart(Year,VoucherDate)    
order by Tyear desc ,TMonth desc",acc,new DateTime(period.Year,period.Month,1).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)));
            YearMon lmon = lastMonthDateTime.SingleOrDefault();
            if (lastMonthDateTime.SingleOrDefault() == null)
            {
                ComputeForAccount(acc, ef);
                return;
            }

            if(period.Month <= sMY.Mo && period.Year <= sMY.Yr)
            {
                ComputeForAccount(acc, ef);
                return;
            }
            var currentRecord = ef.Database.SqlQuery<OpenCloseBalance>(string.Format(@"select * from OpenCloseBalance where TranDate = '{0}' and AccountNo ='{1}'",new DateTime(lmon.Tyear,lmon.Tmonth,1).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture),acc));
            OpenCloseBalance curRecord = currentRecord.SingleOrDefault();
            if(curRecord == null)
            {
                ComputeForAccount(acc, ef);
                return;
            }
           
            
            var MOnthWiseTran = ef.Database.SqlQuery<YearMon>(string.Format(@"select Datepart(Month,VoucherDate) TMonth,Datepart(Year,VoucherDate) TYear from TransactionReport where AccountNo ='{0}' and VoucherDate > '{1}' group by Datepart(Month,VoucherDate),Datepart(Year,VoucherDate)  order by Tyear asc ,TMonth asc", acc,new DateTime(period.Year,period.Month,1).ToString("yyyy-MM-dd",CultureInfo.InvariantCulture)));
            List<YearMon> totalMonwise = MOnthWiseTran.OrderBy(m => m.Tyear).ThenBy(m => m.Tmonth).ToList();
            //bool isfirst = true;
            opBal.openbal = curRecord.closingBalance;
            OpenCloseBalance openclbal = new OpenCloseBalance();
            decimal FinalOpenbal = curRecord.closingBalance;
            decimal FinalClosebal = 0;
            foreach (var rec in totalMonwise)
            {
                string TotalCredit = string.Format(@"select sum(Amount) as [Tamount]
  from [Imprest].[dbo].[TransactionReport] where AccountNo='{0}' 
  and DATEPART(Year, VoucherDate) = {1} and DATEPART(Month, VoucherDate) = {2} and credit =1", acc, rec.Tyear, rec.Tmonth);

                string TotalDebit = string.Format(@"select sum(Amount) as [Tamount]
  from [Imprest].[dbo].[TransactionReport] where AccountNo='{0}' 
  and DATEPART(Year, VoucherDate) = {1} and DATEPART(Month, VoucherDate) = {2} and credit =0", acc, rec.Tyear, rec.Tmonth);

                var tc = ef.Database.SqlQuery<decimal?>(TotalCredit);
                decimal? stc = tc.SingleOrDefault();
                stc = stc ?? 0;
                var td = ef.Database.SqlQuery<decimal?>(TotalDebit);
                decimal? std = td.SingleOrDefault();
                std = std ?? 0;
                //if (isfirst)
                //    FinalClosebal = (decimal)stc - (decimal)std;
                //else
                    FinalClosebal = (decimal)stc - (decimal)std + FinalOpenbal;
                 openclbal = ef.OpenCloseBalances.Where(m=>m.AccountNo == acc && m.TranDate == new DateTime(rec.Tyear, rec.Tmonth, 1)).SingleOrDefault();
                bool isRecordExitst = false;
                if (openclbal == null)
                    isRecordExitst = false;
                else
                    isRecordExitst = true;
                if (!isRecordExitst)
                    openclbal = new OpenCloseBalance();

                openclbal.AccountNo = acc;
                openclbal.closingBalance = FinalClosebal;

                openclbal.OpeningBalance = FinalOpenbal;
                openclbal.TranDate = new DateTime(rec.Tyear, rec.Tmonth, 1);

                FinalOpenbal = FinalClosebal;
                if(!isRecordExitst)
                ef.OpenCloseBalances.Add(openclbal);
                ef.SaveChanges();
                //isfirst = false;

            }

        }

        public static void Smoothout(string acc, DateTime period,ImprestEntities ef)
        {
            var maxdate =ef.Database.SqlQuery<DateTime?>(string.Format("select max(TranDate) from OpenCloseBalance where AccountNo ='{0}'", acc));
            DateTime? dt = maxdate.SingleOrDefault();
            if (dt != null)
            {
                if(period > (DateTime)dt)
                {
                    ef.Database.SqlQuery<decimal?>(string.Format("select closingBalance from OpenCloseBalance where AccountNo = '{0}' and TranDate = (select max(TranDate) from OpenCloseBalance where AccountNo ='{0}')", acc));
                    //while()
                }
            }
        }

        public static void CheckCloseBalance(string Accno,ImprestEntities ef)
        {
            var rec=ef.ComputeOpenCloseBals.Where(m => m.AccountNo == Accno).SingleOrDefault();
            if (rec != null)
            {
                if (rec.Checked)
                {
                    ComputeForAccount(Accno, rec.Trandate, ef);
                }
            }

        }
    }
}
