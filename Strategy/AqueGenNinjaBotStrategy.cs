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
		
		private int _rightZigZag = 4;
		private int _leftZigZag = 4;
		private double addTicksForOrderLevel = 0.5;
		
		private double averageVolume = 0;
		private int averageVolumeForLastBars = 5;
		
		
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
		private double profitTargetLarge = 100;
		private double profitTargetSmall = 30;
		private double breakeven = 30;
			

		
		
		//Analog RSI
		private double lowLineRSIAnalog = 0;
		private double highLineRSIAnalog = 100000;
		
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
		
		private int stopLossLevel = 0;
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {

            CalculateOnBarClose = true;

			//zigzag
			zigZagHighSeries	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagHighZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagLowSeries		= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagLowZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 

			Add(PeriodType.Tick, 1);
			Add(PeriodType.Day, 1);
			
			historyData = new HistoryData(SaveZigZagDaysOnHistory);
			dailyData = new DailyData(Time[0]);
			zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel);
			onBarData = new OnBarData(0);
			
			
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		

        protected override void OnBarUpdate()
        {	

			if(BarsInProgress == 0)
			{
				Print("==============");
				smaLine = SMA(SMAPeriod)[0];
				Print("Day middleValot " + middleValot);
				Print("SMA Line -> " + smaLine);
				
				double procentOfMiddleValot = (middleValot / 100) * ProcentFromMiddleValot;
				
				lowLineRSIAnalog = smaLine - procentOfMiddleValot;
				highLineRSIAnalog = smaLine + procentOfMiddleValot;
				
				Print("lowLineRSIAnalog " + lowLineRSIAnalog);
				Print("highLineRSIAnalog " + highLineRSIAnalog);
				Print(Time[0].ToString());
				
				
				OnBarUpdateMain();
				Print(Time[0].ToString());
				
				Print("Count PriceVolume: " + onBarData.PriceVolumeOnBar.VolumePriceOnBar.Count);
				foreach(KeyValuePair<double, double> a in onBarData.PriceVolumeOnBar.VolumePriceOnBar)
				{
					string priceVolumeText = string.Format("Price: {0}, CountOrders: {1}", a.Key, a.Value);
					Print(priceVolumeText);
				}
				
				dailyData.OnBarDataList.Add(onBarData);
				averageVolume = dailyData.GetAveragePriceVolume(AverageVolumeForLastBars);
				Print(string.Format("Average volume for last {1} is: {0}", AverageVolumeForLastBars, averageVolume));
				
				onBarData = new OnBarData(CurrentBar);
				
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
				historyData.AddDaylyZigZag(dailyData);
				
				for(int i = 0; i < dailyData.ZigZagDiapasoneList.Count; i++)
				{
					Print("Level by Day ->   Index: " + i + " Sell Level: " + dailyData.ZigZagDiapasoneList[i].SellLevel + " Buy Level: " + dailyData.ZigZagDiapasoneList[i].BuyLevel);
				}
				
				dailyData = new DailyData(Time[0]);
				
				middleValot = 0;
				
				for(int i = 0; i < DayOfSMAValot; i++)
				{
					middleValot = middleValot + Highs[2][i] - Lows[2][i];
				}
				middleValot = middleValot / DayOfSMAValot;
				Print("Day middleValot " + middleValot);	
			}	
        }
		
		private void OnBarUpdateMain()
		{
			Print("----------------------------");
			Print("------OnBarUpdateMain-------");
			
			ZigZagUpdateOnBar();
			
			if(lowBarPeriod == 0 || highBarPeriod == 0)
				return;
			
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

				zigZagDiapasone.BuyLevel = buyLevelPrice;
				zigZagDiapasone.SellLevel = sellLevelPrice;
				zigZagDiapasone.BuyZigZag = lowZigZagPrice;
				zigZagDiapasone.SellZigZag = highZigZagPrice;
	
				Print("OnBarUpdateMain -> " + zigZagDiapasone.ToString(OrderAction.Buy));
				Print("OnBarUpdateMain -> " + zigZagDiapasone.ToString(OrderAction.Sell));
				
				dailyData.ZigZagDiapasoneList.Add(zigZagDiapasone);
				
				zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel);
				
				isChangePeriod = false;
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
						//Print("add High");
						//Print("lowBarPeriod -> " + lowBarPeriod);
					}
					if(!addHigh && updateHigh){
						highBar = CurrentBar;
						//Print("update High");
						//Print("highBar -> " + highBar);
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
						//Print("add Low");
						//Print("highBarPeriod ->" + highBarPeriod);
					}
					if(!addLow && updateLow){
						lowBar = CurrentBar;
						//Print("update Low");
						//Print("lowBar -> " + lowBar);
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
						if(IsPriceInOrderPeriod(price, zigZag.SellLevelWithPostTicks, zigZag.SellZigZag, OrderAction.Sell))
						{
							if(currentVolume > averageVolume && price > highLineRSIAnalog)
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
								zigZag.DeleteZigZagDiapasone(OrderAction.Sell);
								Print(zigZag.ToString(OrderAction.Sell));
								break;
							}
						}
						else if(IsPriceInOrderPeriod(price, zigZag.BuyLevelWithPostTicks, zigZag.BuyZigZag, OrderAction.Buy))
						{
							if(currentVolume > averageVolume && price < lowLineRSIAnalog)
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
								zigZag.DeleteZigZagDiapasone(OrderAction.Buy);
								Print(zigZag.ToString(OrderAction.Buy));
								break;
							}
						}
					}
					
					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList){
						
						if(IsPriceInOrderPeriod(price, zigZag.BuyLevelWithPostTicks, zigZag.BuyZigZag, OrderAction.Buy))
						{
							//Print(zigZag.ToString(OrderAction.Buy));
							zigZag.DeleteZigZagDiapasone(OrderAction.Buy);
							//Print(zigZag.ToString(OrderAction.Buy));
						}
						else if(IsPriceInOrderPeriod(price, zigZag.SellLevelWithPostTicks, zigZag.SellZigZag, OrderAction.Sell))
						{
							//Print(zigZag.ToString(OrderAction.Sell));
							zigZag.DeleteZigZagDiapasone(OrderAction.Sell);
							//Print(zigZag.ToString(OrderAction.Sell));
						}
					}	
					
				}
				
				
			}	
				
		}
		

		private bool IsPriceInOrderPeriod(double price, double levelWithPostTicks, double zigZagApex, OrderAction orderAction){
			if(price < levelWithPostTicks && price > zigZagApex && orderAction == OrderAction.Buy)
			{
				return true;
			}
			else if(price > levelWithPostTicks && price < zigZagApex && orderAction == OrderAction.Sell)
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
			public List<OnBarData> OnBarDataList{get; private set;}
			public DateTime DailyDateTime {get; private set;}
			
			public DailyData(DateTime dateTime)
			{
				DailyDateTime = dateTime;
				ZigZagDiapasoneList = new List<ZigZagDiapasone>();
				OnBarDataList = new List<OnBarData>();
			}
			
			public double GetAveragePriceVolume(int lastBars)
			{
				double volumeAverage = 0;
				for(int i = OnBarDataList.Count - 1; i > OnBarDataList.Count - 1 - lastBars; i--)
				{
					volumeAverage = volumeAverage + OnBarDataList[i].LargestPriceOnVolume;
				}
				volumeAverage = volumeAverage / lastBars;
				return volumeAverage;
			}
			
			public override string ToString()
			{
				return string.Format("DailyDateTime: {0}, OnBarDataList count: {1}, ZigZagDiapasoneList count: {2}", DailyDateTime, OnBarDataList.Count, ZigZagDiapasoneList.Count);
			}
		}
		
		public class ZigZagDiapasone{
			
			private double _ticks = 0;
			
			private double _sellLevel = 0;
			public double SellLevel{
				get
				{
					return _sellLevel;
				}
				set
				{
					_sellLevel = value;
					SellLevelWithPostTicks = _sellLevel - _ticks ;
				}
			}
			public double SellZigZag{get;set;}
			public double SellLevelWithPostTicks{get; private set;}
			
			
			private double _buyLevel = 0;
			public double BuyLevel{
				get
				{
					return _buyLevel;
				}
				set
				{	
					_buyLevel = value;
					BuyLevelWithPostTicks = _buyLevel + _ticks;
				}
			}
			public double BuyZigZag{get;set;}
			public double BuyLevelWithPostTicks{get; private set;}
			
				
			
			public ZigZagDiapasone(double ticks)
			{
				_ticks = ticks;
			}

			public bool DeleteZigZagDiapasone(OrderAction orderAction)
			{
				if(orderAction == OrderAction.Buy)
				{
					BuyLevel = 0;
					BuyZigZag = 0;
					BuyLevelWithPostTicks = 0;
					return true;
				}
				else if(orderAction == OrderAction.Sell)
				{
					SellLevel = 0;
					SellZigZag = 0;
					SellLevelWithPostTicks = 0;
					return true;
				}
				else
				{
					return false;
				}
			}
				
			
			public string ToString(OrderAction orderAction)
			{
				if(orderAction == OrderAction.Sell)
				{
					return string.Format("SellLevel: {0}, SellZigZag: {1}, SellLevelWithPostTicks: {2}" , SellLevel, SellZigZag, SellLevelWithPostTicks);
				}
				else
				{
					return string.Format("BuyLevel: {0}, BuyZigZag: {1}, BuyLevelWithPostTicks: {2}", BuyLevel, BuyZigZag, BuyLevelWithPostTicks);
				}
			}
			
			public override string ToString()
			{
				return string.Format("SellLevel: {0}, SellZigZag: {1}, SellLevelWithPostTicks: {2}.   "
								     + "BuyLevel: {3}, BuyZigZag: {4}, BuyLevelWithPostTicks: {5}" 
					, SellLevel, SellZigZag, SellLevelWithPostTicks, BuyLevel, BuyZigZag, BuyLevelWithPostTicks);
			}
		}
		
		
		
		
		public class OnBarData
		{
			public PriceVolume PriceVolumeOnBar{get; private set;}
			public int BarIndex{get;private set;}
			public double LargestPriceOnVolume {get; private set;}
			
			public OnBarData(int index)
			{
				BarIndex = index;
				PriceVolumeOnBar = new PriceVolume();
				LargestPriceOnVolume = GetMostLargePriceOfVolume();
			}
			
			private double GetMostLargePriceOfVolume()
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
				return string.Format("Bar index: {0}, LargestPriceOnVolume: {1}, PriceVolumeOnBarCount: {2}", BarIndex, LargestPriceOnVolume, PriceVolumeOnBar);
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
			
        #endregion
    }
}
