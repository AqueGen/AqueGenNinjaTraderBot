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
		

		
		private double _price = 0;
		private double _lastPrice = 0;
		
		private double startOrderPrice = 0;
		private bool isBuyOrder = false;
		private bool isSellOrder = false;
		
		
      	private int barIndex = 0;
      	private int singleBarIndex = 0;
      
		
		//zigzag
		private double			currentZigZagHigh	= 0;
		private double			currentZigZagLow	= 0;
		private DeviationType	deviationType		= DeviationType.Percent;
		private double			deviationValue		= 0.55;
		private DataSeries		zigZagHighZigZags; 
		private DataSeries		zigZagLowZigZags; 
		private DataSeries		zigZagHighSeries; 
		private DataSeries		zigZagLowSeries; 
		private int				lastSwingIdx		= -1;
		private double			lastSwingPrice		= 0.0;
		private int				trendDir			= 0; // 1 = trend up, -1 = trend down, init = 0
		private bool			useHighLow			= false;
		
		private int _rightZigZag = 2;
		private int _leftZigZag = 2;
		
		private int startBar = 0;
		private int endBar = 0;
		
		private bool isTrendOnPeriodDown;
		private double sellLevelPrice = 0;
		private double buyLevelPrice = 0;
		
		private int highBar = 0;
		private int lowBar = 0;
		
		private int highBarPeriod = 0;
		private int lowBarPeriod = 0;
		private int lastHighBarPeriod = 0;
		private int lastLowBarPeriod = 0;
		
		
		
		private bool isDiapasoned = false;
		
		private int periodOfCalculate = 0;

		
		//Analog RSI
		
		private bool isCanBuyRSI = false;
		private bool isCanSellRSI = false;
		

		private bool isChangePeriod = false;
		
		//Diapason
		private int firstDiapason = 40;
		private int thirtDiapason = 40;
		
		private double firstDiapasonPriceDifferent = 100;
		private double thirtDiapasonPriceDifferent = 100;
		
		
		//Orders
		private double stopLoss = 10;
		private double profitTarget = 50;
		
		
		
		private int startTimeTrading = 91500;
		private int endTimeTrading = 204500;
		
		
		
		//Analog RSI
		private double lowLineRSIAnalog = 0;
		private double highLineRSIAnalog = 100000;
		
		//SMA
		private int smaPeriod = 100;
		private double  smaLine = 0;
		private double middleValot = 0;
		private int dayOfSMAValot = 5;
		private int procentFromMiddleValot = 50;
		
		
		//ZigZag history
		private ZigZagHistory zigZagHistory;
		private DailyZigZag dailyZigZag;
		
		private int _saveZigZagDaysOnHistory = 60;
		
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

			//DisplayInDataBox	= false;
            //Overlay				= true;
			//PaintPriceMarkers	= false;
			
			
			
			
			SetStopLoss(CalculationMode.Ticks, StopLoss);
			SetProfitTarget(CalculationMode.Ticks, ProfitTarget);
			
			//Add(PeriodType.Minute, MainMinuteTimeFrame);
			Add(PeriodType.Tick, 1);
			Add(PeriodType.Day, 1);
			
			zigZagHistory = new ZigZagHistory(SaveZigZagDaysOnHistory);
			dailyZigZag = new DailyZigZag(Time[0]);
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		
		private bool IsZigZagChangedPosition(int firstBar, int secondBar){
			return firstBar != secondBar;
		}
		
		
        protected override void OnBarUpdate()
        {	
			//Print("CurrentBars[2] " + CurrentBars[2]);
			
			//if(CurrentBar < SMAPeriod/* || CurrentBars[2] < 5*/)
				//return;
			
			if(BarsInProgress == 0){

				
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
			}
			
			 if(BarsInProgress == 1){	
				double openPrice = Opens[1][0];
			
				_lastPrice = Price;
				Price = openPrice;

				if(isSellOrder){
					if(Price > (startOrderPrice + (ProfitTarget * TickSize * 0.3))){
						SetStopLoss(CalculationMode.Price, startOrderPrice);
					}
				}
				else if(isBuyOrder){
					if(Price < (startOrderPrice - (ProfitTarget * TickSize * 0.3))){
						SetStopLoss(CalculationMode.Price, startOrderPrice);
					}
				}
				
				
				BuyOrSell(Price, _lastPrice);
				
			}
			
			if(BarsInProgress == 2){
				
				zigZagHistory.AddDaylyZigZag(dailyZigZag);
				
				int count = 0;
				if(dailyZigZag.HighLevelList.Count > dailyZigZag.LowLevelList.Count)
					count = dailyZigZag.LowLevelList.Count;
				else
					count = dailyZigZag.HighLevelList.Count;
				
				for(int i = 0; i < count; i++){
					Print("Level by Day ->   Index: " + i + " Sell Level: " + dailyZigZag.HighLevelList[i] + " Buy Level: " + dailyZigZag.LowLevelList[i]);
				}
				
				
				dailyZigZag = new DailyZigZag(Time[0]);
				
				middleValot = 0;
				
				for(int i = 0; i < DayOfSMAValot; i++){
					middleValot = middleValot + Highs[2][i] - Lows[2][i];
				}
				middleValot = middleValot / DayOfSMAValot;
				Print("Day middleValot " + middleValot);	
			}	
        }
		
		private void OnBarUpdateMain(){


			barIndex++;
			Print("-------------------");
			
			ZigZagUpdateOnBar();
			
			if(lowBarPeriod == 0 || highBarPeriod == 0)
				return;
			
		//	Print("lowBar -> " + lowBar);
			//Print("highBar -> " + highBar);
		
			Print("Now Bar is -> " + CurrentBar);
			
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
			
			
			Print("StartPeriod -> " + startBar);
			Print("EndPeriod -> " + endBar);
			
			
			if(startBar < endBar && isChangePeriod){
				sellLevelPrice = GetLowOrHighPriceOfBar(true, startBar, endBar);
				buyLevelPrice = GetLowOrHighPriceOfBar(false, startBar, endBar);

				dailyZigZag.AddHigh(sellLevelPrice);
				dailyZigZag.AddLow(buyLevelPrice);
				
				isChangePeriod = false;
			}
				
			Print("sellLevelPrice -> " + sellLevelPrice);
			Print("buyLevelPrice -> " + buyLevelPrice);
		}
		
		
		private int GetProcentValue(double value, int procent){
			return Convert.ToInt32((value/100) * procent);
		}
		
		private double GetLowOrHighPriceOfBar(bool isFoundLowPriceOnBar, int startBar, int endBar){

			double low = Low[CurrentBar - startBar];
			double high = High[CurrentBar - startBar];
			int start = CurrentBar - startBar;
			int end = CurrentBar - endBar;
			
			for(;start > end; start--){
				if(isFoundLowPriceOnBar){
					if(low < Low[start]){
						low = Low[start];
					}
				}
				else{
					if(high > High[start]){
						high = High[start];
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
						}
						highBar = CurrentBar;
						isChangePeriod = true;
						Print("add High");
						Print("lowBarPeriod -> " + lowBarPeriod);
					}
					if(!addHigh && updateHigh){
						highBar = CurrentBar;
						Print("update High");
						Print("highBar -> " + highBar);
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
						}
						lowBar = CurrentBar;
						isChangePeriod = true;
						Print("add Low");
						Print("highBarPeriod ->" + highBarPeriod);
					}
					if(!addLow && updateLow){
						lowBar = CurrentBar;
						Print("update Low");
						Print("lowBar -> " + lowBar);
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
		
		
		
		/*protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Last) {
				_lastPrice = Price;
				Price = e.Price;
								
				BuyOrSell(Price, _lastPrice);
			}
		}*/
			
		
		private void BuyOrSell(double price, double lastPrice){

			if (Position.MarketPosition == MarketPosition.Flat)
			{

				isBuyOrder = false;
				isSellOrder = false;
				
				SetStopLoss(CalculationMode.Ticks, StopLoss);
				SetProfitTarget(CalculationMode.Ticks, ProfitTarget);
				
				for(int i = 0; i < dailyZigZag.LowLevelList.Count; i++){
					if(IsPriceInOrderPeriod(price, lastPrice, dailyZigZag.LowLevelList[i]) && (price < lowLineRSIAnalog)){
						EnterLong("BuyOrder");
						Print("OrderAction.Buy");
						
						startOrderPrice = price;
						isBuyOrder = true;
						
						
						Print("price" + price);
						Print("lastPrice" + lastPrice);
						Print("Before dayli low if index" + i + " value " + dailyZigZag.LowLevelList[i]);
						dailyZigZag.LowLevelList[i] = 0;
						Print("After dayli low if index" + i + " value " + dailyZigZag.LowLevelList[i]);
					}
					else{
						if(IsPriceInOrderPeriod(price, lastPrice, dailyZigZag.LowLevelList[i]) && !(price < lowLineRSIAnalog)){
							Print("Before dayli low else index" + i + " value " + dailyZigZag.LowLevelList[i]);
							dailyZigZag.LowLevelList[i] = 0;
							Print("After dayli low else index" + i + " value " + dailyZigZag.LowLevelList[i]);
						}
					}
				}
				//else
				for(int i = 0; i < dailyZigZag.HighLevelList.Count; i++){
					if(IsPriceInOrderPeriod(price, lastPrice, dailyZigZag.HighLevelList[i]) && (price > highLineRSIAnalog)){
						EnterShort("SellOrder");
						Print("OrderAction.Sell");
						
						startOrderPrice = price;
						isSellOrder = true;
						
						Print("price" + price);
						Print("lastPrice" + lastPrice);
						Print("Before dayli high if index" + i + " value " + dailyZigZag.HighLevelList[i]);
						dailyZigZag.HighLevelList[i] = 0;
						Print("After dayli high if index" + i + " value " + dailyZigZag.HighLevelList[i]);
					}
					else{ 
						if(IsPriceInOrderPeriod(price, lastPrice, dailyZigZag.HighLevelList[i])){
							Print("Before dayli high else index" + i + " value " + dailyZigZag.HighLevelList[i]);
							dailyZigZag.HighLevelList[i] = 0;
							Print("After dayli high else index" + i + " value " + dailyZigZag.HighLevelList[i]);
						}
					}
				}
				
				
				for(int i = 0; i < dailyZigZag.LowLevelList.Count; i++){
					if(IsPriceInOrderPeriod(price, lastPrice, dailyZigZag.LowLevelList[i]) && !(price < lowLineRSIAnalog)){
							Print("Before dayli low else index" + i + " value " + dailyZigZag.LowLevelList[i]);
							dailyZigZag.LowLevelList[i] = 0;
							Print("After dayli low else index" + i + " value " + dailyZigZag.LowLevelList[i]);
						}
				}
				
				for(int i = 0; i < dailyZigZag.HighLevelList.Count; i++){
					if(IsPriceInOrderPeriod(price, lastPrice, dailyZigZag.HighLevelList[i])){
						Print("Before dayli high else index" + i + " value " + dailyZigZag.HighLevelList[i]);
						dailyZigZag.HighLevelList[i] = 0;
						Print("After dayli high else index" + i + " value " + dailyZigZag.HighLevelList[i]);
					}
				}
				
				
			}					
		}
		

		
		private bool IsPriceInOrderPeriod(double price, double lastPrice, double level){
			if((lastPrice < level && price >= level) || (lastPrice > level && price <= level)){
				return true;
			}
			else
				return false;
		}
		
		
		
		public class ZigZagHistory{

			private readonly int _dayOfHistory;

			private readonly List<DailyZigZag> _dailyZigZagList;

			public ZigZagHistory(int days)
			{
				_dayOfHistory = days;
				_dailyZigZagList = new List<DailyZigZag>();
			}

			public void AddDaylyZigZag(DailyZigZag zigZag)
			{
				Resize();
				_dailyZigZagList.Add(zigZag);
			}

			private void Resize()
			{
				if (_dailyZigZagList.Count > _dayOfHistory)
				{
					_dailyZigZagList.RemoveAt(0);
				}
			}

			public List<DailyZigZag> DailyZigZagList()
			{
				return _dailyZigZagList;
			}
		}
		
		
		public class DailyZigZag
		{
			public List<double> HighLevelList{get;set;}
			public List<double> LowLevelList{get;set;}

			public DateTime DailyZigZagDateTime { get; set; }

			public int Size{
				get{
					return HighLevelList.Count;
				}
			}
			
			public DailyZigZag(DateTime dateTime)
			{
				DailyZigZagDateTime = dateTime;

				HighLevelList = new List<double>();
				LowLevelList = new List<double>();
			}

			public void AddHigh(double highLevel)
			{
				HighLevelList.Add(highLevel);
			}

			public void AddLow(double lowLevel)
			{
				LowLevelList.Add(lowLevel);
			}

			public override string ToString()
			{
				return DailyZigZagDateTime.ToString();
			}
		}	
		
		
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
        public double ProfitTarget
        {
          get{return profitTarget;}
          set{profitTarget = value;}
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
		
		
		private double Price {
			get{return _price;} 
			set{_price = value;}
		}
		
		
        #endregion
    }
}
