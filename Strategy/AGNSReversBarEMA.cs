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
    public class AGNSReversBarEMA : Strategy
    {

        private int fast = 1; // Default setting for Fast
        private int slow = 1; // Default setting for Slow

		private double startOrderPrice = 0;
		private OrderType orderType = OrderType.FLAT;
		private bool isBreakevenEnable = false;
		private bool isCanEnterOrder = false;
		
		
		private double previousFastEMA = 0;
		private double currentFastEMA = 0;
		
		private double previousSlowEMA = 0;
		private double currentSlowEMA = 0;

		private double stopLossPrice = 0;
		private double trailStopLossPrice = 0;
		
		private bool isTrailStopEnable = false;
		
		
		private double previousEMAFast = 0;
		private double currentEMAFast = 0;
		
		private double previousEMASlow = 0;
		private double currentEMASlow = 0;
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
			
			SetProfitTarget("Order", CalculationMode.Ticks, ProfitTarget);
        }

		protected override void OnTermination()
		{
			ExitShort("Order");
			ExitLong("Order");
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
			
			if (Historical == true)
			{
				return;
			}
			
			IDataSeries fastDataSeries = Close;
			IDataSeries slowDataSeries = Close;

				
			switch (FastInputSeries)
            {
                case InputSeries.CLOSE:
                    fastDataSeries = Close;
                    break;
                case InputSeries.HIGH:
                    fastDataSeries = High;
                    break;
                case InputSeries.LOW:
                    fastDataSeries = Low;
                    break;
                case InputSeries.MEDIAN:
                    fastDataSeries = Median;
                    break;
                case InputSeries.OPEN:
                    fastDataSeries = Open;
                    break;
                case InputSeries.TYPICAL:
                    fastDataSeries = Typical;
                    break;
                case InputSeries.WEIGHTED:
                    fastDataSeries = Weighted;
                    break;
            }
			
			switch (SlowInputSeries)
            {
                case InputSeries.CLOSE:
                    slowDataSeries = Close;
                    break;
                case InputSeries.HIGH:
                    slowDataSeries = High;
                    break;
                case InputSeries.LOW:
                    slowDataSeries = Low;
                    break;
                case InputSeries.MEDIAN:
                    slowDataSeries = Median;
                    break;
                case InputSeries.OPEN:
                    slowDataSeries = Open;
                    break;
                case InputSeries.TYPICAL:
                    slowDataSeries = Typical;
                    break;
                case InputSeries.WEIGHTED:
                    slowDataSeries = Weighted;
                    break;
            }
			
			previousEMAFast = currentEMAFast;
			currentEMAFast = EMA(fastDataSeries, Fast)[0];
			
			previousEMASlow = currentEMASlow;
			currentEMASlow = EMA(slowDataSeries, Slow)[0];

			//Print("previousEMAFast " + previousEMAFast);
			//Print("currentEMAFast " + currentEMAFast);
			//Print("currentEMASlow " + currentEMASlow);
			
			if(previousEMAFast == 0 || currentEMAFast == 0 || previousEMASlow == 0 || currentEMASlow == 0)
				return;
			
			if (previousEMAFast < currentEMASlow && currentEMAFast > currentEMASlow)
			{
				SetStopLoss("Order", CalculationMode.Ticks, StopLoss, false);
				
				if(orderType == OrderType.SELL || orderType == OrderType.FLAT)
				{
					isCanEnterOrder = true;
					isBreakevenEnable = false;
					isTrailStopEnable = false;
					Print("---------------");
					Print("Can Enter Order BUY");
				}
				else if(orderType == OrderType.BUY)
				{
					isCanEnterOrder = false;
					return;
				}
				Print("EMA buy");
				orderType = OrderType.BUY;
			}
			else if (previousEMAFast > currentEMASlow && currentEMAFast < currentEMASlow)
			{	
				SetStopLoss("Order", CalculationMode.Ticks, StopLoss, false);
				
				if(orderType == OrderType.BUY || orderType == OrderType.FLAT)
				{
					isCanEnterOrder = true;
					isBreakevenEnable = false;
					isTrailStopEnable = false;
					Print("---------------");
					Print("Can Enter Order SELL");
				}
				else if(orderType == OrderType.SELL)
				{
					isCanEnterOrder = false;
					return;
				}
				Print("EMA sell");
				orderType = OrderType.SELL;
			}

        }
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Last) 
			{
				double price = e.Price;
				if(isCanEnterOrder && orderType == OrderType.SELL)
				{
					isCanEnterOrder = false;
					startOrderPrice = price;
					trailStopLossPrice = startOrderPrice;
					Print("Sell startOrderPrice -> " + startOrderPrice);
					EnterShort("Order");
				}
				else if(isCanEnterOrder && orderType == OrderType.BUY)
				{
					isCanEnterOrder = false;
					startOrderPrice = price;
					trailStopLossPrice = startOrderPrice;
					Print("Buy startOrderPrice -> " + startOrderPrice);
					EnterLong("Order");
				}
				
				
				if (Position.MarketPosition != MarketPosition.Flat)
				{
					if(BreakevenSwitch == Switch.ON)
					{
						if(orderType == OrderType.BUY)
						{
							if(startOrderPrice + Breakeven * TickSize <= price && !isBreakevenEnable)
							{
								stopLossPrice = startOrderPrice + BreakevenPosition * TickSize;
								SetStopLoss("Order", CalculationMode.Price, stopLossPrice, false);
								isBreakevenEnable = true;
								Print("BuyStopLossBreakeven -> " + (startOrderPrice + BreakevenPosition * TickSize));
								Print("price -> " + price);
							}
						}
						else if(orderType == OrderType.SELL)
						{
							if(startOrderPrice - Breakeven * TickSize >= price && !isBreakevenEnable)
							{
								stopLossPrice = startOrderPrice - BreakevenPosition * TickSize;
								SetStopLoss("Order", CalculationMode.Price, stopLossPrice, false);
								isBreakevenEnable = true;
								Print("SellStopLossBreakeven -> " + (startOrderPrice - BreakevenPosition * TickSize));
								Print("price -> " + price);
							}
						}
					}
					if(TrailStopSwitch == Switch.ON)
					{
						if(orderType == OrderType.BUY && startOrderPrice + ProfitTrigger * TickSize <= price)
						{
							if(!isTrailStopEnable)
							{
								Print("Trail ON");
								isTrailStopEnable = true;
								trailStopLossPrice = price - TrailStop * TickSize;
								Print("Buy Change TrailStop " + trailStopLossPrice);
								Print("price -> " + price);
							}
							
							if(trailStopLossPrice + TrailStop * TickSize+ Frequency * TickSize <= price && isTrailStopEnable)
							{
								trailStopLossPrice = price - TrailStop * TickSize;
								SetStopLoss("Order", CalculationMode.Price, trailStopLossPrice, false);
								Print("Buy Change TrailStop " + trailStopLossPrice);
								Print("price -> " + price);
							}
						}
						else if(orderType == OrderType.SELL && startOrderPrice - ProfitTrigger * TickSize >= price)
						{
							if(!isTrailStopEnable)
							{
								Print("Trail ON");
								isTrailStopEnable = true;
								trailStopLossPrice = price + TrailStop * TickSize;
								Print("Sell Change TrailStop " + trailStopLossPrice);
								Print("price -> " + price);
							}
							
							if(trailStopLossPrice - TrailStop * TickSize - Frequency * TickSize >= price && isTrailStopEnable)
							{
								trailStopLossPrice = price + TrailStop * TickSize;
								SetStopLoss("Order", CalculationMode.Price, trailStopLossPrice, false);
								Print("Sell Change TrailStop " + trailStopLossPrice);
								Print("price -> " + price);
							}
						}
					}
				}
			}
		}
	
		
		
		public enum Switch
		{
			ON,
			OFF
		}
		
		public enum OrderType
		{
			BUY,
			SELL,
			FLAT
		}
		
		public enum InputSeries
		{
			CLOSE,
			HIGH,
			LOW,
			MEDIAN,
			OPEN,
			TYPICAL,
			WEIGHTED
		}
		
		
		[GridCategory("History")]
		public Switch History
		{get; set;}
		
		
		[GridCategory("Order")]
		public Switch BreakevenSwitch
		{get; set;}
		
		[Description("Цена безубытка, в тиках")]
		[GridCategory("Order")]
		public int BreakevenPosition
		{get; set;}
		
		[Description("Перенос стоп-лосса в безубыток, в тиках")]
		[GridCategory("Order")]
		public int Breakeven
		{get; set;}
		
		[Description("")]
		[GridCategory("Order")]
		public int StopLoss
		{get; set;}
		
		[Description("")]
		[GridCategory("Order")]
		public int ProfitTarget
		{get; set;}
		
		[Description("Включить трейлинг через, в тиках")]
		[GridCategory("TrailStop")]
		public int ProfitTrigger
		{get; set;}
		
		[Description("Расположение стоп-лосса за ценой, в тиках")]
		[GridCategory("TrailStop")]
		public int TrailStop
		{get; set;}
		
		[Description("Расположение стоп-лосса за ценой, в тиках")]
		[GridCategory("TrailStop")]
		public Switch TrailStopSwitch
		{get; set;}
		
		[Description("Как часто подтягивать стоп-лосс за ценой, в тиках")]
		[GridCategory("TrailStop")]
		public int Frequency
		{get; set;}
		
        [Description("")]
        [GridCategory("EMA")]
        public int Fast
        {
            get { return fast; }
            set { fast = Math.Max(1, value); }
        }
		[Description("")]
        [GridCategory("EMA")]
        public int Slow
        {
            get { return slow; }
            set { slow = Math.Max(1, value); }
        }
		[GridCategory("EMA")]
		public InputSeries FastInputSeries
		{get; set;}
		
		[GridCategory("EMA")]
		public InputSeries SlowInputSeries
		{get; set;}



    }
}
