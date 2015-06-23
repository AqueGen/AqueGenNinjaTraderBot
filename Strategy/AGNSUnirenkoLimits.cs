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
using System.Collections.Generic;


#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class AGNSUnirenkoLimits : Strategy
    {

        private int fast = 1; // Default setting for Fast
        private int slow = 1; // Default setting for Slow

		
		private OrderType orderType = OrderType.FLAT;
		
		
		private double stopLossPrice = 0;
		private double trailStopLossPrice = 0;
		
		private bool isTrailStopEnable = false;
		
		
		private int orderEnterBar = 0;
		
		private Bar previousBar = null;
		private Bar currentBar = null;
		
		
		private double startOrderPrice = 0;
		private DateTime startOrderDateTime;
		private OrderType startOrderType = OrderType.FLAT;
		
		private int usedOrders = 0;
		
		private bool isStopLimitOrder = false;
		private IOrder order = null;
		//private IOrder buyOrder = null;
		//private IOrder sellOrder = null;

		private double Price;
		private bool isCanEnterReversOrder = false;
		
		private Dictionary<double, long> depthDictionary;
		
		private double _smaValue1 = 0;
		private double _smaValue2 = 0;
			
		private int _countOfDepthRows = 5;
		private int _timeFrameSMA = 1;
		
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
						
			SetStopLoss(CalculationMode.Ticks, StopLoss);
			SetProfitTarget(CalculationMode.Ticks, ProfitTarget);
			
			Add(PeriodType.Minute, TimeFrameSMA);
        }

		protected override void OnTermination()
		{

		}
		
		protected override void OnStartUp()
		{
			//BuyOrder = new EnteredOrder();
			//SellOrder = new EnteredOrder();
			if(CurrentBar >= 1)
			{
				previousBar = new Bar
				{
					High = High[1],
					Low = Low[1]
				};
				currentBar = new Bar
				{
					High = High[0],
					Low = Low[0]
				};
			}
			
			depthDictionary = new Dictionary<double, long>();
		}
		
		
		private string GetOrderState(IOrder order)
		{						
			switch(order.OrderState)
			{
				case OrderState.Accepted: return "OrderState.Accepted"; break;
				case OrderState.Cancelled: return "OrderState.Cancelled"; break;
				case OrderState.Filled: return "OrderState.Filled"; break;
				case OrderState.Initialized: return "OrderState.Initialized"; break;
				case OrderState.PartFilled: return "OrderState.PartFilled"; break;
				case OrderState.PendingCancel: return "OrderState.PendingCancel"; break;
				case OrderState.PendingChange: return "OrderState.PendingChange"; break;
				case OrderState.PendingSubmit: return "OrderState.PendingSubmit"; break;
				case OrderState.Rejected: return "OrderState.Rejected"; break;
				case OrderState.Unknown: return "OrderState.Unknown"; break;
				case OrderState.Working: return "OrderState.Working"; break;	
				default: return "Not found state";
			}
			
		}
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		/// 
				 
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
			
			
			
			if(BarsInProgress == 1)
			{
				if(SMAFilterSwitch == Switch.ON)
				{
				Print("-----------------");
				
				_smaValue1 = SMA(BarsArray[1], SMAPeriod1)[0];
				_smaValue2 = SMA(BarsArray[1], SMAPeriod2)[0];
				
				Print("Custom timeframe: " + Times[1][0]);
				Print("SMA1 in Custom timeframe " + _smaValue1);
				Print("SMA2 in Custom timeframe " + _smaValue2);
				Print("-----------------");
				}
			}
			
			
			if(BarsInProgress == 0)
			{		
				Print("================");
				if(!IsStrategyCanWork(Time[0], StartWorkHour, StartWorkMinutes, EndWorkHour, EndWorkMinutes) && WorkTimeSwitch == Switch.ON)
				{
					Print(Time[0].Hour);
					Print(string.Format("Time for work is: start: {0}:{1}, end: {2}:{3}. But now time is: {4}", StartWorkHour, StartWorkMinutes, EndWorkHour, EndWorkMinutes, Time[0]));
					Print("Bad time for work");
					return;
				}
				
				bool isCanSetLimitOrder = false;
				if(SMAFilterSwitch == Switch.ON)
				{
					if(_smaValue1 <= 0 || _smaValue2 <= 0)
					{
						Print(string.Format("SMAValue1 = {0}, SMAValue2 = {1}", _smaValue1, _smaValue2));
						Print("Please wait while SMA will be more than 0");
						return;
					}

					
					double ticksBetweenSMA = Math.Abs(_smaValue1 - _smaValue2) / TickSize;
					
					Print("SMA1 in unirenko timeframe " + _smaValue1);
					Print("SMA2 in unirenko timeframe " + _smaValue2);
					Print("Tick between SMAs: " + ticksBetweenSMA);
						
					if(ticksBetweenSMA >= TicksBetweenSMA)
					{
						isCanSetLimitOrder = true;
						Print("Limit Order can set with good SMA");
					}
					else
					{
						isCanSetLimitOrder = false;
						Print("Limit Order can`t set with bad SMA");
					}
					
				}

				previousBar = currentBar;
				currentBar = new Bar
				{
					High = Highs[0][0],
					Low = Lows[0][0]
				};
				
				if(previousBar == null || currentBar == null)
				{
					Print("previousBar or currentBar is null");
					Print("Return");
					return;
				}

				double barDiapasone = Math.Round(currentBar.Diapasone, 2) - TickSize;
				double openOffsetTickPrice = OpenOffset * TickSize;
				
				if(openOffsetTickPrice != barDiapasone)
				{
					Print("OpenOffset-> " + openOffsetTickPrice);
					Print("Bar Diapasone -> " + barDiapasone);
					Print("Not correct bar size");
					Print("Return");
					return;
				}

				
				usedOrders = 0;

				if(SMAFilterSwitch == Switch.ON && isCanSetLimitOrder || SMAFilterSwitch == Switch.OFF)
				{
					if(Position.MarketPosition == MarketPosition.Flat)
					{
						isCanEnterReversOrder = false;
						
						if(previousBar.Low > currentBar.Low /*&& previousBar.High > currentBar.High*/)
						{
							if(Price < currentBar.High)
							{
								SetStopLoss(CalculationMode.Ticks, StopLoss);
								Print("----------");
								double high = currentBar.High - 1 * TickSize;
								//order = EnterLongStopLimit(high, high);
								order = EnterLongStopLimit(0, true, 1, high, high, "");
								orderType = OrderType.BUY;
								isStopLimitOrder = true;

								Print(Time[0]);
								Print("Price " + Price);
								Print("Enter Long Limit " + high);
								Print("Enter Long Stop " + high);

							}
						}
						else if(/*previousBar.Low < currentBar.Low &&*/ previousBar.High < currentBar.High)
						{
							if(Price > currentBar.Low)
							{
								SetStopLoss(CalculationMode.Ticks, StopLoss);
								Print("----------");
								double low = currentBar.Low + 1 * TickSize;
								//order = EnterShortStopLimit(low, low);
								order = EnterShortStopLimit(0, true, 1, low, low, "");
								orderType = OrderType.SELL;
								isStopLimitOrder = true;
								
								Print(Time[0]);
								Print("Price " + Price);
								Print("Enter Short Limit " + low);
								Print("Enter Short Stop " + low);
							}
						}
					}
				}
			}
        }
		
		
		private	List<LadderRow>	askRows	= new List<LadderRow>();
		private	List<LadderRow>	bidRows	= new List<LadderRow>();
		
		private bool firstAskEvent	= true;
		private bool firstBidEvent	= true;
		
		
		public class LadderRow
		{
			public	string	MarketMaker;			// relevant for stocks only
			public	double	Price;
			public	long	Volume;

			public LadderRow(double myPrice, long myVolume, string myMarketMaker)
			{
				MarketMaker	= myMarketMaker;
				Price		= myPrice;
				Volume		= myVolume;
			}
		}
		
		private void AddUpdatedItem(Dictionary<double, long> dictionary, double key, long value)
        {
            if (dictionary.ContainsKey(key))
            {
                dictionary.Remove(key);
            }
            dictionary.Add(key, value);
        }
		
		protected override void OnMarketDepth(MarketDepthEventArgs e)
        {
			List<LadderRow> rows = null;

			if (e.MarketDataType == MarketDataType.Ask)
			{
				rows = askRows;
				
				if (firstAskEvent)
				{
					if (e.Operation == Operation.Update)
					{
						lock (e.MarketDepth.Ask)
						{
							for (int idx = 0; idx < e.MarketDepth.Ask.Count; idx++)
							{
								rows.Add(new LadderRow(e.MarketDepth.Ask[idx].Price, e.MarketDepth.Ask[idx].Volume, e.MarketDepth.Ask[idx].MarketMaker));
								AddUpdatedItem(depthDictionary, e.MarketDepth.Ask[idx].Price, e.MarketDepth.Ask[idx].Volume);
								//Print(string.Format("Add Ask {0}, volume {1}", e.MarketDepth.Ask[idx].Price, e.MarketDepth.Ask[idx].Volume));
							}
							//Print("e.MarketDepth.Ask.Count " + e.MarketDepth.Ask.Count);
						}
					}
					firstAskEvent = false;
				}
			}
			else if (e.MarketDataType == MarketDataType.Bid)
			{
				rows = bidRows;
				
				if (firstBidEvent)
				{
					if (e.Operation == Operation.Update)
					{
						lock (e.MarketDepth.Bid)
						{
							for (int idx = 0; idx < e.MarketDepth.Bid.Count; idx++)
							{
								rows.Add(new LadderRow(e.MarketDepth.Bid[idx].Price, e.MarketDepth.Bid[idx].Volume, e.MarketDepth.Bid[idx].MarketMaker));
								AddUpdatedItem(depthDictionary, e.MarketDepth.Bid[idx].Price, e.MarketDepth.Bid[idx].Volume);
							//	Print(string.Format("Add bid {0}, volume {1}", e.MarketDepth.Bid[idx].Price, e.MarketDepth.Bid[idx].Volume));
							}
							//Print("e.MarketDepth.Bid.Count " + e.MarketDepth.Bid.Count);
						}
					}
					firstBidEvent = false;
				}
			}

			if (rows == null)
			{
				return;
			}
			
			if (e.Operation == Operation.Insert)
			{
				if (e.Position >= rows.Count)
				{
					rows.Add(new LadderRow(e.Price, e.Volume, e.MarketMaker));
				}
				else
				{
					rows.Insert(e.Position, new LadderRow(e.Price, e.Volume, e.MarketMaker));
					AddUpdatedItem(depthDictionary, e.Price, e.Volume);
					//Print(string.Format("Insert {0}, volume {1}", e.Price, e.Volume));
				}
			}
			else if (e.Operation == Operation.Remove && e.Position < rows.Count)
			{
				rows.RemoveAt(e.Position);
				depthDictionary.Remove(e.Price);
				//Print("Remove " + e.Price);
				
			}
			else if (e.Operation == Operation.Update && e.Position < rows.Count)
			{
				rows[e.Position].MarketMaker	= e.MarketMaker;
				rows[e.Position].Price			= e.Price;
				rows[e.Position].Volume			= e.Volume;
				
				//Print("-----");
				//Print(string.Format("Update {0}, volume {1}", e.Price, e.Volume));
				AddUpdatedItem(depthDictionary, e.Price, e.Volume);
					
			}
        }
		
		private bool IsOrderCanEnterRevers(double ask, double bid, OrderType orderType)
		{
			long sellValue = 0;
			long buyValue = 0;
			
			long sellValueSumm = 0;
			long buyValueSumm = 0;
					
			bool isPresentZero = false;
			bool isCanEnterReversOrder = false;
			
			Print("-=-=-=-=-=-=-=-");
			Print("Ask.Price " + ask);
			Print("Bid.Price " + bid);
			
			//for(int i = 0; i < CountOfDepthRows; i++)
			foreach(KeyValuePair<double, long> depthItem in depthDictionary)
			{
				//double indexTick = i * TickSize;
				
				//depthDictionary.TryGetValue(ask + indexTick, out sellValue);
				//depthDictionary.TryGetValue(bid - indexTick, out buyValue);
				
				if(depthItem.Key >= ask && depthItem.Key <= ask + CountOfDepthRows * TickSize)
				{
					Print("Price ASK : " + depthItem.Key + ", volume: " + depthItem.Value);
					sellValueSumm += depthItem.Value;
				}
				if(depthItem.Key <= bid && depthItem.Key >= bid - CountOfDepthRows * TickSize)
				{
					Print("Price BID : " + depthItem.Key + ", volume: " + depthItem.Value);
					buyValueSumm += depthItem.Value;
				}
				
				if(depthItem.Value == 0)
				{
					isPresentZero = true;
				}
				
			}
			
			
			isCanEnterReversOrder = false;

			if(orderType == OrderType.BUY)
			{
				if(!isPresentZero)
				{
					if(sellValueSumm > buyValueSumm)
					{
						isCanEnterReversOrder = false;
					}
					else if(sellValueSumm < buyValueSumm)
					{
						isCanEnterReversOrder = true;
					}
				}
				else
				{
					Print("Was be Present 0 in depth");
				}
				Print(string.Format("Enter Long ({0})", startOrderDateTime));
				Print("BUY VOLUME: " + buyValueSumm);
				Print("SELL VOLUME: " + sellValueSumm);
				Print("REVERSE POSITION: " + isCanEnterReversOrder);
			}
			else if(orderType == OrderType.SELL)
			{
				if(!isPresentZero)
				{
					if(sellValueSumm < buyValueSumm)
					{
						isCanEnterReversOrder = false;
					}
					else if(sellValueSumm > buyValueSumm)
					{
						isCanEnterReversOrder = true;
					}
				}
				else
				{
					Print("Was be Present 0 in depth");
				}
				Print(string.Format("Enter Short ({0})", startOrderDateTime));
				Print("BUY VOLUME: " + buyValueSumm);
				Print("SELL VOLUME: " + sellValueSumm);
				Print("REVERSE POSITION: " + isCanEnterReversOrder);
			}
			
			return isCanEnterReversOrder;	
		}
		
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if(!IsStrategyCanWork(Time[0], StartWorkHour, StartWorkMinutes, EndWorkHour, EndWorkMinutes) && WorkTimeSwitch == Switch.ON)
			{
				Print(Time[0].Hour);
				Print(string.Format("Time for work is: start: {0}:{1}, end: {2}:{3}. But now time is: {4}", StartWorkHour, StartWorkMinutes, EndWorkHour, EndWorkMinutes, Time[0]));
				Print("Bad time for work");
				return;
			}
			
			if (e.MarketDataType == MarketDataType.Last) 
			{
				
				Price = e.Price;

				if(Position.MarketPosition == MarketPosition.Short)
				{
					if(usedOrders < 2)
					{
						
						if(isStopLimitOrder)
						{
							isStopLimitOrder = false;
							startOrderPrice = Price;
							startOrderDateTime = Time[0];
							trailStopLossPrice = startOrderPrice;
							startOrderType = OrderType.SELL;
							orderEnterBar = CurrentBar;
							isTrailStopEnable = false;
							
							Print("EnterShortStopLimit");
							Print("Price " + Price);
							Print("startOrderPrice " + startOrderPrice);
							usedOrders++;
							
							isCanEnterReversOrder = IsOrderCanEnterRevers(e.MarketData.Ask.Price, e.MarketData.Bid.Price, startOrderType);
						}
						
						
						
						if(ReversOrderSwitch == Switch.ON && isCanEnterReversOrder)
						{
							if(currentBar.Low - 1 * TickSize + TickInDiapasone * TickSize < Price && orderEnterBar == CurrentBar)
							{
								SetStopLoss(CalculationMode.Ticks, StopLoss);
								
								Print(Time[0]);
								Print("Enter LONG REVERS Order");
								Print("Price " + Price);
								
								EnterLong();
								
								startOrderPrice = Price;
								startOrderDateTime = Time[0];
								trailStopLossPrice = startOrderPrice;
								startOrderType = OrderType.BUY;
								orderEnterBar = CurrentBar;
								isTrailStopEnable = false;
								usedOrders++;
							}
						}
					}
				}
				else if(Position.MarketPosition == MarketPosition.Long)
				{
					if(usedOrders < 2)
					{
						if(isStopLimitOrder)
						{
							isStopLimitOrder = false;
							startOrderPrice = Price;
							startOrderDateTime = Time[0];
							trailStopLossPrice = startOrderPrice;
							startOrderType = OrderType.BUY;
							orderEnterBar = CurrentBar;
							isTrailStopEnable = false;
							Print("EnterLongStopLimit");
							Print("Price " + Price);
							Print("startOrderPrice " + startOrderPrice);
							usedOrders++;
							isCanEnterReversOrder = IsOrderCanEnterRevers(e.MarketData.Ask.Price, e.MarketData.Bid.Price, startOrderType);
						}
						
						 
						
						if(ReversOrderSwitch == Switch.ON && isCanEnterReversOrder)
						{
							if(currentBar.High + 1 * TickSize - TickInDiapasone * TickSize > Price && orderEnterBar == CurrentBar)
							{
								SetStopLoss(CalculationMode.Ticks, StopLoss);
								
								Print(Time[0]);
								Print("Enter SHORT REVERS Order");
								Print("Price " + Price);
								
								EnterShort();
								
								startOrderPrice = Price;
								startOrderDateTime = Time[0];
								trailStopLossPrice = startOrderPrice;
								startOrderType = OrderType.SELL;
								orderEnterBar = CurrentBar;
								isTrailStopEnable = false;
								usedOrders++;
							}
						}
					}
				}
				
				
				//if(isCanEnterTrailStop)
				{
					if(TrailStopSwitch == Switch.ON)
					{
						if(Position.MarketPosition == MarketPosition.Long)
						{
							if(startOrderType == OrderType.BUY && startOrderPrice + (ProfitTrigger - 1) * TickSize <= Price)
							{
								if(!isTrailStopEnable)
								{
									Print("Trail ON");
									isTrailStopEnable = true;
									trailStopLossPrice = Price - TrailStop * TickSize;
									Print("Buy startOrderPrice " + startOrderPrice);
									Print("Buy Change TrailStop " + trailStopLossPrice);
									Print("price -> " + Price);
								}
								
								if(trailStopLossPrice + TrailStop * TickSize + Frequency * TickSize <= Price && isTrailStopEnable)
								{
									trailStopLossPrice = Price - TrailStop * TickSize;
									Print("Buy Change TrailStop " + trailStopLossPrice);
									SetStopLoss(CalculationMode.Price, trailStopLossPrice);
									Print("price -> " + Price);
								}
							}
						}
						if(Position.MarketPosition == MarketPosition.Short)
						{
							if(startOrderType == OrderType.SELL && startOrderPrice - (ProfitTrigger - 1) * TickSize >= Price)
							{
								if(!isTrailStopEnable)
								{
									Print("Trail ON");
									isTrailStopEnable = true;
									trailStopLossPrice = Price + TrailStop * TickSize;
									Print("Sell startOrderPrice " + startOrderPrice);
									Print("Sell Change TrailStop " + trailStopLossPrice);
									Print("price -> " + Price);
								}
								
								if(trailStopLossPrice - TrailStop * TickSize - Frequency * TickSize >= Price && isTrailStopEnable)
								{
									trailStopLossPrice = Price + TrailStop * TickSize;
									Print("Sell Change TrailStop " + trailStopLossPrice);
									SetStopLoss(CalculationMode.Price, trailStopLossPrice);
									Print("price -> " + Price);
								}
							}
						}
					}
				}
			}
		}
	
		public class Bar
		{
			public double High {get; set;}
			public double Low {get; set;}
			public double Diapasone
			{
				get
				{
					return High - Low;
				}
			}
			
			public override string  ToString()
			{
				return string.Format("Bar-> High: {0}, Low: {1}", High, Low);
			}
		}
		
		public class EnteredOrder
		{
			public OrderType Type {get; set;}
			public int IndexBar {get; set;}
		}
		
		public bool IsStrategyCanWork(DateTime currentTime, int startWorkHour, int startWorkMinutes, int endWorkHour, int endWorkMinutes)
		{
			DateTime starWorkTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, startWorkHour, startWorkMinutes, 0, 0);
			DateTime endWorkTime = new DateTime(currentTime.Year, currentTime.Month, currentTime.Day, endWorkHour, endWorkMinutes, 0, 0);
				
			if(currentTime > starWorkTime && currentTime < endWorkTime)
			{
				return true;
			}
			else
			{
				return false;
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
		
		[Description("")]
		[GridCategory("TrailStop")]
		public Switch TrailStopSwitch
		{get; set;}
		
		[Description("Как часто подтягивать стоп-лосс за ценой, в тиках")]
		[GridCategory("TrailStop")]
		public int Frequency
		{get; set;}
		
		[Description("")]
		[GridCategory("EnterOrder")]
		public int TickInDiapasone
		{get; set;}
		
		[Description("")]
		[GridCategory("EnterOrder")]
		public Switch ReversOrderSwitch
		{get; set;}
		
		
		
		[Description("")]
		[GridCategory("DataSeriesChart")]
		public int OpenOffset
		{get; set;}
		
		[Description("")]
		[GridCategory("Depth")]
		public int CountOfDepthRows
		{
			get{return _countOfDepthRows;}
			set{_countOfDepthRows = value;}
		}
		
		
		[Description("")]
		[GridCategory("SMA filter")]
		public int TimeFrameSMA
		{
			get{return Math.Max(1, _timeFrameSMA);}
			set{_timeFrameSMA = value;}
		}
		
		[Description("")]
		[GridCategory("SMA filter")]
		public int SMAPeriod1
		{get; set;}
		
		[Description("")]
		[GridCategory("SMA filter")]
		public int SMAPeriod2
		{get; set;}
		
		[Description("")]
		[GridCategory("SMA filter")]
		public Switch SMAFilterSwitch
		{get; set;}
		
		[Description("")]
		[GridCategory("SMA filter")]
		public int TicksBetweenSMA
		{get; set;}
		
		
		[Description("")]
		[GridCategory("Strategy configuration")]
		public Switch WorkTimeSwitch
		{get; set;}
		
		[Description("")]
		[GridCategory("Strategy configuration")]
		public int StartWorkHour
		{get; set;}
		
		[Description("")]
		[GridCategory("Strategy configuration")]
		public int StartWorkMinutes
		{get; set;}
		
		[Description("")]
		[GridCategory("Strategy configuration")]
		public int EndWorkHour
		{get; set;}
		
		[Description("")]
		[GridCategory("Strategy configuration")]
		public int EndWorkMinutes
		{get; set;}
		
		
		/*
		[Description("")]
		[GridCategory("DataSeriesChart")]
		public int TickTrend
		{get; set;}
		
		[Description("")]
		[GridCategory("DataSeriesChart")]
		public int TickReversal
		{get; set;}
		*/



    }
}
