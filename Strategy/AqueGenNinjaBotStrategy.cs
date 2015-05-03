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
		private double addTicksForOrderLevel = 0.5;
		
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
		
		private int periodOfCalculate = 0;

		private bool isChangePeriod = false;
		

		//Orders stop or profit
		private double stopLoss = 40;
		private double profitTargetLarge = 80;
		private double profitTargetSmall = 40;
		private double breakeven = 50;
			

		
		
		//Analog RSI
		private double lowLineRSIAnalog = 0;
		private double highLineRSIAnalog = 100000;
		
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
		
		private OnBarData onBarData;
		
		private double highZigZagPrice = 0;
		private double lowZigZagPrice = 0;
		
		private int _saveZigZagDaysOnHistory = 60;
		
		private int stopLossLevel = 0;
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
			ExitOnClose = false;
			
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
			
			
			double procentOfValot = middleValot * ProcentOfValotForNextLevel / 100;
			
			dailyData = new DailyData(Time[0], procentOfValot, historyData);
			zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel);
			onBarData = new OnBarData(0);
		}

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		

        protected override void OnBarUpdate()
        {	
			if(CurrentBars[0] < 5 && CurrentBars[1] < 5 && CurrentBars[2] < 5){
			//	return;
			}

			if(BarsInProgress == 0)
			{
				Print("=================================");
				Print("------OnBarUpdateMain Start-------");
				smaLine = SMA(SMAPeriod)[0];
				Print("Day middleValot " + middleValot);
				Print("SMA Line -> " + smaLine);
				
				double procentOfMiddleValot = (middleValot / 100) * ProcentFromMiddleValot;
				
				lowLineRSIAnalog = smaLine - procentOfMiddleValot;
				highLineRSIAnalog = smaLine + procentOfMiddleValot;
				
				Print("lowLineRSIAnalog " + lowLineRSIAnalog);
				Print("highLineRSIAnalog " + highLineRSIAnalog);
				Print(Time[0].ToString());
				
				ZigZagUpdateOnBar();
				
				if(lowBarPeriod == 0 || highBarPeriod == 0) { return; } 
				
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
				periodOfCalculate = endBar - startBar;
					
				if(startBar < endBar && isChangePeriod)
				{
					sellLevelPrice = GetLowOrHighPriceOfBar(true, startBar, endBar);
					buyLevelPrice = GetLowOrHighPriceOfBar(false, startBar, endBar);

					zigZagDiapasone.BuyZigZagDiapasone.Level = buyLevelPrice;
					zigZagDiapasone.SellZigZagDiapasone.Level = sellLevelPrice;
					zigZagDiapasone.BuyZigZagDiapasone.ZigZagApex = lowZigZagPrice;
					zigZagDiapasone.SellZigZagDiapasone.ZigZagApex = highZigZagPrice;
					
					historyData.LastBuyLevel = buyLevelPrice;
					historyData.LastSellLevel = sellLevelPrice;
		
					Print("OnBarUpdateMain -> " + zigZagDiapasone.ToString(OrderAction.Buy));
					Print("OnBarUpdateMain -> " + zigZagDiapasone.ToString(OrderAction.Sell));
					
					
					Print("dailyData Valot before " + dailyData.Valot);
					Print("dailyData count before " + dailyData.BuyZigZagDiapasoneList.Count);					
					foreach(IZigZagDiapasone zigzag in dailyData.BuyZigZagDiapasoneList){
						Print(zigzag.ToString());
					}
					
					Print("Current buy level is " + zigZagDiapasone.BuyZigZagDiapasone.Level);
					dailyData.AddZigZagDiapasone(zigZagDiapasone);
					
					Print("dailyData count after " + dailyData.ZigZagDiapasoneList.Count);
					foreach(IZigZagDiapasone zigzag in dailyData.BuyZigZagDiapasoneList){
						Print(zigzag.ToString());
					}
					
					
					zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel);
					
					isChangePeriod = false;
				}
					
				Print("Count PriceVolume: " + onBarData.PriceVolumeOnBar.VolumePriceOnBar.Count);
				Print("Start foreach PriceVolume");
				foreach(KeyValuePair<double, double> a in onBarData.PriceVolumeOnBar.VolumePriceOnBar)
				{
					string priceVolumeText = string.Format("Price: {0}, CountOrders: {1}", a.Key, a.Value);
					//Print(priceVolumeText);
				}
				Print("End foreach PriceVolume");
				
				dailyData.OnBarDataList.Add(onBarData);
				averageVolume = dailyData.GetAveragePriceVolume(AverageVolumeForLastBars);
				Print(string.Format("Average volume for last {0} is: {1}", AverageVolumeForLastBars, averageVolume));
				onBarData = new OnBarData(CurrentBar);
				
				Print("------OnBarUpdateMain End-------");
				Print("================================");
			}
			
			if(BarsInProgress == 1)
			{	
				double openPrice = Opens[1][0];
			
				_previousPrice = _price;
				_price = openPrice;

				StopLossAndTakeProfit(_price);
				
				double volume = Volumes[1][0];
				onBarData.PriceVolumeOnBar.AddPriceVolume(_price, volume);
				double currentVolume = onBarData.PriceVolumeOnBar.VolumePriceOnBar[_price];
				
				
				BuyOrSell(_price, _previousPrice, currentVolume, averageVolume);
			}
			
			if(BarsInProgress == 2)
			{		
				Print("----------------------------");
				Print("------Daily Start-------");
				historyData.AddDaylyZigZag(dailyData);
				
				for(int i = 0; i < dailyData.ZigZagDiapasoneList.Count; i++)
				{
					Print("Level by Day ->   Index: " + i + " Sell Level: " + dailyData.SellZigZagDiapasoneList[i].Level + " Buy Level: " + dailyData.BuyZigZagDiapasoneList[i].Level);
				}
				
				double procentOfValot = middleValot * ProcentOfValotForNextLevel / 100;
				dailyData = new DailyData(Time[0], procentOfValot, historyData);
				
				middleValot = 0;
				
				for(int i = 0; i < DayOfSMAValot; i++)
				{
					middleValot = middleValot + Highs[2][i] - Lows[2][i];
				}
				middleValot = middleValot / DayOfSMAValot;
				Print("Day middleValot " + middleValot);	
				Print("------Daily end-------");
				Print("----------------------------");
			}	
	
        }
		
		private int GetProcentValue(double value, int procent)
		{
			return Convert.ToInt32((value/100) * procent);
		}
		
		private double GetLowOrHighPriceOfBar(bool isFoundLowPriceOnBar, int startBar, int endBar)
		{
			double low = Low[CurrentBar - startBar];
			double high = High[CurrentBar - startBar];
			int start = CurrentBar - startBar;
			int end = CurrentBar - endBar;
			
			for(;start > end; start--)
			{
				if(isFoundLowPriceOnBar)
				{
					if(low < Low[start])
					{
						low = Low[start];
					}
				}
				else
				{
					if(high > High[start])
					{
						high = High[start];
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
							Print("lowBarPeriod " + (CurrentBar - lowBarPeriod + 1));
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
							Print("highBarPeriod " + (CurrentBar - highBarPeriod + 1));
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
			Print("test");
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
			
		private void StopLossAndTakeProfit(double price){
			double formula = ProfitTargetLarge * TickSize;
		
			if(isSellOrder)
			{
				if(price < (startOrderPrice - (formula * Breakeven / 100)) && stopLossLevel == 0)
				{
					stopLossLevel = 1;
					SetStopLoss("SellOrder1", CalculationMode.Price, startOrderPrice, false);
				}
			}
			else if(isBuyOrder)
			{
				if(price > (startOrderPrice + (formula * Breakeven / 100)) && stopLossLevel == 0)
				{
					stopLossLevel = 1;
					SetStopLoss("BuyOrder1", CalculationMode.Price, startOrderPrice, false);
				}
			}
		}
		
		
		private void BuyOrSell(double price, double previousPrice, double currentVolume, double averageVolume)
		{
			if (Position.MarketPosition == MarketPosition.Flat)
			{				
				isBuyOrder = false;
				isSellOrder = false;
				stopLossLevel = 0;	
				
				foreach(DailyData dailyData in historyData.DailyDataList)
				{
					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList)
					{						
						if(IsPriceInOrderPeriod(price, zigZag.SellZigZagDiapasone.LevelWithPostTicks, zigZag.SellZigZagDiapasone.ZigZagApex))
						{
							if(currentVolume > averageVolume)
							{
								if(price > highLineRSIAnalog)
								{
									EnterShort("SellOrder1");
									EnterShort("SellOrder2");
									Print("OrderAction.Sell");
									
									startOrderPrice = price;
									isSellOrder = true;
									
									SetProfitTarget("SellOrder1", CalculationMode.Ticks, ProfitTargetLarge);
									SetProfitTarget("SellOrder2", CalculationMode.Ticks, ProfitTargetSmall);
									SetStopLoss("SellOrder1",CalculationMode.Ticks, StopLoss, false);
									SetStopLoss("SellOrder2",CalculationMode.Ticks, StopLoss, false);
										
									//по вершинам зигзага
									//SetStopLoss(CalculationMode.Price, dayZigZagLevelList.DailyZigZagList[i].HighZigZag + 10);
										
									Print("Price" + price);
									Print("Previous Price" + previousPrice);
									Print(zigZag.ToString(OrderAction.Sell));
									zigZag.SellZigZagDiapasone.DeleteZigZagDiapasone();
									Print(zigZag.ToString(OrderAction.Sell));
									break;
								}
							}
						}
						else if(IsPriceInOrderPeriod(price, zigZag.BuyZigZagDiapasone.LevelWithPostTicks, zigZag.BuyZigZagDiapasone.ZigZagApex))
						{
							if(currentVolume > averageVolume)
							{
								if(price < lowLineRSIAnalog)
								{
									EnterLong("BuyOrder1");
									EnterLong("BuyOrder2");
									Print("OrderAction.Buy");
									
									startOrderPrice = price;
									isBuyOrder = true;
									
									SetProfitTarget("BuyOrder1", CalculationMode.Ticks, ProfitTargetLarge);
									SetProfitTarget("BuyOrder2", CalculationMode.Ticks, ProfitTargetSmall);
									SetStopLoss("BuyOrder1",CalculationMode.Ticks, StopLoss, false);
									SetStopLoss("BuyOrder2",CalculationMode.Ticks, StopLoss, false);
										
									//по вершинам зигзага
									//SetStopLoss(CalculationMode.Price, dayZigZagLevelList.DailyZigZagList[i].LowZigZag - 10);
									
									Print("Price" + price);
									Print("Previous Price" + previousPrice);
									Print(zigZag.ToString(OrderAction.Buy));
									zigZag.BuyZigZagDiapasone.DeleteZigZagDiapasone();
									Print(zigZag.ToString(OrderAction.Buy));
									break;
								}
							}
						}
					}
					
					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList){
						
						if(IsPriceInOrderPeriod(price, zigZag.BuyZigZagDiapasone.LevelWithPostTicks, zigZag.BuyZigZagDiapasone.ZigZagApex))
						{
							Print("Delete buy level: " + zigZag.BuyZigZagDiapasone.Level);
							zigZag.BuyZigZagDiapasone.DeleteZigZagDiapasone();
						}
						else if(IsPriceInOrderPeriod(price, zigZag.SellZigZagDiapasone.LevelWithPostTicks, zigZag.SellZigZagDiapasone.ZigZagApex))
						{
							Print("Delete sell level: " + zigZag.SellZigZagDiapasone.Level);
							zigZag.SellZigZagDiapasone.DeleteZigZagDiapasone();
						}
					}	
				}
			}	
		}
		


		private bool IsPriceInOrderPeriod(double price, double firstPeriod, double secondPeriod){
			
			double minValue = 0;
			double maxValue = 0;
			
			if(firstPeriod < secondPeriod)
			{
				minValue = firstPeriod;
				maxValue = secondPeriod;
			}
			else
			{
				minValue = secondPeriod;
				maxValue = firstPeriod;
			}
			
			if(price >= minValue && price <= maxValue)
			{
				return true;
			}
			else
			{
				return false;
			}
		}
		
		
		#region Data Objects and Lists
		
		public class HistoryData
		{
			private readonly int _daysOfHistory;

			public List<DailyData> DailyDataList {get; private set;}

		
			public double LastBuyLevel {get;set;}
			public double LastSellLevel{get;set;}

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
				if (DailyDataList.Count > _daysOfHistory)
				{
					DailyDataList.RemoveAt(0);
				}
			}
		}
		
		public enum OrderAction{
			Buy,
			Sell
		}
		
		public class DailyData
		{
			public List<ZigZagDiapasone> ZigZagDiapasoneList{get; private set;}
			public List<IZigZagDiapasone> BuyZigZagDiapasoneList{get; private set;}
			public List<IZigZagDiapasone> SellZigZagDiapasoneList{get; private set;}
			
			public List<OnBarData> OnBarDataList{get; private set;}
			
			public DateTime DailyDateTime {get; private set;}
			public double Valot {get; private set;}
			
			private HistoryData _historyData;
			
			public DailyData(DateTime dateTime, double valot, HistoryData historyData)
			{
				DailyDateTime = dateTime;
				Valot = valot;
				ZigZagDiapasoneList = new List<ZigZagDiapasone>();
				
				BuyZigZagDiapasoneList = buyZigZagDiapasoneList;
				SellZigZagDiapasoneList = sellZigZagDiapasoneList;
				
				_historyData = historyData;
				
				OnBarDataList = new List<OnBarData>();
			}
			
			public void AddZigZagDiapasone(ZigZagDiapasone zigZagDiapasone)
			{	
				ZigZagDiapasoneList.Add(zigZagDiapasone);
				
				CheckForDeletedNextLevels(zigZagDiapasone);
			}
			
			private void CheckForDeletedNextLevels(ZigZagDiapasone zigZagDiapasone){
				
				buyZigZagDiapasoneList.Add(zigZagDiapasone.BuyZigZagDiapasone);
				buyZigZagDiapasoneList = (from element in BuyZigZagDiapasoneList
										  orderby element.Level descending
										  select element).ToList();
				DeleteNextLevelInValotArea(zigZagDiapasone.BuyZigZagDiapasone.Level, buyZigZagDiapasoneList, Valot, OrderAction.Buy); 
				
				sellZigZagDiapasoneList.Add(zigZagDiapasone.SellZigZagDiapasone);
				sellZigZagDiapasoneList = (from element in SellZigZagDiapasoneList
										   orderby element.Level ascending
										   select element).ToList();
				DeleteNextLevelInValotArea(zigZagDiapasone.SellZigZagDiapasone.Level, sellZigZagDiapasoneList, Valot, OrderAction.Sell); 
				
				BuyZigZagDiapasoneList = buyZigZagDiapasoneList;
				SellZigZagDiapasoneList = sellZigZagDiapasoneList;
			}
			
			
			private void DeleteNextLevelInValotArea(double currentLevel, List<IZigZagDiapasone> zigZagDiapasoneList, double valot, OrderAction orderAction)
			{
				for(int i = 1; i < zigZagDiapasoneList.Count; i++)
				{
					if(orderAction == OrderAction.Buy)
					{
						if(zigZagDiapasoneList[i - 1].Level <= currentLevel && zigZagDiapasoneList[i].Level > zigZagDiapasoneList[i - 1].Level - valot)
						{
							zigZagDiapasoneList[i - 1].DeleteZigZagDiapasone();
						}
					}
					else if(orderAction == OrderAction.Sell)
					{
						if(zigZagDiapasoneList[i - 1].Level >= currentLevel && zigZagDiapasoneList[i].Level < zigZagDiapasoneList[i - 1].Level + valot)
						{
							zigZagDiapasoneList[i - 1].DeleteZigZagDiapasone();
						}
					}
				}
			}
			
			public double GetAveragePriceVolume(int lastBars)
			{
				double volumeAverage = 0;
				if(OnBarDataList.Count > lastBars)
				{
					for(int i = OnBarDataList.Count - 1; i > OnBarDataList.Count - 1 - lastBars; i--)
					{
						volumeAverage = volumeAverage + OnBarDataList[i].GetMostLargePriceOfVolume();
					}
					volumeAverage = volumeAverage / lastBars;
				}
				return volumeAverage;
			}
			
			public override string ToString()
			{
				return string.Format("DailyDateTime: {0}, OnBarDataList count: {1}, ZigZagDiapasoneList count: {2}", DailyDateTime, OnBarDataList.Count, ZigZagDiapasoneList.Count);
			}
		}
		
		public interface IZigZagDiapasone{
			bool IsDeleted{get;set;}
			double Level{get;set;}
			double ZigZagApex{get;set;}
			double LevelWithPostTicks{get;set;}
			void DeleteZigZagDiapasone();
		}
		
		public class BuyZigZagDiapasone : IZigZagDiapasone{
		
			public bool IsDeleted {get; set;}
			
			private double _ticks = 0;
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
			
			public BuyZigZagDiapasone(double ticks){
				_ticks = ticks;
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
				return string.Format("BuyLevel: {0}, BuyZigZag: {1}, BuyLevelWithPostTicks: {2}", Level, ZigZagApex, LevelWithPostTicks);
			}
			
		}
		
		public class SellZigZagDiapasone : IZigZagDiapasone{
		
			public bool IsDeleted {get; set;}
			
			private double _ticks = 0;
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
			
			
			public SellZigZagDiapasone(double ticks){
				_ticks = ticks;
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
				return string.Format("SellLevel: {0}, SellZigZag: {1}, SellLevelWithPostTicks: {2}", Level, ZigZagApex, LevelWithPostTicks);
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
			
			public SellZigZagDiapasone SellZigZagDiapasone{get; set;}
			public BuyZigZagDiapasone BuyZigZagDiapasone{get; set;}

			public ZigZagDiapasone(double ticks)
			{
				SellZigZagDiapasone = new SellZigZagDiapasone(ticks);
				BuyZigZagDiapasone = new BuyZigZagDiapasone(ticks);
				Ticks = ticks;
				IsBuyDeleted = false;
				IsSellDeleted = false;
			}
			

				
			
			public string ToString(OrderAction orderAction)
			{
				if(orderAction == OrderAction.Sell)
				{
					return string.Format("SellLevel: {0}, SellZigZag: {1}, SellLevelWithPostTicks: {2}" 
						, SellZigZagDiapasone.Level, SellZigZagDiapasone.ZigZagApex, SellZigZagDiapasone.LevelWithPostTicks);
				}
				else
				{
					return string.Format("BuyLevel: {0}, BuyZigZag: {1}, BuyLevelWithPostTicks: {2}"
						, BuyZigZagDiapasone.Level, BuyZigZagDiapasone.ZigZagApex, BuyZigZagDiapasone.LevelWithPostTicks);
				}
			}
			
			public override string ToString()
			{
				return string.Format("SellLevel: {0}, SellZigZag: {1}, SellLevelWithPostTicks: {2}.   "
								     + "BuyLevel: {3}, BuyZigZag: {4}, BuyLevelWithPostTicks: {5}" 
					, SellZigZagDiapasone.Level, SellZigZagDiapasone.ZigZagApex, SellZigZagDiapasone.LevelWithPostTicks
					, BuyZigZagDiapasone.Level, BuyZigZagDiapasone.ZigZagApex, BuyZigZagDiapasone.LevelWithPostTicks);
			}
		}
		
		
		
		
		public class OnBarData
		{
			public PriceVolume PriceVolumeOnBar{get; private set;}
			public int BarIndex{get;private set;}

			public OnBarData(int index)
			{
				BarIndex = index;
				PriceVolumeOnBar = new PriceVolume();
				
			}
			
			public double GetMostLargePriceOfVolume()
			{
				double price = 0;
				double volume = 0;
				foreach(KeyValuePair<double, double> priceVolumeOnBar in PriceVolumeOnBar.VolumePriceOnBar)
				{
					if(priceVolumeOnBar.Value > volume)
					{
						volume = priceVolumeOnBar.Value;
						price = priceVolumeOnBar.Key;
					}
				}
				return price;
			}
			
			public override string ToString()
			{
				return string.Format("Bar index: {0}, GetMostLargePriceOfVolume: {1}, PriceVolumeOnBarCount: {2}", BarIndex, GetMostLargePriceOfVolume(), PriceVolumeOnBar);
			}
		}
		
		
		public class PriceVolume
		{
			public Dictionary<double, double> VolumePriceOnBar {get; private set;}	
			private double _price = 0;
			
			public PriceVolume()
			{
				VolumePriceOnBar = new Dictionary<double, double>();
			}
			
			public void AddPriceVolume(double price, double volume)
			{
				if(VolumePriceOnBar.ContainsKey(price))
				{
					double count = VolumePriceOnBar[price];
					VolumePriceOnBar.Remove(price);
					VolumePriceOnBar.Add(price, count + volume);
				}
				else
				{
					VolumePriceOnBar.Add(price, volume);
				}
				_price = price;
			}
			
			public override string ToString()
			{
				string text = "";
				foreach(KeyValuePair<double, double> priceVolume in VolumePriceOnBar)
				{
					text = text + string.Format("Price: {0}, volume: {1}", priceVolume.Key, priceVolume.Value) + "| ";
				}
				return text;
			}
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
		
		[Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("ZigZag")]
        public double AddTicksForOrderLevel
        {
            get { return addTicksForOrderLevel; }
            set { addTicksForOrderLevel = value; }
        }
		
		[Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("Volume")]
        public int AverageVolumeForLastBars
        {
            get { return averageVolumeForLastBars; }
            set { averageVolumeForLastBars = value; }
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
		
		[Description("Numbers of bars used for calculations")]
		[GridCategory("Level")]
		public int ProcentOfValotForNextLevel
		{
			get { return procentOfValotForNextLevel; }
			set { procentOfValotForNextLevel = Math.Max(1, value); }
		}
			
        #endregion
    }
}
