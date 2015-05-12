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
    public class AqueGenNinjaBotStrategy : Strategy
    {
        //#region Variables
        // Wizard generated variables
        private int myInput0 = 1; // Default setting for MyInput0
		

		//Price
		private double _price = 0;
		private double _previousPrice = 0;
		
		
		
		//zigzag
		private double			currentZigZagHigh	= 0;
		private double			currentZigZagLow	= 0;
		private DeviationType	deviationType		= DeviationType.Percent;
		private double			deviationValue		= 0.8;
		private DataSeries		zigZagHighZigZags; 
		private DataSeries		zigZagLowZigZags; 
		private DataSeries		zigZagHighSeries; 
		private DataSeries		zigZagLowSeries; 
		private int				lastSwingIdx		= -1;
		private double			lastSwingPrice		= 0.0;
		private int				trendDir			= 0; // 1 = trend up, -1 = trend down, init = 0
		private bool			useHighLow			= false;
		
		private int _rightZigZag = 5;
		private int _leftZigZag = 5;
		
		private int addTicksForOrderLevel = 5;
		private double addTicksForOrderLevelTickSize = 0;
		
		private double averageVolume = 0;
		private int averageVolumeForLastBars = 5;
		
		private int procentOfValotForNextLevel = 50;
		
		private static List<IZigZagDiapasone> buyZigZagDiapasoneList  = new List<IZigZagDiapasone>();
		private static List<IZigZagDiapasone> sellZigZagDiapasoneList  = new List<IZigZagDiapasone>();
		
		
		private int startBar = 0;
		private int endBar = 0;
		
		//Orders
		private double startOrderPrice = 0;
		private bool isBuyOrder = false;
		private bool isSellOrder = false;
		
		private bool isTrendOnPeriodDown;
		private double sellLevelPrice = 0;
		private double buyLevelPrice = 0;
		
		private int highBar = 0;
		private int lowBar = 0;
		
		private int highBarPeriod = 0;
		private int lowBarPeriod = 0;
		private int lastHighBarPeriod = 0;
		private int lastLowBarPeriod = 0;

		private bool isChangePeriod = false;
		
		private double previousLowBarApex = -1;
		private double previousHighBarApex = -1;
		private bool isStopLossOutOfApex = false;
		
		
		

		//Orders stop or profit
		private double stopLoss = 40;
		private double profitTargetLarge = 80;
		private double profitTargetSmall = 40;
		private double breakeven = 50;
		
		private double _changePriceAfterLastZigZagApexInProcent = 50;
	
		
		private StopLossOutOfApex stopLossOutOfApex;
			
		private double lastCompletedZigZagApexPriceDistance = -1;
		private double lastApexPrice = -1;
		
		
		//Analog RSI
		private double lowLineRSIAnalog = 0;
		private double highLineRSIAnalog = Double.MaxValue;
		
		//SMA
		private int smaPeriod = 120;
		private double  smaLine = 0;
		private double middleValot = 0;
		private int dayOfSMAValot = 5;
		private int procentFromMiddleValot = 100;
		
		
		//ZigZag history
		private HistoryData historyData;
		private DailyData dailyData;
		private ZigZagDiapasone zigZagDiapasone;
		
		private BarData barData;
		
		private double highZigZagPrice = 0;
		private double lowZigZagPrice = 0;
		
		private int _saveZigZagDaysOnHistory = 3;
		
		
		//Conditions
		private bool isDeleteLevelsIfPriceInDiapasoneEnabled = true;
		
		
		//Logs
		private bool isLogEnabled = false;
		private Logger logger;
		
		
		private int indexBar = 0;
		private int orderIndexBar = 0;
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
			ExitOnClose = false;
			
			EntriesPerDirection = 2;
			
			Add(PeriodType.Tick, 1);
			Add(PeriodType.Day, 1);
        }
		
		protected override void OnStartUp()
		{	
			//zigzag
			zigZagHighSeries	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagHighZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagLowSeries		= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagLowZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			
			
			historyData = new HistoryData(SaveZigZagDaysOnHistory);
			
			dailyData = new DailyData(Time[0]);
			zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel * TickSize, CurrentBars[0]);
			barData = new BarData(0, Time[0]);
			
			
		    
			//Log
			logger = new Logger(LogType.Console, IsLogEnabled);
		}

        protected override void OnBarUpdate()
        {	
			if(CurrentBars[0] < 5 && CurrentBars[1] < 5 && CurrentBars[2] < 5){
			//	return;
			}

			
			if(BarsInProgress == 0)
			{				
				indexBar = CurrentBars[0];
				
				previousLowBarApex = Lows[0][0];
				previousHighBarApex = Highs[0][0];
				Log("CurrentBar -> " + indexBar);
				
				ZigZagUpdateOnBar();
				if(lowBarPeriod == 0 || highBarPeriod == 0) { return; } 
				
				Log("==================================================");
				Log("------Старт Обработки дефолтного таймфрейма-------");
				smaLine = SMA(SMAPeriod)[0];
				Log("Дневная валотильность: " + middleValot);
				Log("Текущая цена SMA линии -> " + smaLine);
				
				double procentOfMiddleValot = middleValot * ProcentFromMiddleValot / 100;
				
				lowLineRSIAnalog = smaLine - procentOfMiddleValot;
				highLineRSIAnalog = smaLine + procentOfMiddleValot;
				
				Log("Уровень RSIA на покупку: " + lowLineRSIAnalog);
				Log("Уровень RSIA на продажу: " + highLineRSIAnalog);
				

				

				
				if(highBarPeriod > lowBarPeriod)
				{
					isTrendOnPeriodDown = false;
					startBar = lowBarPeriod - 1 - LeftZigZag;
					endBar = highBarPeriod - 1 + RightZigZag;
				}
				else
				{
					isTrendOnPeriodDown = true;
					startBar = highBarPeriod - 1 - LeftZigZag;
					endBar = lowBarPeriod - 1 + RightZigZag;
				}

					
				if(startBar < endBar && isChangePeriod)
				{
					sellLevelPrice = GetLowOrHighPriceOfBar(true, startBar, endBar);
					buyLevelPrice = GetLowOrHighPriceOfBar(false, startBar, endBar);

					
					zigZagDiapasone.AddedDateTime = Time[0];
					zigZagDiapasone.BuyZigZagDiapasone.Level = buyLevelPrice;
					zigZagDiapasone.SellZigZagDiapasone.Level = sellLevelPrice;
					zigZagDiapasone.BuyZigZagDiapasone.ZigZagApex = lowZigZagPrice;
					zigZagDiapasone.SellZigZagDiapasone.ZigZagApex = highZigZagPrice;
					zigZagDiapasone.HighApexPrice = Highs[0][CurrentBars[0] - highBarPeriod + 1];
					zigZagDiapasone.LowApexPrice = Lows[0][CurrentBars[0] - lowBarPeriod + 1];
								
					Log(zigZagDiapasone.ToString());
					
					if(IsChangePriceAfterLastZigZagApexEnabled)
					{
						lastCompletedZigZagApexPriceDistance = 0;
						int zigZagIndex = 0;
						foreach(DailyData day in historyData.DailyDataList.Reverse<DailyData>())
						{
							foreach(ZigZagDiapasone zigZag in day.ZigZagDiapasoneList.Reverse<ZigZagDiapasone>())
							{
								if(zigZagIndex <= ChangePriceAfterZigZagCount)
								{
									lastCompletedZigZagApexPriceDistance = lastCompletedZigZagApexPriceDistance + zigZag.DifferenceBetweenApex;
									zigZagIndex++;
									Log("zigZag.DifferenceBetweenApex -> " + zigZag.DifferenceBetweenApex);
								}
								else
								{
									break;
									break;
								}
							}
						}
							
						Log("lastCompletedZigZagApexPriceDistance -> " + lastCompletedZigZagApexPriceDistance);
						Log("zigZagIndex -> " + zigZagIndex.ToString());
						lastCompletedZigZagApexPriceDistance = lastCompletedZigZagApexPriceDistance / zigZagIndex;
						Log("lastCompletedZigZagApexPriceDistance after-> " + lastCompletedZigZagApexPriceDistance);
						
						if(isTrendOnPeriodDown)
						{
							lastApexPrice = zigZagDiapasone.HighApexPrice;
						}
						else if(!isTrendOnPeriodDown)
						{
							lastApexPrice = zigZagDiapasone.LowApexPrice;
						}
					}
					
					
					Log("Добавлен новый уровень на покупку: " + zigZagDiapasone.ToString(OrderAction.BUY));
					Log("Добавлен новый уровень на продажу: " + zigZagDiapasone.ToString(OrderAction.SELL));
					
					dailyData.AddZigZagDiapasone(zigZagDiapasone);
					
					zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel * TickSize, CurrentBars[0]);
					
					isChangePeriod = false;
				}
					
				
				Log("Текущие уровни для входа в сделки: " + dailyData.ZigZagDiapasoneList.Count);
				foreach(DailyData daily in historyData.DailyDataList)
				{
					foreach(ZigZagDiapasone zigzag in daily.ZigZagDiapasoneList)
					{
						Log(zigzag.ToString());
					}
				}	
				dailyData.OnBarDataList.Add(barData);
				
				
				barData = new BarData(CurrentBar, Time[0]);
				
				
				Log("------Конец Обработки дефолтного таймфрейма-------");
				Log(Time[0].ToString());
				Log("==================================================");
			}
			
			if(BarsInProgress == 1)
			{	
				double openPrice = Opens[1][0];
			
				_previousPrice = _price;
				_price = openPrice;

				BuyOrSell(_price, _previousPrice, CurrentBars[0]);
			}
			
			if(BarsInProgress == 2)
			{		
				Log("================================================");
				Log("------Старт Обработки Дневного таймфрейма-------");
				
				historyData.AddDaylyZigZag(dailyData);
				
				Log("Дней сохранено в истории: " + historyData.DailyDataList.Count);

				middleValot = 0;
				for(int i = 0; i < DayOfSMAValot; i++)
				{
					middleValot = middleValot + Highs[2][i] - Lows[2][i];
				}
				middleValot = middleValot / DayOfSMAValot;
				
				dailyData = new DailyData(Time[0]);
				
				Log("Дневная валотильность: " + middleValot);
				Log("------Конец Обработки Дневного таймфрейма-------");
				Log(Time[0].ToString());
				Log("================================================");
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
			{
				return low;
			}
			else
			{
				return high;
			}
		}
		
		#region ZigZag
		private void ZigZagUpdateOnBar(){
		if (CurrentBar < 2) // need 3 bars to calculate Low/High
			{
				zigZagHighSeries.Set(0);
				zigZagHighZigZags.Set(0);
				zigZagLowSeries.Set(0);
				zigZagLowZigZags.Set(0);
				return;
			}
			// Initialization
			if (lastSwingPrice == 0.0)
				lastSwingPrice = Input[0];

			IDataSeries highSeries	= High;
			IDataSeries lowSeries	= Low;

			if (!useHighLow)
			{
				highSeries	= Input;
				lowSeries	= Input;
			}

			// Calculation always for 1-bar ago !

			double tickSize = Bars.Instrument.MasterInstrument.TickSize;
			bool isSwingHigh	= highSeries[1] >= highSeries[0] - double.Epsilon 
								&& highSeries[1] >= highSeries[2] - double.Epsilon;
			bool isSwingLow		= lowSeries[1] <= lowSeries[0] + double.Epsilon 
								&& lowSeries[1] <= lowSeries[2] + double.Epsilon;  
			bool isOverHighDeviation	= (deviationType == DeviationType.Percent && IsPriceGreater(highSeries[1], (lastSwingPrice * (1.0 + deviationValue * 0.01))))
										|| (deviationType == DeviationType.Points && IsPriceGreater(highSeries[1], lastSwingPrice + deviationValue));
			bool isOverLowDeviation		= (deviationType == DeviationType.Percent && IsPriceGreater(lastSwingPrice * (1.0 - deviationValue * 0.01), lowSeries[1]))
										|| (deviationType == DeviationType.Points && IsPriceGreater(lastSwingPrice - deviationValue, lowSeries[1]));

			double	saveValue	= 0.0;
			bool	addHigh		= false; 
			bool	addLow		= false; 
			bool	updateHigh	= false; 
			bool	updateLow	= false; 

			zigZagHighZigZags.Set(0);
			zigZagLowZigZags.Set(0);

			if (!isSwingHigh && !isSwingLow)
			{
				zigZagHighSeries.Set(currentZigZagHigh);
				zigZagLowSeries.Set(currentZigZagLow);
				return;
			}
			
			if (trendDir <= 0 && isSwingHigh && isOverHighDeviation)
			{	
				saveValue	= highSeries[1];
				addHigh		= true;
				trendDir	= 1;
			}	
			else if (trendDir >= 0 && isSwingLow && isOverLowDeviation)
			{	
				saveValue	= lowSeries[1];
				addLow		= true;
				trendDir	= -1;
			}	
			else if (trendDir == 1 && isSwingHigh && IsPriceGreater(highSeries[1], lastSwingPrice)) 
			{
				saveValue	= highSeries[1];
				updateHigh	= true;
			}
			else if (trendDir == -1 && isSwingLow && IsPriceGreater(lastSwingPrice, lowSeries[1])) 
			{
				saveValue	= lowSeries[1];
				updateLow	= true;
			}

			if (addHigh || addLow || updateHigh || updateLow)
			{
				if (updateHigh && lastSwingIdx >= 0)
				{
					zigZagHighZigZags.Set(CurrentBar - lastSwingIdx, 0);
					//Value.Reset(CurrentBar - lastSwingIdx);
				}
				else if (updateLow && lastSwingIdx >= 0)
				{
					zigZagLowZigZags.Set(CurrentBar - lastSwingIdx, 0);
					//Value.Reset(CurrentBar - lastSwingIdx);
				}

				if (addHigh || updateHigh)
				{
					zigZagHighZigZags.Set(1, saveValue);
					zigZagHighZigZags.Set(0, 0);

					currentZigZagHigh = saveValue;
					zigZagHighSeries.Set(1, currentZigZagHigh);
					//Value.Set(1, currentZigZagHigh);
					

					
					if(addHigh && !updateHigh){
						if(lastLowBarPeriod != lowBar){
							lastLowBarPeriod = lowBarPeriod;
							lowBarPeriod = lowBar;
							//Log("lowBarPeriod " + (CurrentBar - lowBarPeriod + 1));
							if (!useHighLow){
								lowZigZagPrice = Close[CurrentBar - lowBarPeriod + 1];
							}
							else{
								lowZigZagPrice = Low[CurrentBar - lowBarPeriod + 1];
							}
						}
						highBar = CurrentBar;
						isChangePeriod = true;
					}
					if(!addHigh && updateHigh){
						highBar = CurrentBar;
					}
					
					
				}
				else if (addLow || updateLow) 
				{
					zigZagLowZigZags.Set(1, saveValue);
					zigZagLowZigZags.Set(0, 0);

					currentZigZagLow = saveValue;
					zigZagLowSeries.Set(1, currentZigZagLow);
					//Value.Set(1, currentZigZagLow);
					
					if(addLow && !updateLow){
						
						if(lastHighBarPeriod != highBar){
							lastHighBarPeriod = highBarPeriod;
							highBarPeriod = highBar;
							//Log("highBarPeriod " + (CurrentBar - highBarPeriod + 1));
							if (!useHighLow){
								highZigZagPrice = Close[CurrentBar - highBarPeriod+ 1];
							}
							else{
								highZigZagPrice = High[CurrentBar - highBarPeriod+ 1];
							}
							
						}
						lowBar = CurrentBar;
						isChangePeriod = true;

					}
					if(!addLow && updateLow){
						lowBar = CurrentBar;

					}
					
					
				}

				lastSwingIdx	= CurrentBar - 1;
				lastSwingPrice	= saveValue;
			}

			zigZagHighSeries.Set(currentZigZagHigh);
			zigZagLowSeries.Set(currentZigZagLow);
		}
		
		

		private bool IsPriceGreater(double a, double b)
		{
			if (a > b && a - b > TickSize / 2)
				return true; 
			else 
				return false;
		}
		#endregion
		
		
		/*
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			Log("test");
			if (e.MarketDataType == MarketDataType.Last) {
				double openPrice =e.Price;
			
				_previousPrice = _price;
				_price = openPrice;

				StopLossAndTakeProfit(_price);
				
				double volume = Volumes[1][0];
				onBarData.PriceVolumeOnBar.AddPriceVolume(_price, volume);
				double currentVolume = onBarData.PriceVolumeOnBar.VolumePriceOnBar[_price];
				
				
				BuyOrSell(_price, _previousPrice, currentVolume);
				
				
			}
		}*/
			
		private void BuyOrSell(double price, double previousPrice, int indexBar)
		{
			if (Position.MarketPosition == MarketPosition.Flat)
			{				
				isBuyOrder = false;
				isSellOrder = false;
				isStopLossOutOfApex = false;
				
				
				foreach(DailyData dailyData in historyData.DailyDataList)
				{
					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList)
					{
						double zigZagChangedPrice = lastCompletedZigZagApexPriceDistance * ChangePriceAfterLastZigZagApexInProcent / 100;
						if((IsChangePriceAfterLastZigZagApexEnabled && !IsObjectInDiapasone(price, lastApexPrice + zigZagChangedPrice, lastApexPrice - zigZagChangedPrice)) 
							|| !IsChangePriceAfterLastZigZagApexEnabled)
						{
						
							if(price > zigZag.SellZigZagDiapasone.LevelWithPostTicks && price < zigZag.SellZigZagDiapasone.ZigZagApex && !zigZag.IsSellDeleted)
							{
								if(price > highLineRSIAnalog)
								{
									orderIndexBar = indexBar;
									EnterOrders("SellOrder1", "SellOrder2", price, previousPrice, OrderAction.SELL, zigZag);
									break;
								}
							}
							else if(price < zigZag.BuyZigZagDiapasone.LevelWithPostTicks && price > zigZag.BuyZigZagDiapasone.ZigZagApex && !zigZag.IsBuyDeleted)
							{
								if(price < lowLineRSIAnalog)
								{
									orderIndexBar = indexBar;
									EnterOrders("BuyOrder1", "BuyOrder2", price, previousPrice, OrderAction.BUY, zigZag);
									break;
								}
							}
						}
					}
				
					if(IsDeleteLevelsIfPriceInDiapasoneEnabled)  
					{
						foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList)
						{
							if(price < zigZag.BuyZigZagDiapasone.LevelWithPostTicks && !zigZag.BuyZigZagDiapasone.IsDeleted)
							{
								Log("Удаление уровня на покупку: " + zigZag.BuyZigZagDiapasone.ToString());
								zigZag.BuyZigZagDiapasone.DeleteZigZagDiapasone();
								Log("После Удаление уровня на покупку: " + zigZag.BuyZigZagDiapasone.ToString());
							}
							else if(price > zigZag.SellZigZagDiapasone.LevelWithPostTicks && !zigZag.SellZigZagDiapasone.IsDeleted)
							{
								Log("Удаление уровня на продажу: " + zigZag.SellZigZagDiapasone.ToString());
								zigZag.SellZigZagDiapasone.DeleteZigZagDiapasone();
								Log("После Удаление уровня на продажу: " + zigZag.SellZigZagDiapasone.ToString());
							}	
						}
					}
				}
			}
			else
			{
				if(IsPriceAwayEnabled && orderIndexBar < indexBar && !isStopLossOutOfApex)
				{
					if(((startOrderPrice > price + PriceAway * TickSize) && isSellOrder) || ((startOrderPrice < price - PriceAway * TickSize) && isBuyOrder))
					{
						if(isSellOrder)
						{
							isStopLossOutOfApex = true;
							stopLossOutOfApex = new StopLossOutOfApex(previousHighBarApex, PriceAway * TickSize, OrderAction.SELL);
							
							Log("SellOrders -> " + stopLossOutOfApex.ToString());
							SetStopLoss("SellOrder1", CalculationMode.Price, stopLossOutOfApex.StopLossPrice + (2 * TickSize), false);
							SetStopLoss("SellOrder2", CalculationMode.Price, stopLossOutOfApex.StopLossPrice + (2 * TickSize), false);
						}
						else if(isBuyOrder)
						{
							isStopLossOutOfApex = true;
							stopLossOutOfApex = new StopLossOutOfApex(previousLowBarApex, PriceAway * TickSize, OrderAction.BUY);
							
							Log("BuyOrders -> " + stopLossOutOfApex.ToString());
							SetStopLoss("BuyOrder1", CalculationMode.Price, stopLossOutOfApex.StopLossPrice - (2 * TickSize), false);
							SetStopLoss("BuyOrder2", CalculationMode.Price, stopLossOutOfApex.StopLossPrice - (2 * TickSize), false);
						}
					}
				}
				
				if(IsBreakevenEnabled)
				{
					Log("IsBreakevenEnabled");
					double formula = ProfitTargetLarge * TickSize;
					if(isSellOrder)
					{
						if(price < (startOrderPrice - (formula * Breakeven / 100)))
						{
							isStopLossOutOfApex = true;
							SetStopLoss("SellOrder1", CalculationMode.Price, startOrderPrice, false);
							SetStopLoss("SellOrder2", CalculationMode.Price, startOrderPrice, false);
						}
					}
					else if(isBuyOrder)
					{
						if(price > (startOrderPrice + (formula * Breakeven / 100)))
						{
							isStopLossOutOfApex = true;
							SetStopLoss("BuyOrder1", CalculationMode.Price, startOrderPrice, false);
							SetStopLoss("BuyOrder2", CalculationMode.Price, startOrderPrice, false);
						}
					}
				}
			}
				
		}
		
		private bool IsObjectInDiapasone(double target, double period1, double period2)
		{
			if((target > period1 && target < period2) || (target > period2 && target < period1))
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		
		
		private void EnterOrders(string order1, string order2, double price, double previousPrice, OrderAction orderAction, ZigZagDiapasone zigZag){
			
			Log("Текущая цена: " + price);
			Log("Предыдущая цена: " + previousPrice);
			
			{
				if(orderAction == OrderAction.BUY)
				{
					EnterLong(order1);
					EnterLong(order2);
					
					Log("OrderAction.Buy");
					
					isBuyOrder = true;

					Log(zigZag.ToString(OrderAction.BUY));
					zigZag.BuyZigZagDiapasone.DeleteZigZagDiapasone();
					Log(zigZag.ToString(OrderAction.BUY));

				}
				else if(orderAction == OrderAction.SELL)
				{
					EnterShort(order1);
					EnterShort(order2);
					
					Log("OrderAction.Sell");
					
					isSellOrder = true;
					
					Log(zigZag.ToString(OrderAction.SELL));
					zigZag.SellZigZagDiapasone.DeleteZigZagDiapasone();
					Log(zigZag.ToString(OrderAction.SELL));
				}	
				
				startOrderPrice = price;

				
				SetProfitTarget(order1, CalculationMode.Ticks, ProfitTargetLarge);
				SetProfitTarget(order2, CalculationMode.Ticks, ProfitTargetSmall);
				SetStopLoss(order1,CalculationMode.Ticks, StopLoss, false);
				SetStopLoss(order2,CalculationMode.Ticks, StopLoss, false);
					
			}
		}
		
		private void Log(string message)
		{
			if((DateLogEnabled && Time[0] > DateLogFrom && Time[0] < DateLogTo) || (!DateLogEnabled))
			{
				if(logger.IsLogEnabled && logger.LoggerType == LogType.Console)
				{
					Print(message);
				}
				else if(logger.IsLogEnabled && logger.LoggerType == LogType.File)
				{
				
				}
			}
		}


		#region Data Objects and Lists
		
		
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
		
		
		
		public class HistoryData
		{
			private int _daysOfHistory;
			
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
				{
					DailyDataList.RemoveAt(0);
				}
			}
		}
		
		public enum OrderAction{
			BUY,
			SELL
		}
		
		public enum State
		{
			OFF,
			ON
		}
		
		
		
		public class DailyData
		{
			public List<ZigZagDiapasone> ZigZagDiapasoneList{get; private set;}
			public List<BarData> OnBarDataList{get; private set;}
			public DateTime DailyDateTime {get; private set;}

			public DailyData(DateTime dateTime)
			{
				DailyDateTime = dateTime;
				ZigZagDiapasoneList = new List<ZigZagDiapasone>();
				
				OnBarDataList = new List<BarData>();
			}
			
			public void AddZigZagDiapasone(ZigZagDiapasone zigZagDiapasone)
			{	
				ZigZagDiapasoneList.Add(zigZagDiapasone);
			}
			
			public override string ToString()
			{
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
					if(HighApexPrice > LowApexPrice)
					{
						return HighApexPrice - LowApexPrice;
					}
					else
					{
						return LowApexPrice - HighApexPrice;
					}
					 
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
		
		
		
		
		public class BarData
		{
			public int BarIndex{get;private set;}
			public DateTime BarDateTime {get; private set;}

			public BarData(int barIndex, DateTime barDateTime)
			{
				BarIndex = barIndex;
				BarDateTime = barDateTime;
			}
			
			public override string ToString()
			{
				return string.Format("Бар -> Индекс бара: {0}, Создан: {1}", BarIndex, BarDateTime);
			}
		}

		public class Logger
		{
			
			public bool IsLogEnabled {get;set;}
			public LogType LoggerType{get; private set;}
			public Logger(LogType logType, bool isLogEnabled)
			{
				LoggerType = logType;
				IsLogEnabled = isLogEnabled;
			}
			
			public void Write(string message)
			{
				if(IsLogEnabled)
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
		
        #region Properties	
		
			
		[Description("Расширение диапазона от точки зигзага в барах")]
		[GridCategory("ZigZag")]
		public int LeftZigZag
		{
			get{return _leftZigZag;}
			set{_leftZigZag = value;}
		}
		
		[Description("Расширение диапазона от точки зигзага в барах")]
		[GridCategory("ZigZag")]
		public int RightZigZag
		{
			get{return _rightZigZag;}
			set{_rightZigZag = value;}
		}

       
		[Description("Deviation in percent or points regarding on the deviation type")]
        [GridCategory("ZigZag")]
		[Gui.Design.DisplayName("Deviation value")]
        public double DeviationValue
        {
            get { return deviationValue; }
            set { deviationValue = Math.Max(0.0, value); }
        }

        [Description("Type of the deviation value")]
        [GridCategory("ZigZag")]
		[Gui.Design.DisplayName("Deviation type")]
        public DeviationType DeviationType
        {
            get { return deviationType; }
            set { deviationType = value; }
        }

        [Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("ZigZag")]
		[Gui.Design.DisplayName("Use high and low")]
		[RefreshProperties(RefreshProperties.All)]
        public bool UseHighLow
        {
            get { return useHighLow; }
            set { useHighLow = value; }
        }
		
		
        [Description("Сохранение исторических уровней (дней)")]
		[Gui.Design.DisplayName("Save Levels")]
        [GridCategory("ZigZag")]
        public int SaveZigZagDaysOnHistory
        {
            get { return _saveZigZagDaysOnHistory; }
            set { _saveZigZagDaysOnHistory = value; }
        }
		
	
		[Description("Короткий Тэйк-Профит (тики)")]
		[Gui.Design.DisplayName("Take Profit Small")]
        [GridCategory("Orders")]
        public double ProfitTargetSmall
        {
          get{return profitTargetSmall;}
          set{profitTargetSmall = value;}
        }
		
		[Description("Длинный Тэйк-Профит (тики)")]
		[Gui.Design.DisplayName("Take Profit Large")]
        [GridCategory("Orders")]
        public double ProfitTargetLarge
        {
          get{return profitTargetLarge;}
          set{profitTargetLarge = value;}
        }
		
		[Description("Перенос в безубыток. % от Длинного Тейк-профита")]
		[Gui.Design.DisplayName("Breakeven")]
        [GridCategory("Orders")]
        public double Breakeven
        {
          get{return breakeven;}
          set{breakeven = value;}
        }
		
        [GridCategory("Orders")]
        public bool IsBreakevenEnabled
        {get;set;}
		
		[GridCategory("Orders")]
        public bool IsPriceAwayEnabled
        {get;set;}
		
		[GridCategory("Orders")]
        public double ChangePriceAfterLastZigZagApexInProcent
        {
			get
			{
				return _changePriceAfterLastZigZagApexInProcent;
			}
			set
			{
				_changePriceAfterLastZigZagApexInProcent = value;
			}
		}
		
		[GridCategory("Orders")]
		public bool IsChangePriceAfterLastZigZagApexEnabled
		{get; set;}
		
		[GridCategory("Orders")]
		public int ChangePriceAfterZigZagCount
		{get; set;}
		

		[Description("Стоп-лосс для 2 контрактов (тики)")]
		[Gui.Design.DisplayName("Stop Loss")]
        [GridCategory("Orders")]
        public double StopLoss
        {
          get{return stopLoss;}
          set{stopLoss = value;}
        }
		
        [GridCategory("Orders")]
        public int PriceAway
        {get;set;}
		
		[Description("период скользящей средней")]
		[GridCategory("SMA Parameters")]
		public int SMAPeriod
		{
			get { return smaPeriod; }
			set { smaPeriod = value; }
		}
		
		[Description("Кол-во дней для подсчета дневной волатильности")]
		[Gui.Design.DisplayName("Volatility Days")]
		[GridCategory("SMA Parameters")]
		public int DayOfSMAValot
		{
			get { return dayOfSMAValot; }
			set { dayOfSMAValot = value; }
		}
		
		[Description("% использования дневной волатильности")]
		[Gui.Design.DisplayName("Procent of Volatility")]
		[GridCategory("SMA Parameters")]
		public int ProcentFromMiddleValot
		{
			get { return procentFromMiddleValot; }
			set { procentFromMiddleValot = value; }
		}

		[Description("")]
        [GridCategory("Level")]
        public int AddTicksForOrderLevel
        {
            get { return addTicksForOrderLevel; }
            set { addTicksForOrderLevel = value; }
        }
		
	
		
		[GridCategory("Filters")]
		public bool IsDeleteLevelsIfPriceInDiapasoneEnabled
		{
			get { return isDeleteLevelsIfPriceInDiapasoneEnabled; }
			set { isDeleteLevelsIfPriceInDiapasoneEnabled = value; }
		}
		

		
		[GridCategory("Logs")]
		public bool IsLogEnabled
		{
			get { return isLogEnabled; }
			set { isLogEnabled = value; }
		}
		
		[GridCategory("Logs")]
		public LogType LoggerType
		{get;set;}
		
		[GridCategory("Logs")]
		public bool DateLogEnabled
		{get; set;}
		
		[GridCategory("Logs")]
		public DateTime DateLogFrom
		{get;set;}
		
		[GridCategory("Logs")]
		public DateTime DateLogTo
		{get;set;}
		
	
		
		#region (c)
			/*© AqueGen (Artem Frolov)
				Emails: aquegen@yandex.ru, artem.frolov.aquegen@gmail.com
		*/
		#endregion	
        #endregion
    }
}
