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

		private bool isLongLimitOrderStartOutOfRsiDiapasone = false;
		private bool isShortLimitOrderStartOutOfRsiDiapasone = false;
		
		private double Price;
		private double PreviousPrice;
		
		private double LimitOrderCancelPrice;
		
		private bool isCanEnterReversOrder = false;
		
		private Dictionary<double, long> depthDictionary;
		
		private double _smaValue1 = 0;
		private double _smaValue2 = 0;
			
		private int _countOfDepthRows = 5;
		private int _timeFrameSMA = 1;
		
		
		//RSI
		private int smooth = 1;
		private int _rsiHigh = 1;
		private int _rsiLow = 1;
		private int _rsiPeriod = 1;
		private int _rsiTimeFrame = 1;
		
		private double rsi = 0;
		private double rsiAvg = 0;
		
		
		//DayTimeFrame
		private DailyHistory _dailyHistory;
		private Day _day;
		
		
		//Price
		private bool _isFirstTickPriceAfterStart = false;
		
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
			Print("Initialize");
            CalculateOnBarClose = true;
						
			SetStopLoss(CalculationMode.Ticks, StopLoss);
			SetProfitTarget(CalculationMode.Ticks, ProfitTarget);
			
			Add(PeriodType.Minute, TimeFrameSMA);
			Add(PeriodType.Minute, RSITimeFrame);
			Add(PeriodType.Day, 1);
			
			

			
        }

		protected override void OnTermination()
		{

		}
		
		protected override void OnOrderUpdate(IOrder order)
		{
			if(order.OrderState == OrderState.Cancelled)
			{
				isLongLimitOrderStartOutOfRsiDiapasone = false;
				isShortLimitOrderStartOutOfRsiDiapasone = false;
				Print("Long or Short order was cancelled");
			}
		}
		
		protected override void OnStartUp()
		{
			Print("OnStartUp");
			
			previousBar = new Bar(TickSize);
			currentBar = new Bar(TickSize);
					
			_day = new Day();
			_dailyHistory = new DailyHistory(HistoryDays);
			
			_dailyHistory.AddDay(_day);
			
			
			depthDictionary = new Dictionary<double, long>();
		}
		
		#region GetOrderState
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
		#endregion
		
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
			
			if(!IsStrategyCanWork(Time[0], StartWorkHour, StartWorkMinutes, EndWorkHour, EndWorkMinutes) && WorkTimeSwitch == Switch.ON)
			{
				Print(Time[0].Hour);
				Print(string.Format("Time for work is: start: {0}:{1}, end: {2}:{3}. But now time is: {4}", StartWorkHour, StartWorkMinutes, EndWorkHour, EndWorkMinutes, Time[0]));
				Print("Bad time for work");
				return;
			}
			
			if(BarsInProgress == 1)
			{
				if(SMAFilterSwitch == Switch.ON)
				{
					_smaValue1 = SMA(BarsArray[1], SMAPeriod1)[0];
					_smaValue2 = SMA(BarsArray[1], SMAPeriod2)[0];
						
					Print("-----SMA TimeFrame-----");
					Print("Custom timeframe: " + Times[1][0]);
					Print("SMA1 in Custom timeframe " + _smaValue1);
					Print("SMA2 in Custom timeframe " + _smaValue2);
					Print("-----------------------");
				}
			}
			
			if(BarsInProgress == 2)
			{
				if(RSIFilterSwitch == Switch.ON)
				{
					
					rsi = RSI(RSIPeriod, Smooth)[0];
					rsiAvg = RSI(RSIPeriod, Smooth).Avg[0];
					
					Print("----RSI TimeFrame-----");
					Print("RSIHigh " + RSIHigh);
					Print("RSILow " + RSILow);
					Print("RSI " + rsi);
					Print("RSIAvg " + rsiAvg);
					Print("----------------------");
					
					if(EnterLimitOrderInRSITimeFrame == Switch.ON && previousBar != null && currentBar != null)
					{
						if(previousBar.Low > currentBar.Low)
						{
							if(RSILow > rsi && RSILow > rsiAvg && !isLongLimitOrderStartOutOfRsiDiapasone)
							{
								EnterLongLimitOrder(Price, currentBar.High, StopLoss, TickSize, EnterIndexApexTick);
								isLongLimitOrderStartOutOfRsiDiapasone = true;
							}
							else if(isLongLimitOrderStartOutOfRsiDiapasone)
							{
								EnterLongLimitOrder(Price, currentBar.High, StopLoss, TickSize, EnterIndexApexTick);
							}
						}
						
						if(previousBar.High < currentBar.High)
						{
							if(RSIHigh < rsi && RSIHigh < rsiAvg && !isShortLimitOrderStartOutOfRsiDiapasone)
							{
								EnterShortLimitOrder(Price, currentBar.Low, StopLoss, TickSize, EnterIndexApexTick);
								isShortLimitOrderStartOutOfRsiDiapasone = true;
							}	
							else if(isShortLimitOrderStartOutOfRsiDiapasone)
							{
								EnterShortLimitOrder(Price, currentBar.Low, StopLoss, TickSize, EnterIndexApexTick);
							}								
						}
					}

				}
			}
			
			if(BarsInProgress == 3)
			{
				Print("Day timeFrame");
				_day = new Day(Time[0]);
				Print("History Days before add-> " + _dailyHistory.Days.Count);
				foreach(Day day in _dailyHistory.Days)
				{
					Print("Day -> " + day.ToString());
					foreach(Level level in day.Levels)
					{
						Print(level.ToString());
					}
				}
				_dailyHistory.AddDay(_day);
				Print("History Days after add-> " + _dailyHistory.Days.Count);
				foreach(Day day in _dailyHistory.Days)
				{
					Print("Day -> " + day.ToString());
				}
			}
			
			
			if(BarsInProgress == 0)
			{			
				Print("====Unirenko TimeFrame====");
				
				bool isCanSetLimitOrder = false;
				if(SMAFilterSwitch == Switch.ON)
				{
					Print("------------");
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
					Print("------------");
				}
				
				if(RSIFilterSwitch == Switch.ON)
				{
					if(rsi != 0 && rsiAvg != 0)
					{
						Print("------------");

						if(rsi < RSILow && rsiAvg < RSILow)
						{
							isCanSetLimitOrder = true;
							Print("Long Limit Order can set with good RSI");
							Print("RSI < RSILow and RSIAvg < RSILow");
						}
						else if(rsi > RSIHigh && rsiAvg > RSIHigh)
						{
							isCanSetLimitOrder = true;
							Print("Short Limit Order can set with good RSI");
							Print("RSI > RSIHigh && RSIAvg > RSIHigh");
						}
						else
						{
							isCanSetLimitOrder = false;
							Print("Limit Order can`t set with bad RSI");
						}
						Print("------------");
					}
					else
					{
						isCanSetLimitOrder = false;
						Print("Limit Order can`t set with bad RSI");
					}
				}
				
				previousBar = new Bar(TickSize);
				previousBar.High = Highs[0][1];
				previousBar.Low = Lows[0][1];
				previousBar.BarDateTime = Times[0][1];
				
				
				currentBar = new Bar(TickSize);
				currentBar.High = Highs[0][0];
				currentBar.Low = Lows[0][0];
				currentBar.BarDateTime = Times[0][0];
				
				Print("Unirenko previousBar -> " + previousBar.ToString());
				Print("Unirenko currentBar -> " + currentBar.ToString());
				Print("Unirenko previousBar diapasone -> "  + previousBar.Diapasone);
				Print("Unirenko currentBar diapasone -> "  + currentBar.Diapasone);

				double openOffset = OpenOffset * TickSize;
				double tickReversal = TickReversal * TickSize;
				
				
				
				/*if(openOffsetTickPrice != currentBar.Diapasone)
				{
					Print("OpenOffset-> " + openOffsetTickPrice);
					Print("Bar Diapasone -> " + currentBar.Diapasone);
					Print("Not correct bar size");
					Print("Return");
					return;
				}*/
				
				Print("----Levels-----");
				foreach(Day day in _dailyHistory.Days)
				{
					foreach(Level level in day.Levels)
					{
						if(level.LevelPrice <= currentBar.High && level.LevelPrice >= currentBar.Low)
						{
							level.IsDeleted = true;
							Print("Deleted level in OnBarUpdate-> " + level.ToString());
						}
						//if(!level.IsDeleted)
						{
							Print(level.ToString());
						}
					}
				}
				Print("------End levels-----");

				if(tickReversal == currentBar.Diapasone)
				{
					//Print("tickReversal == currentBar.Diapasone");
					if(openOffset == previousBar.Diapasone)
					{
						//Print("openOffset == previousBar.Diapasone");
						
						double tickTrend = TickTrend * TickSize;
						//Print("TickTrend tick -> " + tickTrend);
						
						//Print("previousBar.Low " + previousBar.Low);
						//Print("previousBar.High " + previousBar.High);
						
						//Print("currentBar.Low " + currentBar.Low);
						//Print("currentBar.High " + currentBar.High);
						
						/*Print("buy");
						Print("previousBar.Low > currentBar.Low " + (previousBar.Low > currentBar.Low));
						Print("previousBar.Low - TickTrend * TickSize == currentBar.Low " + (Math.Round(previousBar.Low - TickTrend * TickSize, RoundPrice) == currentBar.Low));
						Print("previousBar.High < currentBar.High " + (previousBar.High < currentBar.High));
						
						Print("");
						Print("sell");
						Print("previousBar.High < currentBar.High " + (previousBar.High < currentBar.High));
						Print("previousBar.High + TickTrend * TickSize == currentBar.High " + (Math.Round(previousBar.High + TickTrend * TickSize, 2) == currentBar.High));
						Print("previousBar.Low > currentBar.Low " + (previousBar.Low > currentBar.Low));
						*/
						
						if(previousBar.Low > currentBar.Low && Math.Round(previousBar.Low - TickTrend * TickSize, RoundPrice) == currentBar.Low
							&& previousBar.High < currentBar.High)
						{
							Level level = new Level(OrderType.BUY, previousBar, previousBar.BarDateTime, TicksFromApexToCancelOrder * TickSize - tickTrend);
							_day.Levels.Add(level);
							Print("Add buy level -> " + level.ToString());
						}
						else if(previousBar.High < currentBar.High && Math.Round(previousBar.High + TickTrend * TickSize, RoundPrice) == currentBar.High
							&& previousBar.Low > currentBar.Low)
						{
							Level level = new Level(OrderType.SELL, previousBar, previousBar.BarDateTime, TicksFromApexToCancelOrder * TickSize - tickTrend);
							_day.Levels.Add(level);
							Print("Add sell level -> " + level.ToString());
						}
					}
				}
				

				
				usedOrders = 0;

				if(SMAFilterSwitch == Switch.ON && isCanSetLimitOrder || SMAFilterSwitch == Switch.OFF)
				{
					if(RSIFilterSwitch == Switch.ON && isCanSetLimitOrder || RSIFilterSwitch == Switch.OFF)
					{
						if(Position.MarketPosition == MarketPosition.Flat)
						{
							isCanEnterReversOrder = false;
							
							if(previousBar.Low > currentBar.Low /*&& previousBar.High > currentBar.High*/)
							{
								/*if(RSILow > rsi && RSILow > rsiAvg && !isLongLimitOrderStartOutOfRsiDiapasone)
								{
									EnterLongLimitOrder(Price, currentBar.High, StopLoss, TickSize, EnterIndexApexTick);
									isLongLimitOrderStartOutOfRsiDiapasone = true;
								}
								else */
								if(isLongLimitOrderStartOutOfRsiDiapasone)
								{
									EnterLongLimitOrder(Price, currentBar.High, StopLoss, TickSize, EnterIndexApexTick);
								}
							}
							else if(/*previousBar.Low < currentBar.Low &&*/ previousBar.High < currentBar.High)
							{
								/*if(RSIHigh < rsi && RSIHigh < rsiAvg && !isShortLimitOrderStartOutOfRsiDiapasone)
								{
									EnterShortLimitOrder(Price, currentBar.Low, StopLoss, TickSize, EnterIndexApexTick);
									isShortLimitOrderStartOutOfRsiDiapasone = true;
								}
								else */
								if(isShortLimitOrderStartOutOfRsiDiapasone)
								{
									EnterShortLimitOrder(Price, currentBar.Low, StopLoss, TickSize, EnterIndexApexTick);
								}
							}
						}
					}
				}
				

			}
        }
		
		
		private void CancelLimitOrder(double limitOrderCancelPrice, double price, double previousPrice)
		{
			Print("Start cancell process order");
			Print("LimitOrderCancelPrice -> " + limitOrderCancelPrice);
			Print(string.Format("Price: {0}, PreviousPrice: {1}", price, previousPrice));
			Print("CurrentBar diapasone -> Low: " + currentBar.Low + " High: " + currentBar.High);
			if(order != null)
			{
				CancelOrder(order);
				Print("Success cancel");
			}
			else
			{
				Print("Order is null");
			}
			orderType = OrderType.FLAT;
			isStopLimitOrder = false;
		}
		
		private void EnterShortLimitOrder(double price, double currentBarLow, int stopLoss, double tickSize, int enterIndexApexTick)
		{
			if(price > currentBarLow)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				Print("----------");
				
				double low = currentBarLow + 1 * tickSize - enterIndexApexTick * tickSize;

				order = EnterShortStopLimit(0, true, 1, low, low, "");
				orderType = OrderType.SELL;
				isStopLimitOrder = true;
				
				Print(Time[0]);
				Print("Price " + Price);
				Print("Enter Short Limit " + low);
				Print("Enter Short Stop " + low);
			}
		}
		
		private void EnterLongLimitOrder(double price, double currentBarHigh, int stopLoss, double tickSize, int enterIndexApexTick)
		{
			if(price < currentBarHigh)
			{
				SetStopLoss(CalculationMode.Ticks, stopLoss);
				Print("----------");

				double high = currentBarHigh - 1 * tickSize + enterIndexApexTick * tickSize;

				order = EnterLongStopLimit(0, true, 1, high, high, "");
				orderType = OrderType.BUY;
				isStopLimitOrder = true;

				Print(Time[0]);
				Print("Price " + Price);
				Print("Enter Long Limit " + high);
				Print("Enter Long Stop " + high);
			}
		}
		
		
		#region OnMarketDepth
		
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
			
			foreach(KeyValuePair<double, long> depthItem in depthDictionary)
			{
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
		#endregion
		
		
		
		private bool IsCrossedNumber(double checkNumber, double currentNumber, double previousNumber)
		{
			if(previousNumber > checkNumber && currentNumber <= checkNumber || previousNumber < checkNumber && currentNumber >= checkNumber)
				return true;
			else
				return false;
		}
		
		/// <summary>
		/// OnMarketData
		/// </summary>
		/// <param name="e"></param>
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			/*if(_isFirstTickPriceAfterStart == false)
			{
				Day day = new Day(Time[0]);
				_dailyHistory = new DailyHistory(HistoryDays);
				_dailyHistory.AddDay(day);
				_isFirstTickPriceAfterStart = true;
			}*/
			
			
			if(!IsStrategyCanWork(Time[0], StartWorkHour, StartWorkMinutes, EndWorkHour, EndWorkMinutes) && WorkTimeSwitch == Switch.ON)
			{
				Print(Time[0].Hour);
				Print(string.Format("Time for work is: start: {0}:{1}, end: {2}:{3}. But now time is: {4}", StartWorkHour, StartWorkMinutes, EndWorkHour, EndWorkMinutes, Time[0]));
				Print("Bad time for work");
				return;
			}
			
			if (e.MarketDataType == MarketDataType.Last) 
			{
				PreviousPrice = Price;
				Price = e.Price;

				
				//Print("-------Levels-----");
				foreach(Day day in _dailyHistory.Days)
				{
					foreach(Level level in day.Levels)
					{
						//Print(level.ToString());
						if(IsCrossedNumber(level.LevelPrice, Price, PreviousPrice) && !level.IsDeleted)
						{
							if(previousBar.Low > currentBar.Low)
							{
								if(level.TypeOfOrder == OrderType.BUY && !level.IsDeleted)
								{
									if(Price < currentBar.High)
									{
										Print("Set Long Limit Order. Level: " + level.ToString());
										level.IsDeleted = true;
										Print("Deleted level -> " + level.ToString());
										EnterLongLimitOrder(Price, currentBar.High, StopLoss, TickSize, EnterIndexApexTick);
										isLongLimitOrderStartOutOfRsiDiapasone = true;
										LimitOrderCancelPrice = level.CancelPrice;
									}
								}
							}
							else if(previousBar.High < currentBar.High)
							{ 
								if(level.TypeOfOrder == OrderType.SELL && !level.IsDeleted)
								{
									if(Price > currentBar.Low)
									{
										Print("Set Short Limit Order. Level: " + level.ToString());
										level.IsDeleted = true;
										Print("Deleted level -> " + level.ToString());
										
										EnterShortLimitOrder(Price, currentBar.Low, StopLoss, TickSize, EnterIndexApexTick);
										isShortLimitOrderStartOutOfRsiDiapasone = true;
										LimitOrderCancelPrice = level.CancelPrice;
									}
								}
							}
							level.IsDeleted = true;
							Print("Deleted level -> " + level.ToString());
						}
					}
				}
				//Print("-------Levels end-----");
				//if(Price != PreviousPrice)
				//{
				//	Print("Single Price " + Price);
					//Print("LimitOrderCancelPrice " + LimitOrderCancelPrice);
				//}
				
				if(IsCrossedNumber(LimitOrderCancelPrice, Price, PreviousPrice)
					|| LimitOrderCancelPrice <= currentBar.High && LimitOrderCancelPrice >= currentBar.Low)
				{
					if(isLongLimitOrderStartOutOfRsiDiapasone || isShortLimitOrderStartOutOfRsiDiapasone)
					{
						Print("Cancel Code section");
						CancelLimitOrder(LimitOrderCancelPrice, Price, PreviousPrice);
					}
				}
				
				
				
				
				
				
				if(usedOrders < 2)
				{
					if(Position.MarketPosition == MarketPosition.Short)
					{
						if(isStopLimitOrder)
						{
							Print("FirstEnterLimitOrder SHORT");
							FirstEnterLimitOrder(Order.SHORT, e.MarketData.Ask.Price, e.MarketData.Bid.Price);
						}
						
						if(ReversOrderSwitch == Switch.ON && isCanEnterReversOrder)
						{
							if(currentBar.Low - 1 * TickSize + TickInDiapasone * TickSize < Price && orderEnterBar == CurrentBar)
							{
								EnterReversOrder(Order.LONG, Price);
							}
						}
					}
					else if(Position.MarketPosition == MarketPosition.Long)
					{
						if(isStopLimitOrder)
						{
							Print("FirstEnterLimitOrder LONG");
							FirstEnterLimitOrder(Order.LONG, e.MarketData.Ask.Price, e.MarketData.Bid.Price);
						}
						
						if(ReversOrderSwitch == Switch.ON && isCanEnterReversOrder)
						{
							if(currentBar.High + 1 * TickSize - TickInDiapasone * TickSize > Price && orderEnterBar == CurrentBar)
							{
								EnterReversOrder(Order.SHORT, Price);
							}
						}
					}
				}
				
				if(TrailStopSwitch == Switch.ON)
				{
					if(Position.MarketPosition == MarketPosition.Long)
					{
						if(startOrderType == OrderType.BUY && startOrderPrice + (ProfitTrigger - 1) * TickSize <= Price)
						{
							if(!isTrailStopEnable)
							{
								TrailStart(Order.LONG, Price);
							}
							
							if(trailStopLossPrice + TrailStop * TickSize + Frequency * TickSize <= Price && isTrailStopEnable)
							{
								ChangeTrailPrice(Order.LONG, Price, TickSize);
							}
						}
					}
					else if(Position.MarketPosition == MarketPosition.Short)
					{
						if(startOrderType == OrderType.SELL && startOrderPrice - (ProfitTrigger - 1) * TickSize >= Price)
						{
							if(!isTrailStopEnable)
							{
								TrailStart(Order.SHORT, Price);
							}
							
							if(trailStopLossPrice - TrailStop * TickSize - Frequency * TickSize >= Price && isTrailStopEnable)
							{
								ChangeTrailPrice(Order.SHORT, Price, TickSize);
							}
						}
					}
				}
			}
		}
		
		private void FirstEnterLimitOrder(Order order, double askPrice, double bidPrice)
		{
			isStopLimitOrder = false;
			startOrderPrice = Price;
			startOrderDateTime = Time[0];
			trailStopLossPrice = startOrderPrice;
			orderEnterBar = CurrentBar;
			isTrailStopEnable = false;
			usedOrders++;
			
			if(order == Order.LONG)
			{
				isLongLimitOrderStartOutOfRsiDiapasone = false;
				startOrderType = OrderType.BUY;
				Print("EnterLongStopLimit");
				Print("Price " + Price);
				Print("startOrderPrice " + startOrderPrice);
			}
			else if(order == Order.SHORT)
			{
				isShortLimitOrderStartOutOfRsiDiapasone = false;
				startOrderType = OrderType.SELL;
				Print("EnterShortStopLimit");
				Print("Price " + Price);
				Print("startOrderPrice " + startOrderPrice);
			}
			isCanEnterReversOrder = IsOrderCanEnterRevers(askPrice, bidPrice, startOrderType);
		}
		
		private void EnterReversOrder(Order order, double price)
		{
			SetStopLoss(CalculationMode.Ticks, StopLoss);
			
			Print(Time[0]);	
			startOrderPrice = price;
			startOrderDateTime = Time[0];
			trailStopLossPrice = startOrderPrice;
			orderEnterBar = CurrentBar;
			isTrailStopEnable = false;
			usedOrders++;
			
			if(order == Order.LONG)
			{
				Print("Enter LONG REVERS Order");
				Print("Price " + price);
				EnterLong();
				startOrderType = OrderType.BUY;
			}
			else if(order == Order.SHORT)
			{
				Print("Enter SHORT REVERS Order");
				Print("Price " + price);
				EnterShort();
				startOrderType = OrderType.SELL;
			}
		}
		
		private void TrailStart(Order order, double price)
		{
			Print("Trail ON");
			isTrailStopEnable = true;
			
			if(order == Order.LONG)
			{
				trailStopLossPrice = price - TrailStop * TickSize;
				Print("Buy startOrderPrice " + startOrderPrice);
				Print("Buy Change TrailStop " + trailStopLossPrice);
			}
			else if(order == Order.SHORT)
			{
				trailStopLossPrice = price + TrailStop * TickSize;
				Print("Sell startOrderPrice " + startOrderPrice);
				Print("Sell Change TrailStop " + trailStopLossPrice);
			}
			
			Print("price -> " + price);
		}
		
		private void ChangeTrailPrice(Order order, double price, double tickSize)
		{
			if(order == Order.LONG)
			{
				trailStopLossPrice = price - TrailStop * tickSize;
				Print("Buy Change TrailStop " + trailStopLossPrice);
			}
			if(order == Order.SHORT)
			{
				trailStopLossPrice = price + TrailStop * tickSize;
				Print("Sell Change TrailStop " + trailStopLossPrice);
			}
			
			SetStopLoss(CalculationMode.Price, trailStopLossPrice);
			Print("price -> " + price);
		}
		
		
		public class Bar
		{
			public double High {get; set;}
			public double Low {get; set;}
			
			public DateTime BarDateTime {get; set;}
			
			public double TickSize {get; set;}
			public double Diapasone
			{
				get
				{
					return Math.Round(High - Low, 2) - TickSize;
				}
			}
			
			public Bar()
			{
			}
			
			
			public Bar(double tickSize)
			{
				TickSize = tickSize;
			}
			
			public override string  ToString()
			{
				return string.Format("Bar-> High: {0}, Low: {1}, DateTime: {2}", High, Low, BarDateTime);
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
		
		
		public class DailyHistory{

			private readonly int _daysOfHistory;

			public List<Day> Days {get; private set;}

			public DailyHistory(int daysOfHistory)
			{
				_daysOfHistory = daysOfHistory;
				Days = new List<Day>();
			}

			public void AddDay(Day day)
			{
				Resize();
				Days.Add(day);
			}

			private void Resize()
			{
				if (Days.Count >= _daysOfHistory)
					Days.RemoveAt(0);
			}

		}
		
		
		public class Day
		{

			public List<Level> Levels{get; private set;}
			public DateTime DailyDateTime {get; private set;}
			
			public Day(DateTime dateTime)
			{
				DailyDateTime = dateTime;
				Levels = new List<Level>();
			}
			
			public Day()
			{
				Levels = new List<Level>();
			}
			
			
			public new string ToString(){
				return string.Format("Day:: Levels count: {0}, DateTime: {1}", Levels.Count, DailyDateTime);
			}
			
		}
		
		public class Level
		{
			public OrderType TypeOfOrder {get; set;}
			public double LevelPrice {get; set;}
			public DateTime CreatedDateTime {get; set;}
			public Bar LevelBar {get; set;}
			public double CancelPrice 
			{
				get
				{
					//return (LevelBar.High + LevelBar.Low) / 2;
					return _cancelOrderPrice;
				}
			}
			
			public bool IsDeleted {get; set;}
			
			private string _orderTypeString = "";
			private double _cancelOrderPrice;
			
			public Level(OrderType orderType, Bar bar, DateTime createdDateTime, double fromApexToCancelOrder)
			{
				TypeOfOrder = orderType;
				if(orderType == OrderType.BUY)
				{
					LevelPrice = bar.High;
					_cancelOrderPrice = bar.Low + fromApexToCancelOrder;
					_orderTypeString = "BUY";
				}
				else if(orderType == OrderType.SELL)
				{
					LevelPrice = bar.Low;
					_cancelOrderPrice = bar.High - fromApexToCancelOrder;
					_orderTypeString = "SELL";
				}

				CreatedDateTime = createdDateTime;
				LevelBar = bar;
			}		
			
			public new String ToString()
			{
				return string.Format("Level-> OrderType: {0}, LevelPrice: {1}, CancelPrice: {2}, DateTime: {3}, IsDeleted: {4}"
					, _orderTypeString, LevelPrice, CancelPrice, CreatedDateTime, IsDeleted);
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
		
		public enum Order
		{
			LONG,
			SHORT
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
		
		[Description("EnterIndexApexTick not correct, should be 0 or 1")]
		[GridCategory("Order")]
		public int EnterIndexApexTick
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
		[GridCategory("DataSeriesChart")]
		public int TickReversal
		{get; set;}
		
		[Description("")]
		[GridCategory("DataSeriesChart")]
		public int TickTrend
		{get; set;}
		
		[Description("")]
		[GridCategory("HistoryData")]
		public int HistoryDays
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
		[GridCategory("RSI filter")]
		public Switch RSIFilterSwitch
		{get; set;}
		
		[Description("Number of bars for smoothing")]
		[GridCategory("RSI filter")]
		public int Smooth
		{
			get { return smooth; }
			set { smooth = Math.Max(1, value); }
		}
		
		[Description("Number of bars for smoothing")]
		[GridCategory("RSI filter")]
		public int RSIHigh
		{
			get { return _rsiHigh; }
			set { _rsiHigh = Math.Max(1, value); }
		}
		
		[Description("Number of bars for smoothing")]
		[GridCategory("RSI filter")]
		public int RSILow
		{
			get { return _rsiLow; }
			set { _rsiLow = Math.Max(1, value); }
		}
		
		[Description("Number of bars for smoothing")]
		[GridCategory("RSI filter")]
		public int RSIPeriod
		{
			get { return _rsiPeriod; }
			set { _rsiPeriod = Math.Max(1, value); }
		}
		
		[Description("Number of bars for smoothing")]
		[GridCategory("RSI filter")]
		public int RSITimeFrame
		{
			get { return _rsiTimeFrame; }
			set { _rsiTimeFrame = Math.Max(1, value); }
		}
		
		
		
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
		

		[Description("")]
		[GridCategory("RSI TimeFrame")]
		public Switch EnterLimitOrderInRSITimeFrame
		{get; set;}
		
		
		
		[Description("")]
		[GridCategory("CancelOrder Logic")]
		public int TicksFromApexToCancelOrder
		{get; set;}
		
				
		[Description("")]
		[GridCategory("Math")]
		public int RoundPrice
		{get; set;}
		
		
    }
}
