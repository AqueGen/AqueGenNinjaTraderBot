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
		
		private int _rightZigZag = 4;
		private int _leftZigZag = 4;
		
		
		
		private int startBar = 0;
		private int endBar = 0;
		
		//Orders
		private double startOrderPrice = 0;
		private CurrentOrder currentOrder;
		
		private bool isTrendOnPeriodDown;
		private double sellLevelPrice = 0;
		private double buyLevelPrice = 0;
		
		private int highBar = 0;
		private int lowBar = 0;
		
		private int highBarPeriod = 0;
		private int lowBarPeriod = 0;
		private int lastHighBarPeriod = 0;
		private int lastLowBarPeriod = 0;
		
		private int periodOfCalculate = 0;

		private bool isChangePeriod = false;
		

		//Orders stop or profit
		private double stopLoss = 40;
		private double profitTargetLarge = 100;
		private double profitTargetSmall = 30;
		private double breakeven = 30;
			

		
		
		//Analog RSI
		private double lowLineRSIAnalog = 0;
		private double highLineRSIAnalog = Double.MaxValue;
		
		//SMA
		private int smaPeriod = 120;
		private double  smaLine = 0;
		private double middleValot = 0;
		private int dayOfSMAValot = 5;
		private int procentFromMiddleValot = 50;
		
		
		//ZigZag history
		private HistoryData historyData;
		private DailyData dailyData;
		private ZigZagDiapasone zigZagDiapasone;
		private OnBarData onBarData;
		
		private double highZigZagPrice = 0;
		private double lowZigZagPrice = 0;
		
		private int _saveZigZagDaysOnHistory = 7;
		
		
		private int indexBar = -1;
		private int orderIndexBar = -1;
		
		private StopLossOutOfApex stopLossOutOfApex;
		private bool isStopLossOutOfApex = false;
		private double previousLowBarApex = -1;
		private double previousHighBarApex = -1;
		
		private double currentStopLoss = -1;
		
		//Logs
		private Logger logger;

		private double lastCompletedZigZagApexPriceDistance = -1;
		private double lastApexPrice = -1;
		private double procentOfMiddleValot = -1;
		
		//test
		private ExitOrders exitOrders;
		
		private double exitLongStopLoss = -1;
		private double exitShortStopLoss = -1;
		
		private double exitLongProfitLarge = -1;
		private double exitLongProfitSmall = -1;
		private double exitShortProfitLarge = -1;
		private double exitShortProfitSmall = -1;
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {

            CalculateOnBarClose = false;

			//zigzag
			zigZagHighSeries	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagHighZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagLowSeries		= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagLowZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 

			Add(PeriodType.Tick, 1);
			Add(PeriodType.Day, 1);
        }

		protected override void OnStartUp()
		{	
			historyData = new HistoryData(SaveZigZagDaysOnHistory);
			dailyData = new DailyData(Time[0]);
			zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel * TickSize, CurrentBars[0]);
			onBarData = new OnBarData(0);
			
			//Log
			logger = new Logger(LoggerType, LogEnabled);
		}
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		

        protected override void OnBarUpdate()
        {	
			if(CurrentBars[0] < 5 || CurrentBars[1] < 5 || CurrentBars[2] < 5)
			{
				return;
			}

			if(BarsInProgress == 0){
				indexBar = CurrentBars[0];
				
				if(currentOrder == CurrentOrder.SELL || currentOrder == CurrentOrder.BUY)
				{
					previousLowBarApex = Lows[0][0];
					previousHighBarApex = Highs[0][0];
				}
				
				ZigZagUpdateOnBar();
				if(lowBarPeriod == 0 || highBarPeriod == 0) { return; } 
				
				
				onBarData = new OnBarData(CurrentBar);
				dailyData.OnBarDataList.Add(onBarData);
				
				Log("==================================================");
				Log("------Старт Обработки дефолтного таймфрейма-------");
				smaLine = SMA(SMAPeriod)[0];
				Log("Дневная валотильность: " + middleValot);
				Log("Текущая цена SMA линии -> " + smaLine);
				
				procentOfMiddleValot = middleValot * ProcentFromMiddleValot / 100;
				
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

					zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel * TickSize, CurrentBars[0]);
					dailyData.ZigZagDiapasoneList.Add(zigZagDiapasone);
						
					zigZagDiapasone.AddedDateTime = Time[0];
				
					if(isTrendOnPeriodDown)
					{
						zigZagDiapasone.BuyZigZagDiapasone.Level = buyLevelPrice;
						zigZagDiapasone.SellZigZagDiapasone.Level = Double.MaxValue;
					}
					else
					{
						zigZagDiapasone.BuyZigZagDiapasone.Level = Double.MinValue;
						zigZagDiapasone.SellZigZagDiapasone.Level = sellLevelPrice;
					}
					//zigZagDiapasone.BuyZigZagDiapasone.Level = buyLevelPrice;
					//zigZagDiapasone.SellZigZagDiapasone.Level = sellLevelPrice;
					
					
					zigZagDiapasone.BuyZigZagDiapasone.ZigZagApex = lowZigZagPrice;
					zigZagDiapasone.SellZigZagDiapasone.ZigZagApex = highZigZagPrice;
					
					zigZagDiapasone.HighApexPrice = Highs[0][CurrentBars[0] - highBarPeriod + 1];
					zigZagDiapasone.LowApexPrice = Lows[0][CurrentBars[0] - lowBarPeriod + 1];

					
					
					
					
					if(EnterOrdersWithAverageZigZagValueSwitch == Switch.ON)
					{
						lastCompletedZigZagApexPriceDistance = 0;
						int zigZagIndex = 0;
						foreach(DailyData day in historyData.DailyDataList.Reverse<DailyData>())
						{
							foreach(ZigZagDiapasone zigZag in day.ZigZagDiapasoneList.Reverse<ZigZagDiapasone>())
							{
								if(zigZagIndex < ChangePriceAfterZigZagCount)
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
						
					}
					
					
					
					
					Log(zigZagDiapasone.ToString());
					
					
					Log("Добавлен новый уровень на покупку: " + zigZagDiapasone.ToString(OrderAction.BUY));
					Log("Добавлен новый уровень на продажу: " + zigZagDiapasone.ToString(OrderAction.SELL));
					
					isChangePeriod = false;
				}
						
				
				
				Log("Текущие уровни для входа в сделки: ");
				foreach(DailyData daily in historyData.DailyDataList)
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
				
				BuyOrSell(_price, _previousPrice);
			}
			
			if(BarsInProgress == 2)
			{
				Log("================================================");
				Log("------Старт Обработки Дневного таймфрейма-------");
				
				dailyData = new DailyData(Time[0]);
				historyData.AddDaylyZigZag(dailyData);	
				
				Log("Дней сохранено в истории: " + historyData.DailyDataList.Count);

				
				
				middleValot = 0;
				for(int i = 0; i < DayOfSMAValot; i++){
					middleValot = middleValot + Highs[2][i] - Lows[2][i];
				}
				middleValot = middleValot / DayOfSMAValot;
				
				
				
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
				return low;
			else
				return high;
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
					

					
					if(addHigh && !updateHigh)
					{
						//if(lastLowBarPeriod != lowBar)
						{
							lastLowBarPeriod = lowBarPeriod;
							lowBarPeriod = lowBar;
							if (!useHighLow){
								lowZigZagPrice = Close[CurrentBar - lowBarPeriod + 1];
							}
							else{
								lowZigZagPrice = Low[CurrentBar - lowBarPeriod + 1];
							}
						}
						highBar = CurrentBar;
						isChangePeriod = true;
						Log("ZIGZAG addHigh " + Time[0]);
					}
					if(!addHigh && updateHigh){
						highBar = CurrentBar;
						Log("ZIGZAG updateHigh " + Time[0]);
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
						
						//if(lastHighBarPeriod != highBar)
						{
							lastHighBarPeriod = highBarPeriod;
							highBarPeriod = highBar;
							if (!useHighLow){
								highZigZagPrice = Close[CurrentBar - highBarPeriod + 1];
							}
							else{
								highZigZagPrice = High[CurrentBar - highBarPeriod + 1];
							}
							
						}
						lowBar = CurrentBar;
						isChangePeriod = true;
						Log("ZIGZAG addLow " + Time[0]);
					}
					if(!addLow && updateLow){
						lowBar = CurrentBar;
						Log("ZIGZAG updateLow " + Time[0]);
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
		
		
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Last) {
				double openPrice = e.Price;
				_previousPrice = _price;
				_price = openPrice;
				
				BuyOrSell(_price, _previousPrice);
				
			}
		}
			
		
		
		private void BuyOrSell(double price, double previousPrice){

			if (Position.MarketPosition == MarketPosition.Flat)
			{
				currentOrder = CurrentOrder.FLAT;
				isStopLossOutOfApex = false;
				
				foreach(DailyData dailyData in historyData.DailyDataList)
				{
					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList)
					{

						if(currentOrder == CurrentOrder.FLAT)
						{
							if((EnterOrdersWithAverageZigZagValueSwitch == Switch.ON && !IsObjectInDiapasone(price, lastCompletedZigZagApexPriceDistance + procentOfMiddleValot, lastCompletedZigZagApexPriceDistance - procentOfMiddleValot)) 
								|| EnterOrdersWithAverageZigZagValueSwitch == Switch.OFF)
							{

								if(price > zigZag.SellZigZagDiapasone.LevelWithPostTicks && !zigZag.SellZigZagDiapasone.IsDeleted)
								{	
									//if(price > highLineRSIAnalog)
									{
										Log(Time[1].ToString());
										Log("lastCompletedZigZagApexPriceDistance " + lastCompletedZigZagApexPriceDistance);
										Log("procentOfMiddleValot " + procentOfMiddleValot);
										Log("lastCompletedZigZagApexPriceDistance + procentOfMiddleValot " + (lastCompletedZigZagApexPriceDistance + procentOfMiddleValot));
										Log("lastCompletedZigZagApexPriceDistance - procentOfMiddleValot " + (lastCompletedZigZagApexPriceDistance - procentOfMiddleValot));
										
										orderIndexBar = indexBar;
										startOrderPrice = price;
										currentOrder = CurrentOrder.SELL;
										EnterOrders("SellOrder1", "SellOrder2", price, previousPrice, OrderAction.SELL, zigZag);
									}
								}
								else if(price < zigZag.BuyZigZagDiapasone.LevelWithPostTicks && !zigZag.BuyZigZagDiapasone.IsDeleted)
								{
									//if(price < lowLineRSIAnalog)
									{
										Log(Time[1].ToString());
										Log("lastApexPrice " + lastApexPrice);
										Log("procentOfMiddleValot " + procentOfMiddleValot);
										Log("lastApexPrice + procentOfMiddleValot " + (lastApexPrice + procentOfMiddleValot));
										Log("lastApexPrice - procentOfMiddleValot " + (lastApexPrice - procentOfMiddleValot));
										
										orderIndexBar = indexBar;
										startOrderPrice = price;
										currentOrder = CurrentOrder.BUY;
										EnterOrders("BuyOrder1", "BuyOrder2", price, previousPrice, OrderAction.BUY, zigZag);
									}
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

				if(PriceAwaySwitch == Switch.ON && orderIndexBar < indexBar && !isStopLossOutOfApex)
				{
					if((startOrderPrice > price + PriceAway * TickSize) && currentOrder == CurrentOrder.SELL)
					{
						isStopLossOutOfApex = true;
						stopLossOutOfApex = new StopLossOutOfApex(previousHighBarApex, PriceAway * TickSize, OrderAction.SELL);
								
						if(RealTime == Switch.ON)
						{
							SetStopLoss("SellOrder1", CalculationMode.Price, stopLossOutOfApex.StopLossPrice + (2 * TickSize), false);
							SetStopLoss("SellOrder2", CalculationMode.Price, stopLossOutOfApex.StopLossPrice + (2 * TickSize), false);
						}
						else if(RealTime == Switch.OFF)
						{
							exitOrders.StopLoss = stopLossOutOfApex.StopLossPrice + (2 * TickSize);	
						}
					}	
					else if((startOrderPrice < price - PriceAway * TickSize) && currentOrder == CurrentOrder.BUY)
					{
						isStopLossOutOfApex = true;
						stopLossOutOfApex = new StopLossOutOfApex(previousLowBarApex, PriceAway * TickSize, OrderAction.BUY);
					
						if(RealTime == Switch.ON)
						{
							SetStopLoss("BuyOrder1", CalculationMode.Price, stopLossOutOfApex.StopLossPrice - (2 * TickSize), false);
							SetStopLoss("BuyOrder2", CalculationMode.Price, stopLossOutOfApex.StopLossPrice - (2 * TickSize), false);
						}
						else if(RealTime == Switch.OFF)
						{
							exitOrders.StopLoss = stopLossOutOfApex.StopLossPrice - (2 * TickSize);
						}
					}
						
				}
				
				if(BreakevenSwitch == Switch.ON)
				{
					double formula = ProfitTargetLarge * TickSize;
				
					if(currentOrder == CurrentOrder.SELL)
					{
						if(price < (startOrderPrice - (formula * Breakeven / 100)))
						{
							isStopLossOutOfApex = true;
							currentStopLoss = startOrderPrice;
							
							if(RealTime == Switch.ON)
							{
								SetStopLoss("SellOrder1", CalculationMode.Price, startOrderPrice, false);
								SetStopLoss("SellOrder2", CalculationMode.Price, startOrderPrice, false);
							}
							else if(RealTime == Switch.OFF)
							{
								exitOrders.StopLoss = startOrderPrice;
							}
						}
					}
					else if(currentOrder == CurrentOrder.BUY)
					{
						if(price > (startOrderPrice + (formula * Breakeven / 100)))
						{
							isStopLossOutOfApex = true;
							currentStopLoss = startOrderPrice;
							if(RealTime == Switch.ON)
							{
								SetStopLoss("BuyOrder1", CalculationMode.Price, startOrderPrice, false);
								SetStopLoss("BuyOrder2", CalculationMode.Price, startOrderPrice, false);
							}
							else if(RealTime == Switch.OFF)
							{
								exitOrders.StopLoss = startOrderPrice;
							}
						}
					}
				}
					
					
				if(RealTime == Switch.OFF)
				{
					if(price <= exitOrders.SmallProfitTarget && currentOrder == CurrentOrder.SELL)
					{
						ExitShort("ExitShort1", "SellOrder1");
					}
					if(price <= exitOrders.LargeProfitTarget && currentOrder == CurrentOrder.SELL)
					{
						ExitShort("ExitShort2", "SellOrder2");
					}
					
					if(price >= exitOrders.SmallProfitTarget && currentOrder == CurrentOrder.BUY)
					{
						ExitLong("ExitLong1", "BuyOrder1");
					}
					if(price >= exitOrders.LargeProfitTarget && currentOrder == CurrentOrder.BUY)
					{
						ExitLong("ExitLong2", "BuyOrder2");
					}
					
					if(price >= exitOrders.StopLoss && currentOrder == CurrentOrder.SELL)
					{
						ExitShort("ExitShort1", "SellOrder1");
						ExitShort("ExitShort2", "SellOrder2");
					}

					if(price <= exitOrders.StopLoss && currentOrder == CurrentOrder.BUY)
					{
						ExitLong("ExitLong1", "BuyOrder1");
						ExitLong("ExitLong2", "BuyOrder2");
					}
				}
			}
			
			foreach(DailyData dailyData in historyData.DailyDataList)
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
				
		private void EnterOrders(string order1, string order2, double price, double previousPrice, OrderAction orderAction, ZigZagDiapasone zigZag){
			
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
						exitOrders = new ExitOrders(price, StopLoss, ProfitTargetLarge, ProfitTargetSmall, OrderAction.BUY, TickSize);
					}
					currentOrder = CurrentOrder.BUY;

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
						exitOrders = new ExitOrders(price, StopLoss, ProfitTargetLarge, ProfitTargetSmall, OrderAction.SELL, TickSize);
					}
					currentOrder = CurrentOrder.SELL;
					
					Log("Before level delete -> " + zigZag.ToString(OrderAction.SELL));
					zigZag.SellZigZagDiapasone.DeleteZigZagDiapasone();
					Log("After level delete -> " + zigZag.ToString(OrderAction.SELL));
					
				}	
				
				if(RealTime == Switch.ON)
				{
					SetProfitTarget(order1, CalculationMode.Ticks, ProfitTargetLarge);
					SetProfitTarget(order2, CalculationMode.Ticks, ProfitTargetSmall);
					SetStopLoss(order1,CalculationMode.Ticks, StopLoss, false);
					SetStopLoss(order2,CalculationMode.Ticks, StopLoss, false);
				}

			}
		}
		
		//private void PriceAway
		
		private bool IsCanChangeStopLoss(double currentStopLoss, double newStopLoss, OrderAction orderAction)
		{
			return false;
		}
		
		private void Log(string message)
		{
			if((DateLogEnabled == Switch.ON && Time[0] > DateLogFrom && Time[0] < DateLogTo) || (DateLogEnabled == Switch.OFF))
			{
				if(logger.LogEnabled == Switch.ON)
				{
					if(logger.LoggerType == LogType.Console)
					{
						Print(message);
					}
					else if(logger.LoggerType == LogType.File)
					{
					
					}
				}
			}
		}
		
		
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
		
		public enum OrderAction{
			BUY,
			SELL
		}
		
				public enum Switch
		{
			ON,
			OFF
		}
		
		public enum CurrentOrder
		{
			BUY,
			SELL,
			FLAT
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
		
		

		
		
        #region Properties	
		
			
		[Description("Установить отступ от крайне левой точки ZigZag")]
		[GridCategory("ZigZag")]
		public int LeftZigZag
		{
			get{return _leftZigZag;}
			set{_leftZigZag = value;}
		}
		
		[Description("Установить отступ от крайне левой точки ZigZag")]
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
		
		
        [Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("ZigZag")]
        public int SaveZigZagDaysOnHistory
        {
            get { return _saveZigZagDaysOnHistory; }
            set { _saveZigZagDaysOnHistory = value; }
        }
		
		
		[Description("Ордера")]
        [GridCategory("OrderParameters")]
        public double ProfitTargetSmall
        {
          get{return profitTargetSmall;}
          set{profitTargetSmall = value;}
        }
		
		[Description("Ордера")]
        [GridCategory("OrderParameters")]
        public double ProfitTargetLarge
        {
          get{return profitTargetLarge;}
          set{profitTargetLarge = value;}
        }
		
		[Description("Ордера")]
        [GridCategory("OrderParameters")]
        public double Breakeven
        {
          get{return breakeven;}
          set{breakeven = value;}
        }

		[Description("Ордера")]
        [GridCategory("OrderParameters")]
        public double StopLoss
        {
          get{return stopLoss;}
          set{stopLoss = value;}
        }
		
		 [GridCategory("Level")]
		public int AddTicksForOrderLevel
		{get; set;}
		
		
		[Description("Numbers of bars used for calculations")]
		[GridCategory("SMA Parameters")]
		public int SMAPeriod
		{
			get { return smaPeriod; }
			set { smaPeriod = Math.Max(1, value); }
		}
		
		[Description("Numbers of bars used for calculations")]
		[GridCategory("SMA Parameters")]
		public int DayOfSMAValot
		{
			get { return dayOfSMAValot; }
			set { dayOfSMAValot = Math.Max(1, value); }
		}
		
		[Description("Numbers of bars used for calculations")]
		[GridCategory("SMA Parameters")]
		public int ProcentFromMiddleValot
		{
			get { return procentFromMiddleValot; }
			set { procentFromMiddleValot = Math.Max(1, value); }
		}
			
		
		[GridCategory("Filters")]
		public Switch PriceAwaySwitch
		{get; set;}
		
		[GridCategory("Filters")]
		public Switch BreakevenSwitch
		{get; set;}
		
		[GridCategory("FiltersZigZag")]
		public Switch EnterOrdersWithAverageZigZagValueSwitch
		{get; set;}
		
		[GridCategory("FiltersZigZag")]
		public int ChangePriceAfterZigZagCount
		{get; set;}
		
		
		[GridCategory("PriceAway")]
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
