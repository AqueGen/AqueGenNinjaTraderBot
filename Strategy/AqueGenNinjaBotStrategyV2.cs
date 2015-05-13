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
		
		private int stopLossLevel = 0;
		
		private int indexBar = -1;
		private int orderIndexBar = -1;
		
		private StopLossOutOfApex stopLossOutOfApex;
		private bool isStopLossOutOfApex = false;
		private double previousLowBarApex = -1;
		private double previousHighBarApex = -1;
		
		private double currentStopLoss = -1;

		
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
			zigZagDiapasone = new ZigZagDiapasone(0);
			onBarData = new OnBarData(0);
			
			
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		

        protected override void OnBarUpdate()
        {	

			if(BarsInProgress == 0){
				indexBar = CurrentBars[0];
				
				if(isSellOrder || isBuyOrder)
				{
					previousLowBarApex = Lows[0][0];
					previousHighBarApex = Highs[0][0];
				}
				
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
					
				dailyData.OnBarDataList.Add(onBarData);
				onBarData = new OnBarData(CurrentBar);
				
			}
			
			
			if(BarsInProgress == 1 && RealTime == Switch.OFF)
			{	
				double openPrice = Opens[1][0];
			
				_previousPrice = _price;
				_price = openPrice;
				
				BuyOrSell(_price, _previousPrice);
			}
			
			if(BarsInProgress == 2){
				
				historyData.AddDaylyZigZag(dailyData);
				
				for(int i = 0; i < dailyData.ZigZagDiapasoneList.Count; i++){
					Print("Level by Day ->   Index: " + i + " Sell Level: " + dailyData.ZigZagDiapasoneList[i].SellLevel + " Buy Level: " + dailyData.ZigZagDiapasoneList[i].BuyLevel);
				}
				
				
				dailyData = new DailyData(Time[0]);
				
				middleValot = 0;
				
				for(int i = 0; i < DayOfSMAValot; i++){
					middleValot = middleValot + Highs[2][i] - Lows[2][i];
				}
				middleValot = middleValot / DayOfSMAValot;
				Print("Day middleValot " + middleValot);	
			}	
        }
		
		private void OnBarUpdateMain(){
			
			Print("-------------------");
			
			ZigZagUpdateOnBar();
			
			if(lowBarPeriod == 0 || highBarPeriod == 0)
				return;
			
			if(highBarPeriod > lowBarPeriod){
				isTrendOnPeriodDown = false;
				startBar = lowBarPeriod - 1 - LeftZigZag;
				endBar = highBarPeriod - 1 + RightZigZag;
			}
			else{
				isTrendOnPeriodDown = true;
				startBar = highBarPeriod - 1 - LeftZigZag;
				endBar = lowBarPeriod - 1 + RightZigZag;
			}
			periodOfCalculate = endBar - startBar;
				
			if(startBar < endBar && isChangePeriod){
				sellLevelPrice = GetLowOrHighPriceOfBar(true, startBar, endBar);
				buyLevelPrice = GetLowOrHighPriceOfBar(false, startBar, endBar);

				zigZagDiapasone.BuyLevel = buyLevelPrice;
				zigZagDiapasone.SellLevel = sellLevelPrice;
				zigZagDiapasone.BuyZigZag = lowZigZagPrice;
				zigZagDiapasone.SellZigZag = highZigZagPrice;

				
				Print("zigZagDiapasone.BuyLevel " + zigZagDiapasone.BuyLevel);
				Print("zigZagDiapasone.SellLevel " + zigZagDiapasone.SellLevel);
				Print("zigZagDiapasone.LowZigZag " + zigZagDiapasone.BuyZigZag);
				Print("zigZagDiapasone.HighZigZag " + zigZagDiapasone.SellZigZag);
				Print("zigZagDiapasone.BuyLevelWithPostTicks " + zigZagDiapasone.BuyLevelWithPostTicks);
				Print("zigZagDiapasone.SellLevelWithPostTicks " + zigZagDiapasone.SellLevelWithPostTicks);
					
				dailyData.ZigZagDiapasoneList.Add(zigZagDiapasone);
				
				zigZagDiapasone = new ZigZagDiapasone(AddTicksForOrderLevel * TickSize);
				
				isChangePeriod = false;
			}
		}
		
		
		private int GetProcentValue(double value, int procent){
			return Convert.ToInt32((value/100) * procent);
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
		
		
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Last) {
				double openPrice = e.Price;
				_previousPrice = _price;
				_price = openPrice;
				
				BuyOrSell(_price, _previousPrice);
				
			}
		}
			
		
		
		private void BuyOrSell(double price, double lastPrice){

			if (Position.MarketPosition == MarketPosition.Flat)
			{
				
				isBuyOrder = false;
				isSellOrder = false;
				isStopLossOutOfApex = false;
				stopLossLevel = 0;
				
				
				foreach(DailyData dailyData in historyData.DailyDataList){

					foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList){

						if(price > zigZag.SellLevelWithPostTicks && price < zigZag.SellZigZag && price > highLineRSIAnalog){
							EnterShort("SellOrder1");
							EnterShort("SellOrder2");
							Print("OrderAction.SELL");
							
							startOrderPrice = price;
							orderIndexBar = indexBar;
							isSellOrder = true;
							
							SetProfitTarget("SellOrder1", CalculationMode.Ticks, ProfitTargetLarge);
							SetProfitTarget("SellOrder2", CalculationMode.Ticks, ProfitTargetSmall);
							currentStopLoss = StopLoss;
							SetStopLoss("SellOrder1",CalculationMode.Ticks, StopLoss, false);
							SetStopLoss("SellOrder2",CalculationMode.Ticks, StopLoss, false);
									
							Print("price" + price);
							Print("lastPrice" + lastPrice);
							zigZag.DeleteZigZagDiapasone(OrderAction.SELL);
							break;
						}
						else
						if(price < zigZag.BuyLevelWithPostTicks && price > zigZag.BuyZigZag && price < lowLineRSIAnalog){
							EnterLong("BuyOrder1");
							EnterLong("BuyOrder2");
							Print("OrderAction.BUY");
							
							startOrderPrice = price;
							orderIndexBar = indexBar;
							isBuyOrder = true;
							
							SetProfitTarget("BuyOrder1", CalculationMode.Ticks, ProfitTargetLarge);
							SetProfitTarget("BuyOrder2", CalculationMode.Ticks, ProfitTargetSmall);
							currentStopLoss = StopLoss;
							SetStopLoss("BuyOrder1",CalculationMode.Ticks, StopLoss, false);
							SetStopLoss("BuyOrder2",CalculationMode.Ticks, StopLoss, false);
								
							Print("price" + price);
							Print("lastPrice" + lastPrice);
							zigZag.DeleteZigZagDiapasone(OrderAction.BUY);
							break;
						}
					}
				}
			}	
			else 
			{
				if(PriceAwaySwitch == Switch.ON && orderIndexBar < indexBar && !isStopLossOutOfApex)
				{
					if((startOrderPrice > price + PriceAway * TickSize) && isSellOrder)
					{
						isStopLossOutOfApex = true;
						stopLossOutOfApex = new StopLossOutOfApex(previousHighBarApex, PriceAway * TickSize, OrderAction.SELL);
						
						Print(stopLossOutOfApex.ToString());
						
						SetStopLoss("SellOrder1", CalculationMode.Price, stopLossOutOfApex.StopLossPrice + (2 * TickSize), false);
						SetStopLoss("SellOrder2", CalculationMode.Price, stopLossOutOfApex.StopLossPrice + (2 * TickSize), false);
						currentStopLoss = stopLossOutOfApex.StopLossPrice + (2 * TickSize);
					}	
					else if((startOrderPrice < price - PriceAway * TickSize) && isBuyOrder)
					{
						isStopLossOutOfApex = true;
						stopLossOutOfApex = new StopLossOutOfApex(previousLowBarApex, PriceAway * TickSize, OrderAction.BUY);
						
						Print(stopLossOutOfApex.ToString());
						
						SetStopLoss("BuyOrder1", CalculationMode.Price, stopLossOutOfApex.StopLossPrice - (2 * TickSize), false);
						SetStopLoss("BuyOrder2", CalculationMode.Price, stopLossOutOfApex.StopLossPrice - (2 * TickSize), false);
						currentStopLoss = stopLossOutOfApex.StopLossPrice - (2 * TickSize);
					}
					
				}
				
				if(BreakevenSwitch == Switch.ON)
				{
					double formula = ProfitTargetLarge * TickSize;
				
					if(isSellOrder)
					{
						if(price < (startOrderPrice - (formula * Breakeven / 100)))
						{
							isStopLossOutOfApex = true;
							SetStopLoss("SellOrder1", CalculationMode.Price, startOrderPrice, false);
							SetStopLoss("SellOrder2", CalculationMode.Price, startOrderPrice, false);
							currentStopLoss = startOrderPrice;
						}
					}
					else if(isBuyOrder)
					{
						if(price > (startOrderPrice + (formula * Breakeven / 100)))
						{
							isStopLossOutOfApex = true;
							SetStopLoss("BuyOrder1", CalculationMode.Price, startOrderPrice, false);
							SetStopLoss("BuyOrder2", CalculationMode.Price, startOrderPrice, false);
							currentStopLoss = startOrderPrice;
						}
					}
				}
			}
			
			foreach(DailyData dailyData in historyData.DailyDataList){
				foreach(ZigZagDiapasone zigZag in dailyData.ZigZagDiapasoneList){
					
					if(price < zigZag.BuyLevelWithPostTicks && price > zigZag.BuyZigZag){
						zigZag.DeleteZigZagDiapasone(OrderAction.BUY);
					}
					else 
					if(price > zigZag.SellLevelWithPostTicks && price < zigZag.SellZigZag){
						zigZag.DeleteZigZagDiapasone(OrderAction.SELL);
					}
				}	
			}
		}
				
		private bool IsCanChangeStopLoss(double currentStopLoss, double newStopLoss, OrderAction orderAction)
		{
			return false;
		}
		
		
		#region Data Objects and Lists
		
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
			
				
			
			public ZigZagDiapasone(double ticks){
				_ticks = ticks;
			}

			public bool DeleteZigZagDiapasone(OrderAction orderAction){
				if(orderAction == OrderAction.BUY)
				{
					BuyLevel = 0;
					BuyZigZag = 0;
					BuyLevelWithPostTicks = 0;
					return true;
				}
				else
				if(orderAction == OrderAction.SELL)
				{
					SellLevel = 0;
					SellZigZag = 0;
					SellLevelWithPostTicks = 0;
					return true;
				}
				return false;	
			}
			
			public override string ToString(){
				return string.Format("SellLevel: {0}, SellZigZag: {1}, SellLevelWithPostTicks: {2}.   BuyLevel: {3}, BuyZigZag: {4}, BuyLevelWithPostTicks: {5}" 
					, SellLevel, SellZigZag, SellLevelWithPostTicks, BuyLevel, BuyZigZag, BuyLevelWithPostTicks);
			}
			
		}
		
		
		
		
		public class OnBarData{
			
			public int BarIndex{get;private set;}
			
			public OnBarData(int index){
				BarIndex = index;
			}
			
			
			public override string ToString(){
				return string.Format("Bar index: {0}", BarIndex);
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
		
		[GridCategory("PriceAway")]
		public int PriceAway
		{get; set;}
		
		[GridCategory("RealTime")]
		public Switch RealTime
		{get; set;}
		

		
		
		
		
		
		#region (c)
		/*© AqueGen (Artem Frolov) 
		Emails: aquegen@yandex.ru, artem.frolov.aquegen@gmail.com */
		#endregion	
		
        #endregion
    }
}
