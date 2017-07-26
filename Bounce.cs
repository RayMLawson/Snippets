using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BounceBack
{

    


    public partial class Form1 : Form
    {

        /* ***************************************************************************************************************************************************************
         *  A set of global variables help keep things running.
         *  
         *  Form_BounceinProgress is turned on when you punch the button to recalculate drop percentages.  That process could take a while and using a switch will keep
         *  the button from running again and again.
         *  
         *  The DataTable Form_BounceGraph_DT is used to hold the details for both the bar chart with recover hits and the secondary graph for details.  I've made it global 
         *  instead of putting it in a class because I want only one AND I didn't know how a data table would react in a class.
         *  
         *  GraphAnalysis is a global class of variables to be used with the DataTable
         *  
         * *************************************************************************************************************************************************************** */

        public bool Form_BounceinProgress = true;

        public DataTable Form_BounceGraph_DT = new DataTable();


        public class GraphAnalysis
        {

            public int i_Rows = 0;
            public int i_ToolTip_col = 0;
            public bool b_Initialized = false;

            // the X Y Coordinates on the form are doubles so we'll set these to double even though we'll always be putting in an integer value.
            public double d_TY = 0;
            public double d_BY = 0;
            public double d_LX = 0;
            public double d_RX = 0;

            // the "Border" points reflect the area of the graph that holds bars.  That space is just shy of the total chart area because there's a margin both left and right.
            // Not exactly sure what this margin is but I expect it's the width of one bar.   So, we'll keep the constant values of the graph and calculate the bar area every time
            // we go to use it.

            private double pd_TY = 15;
            private double pd_BY = 200;
            private double pd_LX = 58;
            private double pd_RX = 518;

            public int i_gmax = 20;

            ///<summary>
            ///Determine the width of the graph holding bars based on the number of bars (i_Rows).
            ///</summary>
            public void Set_Boundary()
            {
                // I was thinking of passing the row count but I already have it in the class so I should be good.

                if (i_Rows == 0)
                {
                    d_TY = 0;
                    d_BY = 0;
                    d_LX = 0;
                    d_RX = 0;
                }
                else
                {
                    // We're not doing anything right now with the y coordinates.  Just initialize them.
                    d_TY = pd_TY;
                    d_BY = pd_BY;

                    // adjust 1/2 A Bar to both the left and the right.
                    d_LX = pd_LX + Convert.ToInt32(((pd_RX - pd_LX) / (i_Rows + 1)) / 2);
                    d_RX = pd_RX - Convert.ToInt32(((pd_RX - pd_LX) / (i_Rows + 1)) / 2);
                }


            }

        }

        GraphAnalysis BG_Detail = new GraphAnalysis();



        /* ***************************************************************************************************************************************************************
         *  The StockAnalysis class is used to hold all variables needed when you select a symbol from the "Bounce List" looking for details.
         *  When you move from symbol to symbol you need to clear the global DataTable Form_BounceGraph_DT before you start a new one.
         * *************************************************************************************************************************************************************** */


        public class StockAnalysis
        {

            public double sa_PricePoint;
            public int sa_DaystoPrice;
            public bool sa_DroppingPrice;

            public int sa_PriceBounces;
            public int sa_AvgBounce;
            public int sa_MedBounce;

            public string sa_DropDate;
            public string sa_DropPct;
            public string sa_HighPrice;


            public double sa_pct;

            public int[] sa_BounceDay = new int[100];


            ///<summary>
            ///Prepare StockAnalysis for a new symbol.
            ///</summary>
            public void ClearBounces()
            {
                for (int i = 0; i < 100; i++) sa_BounceDay[i] = 0;
                sa_PricePoint = 0.0;
                sa_DaystoPrice = 0;
                sa_PriceBounces = 0;
                sa_AvgBounce = 0;
                sa_DroppingPrice = false;

            }

            ///<summary>
            ///Set the percent in the class to reflect the user input.
            ///</summary>
            public void SetPct(double NewPct)
            {
                sa_pct = NewPct;

            }

            ///<summary>
            ///Check the price coming in against yesterday and take the appropriate action.
            ///</summary>
            public void SetPricePoint(int Symbolday, double NewPrice, string NewDate)
            {
                double OldPrice = sa_PricePoint;
                double Pricediff = 0.0;
                int sumdays = 0;


                // So the first thing I need to know is if this is the first price point.  
                // If so, just set it and move on.
                if (OldPrice == 0.0)
                {
                    sa_PricePoint = NewPrice;
                    sa_HighPrice = Convert.ToString(sa_PricePoint);
                    sa_DroppingPrice = false;
                }
                else
                {
                    // Are you in a drop?  If so, just keep incrementing the counter until your new price exceeds the price point.
                    if (sa_DroppingPrice)
                    {
                        if (NewPrice > sa_PricePoint)
                        {

                            //MessageBox.Show(Convert.ToString(sa_DaystoPrice), "Recovered");
                            // If you made it to the price point, register the number of days, calculate the average and increment the bounce.
                            ++sa_PriceBounces;
                            sa_BounceDay[sa_PriceBounces] = sa_DaystoPrice;
                            sa_DaystoPrice = 0;

                            sumdays = 0;
                            for (int i = 1; i <= sa_PriceBounces; i++)
                            {
                                sumdays += sa_BounceDay[i];
                            }

                            sa_AvgBounce = Convert.ToInt16(sumdays / sa_PriceBounces);
                            sa_DroppingPrice = false;
                            sa_PricePoint = NewPrice;
                        }
                        else
                        {
                            ++sa_DaystoPrice;
                        }
                    }
                    else
                    {

                        // If not I want to compare yesterday (which by default here is the price point) to today (The new price)
                        // and see if it dropped and if it did drop did it drop past the user input.
                        Pricediff = ((sa_PricePoint / NewPrice) - 1.0) * 100.0;

                        if (Pricediff > sa_pct)
                        {
                            sa_DroppingPrice = true;
                            sa_DaystoPrice = 1;

                            sa_DropDate = NewDate;
                            sa_DropPct = Convert.ToString(Convert.ToDouble(Convert.ToInt32(Pricediff * 100)) / 100.0);


                            // MessageBox.Show(Convert.ToString(Math.Round(sa_PricePoint, 2)) + " - " + Convert.ToString(Math.Round(NewPrice, 2)), Convert.ToString(Math.Round(Pricediff, 2)));


                        }
                        else
                        {
                            // Otherwise today become yesterday - set the price point.
                            sa_PricePoint = NewPrice;
                        }
                    }
                }


            }

            ///<summary>
            /// Half the recoveries are less than this, half are more.
            ///</summary>
            public int find_median()
            {
                //  so it turns out the average doesn't do me much good.  I'm more interested in the median.  To get there
                // I'm going to sort the array of days and pick the middle.  The bounce day array starts filling at 1.

                int fm_days = 0;
                List<int> med_array = new List<int>();


                if (sa_PriceBounces > 0)
                {
                    for (int i = 0; i < sa_PriceBounces; i++)
                    {
                        med_array.Add(sa_BounceDay[i + 1]);
                    }

                    med_array.Sort();

                    fm_days = med_array[sa_PriceBounces / 2];

                }

                return fm_days;
            }


        }

        ///<summary>
        ///Given the positon of the cursor return the column it sits on in the bar chart IF it's lined up with a bar.
        ///</summary>
        public int BB_Calc_Col(double LX, double RX, double TY, double BY, int NbrCol, double CX, double CY)
        {
            int Col_Val = 0;

            // OK, so this guy is going to make sure your XY coordinates are within the frame and then report back what column XY is on.
            // The Top Y is the smaller number.
            if (CX > LX & CX < RX & CY > TY & CY < BY)
            {

                // So it looks like the graph has space on the ends that need to be taken into account.
                // I'm guessing the "pad" is equivalent to one bar.   Remember the convert is going to round.
                Col_Val = Convert.ToInt32(((CX - LX) / ((RX - LX) / NbrCol)) + 0.5);

            }

            return Col_Val;

        }

        ///<summary>
        ///Display the associated information for a bar in the graph label.
        ///</summary>
        public bool BB_Set_BounceGraphDetail(int NC)
        {
            bool Detail_Set = false;

            // if the value passed is within the column range reset the details in the lable 
            // This routine relies on global variables.



            if (NC <= BG_Detail.i_Rows & NC != BG_Detail.i_ToolTip_col)
            {
                BG_Detail.i_ToolTip_col = NC;

                if (NC != 0)
                    Label_GraphDetail.Text = Convert.ToString(Form_BounceGraph_DT.Rows[NC - 1]["DropDate"]) + "\n\n"
                         + "Days to Recover - " + Convert.ToString(Form_BounceGraph_DT.Rows[NC - 1]["RecoverDays"]) + "\n\n"
                         + "$ " + Convert.ToString(Form_BounceGraph_DT.Rows[NC - 1]["Price"]) + "\n"
                         + "% " + Convert.ToString(Form_BounceGraph_DT.Rows[NC - 1]["DropPct"]);



                Detail_Set = true;

            }

            // Mousemove happens so often you can't put an else error message in here.

            return Detail_Set;

        }


        public Form1()
        {
            InitializeComponent();


            // I think this should be in form load.  I need it to run one time to set up the bouncegraph data table.  Form Load didn't want to run for me??

            DataColumn dc;

            BG_Detail.b_Initialized = true;

            dc = new DataColumn();
            dc.ColumnName = "DropDate";
            Form_BounceGraph_DT.Columns.Add(dc);

            dc = new DataColumn();
            dc.ColumnName = "GraphDays";
            Form_BounceGraph_DT.Columns.Add(dc);

            dc = new DataColumn();
            dc.ColumnName = "RecoverDays";
            Form_BounceGraph_DT.Columns.Add(dc);

            dc = new DataColumn();
            dc.ColumnName = "Price";
            Form_BounceGraph_DT.Columns.Add(dc);

            dc = new DataColumn();
            dc.ColumnName = "DropPct";
            Form_BounceGraph_DT.Columns.Add(dc);

            dc = new DataColumn();
            dc.ColumnName = "TickerID";
            Form_BounceGraph_DT.Columns.Add(dc);

            BounceGraph.DataSource = Form_BounceGraph_DT;
            BounceGraph.Series[0].XValueMember = "DropDate";
            BounceGraph.Series[0].YValueMembers = "GraphDays";



            BounceGraph.DataBind();
        }


        private void Form1_Load(object sender, EventArgs e)
        {

            // This load isn't getting called ?
        }



        /* ***************************************************************************************************************************************************************
         *  This is the best routine I found to identify when a user picks a symbol from the list of symbols.   
         *  It will display the stock activity for that ticker in the line chart on the bottom right of the form
         *  and the times that stock dropped below the percent entered in a single day with the days it took to recover as
         *  the bar chart amount.
         * *************************************************************************************************************************************************************** */

        private void dataGridVBounce_CellRowEnter(object sender, DataGridViewCellEventArgs e)
        {
            // Interesting thing happens here.  The first selection you make after a grid reset doesn't catch here?
            // It acts like you have to double click the first row to take an action?
            string dgr_symbol = "xxx";

            bool DummyFlag = false;

            int bri = e.RowIndex;

            double gvb_TodaysPrice = 0.0;
            int gvb_Tickerid = 0;
            string gvb_Date;
            int gvb_counter = 0;

            StockAnalysis gvb_Look = new StockAnalysis();
            gvb_Look.ClearBounces();

            DummyFlag = Check_on_pct(gvb_Look);



            if (!Form_BounceinProgress)
            {

                // OK so I'm changing things up here a bit.  Instead of adding XY elements to bouncegraph I'm going to fill a datatable bound to the graph in the stock analyis routines.
                // This section will be respobsible for clearing the table and setting the global row value.

                //BounceinProgress = true;

                dgr_symbol = dataGridVBounce.Rows[bri].Cells[0].Value.ToString();

                DataRow dr;

                // Initialize the query


                BounceBack.BounceData2DataSetTableAdapters.DailyQuoteTableAdapter BBDGQ;
                BBDGQ = new BounceData2DataSetTableAdapters.DailyQuoteTableAdapter();

                BounceData2DataSet.DailyQuoteDataTable List_BouncePoints;
                List_BouncePoints = BBDGQ.GetDataBySymbol(dgr_symbol);


                BounceChart.Series[0].Points.Clear();

                BounceChart.Series[0].Name = dgr_symbol;

                //BounceGraph.Series[0].Points.Clear();
                Form_BounceGraph_DT.Clear();



                for (int i = 1; i < List_BouncePoints.Rows.Count; i++)
                {
                    gvb_TodaysPrice = List_BouncePoints.Rows[i].Field<double>("open");
                    gvb_Tickerid = List_BouncePoints.Rows[i].Field<int>("TickerID");
                    gvb_Date = List_BouncePoints.Rows[i].Field<string>("date");
                    BounceChart.Series[0].Points.AddXY(i, gvb_TodaysPrice);

                    // Here I want to run through the same logic the bounce process did when looking up all symbols. 
                    // Then display the bounced days in the Bouncegraph chart.
                    gvb_Look.SetPricePoint(gvb_Tickerid, gvb_TodaysPrice, gvb_Date);

                    // Check to see if a bounce was recorded.
                    if (gvb_Look.sa_PriceBounces > gvb_counter)
                    {

                        gvb_counter = gvb_Look.sa_PriceBounces;

                        dr = Form_BounceGraph_DT.NewRow();
                        dr["DropDate"] = gvb_Look.sa_DropDate;
                        dr["GraphDays"] = Convert.ToString(Math.Min(BG_Detail.i_gmax, gvb_Look.sa_BounceDay[gvb_counter]));
                        dr["RecoverDays"] = Convert.ToString(gvb_Look.sa_BounceDay[gvb_counter]);
                        dr["Price"] = Convert.ToString(gvb_Look.sa_PricePoint);
                        dr["DropPct"] = gvb_Look.sa_DropPct;
                        dr["TickerID"] = Convert.ToString(gvb_Tickerid);


                        Form_BounceGraph_DT.Rows.Add(dr);



                    }
                }


                // Set the row after everything is full.
                BG_Detail.i_Rows = gvb_Look.sa_PriceBounces;
                BG_Detail.Set_Boundary();

                BounceGraph.DataSource = Form_BounceGraph_DT;
                BounceGraph.Series["Days"].XValueMember = "DropDate";
                BounceGraph.Series["Days"].YValueMembers = "GraphDays";



                BounceGraph.DataBind();

            }

        }


        /* ***************************************************************************************************************************************************************
         * Funny thing, the text box on a form doesn't seem to have a setting to allow for numbers only.  
         * So I found a routine to check the keypress on the text label to keep out anything non-numeric but it does allow you to enter two decimal spots.
         * Before you start any analysis call this routine to make sure the percentage entered is actually a number.
         * *************************************************************************************************************************************************************** */

        private bool Check_on_pct(StockAnalysis cop_Look)
        {
            bool alliswell = true;
            double cop_pct = 0.0;

            try
            {
                cop_pct = Convert.ToDouble(Text_Box_Pct.Text);
            }

            catch
            {
                alliswell = false;
                MessageBox.Show("Check your number again.  It doesn't look right.", "OOPS");
                cop_pct = 0.0;
            }

            finally
            {
                cop_Look.SetPct(cop_pct);
            }

            return alliswell;

        }


        /* ***************************************************************************************************************************************************************
         * 1. Confirm the informaiton needed (Drop Percent) has been entered.
         * 2. Clear the last set of data.
         * 3. Collect the symbol information based on the percent entered.
         * *************************************************************************************************************************************************************** */

        private void button1_Click(object sender, EventArgs e)
        {

            String ls_CurSymbol = "";
            String ls_RecSymbol = "";
            double ld_TodaysPrice = 0.0;

            int li_stockday = 0;


            int i = 0;

            StockAnalysis b1c_Look = new StockAnalysis();
            b1c_Look.ClearBounces();

            Form_BounceinProgress = Check_on_pct(b1c_Look);

            if (Form_BounceinProgress && b1c_Look.sa_pct > 0.0)
            {
                // Initialize the query
                BounceBack.BounceData2DataSetTableAdapters.DailyQuoteTableAdapter BBQTA;
                BBQTA = new BounceData2DataSetTableAdapters.DailyQuoteTableAdapter();

                BounceData2DataSet.DailyQuoteDataTable List_BounceHistory;
                List_BounceHistory = BBQTA.GetData();

                dataGridVBounce.Rows.Clear();


                /* So the plan here is to run through the stock history and collect the times the stock fell by the percent entered in a single day.
                 * From that day count how many days it took to get back to the price before the fall or the bounceback.
                 * Then display the number of times that drop happened and the average days back.
                 */

                for (i = 0; i < List_BounceHistory.Rows.Count; i++)
                {


                    DataRow BounceRow = List_BounceHistory.Rows[i];
                    ls_RecSymbol = BounceRow.Field<string>("Symbol");
                    ld_TodaysPrice = BounceRow.Field<double>("open");
                    li_stockday = BounceRow.Field<int>("TickerID");


                    // Are you the first record of a new stock?
                    if (ls_RecSymbol != ls_CurSymbol)
                    {



                        // If you're not the first stock add the details of the current stock.
                        if (i > 0)
                        {
                            dataGridVBounce.Rows.Add(ls_CurSymbol, $"{b1c_Look.sa_PriceBounces}", $"{b1c_Look.find_median()}");
                        }

                        // Set the variables to start a new stock.
                        b1c_Look.ClearBounces();

                        ls_CurSymbol = ls_RecSymbol;
                        //MessageBox.Show("New stock", ls_CurSymbol);


                    }

                    // all the work happens here.
                    b1c_Look.SetPricePoint(1, ld_TodaysPrice, "NoDate");


                }

                // The for loop does not add the last stock.  Do that here.

                dataGridVBounce.Rows.Add(ls_CurSymbol, $"{b1c_Look.sa_PriceBounces}", $"{b1c_Look.sa_AvgBounce}");

                // RML - This highlights the first row. 
                // I'm thinking when this is fleshed out the first row will be one of the exchanges
                // so you won't have to worry about there not being a row to select.
                dataGridVBounce.Rows[0].Selected = true;

                Form_BounceinProgress = false;

             }
        }


        /* ***************************************************************************************************************************************************************
         * Found this clever little routine that keeps data entry to numerics and a decimal.  46 and 8 are corresponding ASCII numbers for . and backspace
         * *************************************************************************************************************************************************************** */

        private void Text_Box_Pct_KeyPress(object sender, KeyPressEventArgs e)
        {
            // Pull the character entered from the key
            char TBP_ch = e.KeyChar;

            // so if you're not a digit, decimal or backspace trigger an issue
            if (!Char.IsDigit(TBP_ch) && TBP_ch != 46 && TBP_ch != 8)
            {
                e.Handled = true;
            }

        }


        /* ***************************************************************************************************************************************************************
         * Found this clever little routine that keeps data entry to numerics and a decimal.  46 and 8 are corresponding ASCII numbers for . and backspace
         * *************************************************************************************************************************************************************** */

        private void BounceGraph_MouseMove(object sender, MouseEventArgs e)
        {
            int MM_series_col = 0;
            bool MM_Series_Detail_Changed = false;

            // When the mouse moves around the bouncegraph we MAY want to change the Detail label.
            // To know if we do we need to know what column the mouse is sitting on.  e.X, e.Y.

            MM_series_col = BB_Calc_Col(BG_Detail.d_LX, BG_Detail.d_RX, BG_Detail.d_TY, BG_Detail.d_BY, BG_Detail.i_Rows, e.X, e.Y);

            // If the current detail column is not the mouse colum, reset the label details

            MM_Series_Detail_Changed = BB_Set_BounceGraphDetail(MM_series_col);
            //Label_GraphDetail.Text = Convert.ToString(MM_series_col) + ", " + Convert.ToString(e.X);


        }

        /* ***************************************************************************************************************************************************************
         * If you click one of the bars in the bouncegraph representing a recovery period this will replace the stock price details in the line chart to reflect just the 
         * days of the recovery period you selected.
         * *************************************************************************************************************************************************************** */

        private void BounceGraph_Click(object sender, EventArgs e)
        {
            
            // If you click in the graph AND the mouse is on a series column the stock detail will redisplay for that period  (Date - Recover days)

            //MessageBox.Show("click", Convert.ToString(BG_Detail.i_ToolTip_col));


            // Interesting thing happens here.  The first selection you make after a grid reset doesn't catch here?
            // It acts like you have to double click the first row to take an action?
            string dgr_symbol = "xxx";

            double gvb_TodaysPrice = 0.0;
            int gvb_Tickerid = 0;
            string gvb_Date;

            double dstartprice = 0.0;
            int istart = 0;
            int istop = 0;

            // There's no analysis in this loop.  Just look up the pricepoints and add them to the graph when you hit the Ticker ID and keep going for recover days.

            if (!Form_BounceinProgress)
            {

                // Initialize the query
                BounceBack.BounceData2DataSetTableAdapters.DailyQuoteTableAdapter BGCQ;
                BGCQ = new BounceData2DataSetTableAdapters.DailyQuoteTableAdapter();

                // We're pulling a detailed set from the existing symbol.
                dgr_symbol = BounceChart.Series[0].Name;

                BounceData2DataSet.DailyQuoteDataTable List_BouncePoints;
                List_BouncePoints = BGCQ.GetDataBySymbol(dgr_symbol);


                BounceChart.Series[0].Points.Clear();


                // It would be cleaner to hold the actual row instead of the Ticker ID but I don't want to go back to fix it.
                // Loop through the records to find the ticker ID.  Back up one day because we want to see the drop.


                for (int i = 1; i < List_BouncePoints.Rows.Count; i++)
                {
                    gvb_Tickerid = List_BouncePoints.Rows[i].Field<int>("TickerID");
                    gvb_TodaysPrice = List_BouncePoints.Rows[i].Field<double>("open");

                    if (gvb_Tickerid == Convert.ToInt32(Form_BounceGraph_DT.Rows[BG_Detail.i_ToolTip_col - 1]["TickerID"]))
                    {
                        istop = i;
                        i = List_BouncePoints.Rows.Count + 1;
                    }
                        
                                   
                }

                if (istop > 0)
                {
                    istart = istop - (Convert.ToInt32(Form_BounceGraph_DT.Rows[BG_Detail.i_ToolTip_col - 1]["RecoverDays"]) + 1);
                    dstartprice = List_BouncePoints.Rows[istart].Field<double>("open");
                }
                    


                for (int i = istart; i <= istop; i++)
                {
                    gvb_TodaysPrice = List_BouncePoints.Rows[i].Field<double>("open") - dstartprice;
                    gvb_Tickerid = List_BouncePoints.Rows[i].Field<int>("TickerID");
                    gvb_Date = List_BouncePoints.Rows[i].Field<string>("date");

                    BounceChart.Series[0].Points.AddXY(i - istart + 1, gvb_TodaysPrice);

                }

            }
        }

      

    }
}
