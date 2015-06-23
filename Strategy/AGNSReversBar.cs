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
    public class AGNSReversBar : Strategy
    {
		private Bar previousBar2;
		private Bar previousBar;
		private Bar currentBar;
		
		private double stopLossReversBar = 0;
		
		public double Price
		{get; set;}
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {


            CalculateOnBarClose = true;
			
			SyncAccountPosition = true;
			RealtimeErrorHandling = RealtimeErrorHandling.TakeNoAction; 
			
			if(RealTime == Switch.OFF)
			{
				Add(PeriodType.Tick, 1);
			}
        }

		
		protected override void OnStartUp()
		{
        	SetProfitTarget(CalculationMode.Ticks, ProfitTargetTicks);
		}
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if(BarsInProgress == 0)
			{
				if (Historical == true && History == Switch.OFF)
				{
					return;
				}
				
				previousBar2 = previousBar;
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
			}
			
			if(BarsInProgress == 1 && RealTime == Switch.OFF)
			{
				Price = Opens[1][0];
			}

        }
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if(RealTime == Switch.ON)
			{
				Price = e.Price;
				
				OrderAction();
			}
			
			
			
			
			
		}
		
		
		public void OrderAction()
		{
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				SetStopLoss(CalculationMode.Ticks, 100);

				if(previousBar.Trend == BarTrend.DOWN && currentBar.Trend == BarTrend.UP)
				{
					if(SwitchCondition == Switch.ON && IsCondition(previousBar.Low, Condition.MoreOrEqual ,currentBar.Low)  || SwitchCondition == Switch.OFF)
					{
						if(previousBar.High >= Price)
						{
							Print("previousBar -> " + previousBar.ToString);
							Print("currentBar -> " + currentBar.ToString);
							
							stopLossReversBar = currentBar.Low - TickSize;

							Print("=========");
							Print("Local time: " + DateTime.Now);
							Print("OrderAction.Buy");
							Print("=========");
							EnterLong();
							SetStopLoss(CalculationMode.Price, stopLossReversBar);
						}
					}
				}
				else if(previousBar.Trend == BarTrend.UP && currentBar.Trend == BarTrend.DOWN)
				{
					if(SwitchCondition == Switch.ON && IsCondition(previousBar.High, Condition.MoreOrEqual, currentBar.High) || SwitchCondition == Switch.OFF)
					{
						if(previousBar.Low <= Price)
						{
							Print("previousBar -> " + previousBar.ToString);
							Print("currentBar -> " + currentBar.ToString);
							
							stopLossReversBar = currentBar.High + TickSize;

							Print("=========");
							Print("OrderAction.Sell");
							Print("=========");
							EnterShort();
							SetStopLoss(CalculationMode.Price, stopLossReversBar);
						}
					}
				}
			}
		}
		
		private bool IsCondition(double number1, Condition condition, double number2)
		{
			switch(condition)
			{
				case Condition.Less:
					return number1 < number2;
				case Condition.More:
					return number1 > number2;
				case Condition.LessOrEqual:
					return number1 <= number2;
				case Condition.MoreOrEqual:
					return number1 >= number2;
				default:
					throw new NotImplementedException();
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
					if(Trend == BarTrend.DOWN) trend = Enum.GetName(typeof(BarTrend), 1);
					else if(Trend == BarTrend.UP) trend = Enum.GetName(typeof(BarTrend), 0);
					return string.Format("Bar :: High: {0}, Low: {1}, Trend: {2}", High, Low, trend);
				}
			}
			
			
		}
		

		public enum Switch
		{
			ON,
			OFF
		}
		
		public enum Condition
		{
			MoreOrEqual,
			LessOrEqual,
			Less,
			More
		}
		
		
        #region Properties

		
		[GridCategory("History")]
		public Switch History
		{get; set;}
		
		[GridCategory("RealTime")]
		public Switch RealTime
		{get; set;}
		
		
		[Description("Numbers of bars used for calculations")]
		[GridCategory("Order")]
		public int ProfitTargetTicks
		{get; set;}
		
		
		[GridCategory("PreviousBar <Condition> CurrentBar")]
		public Switch SwitchCondition
		{get; set;}
		
		[GridCategory("PreviousBar <Condition> CurrentBar")]
		public Condition BuyCondition
		{get; set;}
		
		[GridCategory("PreviousBar <Condition> CurrentBar")]
		public Condition SellCondition
		{get; set;}
				
        #endregion
    }
}
