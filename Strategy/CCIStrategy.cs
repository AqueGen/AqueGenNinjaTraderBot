#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class CCIStrategy : Strategy
    {
		private int	period		= 14;
		
		private Bar previousBar;
		private Bar currentBar;
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            SetStopLoss("", CalculationMode.Ticks, StopLossTicks, false);
            SetProfitTarget("", CalculationMode.Ticks, ProfitTargetTicks);

            CalculateOnBarClose = true;
			
			SyncAccountPosition = true;
			RealtimeErrorHandling = RealtimeErrorHandling.TakeNoAction; 
        }

		
		protected override void OnStartUp()
		{

		}
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			
			if (Historical == true && History == Switch.OFF)
			{
				return;
			}
			

			previousBar = currentBar;
			currentBar = new Bar();
			
			if(previousBar == null || currentBar == null)
			{
				return;
			}
			
			currentBar.High = High[0];
			currentBar.Low = Low[0];
			currentBar.Trend = Close[0] < Open[0] ?  BarTrend.DOWN : BarTrend.UP;

			Print("------");
			Print(Time[0]);
			Print("previousBar -> " + previousBar.ToString);
			Print("currentBar -> " + currentBar.ToString);
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				if(previousBar.Low > currentBar.Low 
					&& previousBar.Trend == BarTrend.DOWN && currentBar.Trend == BarTrend.UP)
				{
					Print("=========");
					Print("Local time: " + DateTime.Now);
					Print("OrderAction.Buy");
					Print("=========");
					EnterLong("");
				}
				else if(previousBar.High < currentBar.High 
					&& previousBar.Trend == BarTrend.UP && currentBar.Trend == BarTrend.DOWN)
				{
					Print("=========");
					Print("OrderAction.Sell");
					Print("=========");
					EnterShort("");
				}
			}
        }
		
		public enum BarTrend
		{
			UP,
			DOWN
		}
		
		
		public class Bar
		{
			public double High
			{get; set;}
			public double Low
			{get; set;}
			
			public BarTrend Trend
			{get; set;}
			
			public new string ToString
			{
				get
				{
					string trend = "";
					if(Trend == BarTrend.DOWN) trend = "DOWN";
					else if(Trend == BarTrend.UP) trend = "UP";
					return string.Format("Bar :: High: {0}, Low: {1}, Trend: {2}", High, Low, trend);
				}
			}
			
			
		}
		
		protected double CCIOnBarUpdate()
		{
			if (CurrentBar == 0)
				return 0;
			else
			{
				double mean = 0;
				for (int idx = Math.Min(CurrentBar, Period - 1); idx >= 0; idx--)
					mean += Math.Abs(Typical[idx] - SMA(Typical, Period)[0]);
				return (Typical[0] - SMA(Typical, Period)[0]) / (mean == 0 ? 1 : (0.015 * (mean / Math.Min(Period, CurrentBar + 1))));
			}
		}
		

		public enum Switch
		{
			ON,
			OFF
		}
		
        #region Properties

		[Description("Numbers of bars used for calculations")]
		[GridCategory("Parameters")]
		public int Period
		{
			get { return period; }
			set { period = Math.Max(1, value); }
		}
		
				
		[Description("Numbers of bars used for calculations")]
		[GridCategory("Order")]
		public int StopLossTicks
		{get; set;}
		
				
		[Description("Numbers of bars used for calculations")]
		[GridCategory("Order")]
		public int ProfitTargetTicks
		{get; set;}
		
		[GridCategory("History")]
		public Switch History
		{get; set;}
		
        #endregion
    }
}
