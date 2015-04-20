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
    public class AqueGenNinjaReCodeZigZag : Strategy
    {
        //#region Variables
        // Wizard generated variables
        private int myInput0 = 1; // Default setting for MyInput0
		

		
		private double _price = 0;
		private double _lastPrice = 0;

		
		private int _waitBarsAfterChangeTrend = 5;
		private int _waitBarsAfterCloseOrder = 10;
      	private bool _isActivateTimeOut = false;
		
      	private int indexBarAfterCloseOrder = 0;
		private int indexBarAfterChangeTrend = 0;
      
	
		private bool isOrderPresent = false;
		
		private double startOrderPrice = 0;
		
		
		private bool isFoundTrend = false;
		
      	private int barIndex = 0;
      	private int singleBarIndex = 0;
      
		
		//zigzag
		private double			currentZigZagHigh	= 0;
		private double			currentZigZagLow	= 0;
		private DeviationType	deviationType		= DeviationType.Percent;
		private double			deviationValue		= 0.5;
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
		
		private int updateAfterChangeZigZagProcent = 50;
		
		private int startBar = 0;
		private int endBar = 0;
		
		private bool isTrendOnPeriodDown;
		private double firstLevelPrice = 0;
		private double secondLevelPrice = 0;
		private double thirdLevelPrice = 0;
		
		private int highBar = 0;
		private int lowBar = 0;
		
		private int highBarPeriod = 0;
		private int lowBarPeriod = 0;
		private int lastHighBarPeriod = 0;
		private int lastLowBarPeriod = 0;
		
		private double highZigZagApexPrice = 0;
		private double lowZigZagApexPrice = 0;
		
		
		private int startBarLevel = 0;
		private int endBarLevel = 0;
		private double highZigZagApexPriceLevel = 0;
		private double lowZigZagApexPriceLevel = 100000;
		private bool isTrendOnPeriodLevelDown = false;
		
		
		
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
				
				double procentOfMiddleValot = (middleValot / 100) * (100 - ProcentFromMiddleValot);
				
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

				BuyOrSell(Price, _lastPrice);
				
			}
			
			if(BarsInProgress == 2){

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
			
			if(highZigZagApexPrice == 0 || lowZigZagApexPrice == 0){
				return;
			}

			if(lowBarPeriod == 0 || highBarPeriod == 0)
				return;
			
			Print("lowBar -> " + lowBar);
			Print("highBar -> " + highBar);
		
			Print("Now Bar is -> " + CurrentBar);
			
			if(highBarPeriod > lowBarPeriod){
				isTrendOnPeriodDown = false;
				startBar = lowBarPeriod - 1;
				endBar = highBarPeriod - 1;
			}
			else{
				isTrendOnPeriodDown = true;
				startBar = highBarPeriod - 1;
				endBar = lowBarPeriod - 1;
			}
			
			
			
			
			
			//Print("highZigZagApexPrice " + highZigZagApexPrice);
			//Print("lowZigZagApexPrice " + lowZigZagApexPrice);
			
			double differentPriceBetweenLevelApex = 0;
			double priceAfterWasChangeApex = 0;
			if(highZigZagApexPriceLevel != 0 && lowZigZagApexPriceLevel != 0){
			 	differentPriceBetweenLevelApex = highZigZagApexPriceLevel - lowZigZagApexPriceLevel;
				priceAfterWasChangeApex = ((differentPriceBetweenLevelApex) / 100 ) * UpdateAfterChangeZigZagProcent;
			}
			Print("differentPriceBetweenLevelApex " + differentPriceBetweenLevelApex);
			
			
			Print("highZigZagApexPriceLevel " + highZigZagApexPriceLevel);
			Print("lowZigZagApexPriceLevel " + lowZigZagApexPriceLevel);
			
			Print("highZigZagApexPrice " + highZigZagApexPrice);
			Print("lowZigZagApexPrice " + lowZigZagApexPrice);
			
			
			Print("isTrendOnPeriodDown " + isTrendOnPeriodDown);
			Print("isTrendOnPeriodLevelDown " + isTrendOnPeriodLevelDown);
			

			Print("startBar -> " + startBar);
			Print("endBar -> " + endBar);
			
			
			Print("firstLevelPrice beforeChange " + firstLevelPrice);
			Print("secondLevelPrice beforeChange " + secondLevelPrice);
			Print("thirdLevelPrice beforeChange " + thirdLevelPrice);
			
			bool isUpdateBars = false;
			bool isUpdateBuyLevel = false;
			bool isUpdateSellLevel = false;
			
			
			if((isTrendOnPeriodLevelDown == true && isTrendOnPeriodDown == true) 
					|| (isTrendOnPeriodLevelDown == false && isTrendOnPeriodDown == false)){
				if(isTrendOnPeriodDown){
					if(lowZigZagApexPrice < lowZigZagApexPriceLevel){
						lowZigZagApexPriceLevel = lowZigZagApexPrice;
						
						endBarLevel = endBar;
						isUpdateBars = true;
						Print("update low Apex");
					}
					else
						return;
				}
				else{
					if(highZigZagApexPrice > highZigZagApexPriceLevel){
						highZigZagApexPriceLevel = highZigZagApexPrice;
						
						endBarLevel = endBar;
						isUpdateBars = true;
						Print("update high Apex");
					}
					else
						return;
				}
			}
			else{
				if(isTrendOnPeriodLevelDown && !isTrendOnPeriodDown){
					if( highZigZagApexPriceLevel - differentPriceBetweenLevelApex + priceAfterWasChangeApex  < highZigZagApexPrice){
						highZigZagApexPriceLevel = highZigZagApexPrice;
						lowZigZagApexPriceLevel = lowZigZagApexPrice;
						
						startBarLevel = endBarLevel;
						endBarLevel = highBar - 1;
						
						isUpdateBars = false;
						isUpdateSellLevel = true;
						Print("add high Apex");
					}
					else
						return;
				}
				else if(!isTrendOnPeriodLevelDown && isTrendOnPeriodDown){
					if(lowZigZagApexPriceLevel  + differentPriceBetweenLevelApex - priceAfterWasChangeApex > lowZigZagApexPrice){
						highZigZagApexPriceLevel = highZigZagApexPrice;
						lowZigZagApexPriceLevel = lowZigZagApexPrice;

						startBarLevel = endBarLevel;
						endBarLevel = lowBar - 1;
						
						isUpdateBars = false;
						isUpdateBuyLevel = true;
						Print("add low Apex");
					}
					else
						return;
				}
			}
			
			
			
			
			if(isUpdateBars){
				if(isTrendOnPeriodDown){
					lowZigZagApexPriceLevel = lowZigZagApexPrice;
				}
				else{
					highZigZagApexPriceLevel = highZigZagApexPrice;
				}
			}
			else{
				highZigZagApexPriceLevel = highZigZagApexPrice;
				lowZigZagApexPriceLevel = lowZigZagApexPrice;
			}
			
			
			//--------------------------------------------------//
			
			
			int startBarLevelForDiapasone = startBarLevel;
			int endBarLevelForDiapasone = endBarLevel;
			if(startBarLevelForDiapasone - LeftZigZag > 0){
				startBarLevelForDiapasone = startBarLevelForDiapasone - LeftZigZag;
				endBarLevelForDiapasone = endBarLevelForDiapasone + RightZigZag;
			}
			
			periodOfCalculate = endBarLevelForDiapasone - startBarLevelForDiapasone;
			int firstDiapasonEnd = startBarLevelForDiapasone + GetProcentValue(periodOfCalculate, FirstDiapason);
			int thirtDiapasoneStart = endBarLevelForDiapasone - GetProcentValue(periodOfCalculate, ThirtDiapason);



			
			Print("isUpdateBuyLevel " + isUpdateBuyLevel);
			Print("isUpdateSellLevel " + isUpdateSellLevel);
			
			isTrendOnPeriodLevelDown = isTrendOnPeriodDown;
			if(!isUpdateBars){
				if(startBarLevelForDiapasone < endBarLevelForDiapasone && isChangePeriod){
					
					if(isUpdateSellLevel){
						firstLevelPrice = GetLowOrHighPriceOfBar(false, startBarLevelForDiapasone, endBarLevelForDiapasone);
					}
					
					secondLevelPrice = 0;
					
					if(isUpdateBuyLevel){
						thirdLevelPrice = GetLowOrHighPriceOfBar(true, startBarLevelForDiapasone, endBarLevelForDiapasone);
					}
							
					isChangePeriod = false;
				}
				
			}
			Print("firstLevelPrice AfterChange " + firstLevelPrice);
			Print("secondLevelPrice AfterChange " + secondLevelPrice);
			Print("thirdLevelPrice AfterChange " + thirdLevelPrice);
		}
		
		
		
		
		/*private double GetAverageBetweenLevel(double apex, double level, double priceDifferent){
			if(Math.Abs((apex * TickSize) - (level * TickSize)) > priceDifferent)
				return Math.Round((apex + level) / 2, 1);
			else
				return level;
		}*/
		
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
							
							if(UseHighLow)
								lowZigZagApexPrice = Low[CurrentBar - lowBar + 1];
							else
								lowZigZagApexPrice = Close[CurrentBar - lowBar + 1];
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
							
							if(UseHighLow)
								highZigZagApexPrice = High[CurrentBar - highBar + 1];
							else
								highZigZagApexPrice = Close[CurrentBar - highBar + 1];
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
				
				if(IsPriceInOrderPeriod(price, lastPrice, firstLevelPrice, "firstLevelPrice")
					//&& (price > highLineRSIAnalog)
					){
					EnterShort("SellOrder");
					Print("OrderAction.Sell");
				}
				else if(IsPriceInOrderPeriod(price, lastPrice, thirdLevelPrice, "thirdLevelPrice") 
						//&& (price < lowLineRSIAnalog)
					){
					EnterLong("BuyOrder");
					Print("OrderAction.Buy");
				} 


			}
			
			DeleteLevelPrice(IsPriceInOrderPeriod(price, lastPrice, firstLevelPrice, "firstLevelPrice"), "firstLevelPrice");
			DeleteLevelPrice(IsPriceInOrderPeriod(price, lastPrice, secondLevelPrice, "secondLevelPrice"), "secondLevelPrice");
			DeleteLevelPrice(IsPriceInOrderPeriod(price, lastPrice, thirdLevelPrice, "thirdLevelPrice"), "thirdLevelPrice");

			//Print("firstLevelPrice -> " + firstLevelPrice);
			//Print("secondLevelPrice -> " + secondLevelPrice);
			//Print("thirdLevelPrice -> " + thirdLevelPrice);
			
			//Print("=====================");
			
				
		}
		
		private void DeleteLevelPrice(bool isPriceInOrderPeriod, string levelName){
			if(isPriceInOrderPeriod){
				switch(levelName){
					case("firstLevelPrice"):{
						firstLevelPrice = 0;
						break;
					}
					case("secondLevelPrice"):{
						secondLevelPrice = 0;
						break;
					}
					case("thirdLevelPrice"):{
						thirdLevelPrice = 0;
						break;
					}
				}
				Print("Delete -> " + levelName);
				//Print("firstLevelPrice -> " + firstLevelPrice);
				//Print("secondLevelPrice -> " + secondLevelPrice);
				//Print("thirdLevelPrice -> " + thirdLevelPrice);
			}
		}
		
		private bool IsPriceInOrderPeriod(double price, double lastPrice, double level, string levelName){
			if((lastPrice < level && price >= level) || (lastPrice > level && price <= level)){
				return true;
			}
			else
				return false;
		}
		
        #region Properties	
		
		
		
		[Description("Установить отступ от крайне левой точки ZigZag")]
		[GridCategory("_ZigZag")]
		public int LeftZigZag
		{
			get{return _leftZigZag;}
			set{_leftZigZag = value;}
		}
		
		[Description("Установить отступ от крайне левой точки ZigZag")]
		[GridCategory("_ZigZag")]
		public int RightZigZag
		{
			get{return _rightZigZag;}
			set{_rightZigZag = value;}
		}


 		[Description("Ожидание свечек после закрытия ордера")]
        [Category("_Time Out")]
        public int WaitBarsAfterCloseOrder 
        {
           get{return _waitBarsAfterCloseOrder;}
           set{_waitBarsAfterCloseOrder = value;}
        }
	
      
        [Description("Активация таймаута")]
        [Category("_Time Out")]
        public bool IsActivateTimeOut 
        {
          get{return _isActivateTimeOut;}
          set{_isActivateTimeOut = value;}
        }
		
		[Description("Диапазон в %")]
        [Category("Diapason")]
        public int FirstDiapason 
        {
          get{return firstDiapason;}
          set{firstDiapason = value;}
        }
		

		[Description("Диапазон в %")]
        [Category("Diapason")]
        public int ThirtDiapason 
        {
          get{return thirtDiapason;}
          set{thirtDiapason = value;}
        }
		
		[Description("Разница цены")]
        [Category("Diapason")]
        public double FirstDiapasonPriceDifferent
        {
          get{return firstDiapasonPriceDifferent;}
          set{firstDiapasonPriceDifferent = value;}
        }
		
		[Description("Разница цены")]
        [Category("Diapason")]
        public double ThirtDiapasonPriceDifferent
        {
          get{return thirtDiapasonPriceDifferent;}
          set{thirtDiapasonPriceDifferent = value;}
        }
		
		
       
		[Description("Deviation in percent or points regarding on the deviation type")]
        [GridCategory("_ZigZag")]
		[Gui.Design.DisplayName("Deviation value")]
        public double DeviationValue
        {
            get { return deviationValue; }
            set { deviationValue = Math.Max(0.0, value); }
        }

        [Description("Type of the deviation value")]
        [GridCategory("_ZigZag")]
		[Gui.Design.DisplayName("Deviation type")]
        public DeviationType DeviationType
        {
            get { return deviationType; }
            set { deviationType = value; }
        }

        [Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("_ZigZag")]
		[Gui.Design.DisplayName("Use high and low")]
		[RefreshProperties(RefreshProperties.All)]
        public bool UseHighLow
        {
            get { return useHighLow; }
            set { useHighLow = value; }
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
		
		
		[Description("Numbers of bars used for calculations")]
		[GridCategory("_ZigZag")]
		public int UpdateAfterChangeZigZagProcent
		{
			get { return updateAfterChangeZigZagProcent; }
			set { updateAfterChangeZigZagProcent = Math.Max(1, value); }
		}
		
		
		
		
		private double Price {
			get{return _price;} 
			set{_price = value;}
		}
		
		
        #endregion
    }
}
