#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using System.Text;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Indicator;
using NinjaTrader.Gui.Chart;
using NinjaTrader.Strategy;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class AqueGenNinjaBotStrategyV2 : Strategy
    {
        //#region Variables
        // Wizard generated variables
        private int myInput0 = 1; // Default setting for MyInput0
		

		//Price
		private double _price = 0;
		private double _previousPrice = 0;
		private OrderAction _orderAction;
		
		
		//zigzag
		private DataSeries	_zigZagHighZigZags;
		private DataSeries	_zigZagLowZigZags;
		private DataSeries	_zigZagHighSeries;
		private DataSeries	_zigZagLowSeries;

		private int _startBar = 0;
		private int _endBar = 0;
		
		//Orders
		private double _startOrderPrice = -1;

		private bool _isTrendOnPeriodDown;
		
		//SMA
		private double _middleValot = 0;
		
		//ZigZag history
		private HistoryData _historyData;
		private DailyData _dailyData;
		private ZigZagDiapasone _zigZagDiapasone;
		private OnBarData _onBarData;
	
		
		private int _indexBar = -1;
		private int _orderIndexBar = -1;
		
		private StopLossOutOfApex _stopLossOutOfApex;
		private bool _isStopLossOutOfApex = false;
		private double _previousLowBarApex = -1;
		private double _previousHighBarApex = -1;
		
		
		//Logs
		private Logger _logger;

		private double _lastCompletedZigZagApexPriceDistance = -1;
		private double _procentOfMiddleValot = -1;
		
		//ZigZag
		private ZigZag zigZagLarge;
		private ZigZag zigZagSmall;
		private double priceBetweenApexLargeZigZagSell = -1;
		private double priceBetweenApexLargeZigZagBuy = -1;
		
		private double zigZagLargeBuyLevel = -1;
		private double zigZagLargeSellLevel = -1;
		
		//Orders on BackTest
		private ExitOrders _exitOrders;
			
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {

            CalculateOnBarClose = false;

			_zigZagHighZigZags = new DataSeries(this, MaximumBarsLookBack.Infinite); 
			_zigZagLowZigZags = new DataSeries(this, MaximumBarsLookBack.Infinite); 
			_zigZagHighSeries = new DataSeries(this, MaximumBarsLookBack.Infinite); 
			_zigZagLowSeries = new DataSeries(this, MaximumBarsLookBack.Infinite); 
			
			Add(PeriodType.Tick, 1);
			Add(PeriodType.Day, 1);
			
			
			
        }

		protected override void OnStartUp()
		{	
			_historyData = new HistoryData(SaveZigZagDaysOnHistory);
			_dailyData = new DailyData(Time[0]);
			_zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel * TickSize, CurrentBars[0]);
			_onBarData = new OnBarData(0);
			
			zigZagLarge = new ZigZag(TickSize, _zigZagHighZigZags,_zigZagLowZigZags, _zigZagHighSeries, _zigZagLowSeries, DeviationTypeLarge, DeviationValueLarge, UseHighLowLarge);
			zigZagSmall = new ZigZag(TickSize, _zigZagHighZigZags,_zigZagLowZigZags, _zigZagHighSeries, _zigZagLowSeries, DeviationTypeSmall, DeviationValueSmall, UseHighLowSmall);
			
			
			//Log
			_logger = new Logger(LoggerType, LogEnabled);
		}
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>

        protected override void OnBarUpdate()
        {	

			
			Log("Test1");
			if(CurrentBars[0] < 10 || CurrentBars[1] < 10 || CurrentBars[2] < 10)
			{
				return;
			}
			Log("Test2");
			if(BarsInProgress == 0){
				_indexBar = CurrentBars[0];
				
			
				if(_orderAction == OrderAction.SELL || _orderAction == OrderAction.BUY)
				{
					_previousLowBarApex = Lows[0][0];
					_previousHighBarApex = Highs[0][0];
				}
				
				Log("Test13");
				zigZagLarge.UpdateOnBar(this, CurrentBar, High, Low, Input[0], Input);
				zigZagSmall.UpdateOnBar(this, CurrentBar, High, Low, Input[0], Input);

				if(zigZagLarge.Trend == 1)
				{
					zigZagLargeBuyLevel = (zigZagLarge.LastPrice + zigZagLarge.LowZigZagPrice) / 2;
					zigZagLargeSellLevel = (zigZagLarge.HighZigZagPrice + zigZagLarge.LowZigZagPrice) / 2;
					
					priceBetweenApexLargeZigZagBuy = Math.Abs(zigZagLarge.LastPrice - zigZagLarge.LowZigZagPrice);
					priceBetweenApexLargeZigZagSell = Math.Abs(zigZagLarge.HighZigZagPrice - zigZagLarge.LowZigZagPrice);
				}
				else if(zigZagLarge.Trend == -1)
				{
					zigZagLargeBuyLevel = (zigZagLarge.HighZigZagPrice + zigZagLarge.LowZigZagPrice) / 2;
					zigZagLargeSellLevel = (zigZagLarge.LastPrice + zigZagLarge.HighZigZagPrice) / 2;
					
					priceBetweenApexLargeZigZagBuy = Math.Abs(zigZagLarge.HighZigZagPrice - zigZagLarge.LowZigZagPrice);
					priceBetweenApexLargeZigZagSell = Math.Abs(zigZagLarge.LastPrice - zigZagLarge.HighZigZagPrice);
					
				}
				
				if(zigZagLargeBuyLevel < 0 || zigZagLargeSellLevel < 0)
				{
					Log("Большой зигзаг не построился");
					return;
				}
				
				
				Log("zigZagLargeBuyLevel -> " + zigZagLargeBuyLevel);
				Log("zigZagLargeSellLevel -> " + zigZagLargeSellLevel);
				
				if(zigZagSmall.LowBarPeriod == 0 || zigZagSmall.HighBarPeriod == 0) { return; } 
				
				
				_onBarData = new OnBarData(CurrentBar);
				_dailyData.OnBarDataList.Add(_onBarData);
				
				Log("==================================================");
				Log("------Старт Обработки дефолтного таймфрейма-------");			

				if(zigZagSmall.HighBarPeriod > zigZagSmall.LowBarPeriod)
				{
					_isTrendOnPeriodDown = false;
					_startBar = zigZagSmall.LowBarPeriod - 1 - LeftZigZag;
					_endBar = zigZagSmall.HighBarPeriod - 1 + RightZigZag;
				}
				else 
				{
					_isTrendOnPeriodDown = true;
					_startBar = zigZagSmall.HighBarPeriod - 1 - LeftZigZag;
					_endBar = zigZagSmall.LowBarPeriod - 1 + RightZigZag;
				}
					
				if(_startBar < _endBar && zigZagSmall.IsChangePeriod)
				{
					double sellLevelPrice = GetLowOrHighPriceOfBar(true, _startBar, _endBar);
					double buyLevelPrice = GetLowOrHighPriceOfBar(false, _startBar, _endBar);

					_zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel * TickSize, CurrentBars[0]);
					_dailyData.ZigZagDiapasoneList.Add(_zigZagDiapasone);
						
					_zigZagDiapasone.AddedDateTime = Time[0];
				
				/*
					if(UpdateLevelsInOneZigZag == LevelUpdater.ONE)
					{
						if(_isTrendOnPeriodDown)
						{
							_zigZagDiapasone.BuyZigZagDiapasone.Level = buyLevelPrice;
							_zigZagDiapasone.SellZigZagDiapasone.Level = Double.MaxValue;
						}
						else
						{
							_zigZagDiapasone.BuyZigZagDiapasone.Level = Double.MinValue;
							_zigZagDiapasone.SellZigZagDiapasone.Level = sellLevelPrice;
						}
					}
					else if(UpdateLevelsInOneZigZag == LevelUpdater.TWO)
					{
						_zigZagDiapasone.BuyZigZagDiapasone.Level = buyLevelPrice;
						_zigZagDiapasone.SellZigZagDiapasone.Level = sellLevelPrice;
					}
					*/
					
					if(zigZagLarge.Trend == 1)
					{
						_zigZagDiapasone.BuyZigZagDiapasone.Level = buyLevelPrice;
						_zigZagDiapasone.SellZigZagDiapasone.Level = Double.MaxValue;
					}
					else if(zigZagLarge.Trend == -1)
					{
						_zigZagDiapasone.BuyZigZagDiapasone.Level = Double.MinValue;
						_zigZagDiapasone.SellZigZagDiapasone.Level = sellLevelPrice;
					}
					
					_zigZagDiapasone.HighApexPrice = Highs[0][CurrentBars[0] - zigZagSmall.HighBarPeriod + 1];
					_zigZagDiapasone.LowApexPrice = Lows[0][CurrentBars[0] - zigZagSmall.LowBarPeriod + 1];

					Log("Добавлен новый уровень: " + _zigZagDiapasone.ToString());
					
					zigZagSmall.IsChangePeriod = false;
				}
						
				
				
				Log("Текущие уровни для входа в сделки: ");
				foreach(DailyData daily in _historyData.DailyDataList)
				{
					foreach(ZigZagDiapasone zigzag in daily.ZigZagDiapasoneList)
					{
						Log(zigzag.ToString());
					}
				}	
				
				Log("------Конец Обработки дефолтного таймфрейма-------");
				Log(Time[0].ToString());
				Log("==================================================");
			}
			
			
			if(BarsInProgress == 1 && RealTime == Switch.OFF)
			{	
				double openPrice = Opens[1][0];
			
				_previousPrice = _price;
				_price = openPrice;
				
				BuyOrSell(_price, _previousPrice, _previousHighBarApex, _previousLowBarApex, _procentOfMiddleValot);
			}
			
			if(BarsInProgress == 2)
			{
				Log("================================================");
				Log("------Старт Обработки Дневного таймфрейма-------");
				
				_dailyData = new DailyData(Time[0]);
				_historyData.AddDaylyZigZag(_dailyData);	
				
				Log("Дней сохранено в истории: " + _historyData.DailyDataList.Count);

				Log("------Конец Обработки Дневного таймфрейма-------");
				Log(Time[0].ToString());
				Log("================================================");	
			}	
        }
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Last) {
				double openPrice = e.Price;
				_previousPrice = _price;
				_price = openPrice;
				
				BuyOrSell(_price, _previousPrice, _previousHighBarApex, _previousLowBarApex, _procentOfMiddleValot);
				
			}
		}
			
		
		
		private void BuyOrSell(double price, double previousPrice, double previousHighBarApex, double previousLowBarApex, double procentOfMiddleValot)
		{
			if (Position.MarketPosition == MarketPosition.Flat)
			{
				_orderAction = OrderAction.FLAT;
				_isStopLossOutOfApex = false;
				
				
				List<IZigZagDiapasone> sortedLongList = new List<IZigZagDiapasone>();
				List<IZigZagDiapasone> sortedShortList = new List<IZigZagDiapasone>();
				
				foreach(DailyData dailyData in _historyData.DailyDataList)
				{
					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList)
					{
						sortedLongList.Add(zigZag.BuyZigZagDiapasone);
						sortedShortList.Add(zigZag.SellZigZagDiapasone);
					}
				}

				foreach(DailyData dailyData in _historyData.DailyDataList)
				{
					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList)
					{
						if(_orderAction == OrderAction.FLAT)
						{
							if(price > zigZagLargeSellLevel)
							{
								if(!IsPresentNextLevelInProcentDiapasone(zigZag, _historyData, priceBetweenApexLargeZigZagSell * ProcentForNextLevel / 100, OrderAction.SELL))
								{
									if(price > zigZag.SellZigZagDiapasone.LevelWithPostTicks && !zigZag.SellZigZagDiapasone.IsDeleted)
									{			
										Log(" priceBetweenApexLargeZigZag * ProcentForNextLevel / 100 " + (priceBetweenApexLargeZigZagSell * ProcentForNextLevel / 100));
										Log("Активные уровни:");
										foreach(DailyData dailyData1 in _historyData.DailyDataList)
										{
											foreach(ZigZagDiapasone zigZag1 in dailyData1.ZigZagDiapasoneList)
											{
												Log(zigZag1.ToString());
											}
										}
										
										_orderAction = OrderAction.SELL;
										EnterOrders("SellOrder1", "SellOrder2", price, previousPrice, _orderAction, zigZag);		
									}
								}
								else
								{
									zigZag.SellZigZagDiapasone.DeleteZigZagDiapasone();
								}
							}
							else if(price < zigZagLargeBuyLevel)
							{
								if(!IsPresentNextLevelInProcentDiapasone(zigZag, _historyData, priceBetweenApexLargeZigZagBuy * ProcentForNextLevel / 100 , OrderAction.BUY))
								{
									if(price < zigZag.BuyZigZagDiapasone.LevelWithPostTicks && !zigZag.BuyZigZagDiapasone.IsDeleted)
									{
										Log("Активные уровни:");
										foreach(DailyData dailyData1 in _historyData.DailyDataList)
										{
											foreach(ZigZagDiapasone zigZag1 in dailyData1.ZigZagDiapasoneList)
											{
												Log(zigZag1.ToString());
											}
										}
										
										_orderAction = OrderAction.BUY;
										EnterOrders("BuyOrder1", "BuyOrder2", price, previousPrice, _orderAction, zigZag);
									}
								}
								else
								{
									zigZag.BuyZigZagDiapasone.DeleteZigZagDiapasone();
								}
							}
						}
						else
						{
							break;
							break;
						}
					}
				}
			}	
			else 
			{
				PriceAwayFilter(price, _startOrderPrice, previousHighBarApex, previousLowBarApex);
				BreakevenFilter(price);
				
				if(RealTime == Switch.OFF)
				{
					RealTimeOffExitOrders(price, _orderAction);
				}
			}
			
			DeleteCrossedLevels(price);
			
		}
		
		public bool IsPresentNextLevelInProcentDiapasone(ZigZagDiapasone zigZagDiapasone, HistoryData historyData, double value, OrderAction orderAction)
		{
				foreach(DailyData dailyData in _historyData.DailyDataList)
				{
					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList)
					{
						if(orderAction == OrderAction.BUY && Math.Abs(zigZagDiapasone.BuyZigZagDiapasone.Level - zigZag.BuyZigZagDiapasone.Level) < value 
							&& zigZagDiapasone.BuyZigZagDiapasone.Level < zigZag.BuyZigZagDiapasone.Level
							)
						{
							Log("zigZagDiapasone " + zigZagDiapasone.ToString());
							Log("zigZag " + zigZag.ToString());
							return true;
						}
						else if(orderAction == OrderAction.SELL && Math.Abs(zigZagDiapasone.SellZigZagDiapasone.Level - zigZag.SellZigZagDiapasone.Level) < value 
							&& zigZagDiapasone.SellZigZagDiapasone.Level > zigZag.SellZigZagDiapasone.Level
							)
						{
							Log("zigZagDiapasone " + zigZagDiapasone.ToString());
							Log("zigZag " + zigZag.ToString());
							return true;
						}
					}
				}
				return false;
		}
		
		
		private void DeleteCrossedLevels(double price)
		{
			foreach(DailyData dailyData in _historyData.DailyDataList)
			{
				foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList)
				{
					
					if(price < zigZag.BuyZigZagDiapasone.LevelWithPostTicks && !zigZag.BuyZigZagDiapasone.IsDeleted)
					{
						zigZag.BuyZigZagDiapasone.DeleteZigZagDiapasone();
					}
					else if(price > zigZag.SellZigZagDiapasone.LevelWithPostTicks && !zigZag.SellZigZagDiapasone.IsDeleted)
					{
						zigZag.SellZigZagDiapasone.DeleteZigZagDiapasone();
					}
				}
			}
		}
		
		private void RealTimeOffExitOrders(double price, OrderAction orderAction)
		{
			if(price <= _exitOrders.SmallProfitTarget && orderAction == OrderAction.SELL)
			{
				ExitShort("ExitShort1", "SellOrder1");
			}
			if(price <= _exitOrders.LargeProfitTarget && orderAction == OrderAction.SELL)
			{
				ExitShort("ExitShort2", "SellOrder2");
			}
			
			if(price >= _exitOrders.SmallProfitTarget && orderAction == OrderAction.BUY)
			{
				ExitLong("ExitLong1", "BuyOrder1");
			}
			if(price >= _exitOrders.LargeProfitTarget && orderAction == OrderAction.BUY)
			{
				ExitLong("ExitLong2", "BuyOrder2");
			}
			
			if(price >= _exitOrders.StopLoss && orderAction == OrderAction.SELL)
			{
				ExitShort("ExitShort1", "SellOrder1");
				ExitShort("ExitShort2", "SellOrder2");
			}

			if(price <= _exitOrders.StopLoss && orderAction == OrderAction.BUY)
			{
				ExitLong("ExitLong1", "BuyOrder1");
				ExitLong("ExitLong2", "BuyOrder2");
			}
		}
		
		private void BreakevenFilter(double price)
		{
			if(BreakevenSwitch == Switch.ON)
				{
					double formula = ProfitTargetLarge * TickSize;
				
					if(_orderAction == OrderAction.SELL)
					{
						if(price < (_startOrderPrice - (formula * Breakeven / 100)))
						{
							_isStopLossOutOfApex = true;
							
							if(RealTime == Switch.ON)
							{
								SetStopLoss("SellOrder1", CalculationMode.Price, _startOrderPrice, false);
								SetStopLoss("SellOrder2", CalculationMode.Price, _startOrderPrice, false);
							}
							else if(RealTime == Switch.OFF)
							{
								_exitOrders.StopLoss = _startOrderPrice;
							}
						}
					}
					else if(_orderAction == OrderAction.BUY)
					{
						if(price > (_startOrderPrice + (formula * Breakeven / 100)))
						{
							_isStopLossOutOfApex = true;
							
							if(RealTime == Switch.ON)
							{
								SetStopLoss("BuyOrder1", CalculationMode.Price, _startOrderPrice, false);
								SetStopLoss("BuyOrder2", CalculationMode.Price, _startOrderPrice, false);
							}
							else if(RealTime == Switch.OFF)
							{
								_exitOrders.StopLoss = _startOrderPrice;
							}
						}
					}
				}
		}
		
		
		private void PriceAwayFilter(double price, double startOrderPrice, double previousHighBarApex, double previousLowBarApex)
		{
			if(PriceAwaySwitch == Switch.ON && _orderIndexBar < _indexBar && !_isStopLossOutOfApex)
			{
				if((startOrderPrice > price + PriceAway * TickSize) && _orderAction == OrderAction.SELL)
				{
					_isStopLossOutOfApex = true;
					_stopLossOutOfApex = new StopLossOutOfApex(previousHighBarApex, PriceAway * TickSize, OrderAction.SELL);
							
					if(RealTime == Switch.ON)
					{
						SetStopLoss("SellOrder1", CalculationMode.Price, _stopLossOutOfApex.StopLossPrice + (2 * TickSize), false);
						SetStopLoss("SellOrder2", CalculationMode.Price, _stopLossOutOfApex.StopLossPrice + (2 * TickSize), false);
					}
					else if(RealTime == Switch.OFF)
					{
						_exitOrders.StopLoss = _stopLossOutOfApex.StopLossPrice + (2 * TickSize);	
					}
				}	
				else if((startOrderPrice < price - PriceAway * TickSize) && _orderAction == OrderAction.BUY)
				{
					_isStopLossOutOfApex = true;
					_stopLossOutOfApex = new StopLossOutOfApex(previousLowBarApex, PriceAway * TickSize, OrderAction.BUY);
				
					if(RealTime == Switch.ON)
					{
						SetStopLoss("BuyOrder1", CalculationMode.Price, _stopLossOutOfApex.StopLossPrice - (2 * TickSize), false);
						SetStopLoss("BuyOrder2", CalculationMode.Price, _stopLossOutOfApex.StopLossPrice - (2 * TickSize), false);
					}
					else if(RealTime == Switch.OFF)
					{
						_exitOrders.StopLoss = _stopLossOutOfApex.StopLossPrice - (2 * TickSize);
					}
				}		
			}
		}
		
		private bool IsObjectInDiapasone(double target, double period1, double period2)
		{
			if((target >= period1 && target <= period2) || (target >= period2 && target <= period1))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
				
		private void EnterOrders(string order1, string order2, double price, double previousPrice, OrderAction orderAction, ZigZagDiapasone zigZag)
		{
			
			Log("Current price: " + price);
			Log("Previous price: " + previousPrice);
			
			{
				if(orderAction == OrderAction.BUY)
				{
					EnterLong(1, 1, order1);
					EnterLong(1, 1, order2);
					
					Log("OrderAction.Buy");
					if(RealTime == Switch.OFF)
					{
						_exitOrders = new ExitOrders(price, StopLoss, ProfitTargetLarge, ProfitTargetSmall, OrderAction.BUY, TickSize);
					}


					Log("Before level delete -> " + zigZag.ToString(OrderAction.BUY));
					zigZag.BuyZigZagDiapasone.DeleteZigZagDiapasone();
					Log("After level delete -> " + zigZag.ToString(OrderAction.BUY));
				}
				else if(orderAction == OrderAction.SELL)
				{
					EnterShort(1, 1, order1);
					EnterShort(1, 1, order2);
					
					Log("OrderAction.Sell");
					if(RealTime == Switch.OFF)
					{
						_exitOrders = new ExitOrders(price, StopLoss, ProfitTargetLarge, ProfitTargetSmall, OrderAction.SELL, TickSize);
					}
					
					Log("Before level delete -> " + zigZag.ToString(OrderAction.SELL));
					zigZag.SellZigZagDiapasone.DeleteZigZagDiapasone();
					Log("After level delete -> " + zigZag.ToString(OrderAction.SELL));
				}	
				
				_orderIndexBar = _indexBar;
				_startOrderPrice = price;
				
				if(RealTime == Switch.ON)
				{
					SetProfitTarget(order1, CalculationMode.Ticks, ProfitTargetLarge);
					SetProfitTarget(order2, CalculationMode.Ticks, ProfitTargetSmall);
					SetStopLoss(order1,CalculationMode.Ticks, StopLoss, false);
					SetStopLoss(order2,CalculationMode.Ticks, StopLoss, false);
				}

			}
		}
		
		private double GetLowOrHighPriceOfBar(bool isFoundLowPriceOnBar, int startBar, int endBar)
		{
			double low = Lows[0][CurrentBars[0] - startBar];
			double high = Highs[0][CurrentBars[0] - startBar];
			int start = CurrentBars[0] - startBar;
			int end = CurrentBars[0] - endBar;
			
			for(;start > end; start--)
			{
				if(isFoundLowPriceOnBar)
				{
					if(low < Lows[0][start])
					{
						low = Lows[0][start];
					}
				}
				else
				{
					if(high > Highs[0][start])
					{
						high = Highs[0][start];
					}
				}
			}
			if(isFoundLowPriceOnBar)
				return low;
			else
				return high;
		}
		
		private void Log(string message)
		{
			if((DateLogEnabled == Switch.ON && Time[0] > DateLogFrom && Time[0] < DateLogTo) || (DateLogEnabled == Switch.OFF))
			{
				if(_logger.LogEnabled == Switch.ON)
				{
					if(_logger.LoggerType == LogType.Console)
					{
						Print(message);
					}
					else if(_logger.LoggerType == LogType.File)
					{
					
					}
				}
			}
		}
		
		#region Enums
		
		public enum OrderAction{
			BUY,
			SELL,
			FLAT
		}
		
				public enum Switch
		{
			ON,
			OFF
		}
		
		public enum LevelUpdater
		{
			OFF,
			ONE,
			TWO
			
		}
		
		#endregion
		
		#region Data Objects and Lists
		
		public class ExitOrders
		{
			public double StopLoss {get; set;}
			public double SmallProfitTarget {get; set;}
			public double LargeProfitTarget {get; set;}
			
			public OrderAction GetOrderAction ()
			{
				return _orderAction;
			}
			
			private double _startOrderPriceTick;
			private double _stopLossTick;
			private double _largeProfitTargetTick;
			private double _smallProfitTargetTick;
			private OrderAction _orderAction; 
			private double _tickSize;
				
			public ExitOrders(double startOrderPrice, double stopLossTick, double largeProfitTargetTick, double smallProfitTargetTick, OrderAction orderAction, double tickSize)
			{
				_startOrderPriceTick = startOrderPrice;
				_stopLossTick = stopLossTick;
				_largeProfitTargetTick = largeProfitTargetTick;
				_smallProfitTargetTick = smallProfitTargetTick;
				_orderAction = orderAction;
				_tickSize = tickSize;
				
				if(orderAction == OrderAction.BUY)
				{
					StopLoss = startOrderPrice - stopLossTick * tickSize;
					SmallProfitTarget = startOrderPrice + smallProfitTargetTick * tickSize;
					LargeProfitTarget = startOrderPrice + largeProfitTargetTick * tickSize;
				}
				else if(orderAction == OrderAction.SELL)
				{
					StopLoss = startOrderPrice + stopLossTick * tickSize;
					SmallProfitTarget = startOrderPrice - smallProfitTargetTick * tickSize;
					LargeProfitTarget = startOrderPrice - largeProfitTargetTick * tickSize;
				}
				
			}
		}
		
		
		public class HistoryData{

			private readonly int _daysOfHistory;

			public List<DailyData> DailyDataList {get; private set;}

			public HistoryData(int days)
			{
				_daysOfHistory = days;
				DailyDataList = new List<DailyData>();
			}

			public void AddDaylyZigZag(DailyData zigZag)
			{
				Resize();
				DailyDataList.Add(zigZag);
			}

			private void Resize()
			{
				if (DailyDataList.Count >= _daysOfHistory)
					DailyDataList.RemoveAt(0);
			}

		}
		

		
		public class StopLossOutOfApex
		{
			public double StopLossPrice {get; private set;}
			public double Apex {get; set;}
			public double PriceAdd {get; set;}
			public OrderAction OrderType {get; private set;}
			
			
			public StopLossOutOfApex(double apex, double priceAdd, OrderAction orderAction)
			{
				Apex = apex;
				PriceAdd = priceAdd;
				OrderType = orderAction;
				
				
				if(OrderType == OrderAction.BUY)
				{
					StopLossPrice = Apex - PriceAdd;
				}
				else if(OrderType == OrderAction.SELL)
				{
					StopLossPrice = Apex + PriceAdd;
				}	
			}
			
			public override string ToString()
			{
				string orderType = "";
				if(OrderType == OrderAction.BUY)
				{
					orderType = "BUY";
				}
				else if(OrderType == OrderAction.SELL)
				{
					orderType = "SELL";
				}
				return string.Format("СтопЛосс за вершину -> СтопЛосс: {0}, Цена вершины: {1}, Добавить к вершине: {2}, Тип ордера: {3}"
					, StopLossPrice, Apex, PriceAdd, orderType);
			}
		}
		
		public class DailyData
		{

			public List<ZigZagDiapasone> ZigZagDiapasoneList{get; private set;}
			public List<OnBarData> OnBarDataList{get; private set;}
			public DateTime DailyDateTime {get; private set;}
			
			public DailyData(DateTime dateTime)
			{
				DailyDateTime = dateTime;
				ZigZagDiapasoneList = new List<ZigZagDiapasone>();
				OnBarDataList = new List<OnBarData>();
			}
			
			public override string ToString(){
				return string.Format("День -> Время: {0}, Количество баров: {1}, Количество зигзагов: {2}", DailyDateTime, OnBarDataList.Count, ZigZagDiapasoneList.Count);
			}
			
		}
		
		public interface IZigZagDiapasone{
			bool IsDeleted{get;set;}
			DateTime AddedDateTime{get;set;}
			double Level{get;set;}
			double ZigZagApex{get;set;}
			double LevelWithPostTicks{get;set;}
			void DeleteZigZagDiapasone();
		}
		
		public class BuyZigZagDiapasone : IZigZagDiapasone{
		
			public bool IsDeleted {get; set;}
			public DateTime AddedDateTime {get; set;}
			
			private double _ticks = 0;
			private int _index = 0;

			private double _buyLevel = 0;
			public double Level{
				get
				{
					return _buyLevel;
				}
				set
				{	
					_buyLevel = value;
					LevelWithPostTicks = _buyLevel + _ticks;
				}
			}
			public double ZigZagApex{get;set;}
			public double LevelWithPostTicks{get;set;}
			
			public BuyZigZagDiapasone(double ticks, int index){
				_ticks = ticks;
				_index = index;
			}
			
			public void DeleteZigZagDiapasone()
			{
				Level = 0;
				ZigZagApex = 0;
				LevelWithPostTicks = 0;
				IsDeleted = true;
			}
			
			public override string ToString()
			{
				return string.Format("Уровень -> Индекс {3}, Добавлен {4} - Уровень на покупку: {0}, Вершина зигзага: {1}, Уровень на покупку после смещения: {2}", Level, ZigZagApex, LevelWithPostTicks, _index, AddedDateTime);
			}
			
		}
		
		public class SellZigZagDiapasone : IZigZagDiapasone{
		
			public bool IsDeleted {get; set;}
			public DateTime AddedDateTime {get; set;}
			
			private double _ticks = 0;
			private int _index = 0;

			private double _sellLevel = 0;
			public double Level{
				get
				{
					return _sellLevel;
				}
				set
				{
					_sellLevel = value;
					LevelWithPostTicks = _sellLevel - _ticks ;
				}
			}
			public double ZigZagApex{get;set;}
			public double LevelWithPostTicks{get;set;}
			
			
			public SellZigZagDiapasone(double ticks, int index){
				_ticks = ticks;
				_index = index;
				
			}		
			
			public void DeleteZigZagDiapasone()
			{
				Level = 0;
				ZigZagApex = 0;
				LevelWithPostTicks = 0;
				IsDeleted = true;
			}
			
			public override string ToString()
			{
				return string.Format("Уровень -> Индекс {3}, Добавлен {4} - Уровень на продажу: {0}, Вершина зигзага: {1}, Уровень на продажу после смещения: {2}", Level, ZigZagApex, LevelWithPostTicks, _index, AddedDateTime);
			}			
			
		}
		
		
		
		public class ZigZagDiapasone{
			
			public bool IsBuyDeleted 
			{
				get
				{
					return BuyZigZagDiapasone.IsDeleted;
				} 
				private set
				{
					BuyZigZagDiapasone.IsDeleted = value;
				}
			}
			public bool IsSellDeleted 
			{
				get
				{
					return SellZigZagDiapasone.IsDeleted;
				}
				private set
				{
					SellZigZagDiapasone.IsDeleted = value;
				}
			}
			
			public double Ticks {get; set;}
			public int Index {get; private set;}
			
			private DateTime _addedDateTime;
			public DateTime AddedDateTime {
				get{return _addedDateTime;}
				set
				{
					_addedDateTime = value;
					SellZigZagDiapasone.AddedDateTime = value;
					BuyZigZagDiapasone.AddedDateTime = value;
				}
			}
			
			public SellZigZagDiapasone SellZigZagDiapasone{get; set;}
			public BuyZigZagDiapasone BuyZigZagDiapasone{get; set;}

			
			public double HighApexPrice {get; set;}
			public double LowApexPrice {get; set;}
			
			public double DifferenceBetweenApex
			{
				get
				{
					return (HighApexPrice + LowApexPrice) / 2;	 
				}
			}
			
			public ZigZagDiapasone(double ticks, int index)
			{
				SellZigZagDiapasone = new SellZigZagDiapasone(ticks, index);
				BuyZigZagDiapasone = new BuyZigZagDiapasone(ticks, index);
				Ticks = ticks;
				Index = index;
				
				IsBuyDeleted = false;
				IsSellDeleted = false;
			}
			
			public string ToString(OrderAction orderAction)
			{
				if(orderAction == OrderAction.SELL)
				{
					return SellZigZagDiapasone.ToString();
				}
				else
				{
					return BuyZigZagDiapasone.ToString();
				}
			}
			
			public override string ToString()
			{
				return string.Format("Уровни -> {0} <---> {1} | Вершина зигзага: {2}, Низина зигзага: {3}", SellZigZagDiapasone.ToString(), BuyZigZagDiapasone.ToString(), HighApexPrice, LowApexPrice);
			}
		}
		
		
		
		
		public class OnBarData{
			
			public int BarIndex{get;private set;}
			
			public OnBarData(int index){
				BarIndex = index;
			}
			
			
			public override string ToString(){
				return string.Format("Бар -> Индекс бара: {0}", BarIndex);
			}
		}
		
		
		public class Logger
		{
			
			public Switch LogEnabled {get;set;}
			public LogType LoggerType{get; private set;}
			public Logger(LogType logType, Switch logEnabled)
			{
				LoggerType = logType;
				LogEnabled = logEnabled;
			}
			
			public void Write(string message)
			{
				if(LogEnabled == Switch.ON)
				{
					
				}
			}
		}
		
		public enum LogType
		{
			Console,
			File
		}
		
		
		#endregion
		
		
		#region ZigZag
		public class ZigZag
		{
			
			//zigzag
			private double			_currentZigZagHigh	= 0;
			private double			_currentZigZagLow	= 0;
			private DeviationType	_deviationType		= DeviationType.Percent;
			private double			_deviationValue		= 0.8;
			private DataSeries		_zigZagHighZigZags;
			private DataSeries		_zigZagLowZigZags;
			private DataSeries		_zigZagHighSeries;
			private DataSeries		_zigZagLowSeries;
			private int				_lastSwingIdx		= -1;
			private double			_lastSwingPrice		= 0.0;
			private int				_trendDir			= 0; // 1 = trend up, -1 = trend down, init = 0
			private bool			_useHighLow			= false;
			private int _highBar = 0;
			private int _lowBar = 0;
		
			private DataSeries _strategy;
			private double _tickSize;
			
			
			public int LastLowBarPeriod
			{get; private set;}
			public int LowBarPeriod
			{get; private set;}
			public double LowZigZagPrice
			{get; private set;}
			
			
			public int LastHighBarPeriod
			{get; private set;}
			public int HighBarPeriod
			{get; private set;}
			public double HighZigZagPrice
			{get; private set;}
			
			public bool IsChangePeriod
			{get; set;}
			
			public int Trend
			{get; private set;}
			
			public double LastPrice
			{get; private set;}
			
			public ZigZag(double tickSize, DataSeries zigZagHighZigZags, DataSeries zigZagLowZigZags, DataSeries zigZagHighSeries, DataSeries zigZagLowSeries
				, DeviationType deviationType, double deviationValue, bool useHighLow)
			{
				_tickSize = tickSize;
				_zigZagHighZigZags = zigZagHighZigZags;
				_zigZagLowZigZags = zigZagLowZigZags;
				_zigZagHighSeries = zigZagHighSeries;
				_zigZagLowSeries = zigZagLowSeries;
				_deviationType = deviationType;
				_deviationValue = deviationValue;
				_useHighLow = useHighLow;
			}
			
			public void UpdateOnBar(AqueGenNinjaBotStrategyV2 strategy, int currentBar, IDataSeries highSeries, IDataSeries lowSeries, double input0, IDataSeries input)
			{
				if (currentBar < 2) // need 3 bars to calculate Low/High
				{
					_zigZagHighSeries.Set(0);
					_zigZagHighZigZags.Set(0);
					_zigZagLowSeries.Set(0);
					_zigZagLowZigZags.Set(0);
					return;
				}
				// Initialization
				if (_lastSwingPrice == 0.0)
					_lastSwingPrice = input0;

				//IDataSeries highSeries = High;
				//IDataSeries lowSeries	= Low;

				if (!_useHighLow)
				{
					highSeries	= input;
					lowSeries	= input;
				}

				// Calculation always for 1-bar ago !

				double tickSize = _tickSize;
				bool isSwingHigh	= highSeries[1] >= highSeries[0] - double.Epsilon 
									&& highSeries[1] >= highSeries[2] - double.Epsilon;
				bool isSwingLow		= lowSeries[1] <= lowSeries[0] + double.Epsilon 
									&& lowSeries[1] <= lowSeries[2] + double.Epsilon;  
				bool isOverHighDeviation	= (_deviationType == DeviationType.Percent && IsPriceGreater(highSeries[1], (_lastSwingPrice * (1.0 + _deviationValue * 0.01))))
											|| (_deviationType == DeviationType.Points && IsPriceGreater(highSeries[1], _lastSwingPrice + _deviationValue));
				bool isOverLowDeviation		= (_deviationType == DeviationType.Percent && IsPriceGreater(_lastSwingPrice * (1.0 - _deviationValue * 0.01), lowSeries[1]))
											|| (_deviationType == DeviationType.Points && IsPriceGreater(_lastSwingPrice - _deviationValue, lowSeries[1]));

				double	saveValue	= 0.0;
				bool	addHigh		= false; 
				bool	addLow		= false; 
				bool	updateHigh	= false; 
				bool	updateLow	= false; 

				_zigZagHighZigZags.Set(0);
				_zigZagLowZigZags.Set(0);

				if (!isSwingHigh && !isSwingLow)
				{
					_zigZagHighSeries.Set(_currentZigZagHigh);
					_zigZagLowSeries.Set(_currentZigZagLow);
					return;
				}
				
				if (_trendDir <= 0 && isSwingHigh && isOverHighDeviation)
				{	
					saveValue	= highSeries[1];
					addHigh		= true;
					_trendDir	= 1;
					Trend = _trendDir;
				}	
				else if (_trendDir >= 0 && isSwingLow && isOverLowDeviation)
				{	
					saveValue	= lowSeries[1];
					addLow		= true;
					_trendDir	= -1;
					Trend = _trendDir;
				}	
				else if (_trendDir == 1 && isSwingHigh && IsPriceGreater(highSeries[1], _lastSwingPrice)) 
				{
					saveValue	= highSeries[1];
					updateHigh	= true;
				}
				else if (_trendDir == -1 && isSwingLow && IsPriceGreater(_lastSwingPrice, lowSeries[1])) 
				{
					saveValue	= lowSeries[1];
					updateLow	= true;
				}

				if (addHigh || addLow || updateHigh || updateLow)
				{
					if (updateHigh && _lastSwingIdx >= 0)
					{
						_zigZagHighZigZags.Set(currentBar - _lastSwingIdx, 0);
						//Value.Reset(CurrentBar - lastSwingIdx);
					}
					else if (updateLow && _lastSwingIdx >= 0)
					{
						_zigZagLowZigZags.Set(currentBar - _lastSwingIdx, 0);
						//Value.Reset(CurrentBar - lastSwingIdx);
					}

					if (addHigh || updateHigh)
					{
						_zigZagHighZigZags.Set(1, saveValue);
						_zigZagHighZigZags.Set(0, 0);

						_currentZigZagHigh = saveValue;
						_zigZagHighSeries.Set(1, _currentZigZagHigh);
						//Value.Set(1, currentZigZagHigh);
						

						
						if(addHigh && !updateHigh)
						{
							//if(lastLowBarPeriod != lowBar)
							{
								LastLowBarPeriod = LowBarPeriod;
								LowBarPeriod = _lowBar;
								if (!_useHighLow){
									LowZigZagPrice = strategy.Close[currentBar - LowBarPeriod + 1];
								}
								else{
									LowZigZagPrice = strategy.Low[currentBar - LowBarPeriod + 1];
								}
							}
							_highBar = currentBar;
							IsChangePeriod = true;
						}
						if(!addHigh && updateHigh){
							_highBar = currentBar;
						}
						
						
					}
					else if (addLow || updateLow) 
					{
						_zigZagLowZigZags.Set(1, saveValue);
						_zigZagLowZigZags.Set(0, 0);

						_currentZigZagLow = saveValue;
						_zigZagLowSeries.Set(1, _currentZigZagLow);
						//Value.Set(1, currentZigZagLow);
						
						if(addLow && !updateLow){
							
							//if(lastHighBarPeriod != highBar)
							{
								LastHighBarPeriod = HighBarPeriod;
								HighBarPeriod = _highBar;
								if (!_useHighLow){
									HighZigZagPrice = strategy.Close[currentBar - HighBarPeriod + 1];
								}
								else{
									HighZigZagPrice = strategy.High[currentBar - HighBarPeriod + 1];
								}
							}
							_lowBar = currentBar;
							IsChangePeriod = true;
						}
						if(!addLow && updateLow){
							_lowBar = currentBar;
						}
						
						
					}

					_lastSwingIdx	= currentBar - 1;
					_lastSwingPrice	= saveValue;
					LastPrice = _lastSwingPrice;
				}

				_zigZagHighSeries.Set(_currentZigZagHigh);
				_zigZagLowSeries.Set(_currentZigZagLow);
			}
		
			private bool IsPriceGreater(double a, double b)
			{
				if (a > b && a - b > _tickSize / 2)
					return true; 
				else 
					return false;
			}
		}
		#endregion
		
		
        #region Properties	
		
			
		[Description("Установить отступ от крайне левой точки ZigZag")]
		[GridCategory("ZigZag")]
		public int LeftZigZag
        {get; set;}
		
		[Description("Установить отступ от крайне левой точки ZigZag")]
		[GridCategory("ZigZag")]
		public int RightZigZag
        {get; set;}

       
		[Description("Deviation in percent or points regarding on the deviation type")]
        [GridCategory("ZigZagLarge")]
		[Gui.Design.DisplayName("Deviation value")]
        public double DeviationValueLarge
        {get; set;}

        [Description("Type of the deviation value")]
        [GridCategory("ZigZagLarge")]
		[Gui.Design.DisplayName("Deviation type")]
        public DeviationType DeviationTypeLarge
        {get; set;}

        [Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("ZigZagLarge")]
		[Gui.Design.DisplayName("Use high and low")]
		[RefreshProperties(RefreshProperties.All)]
        public bool UseHighLowLarge
        {get; set;}
		
		[Description("Deviation in percent or points regarding on the deviation type")]
        [GridCategory("ZigZagSmall")]
		[Gui.Design.DisplayName("Deviation value")]
        public double DeviationValueSmall
        {get; set;}

        [Description("Type of the deviation value")]
        [GridCategory("ZigZagSmall")]
		[Gui.Design.DisplayName("Deviation type")]
        public DeviationType DeviationTypeSmall
        {get; set;}

        [Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("ZigZagSmall")]
		[Gui.Design.DisplayName("Use high and low")]
		[RefreshProperties(RefreshProperties.All)]
        public bool UseHighLowSmall
        {get; set;}
		
		
        [Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("ZigZag")]
        public int SaveZigZagDaysOnHistory
        {get; set;}
		
		
		[Description("Ордера")]
        [GridCategory("OrderParameters")]
        public double ProfitTargetSmall
        {get; set;}
		
		[Description("Ордера")]
        [GridCategory("OrderParameters")]
        public double ProfitTargetLarge
        {get; set;}
		
		[Description("Ордера")]
        [GridCategory("BreakevenFilter")]
        public double Breakeven
        {get; set;}

		[Description("Ордера")]
        [GridCategory("OrderParameters")]
        public double StopLoss
        {get; set;}
		
		[Description("Ордера")]
        [GridCategory("OrderParameters")]
		public int ProcentForNextLevel
		{get;set;}
		
		[GridCategory("Level")]
		public int AddTicksForOrderLevel
		{get; set;}
		
		//[GridCategory("Level")]
		//public LevelUpdater UpdateLevelsInOneZigZag
		//{get; set;}
		
		[GridCategory("PriceAwayFilter")]
		public Switch PriceAwaySwitch
		{get; set;}
		
		[GridCategory("BreakevenFilter")]
		public Switch BreakevenSwitch
		{get; set;}
			
		
		[GridCategory("PriceAwayFilter")]
		public int PriceAway
		{get; set;}
		
		[GridCategory("RealTime")]
		public Switch RealTime
		{get; set;}
		

		[GridCategory("Logs")]
		public Switch LogEnabled
		{get; set;}
		
		[GridCategory("Logs")]
		public LogType LoggerType
		{get;set;}
		
		[GridCategory("Logs")]
		public Switch DateLogEnabled
		{get; set;}
		
		[GridCategory("Logs")]
		public DateTime DateLogFrom
		{get;set;}
		
		[GridCategory("Logs")]
		public DateTime DateLogTo
		{get;set;}
		
		
		
		
		#region (c)
		/*© AqueGen (Artem Frolov) 
		Emails: aquegen@yandex.ru, artem.frolov.aquegen@gmail.com */
		#endregion	
		
        #endregion
    }
}
