#region Using declarations
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// Enter the description of your new custom indicator here
    /// </summary>
    [Description("Enter the description of your new custom indicator here")]
    public class AqueGenNinjaBot : Indicator
    {
         //#region Variables
        // Wizard generated variables
		
		
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
		
		
		private bool initiateInOnBarUpdate = true;
		
		
		//Analog RSI
		private double lowLineRSIAnalog = 0;
		private double highLineRSIAnalog = 100000;
		
		//SMA
		private int smaPeriod = 100;
		private double  smaLine = 0;
		private double middleValot = 0;
		private int dayOfSMAValot = 5;
		private int procentFromMiddleValot = 50;
		
		
		private int updateAfterChangeZigZagProcent = 50;
		
        // User defined variables (add any user defined variables below)
        //#endregion

		
        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
			CalculateOnBarClose = true;
			Overlay				= true;

			//zigzag
			zigZagHighSeries	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagHighZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagLowSeries		= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			zigZagLowZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 

			//DisplayInDataBox	= false;
            
			//PaintPriceMarkers	= false;
			
			
			
			
			//SetStopLoss(CalculationMode.Ticks, StopLoss);
			//SetProfitTarget(CalculationMode.Ticks, ProfitTarget);
			
			//Add(PeriodType.Minute, MainMinuteTimeFrame);
			//Add(PeriodType.Tick, 1);
			//Add(PeriodType.Day, 1);
			
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
    	{
			
			if(CurrentBars[0] < 20)
				return;
			//if(CurrentBars[1] < DayOfSMAValot)
			//	return;
			
			if(BarsInProgress == 0){

				
				//Print("==============");
				smaLine = SMA(SMAPeriod)[0];
				//Print("Day middleValot " + middleValot);
				//Print("SMA Line -> " + smaLine);
				
				double procentOfMiddleValot = (middleValot / 100) * (100 - ProcentFromMiddleValot);
				
				lowLineRSIAnalog = smaLine - procentOfMiddleValot;
				highLineRSIAnalog = smaLine + procentOfMiddleValot;
				
				//Print("lowLineRSIAnalog " + lowLineRSIAnalog);
				//Print("highLineRSIAnalog " + highLineRSIAnalog);
				//Print(Time[0].ToString());
				
				
				OnBarUpdateMain();
				//Print(Time[0].ToString());
			}
			
			 /*if(BarsInProgress == 1){	
				double openPrice = Opens[1][0];
			
				_lastPrice = Price;
				Price = openPrice;

				//BuyOrSell(Price, _lastPrice);
				
			}
			
			if(BarsInProgress == 1){

				middleValot = 0;
				
				for(int i = 0; i < DayOfSMAValot; i++){
					middleValot = middleValot + Highs[2][i] - Lows[2][i];
				}
				middleValot = middleValot / DayOfSMAValot;
				Print("Day middleValot " + middleValot);	
			}	*/
			
		}
		
		private void OnBarUpdateMain(){
           

			

			
			
			barIndex++;
			//Print("-------------------");
			
			ZigZagUpdateOnBar();
			
			if(highZigZagApexPrice == 0 || lowZigZagApexPrice == 0){
				return;
			}
			
			
			
			
			if(lowBarPeriod == 0 || highBarPeriod == 0)
				return;
			
			//Print("lowBar -> " + lowBar);
			//Print("highBar -> " + highBar);
		
			//Print("Now Bar is -> " + CurrentBar);
			
			if(highBarPeriod > lowBarPeriod){
				isTrendOnPeriodDown = false;
				startBar = lowBarPeriod/* - 1 - LeftZigZag*/;
				endBar = highBarPeriod - 1/* + RightZigZag*/;
			}
			else{
				isTrendOnPeriodDown = true;
				startBar = highBarPeriod - 1/* - LeftZigZag*/;
				endBar = lowBarPeriod - 1/* + RightZigZag*/;
			}

			//Print("highZigZagApexPrice " + highZigZagApexPrice);
			//Print("lowZigZagApexPrice " + lowZigZagApexPrice);
			
			double differentPriceBetweenLevelApex = ((highZigZagApexPriceLevel - lowZigZagApexPriceLevel) / 100 ) * UpdateAfterChangeZigZagProcent;
			//Print("differentPriceBetweenLevelApex " + differentPriceBetweenLevelApex);
			
			
			//Print("highZigZagApexPriceLevel " + highZigZagApexPriceLevel);
			//Print("lowZigZagApexPriceLevel " + lowZigZagApexPriceLevel);
			
			//Print("highZigZagApexPrice " + highZigZagApexPrice);
			//Print("lowZigZagApexPrice " + lowZigZagApexPrice);
			
			
			//Print("isTrendOnPeriodDown " + isTrendOnPeriodDown);
			//Print("isTrendOnPeriodLevelDown " + isTrendOnPeriodLevelDown);
			

			//Print("startBar -> " + startBar);
			//Print("endBar -> " + endBar);
			
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
						//Print("update low Apex");
					}
					else
						return;
				}
				else{
					if(highZigZagApexPrice > highZigZagApexPriceLevel){
						highZigZagApexPriceLevel = highZigZagApexPrice;
						
						endBarLevel = endBar;
						isUpdateBars = true;
						//Print("update high Apex");
					}
					else
						return;
				}
			}
			else{
				if(isTrendOnPeriodLevelDown && !isTrendOnPeriodDown){
					if( highZigZagApexPriceLevel  < highZigZagApexPrice + differentPriceBetweenLevelApex){
						highZigZagApexPriceLevel = highZigZagApexPrice;
						lowZigZagApexPriceLevel = lowZigZagApexPrice;
						
						startBarLevel = endBarLevel;
						endBarLevel = highBarPeriod - 1;
						
						isUpdateBars = false;
						isUpdateSellLevel = true;
						//Print("add high Apex");
					}
					else
						return;
				}
				else if(!isTrendOnPeriodLevelDown && isTrendOnPeriodDown){
					if(lowZigZagApexPriceLevel  + differentPriceBetweenLevelApex > lowZigZagApexPrice){
						highZigZagApexPriceLevel = highZigZagApexPrice;
						lowZigZagApexPriceLevel = lowZigZagApexPrice;

						startBarLevel = endBarLevel;
						endBarLevel = lowBarPeriod - 1;
						
						isUpdateBars = false;
						isUpdateBuyLevel = true;
						//Print("add low Apex");
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
			
			//----------------------------------------------------------//
			int startBarLevelForDiapasone = startBarLevel;
			int endBarLevelForDiapasone = endBarLevel;
			if(startBarLevel - LeftZigZag > 0){
				
				DrawVerticalLine("startPeriodZigZag", CurrentBar - startBarLevelForDiapasone, Color.Yellow, DashStyle.Dash, 2);
				DrawVerticalLine("endPeriodZigZag", CurrentBar - endBarLevelForDiapasone, Color.Yellow, DashStyle.Dash, 2);
				
				startBarLevelForDiapasone = startBarLevelForDiapasone - LeftZigZag;
				endBarLevelForDiapasone = endBarLevelForDiapasone + RightZigZag;

				DrawVerticalLine("startPeriodWithIntend", CurrentBar - startBarLevelForDiapasone, Color.DarkBlue, DashStyle.Dash, 4);
				DrawVerticalLine("endPeriodWithIntend", CurrentBar - endBarLevelForDiapasone, Color.DarkBlue, DashStyle.Dash, 4);
			}
	
			
			
			periodOfCalculate = endBarLevelForDiapasone - startBarLevelForDiapasone;
			int firstDiapasonEnd = startBarLevelForDiapasone + GetProcentValue(periodOfCalculate, FirstDiapason);
			int thirtDiapasoneStart = endBarLevelForDiapasone - GetProcentValue(periodOfCalculate, ThirtDiapason);


			
			isTrendOnPeriodLevelDown = isTrendOnPeriodDown;
			if(!isUpdateBars){
				if(isDiapasoned){
					if(startBarLevelForDiapasone < endBarLevelForDiapasone && isChangePeriod){
						if(isTrendOnPeriodDown){
							if(isUpdateSellLevel)
								firstLevelPrice = GetLowOrHighPriceOfBar(true, startBarLevelForDiapasone, firstDiapasonEnd);
							if(isUpdateSellLevel)
								secondLevelPrice = GetLowOrHighPriceOfBar(true, firstDiapasonEnd, thirtDiapasoneStart);
							if(isUpdateBuyLevel)
								thirdLevelPrice = GetLowOrHighPriceOfBar(false, thirtDiapasoneStart, endBarLevelForDiapasone);
						
						}
						else{
							if(isUpdateBuyLevel)
								firstLevelPrice = GetLowOrHighPriceOfBar(false, startBarLevelForDiapasone, firstDiapasonEnd);
							if(isUpdateBuyLevel)
								secondLevelPrice = GetLowOrHighPriceOfBar(false, firstDiapasonEnd, thirtDiapasoneStart);
							if(isUpdateSellLevel)
								thirdLevelPrice = GetLowOrHighPriceOfBar(true, thirtDiapasoneStart, endBarLevelForDiapasone);
							
							
						}
						isChangePeriod = false;
					}
				}
				else{
					if(startBarLevelForDiapasone < endBarLevelForDiapasone && isChangePeriod){
						if(isTrendOnPeriodDown){
							if(isUpdateSellLevel)
								firstLevelPrice = GetLowOrHighPriceOfBar(true, startBarLevelForDiapasone, endBarLevelForDiapasone);
							
								secondLevelPrice = 0;
							
							if(isUpdateBuyLevel)
								thirdLevelPrice = GetLowOrHighPriceOfBar(false, startBarLevelForDiapasone, endBarLevelForDiapasone);
							
						}
						else{
							if(isUpdateBuyLevel)
								firstLevelPrice = GetLowOrHighPriceOfBar(false, startBarLevelForDiapasone, endBarLevelForDiapasone);
							secondLevelPrice = 0;
							if(isUpdateSellLevel)
								thirdLevelPrice = GetLowOrHighPriceOfBar(true, startBarLevelForDiapasone, endBarLevelForDiapasone);
								
						}
						isChangePeriod = false;
					}
				}
			
			}


			//DrawHorizontalLine("level1", firstLevelPrice, Color.Red);
			//DrawHorizontalLine("level2", secondLevelPrice, Color.Green);
			//DrawHorizontalLine("level3", thirdLevelPrice, Color.Blue);
			
			
			
			//Print("firstLevelPrice " + firstLevelPrice);
			//Print("secondLevelPrice " + secondLevelPrice);
			//Print("thirdLevelPrice " + thirdLevelPrice);
		}
	
	

	
	
		private int GetProcentValue(double value, int procent){
			return Convert.ToInt32((value/100) * procent);
		}
		
	
		private double GetLowOrHighPriceOfBar(bool isFoundLowPriceOnBar, int startBar, int endBar){

			double low = Low[CurrentBar - startBar];
			double high = High[CurrentBar - startBar];
			int start = CurrentBar - startBar;
			int end = CurrentBar - endBar;
			
			int highBar = 0;
			int lowBar = 0;
			for(;start > end; start--){
				if(isFoundLowPriceOnBar){
					if(low < Low[start]){
						low = Low[start];
						lowBar = start;
					}
				}
				else{
					if(high > High[start]){
						high = High[start];
						highBar = start;
					}
				}
			}
			
			if(isFoundLowPriceOnBar){
				DrawLine("lowBar", lowBar, low, -1, low, Color.Red);
				return low;
			}
			else{
				DrawLine("highBar", highBar, high, -1, high, Color.Blue);
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
		#endregion
		
		private bool IsPriceGreater(double a, double b)
		{
			if (a > b && a - b > TickSize / 2)
				return true; 
			else 
				return false;
		}

		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			
			
		}

		
		
       #region Properties	
		

		
		
		

		
		[Description("If true - 3 diapasone in zigzag")]
		[GridCategory("Diapasone")]
		public bool IsDiapasoned
		{
			get{return isDiapasoned;}
			set{isDiapasoned = value;}
		}
		
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




#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private AqueGenNinjaBot[] cacheAqueGenNinjaBot = null;

        private static AqueGenNinjaBot checkAqueGenNinjaBot = new AqueGenNinjaBot();

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public AqueGenNinjaBot AqueGenNinjaBot(int dayOfSMAValot, DeviationType deviationType, double deviationValue, bool isDiapasoned, int leftZigZag, int procentFromMiddleValot, double profitTarget, int rightZigZag, int sMAPeriod, double stopLoss, int updateAfterChangeZigZagProcent, bool useHighLow)
        {
            return AqueGenNinjaBot(Input, dayOfSMAValot, deviationType, deviationValue, isDiapasoned, leftZigZag, procentFromMiddleValot, profitTarget, rightZigZag, sMAPeriod, stopLoss, updateAfterChangeZigZagProcent, useHighLow);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public AqueGenNinjaBot AqueGenNinjaBot(Data.IDataSeries input, int dayOfSMAValot, DeviationType deviationType, double deviationValue, bool isDiapasoned, int leftZigZag, int procentFromMiddleValot, double profitTarget, int rightZigZag, int sMAPeriod, double stopLoss, int updateAfterChangeZigZagProcent, bool useHighLow)
        {
            if (cacheAqueGenNinjaBot != null)
                for (int idx = 0; idx < cacheAqueGenNinjaBot.Length; idx++)
                    if (cacheAqueGenNinjaBot[idx].DayOfSMAValot == dayOfSMAValot && cacheAqueGenNinjaBot[idx].DeviationType == deviationType && Math.Abs(cacheAqueGenNinjaBot[idx].DeviationValue - deviationValue) <= double.Epsilon && cacheAqueGenNinjaBot[idx].IsDiapasoned == isDiapasoned && cacheAqueGenNinjaBot[idx].LeftZigZag == leftZigZag && cacheAqueGenNinjaBot[idx].ProcentFromMiddleValot == procentFromMiddleValot && Math.Abs(cacheAqueGenNinjaBot[idx].ProfitTarget - profitTarget) <= double.Epsilon && cacheAqueGenNinjaBot[idx].RightZigZag == rightZigZag && cacheAqueGenNinjaBot[idx].SMAPeriod == sMAPeriod && Math.Abs(cacheAqueGenNinjaBot[idx].StopLoss - stopLoss) <= double.Epsilon && cacheAqueGenNinjaBot[idx].UpdateAfterChangeZigZagProcent == updateAfterChangeZigZagProcent && cacheAqueGenNinjaBot[idx].UseHighLow == useHighLow && cacheAqueGenNinjaBot[idx].EqualsInput(input))
                        return cacheAqueGenNinjaBot[idx];

            lock (checkAqueGenNinjaBot)
            {
                checkAqueGenNinjaBot.DayOfSMAValot = dayOfSMAValot;
                dayOfSMAValot = checkAqueGenNinjaBot.DayOfSMAValot;
                checkAqueGenNinjaBot.DeviationType = deviationType;
                deviationType = checkAqueGenNinjaBot.DeviationType;
                checkAqueGenNinjaBot.DeviationValue = deviationValue;
                deviationValue = checkAqueGenNinjaBot.DeviationValue;
                checkAqueGenNinjaBot.IsDiapasoned = isDiapasoned;
                isDiapasoned = checkAqueGenNinjaBot.IsDiapasoned;
                checkAqueGenNinjaBot.LeftZigZag = leftZigZag;
                leftZigZag = checkAqueGenNinjaBot.LeftZigZag;
                checkAqueGenNinjaBot.ProcentFromMiddleValot = procentFromMiddleValot;
                procentFromMiddleValot = checkAqueGenNinjaBot.ProcentFromMiddleValot;
                checkAqueGenNinjaBot.ProfitTarget = profitTarget;
                profitTarget = checkAqueGenNinjaBot.ProfitTarget;
                checkAqueGenNinjaBot.RightZigZag = rightZigZag;
                rightZigZag = checkAqueGenNinjaBot.RightZigZag;
                checkAqueGenNinjaBot.SMAPeriod = sMAPeriod;
                sMAPeriod = checkAqueGenNinjaBot.SMAPeriod;
                checkAqueGenNinjaBot.StopLoss = stopLoss;
                stopLoss = checkAqueGenNinjaBot.StopLoss;
                checkAqueGenNinjaBot.UpdateAfterChangeZigZagProcent = updateAfterChangeZigZagProcent;
                updateAfterChangeZigZagProcent = checkAqueGenNinjaBot.UpdateAfterChangeZigZagProcent;
                checkAqueGenNinjaBot.UseHighLow = useHighLow;
                useHighLow = checkAqueGenNinjaBot.UseHighLow;

                if (cacheAqueGenNinjaBot != null)
                    for (int idx = 0; idx < cacheAqueGenNinjaBot.Length; idx++)
                        if (cacheAqueGenNinjaBot[idx].DayOfSMAValot == dayOfSMAValot && cacheAqueGenNinjaBot[idx].DeviationType == deviationType && Math.Abs(cacheAqueGenNinjaBot[idx].DeviationValue - deviationValue) <= double.Epsilon && cacheAqueGenNinjaBot[idx].IsDiapasoned == isDiapasoned && cacheAqueGenNinjaBot[idx].LeftZigZag == leftZigZag && cacheAqueGenNinjaBot[idx].ProcentFromMiddleValot == procentFromMiddleValot && Math.Abs(cacheAqueGenNinjaBot[idx].ProfitTarget - profitTarget) <= double.Epsilon && cacheAqueGenNinjaBot[idx].RightZigZag == rightZigZag && cacheAqueGenNinjaBot[idx].SMAPeriod == sMAPeriod && Math.Abs(cacheAqueGenNinjaBot[idx].StopLoss - stopLoss) <= double.Epsilon && cacheAqueGenNinjaBot[idx].UpdateAfterChangeZigZagProcent == updateAfterChangeZigZagProcent && cacheAqueGenNinjaBot[idx].UseHighLow == useHighLow && cacheAqueGenNinjaBot[idx].EqualsInput(input))
                            return cacheAqueGenNinjaBot[idx];

                AqueGenNinjaBot indicator = new AqueGenNinjaBot();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.DayOfSMAValot = dayOfSMAValot;
                indicator.DeviationType = deviationType;
                indicator.DeviationValue = deviationValue;
                indicator.IsDiapasoned = isDiapasoned;
                indicator.LeftZigZag = leftZigZag;
                indicator.ProcentFromMiddleValot = procentFromMiddleValot;
                indicator.ProfitTarget = profitTarget;
                indicator.RightZigZag = rightZigZag;
                indicator.SMAPeriod = sMAPeriod;
                indicator.StopLoss = stopLoss;
                indicator.UpdateAfterChangeZigZagProcent = updateAfterChangeZigZagProcent;
                indicator.UseHighLow = useHighLow;
                Indicators.Add(indicator);
                indicator.SetUp();

                AqueGenNinjaBot[] tmp = new AqueGenNinjaBot[cacheAqueGenNinjaBot == null ? 1 : cacheAqueGenNinjaBot.Length + 1];
                if (cacheAqueGenNinjaBot != null)
                    cacheAqueGenNinjaBot.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheAqueGenNinjaBot = tmp;
                return indicator;
            }
        }
    }
}

// This namespace holds all market analyzer column definitions and is required. Do not change it.
namespace NinjaTrader.MarketAnalyzer
{
    public partial class Column : ColumnBase
    {
        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.AqueGenNinjaBot AqueGenNinjaBot(int dayOfSMAValot, DeviationType deviationType, double deviationValue, bool isDiapasoned, int leftZigZag, int procentFromMiddleValot, double profitTarget, int rightZigZag, int sMAPeriod, double stopLoss, int updateAfterChangeZigZagProcent, bool useHighLow)
        {
            return _indicator.AqueGenNinjaBot(Input, dayOfSMAValot, deviationType, deviationValue, isDiapasoned, leftZigZag, procentFromMiddleValot, profitTarget, rightZigZag, sMAPeriod, stopLoss, updateAfterChangeZigZagProcent, useHighLow);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.AqueGenNinjaBot AqueGenNinjaBot(Data.IDataSeries input, int dayOfSMAValot, DeviationType deviationType, double deviationValue, bool isDiapasoned, int leftZigZag, int procentFromMiddleValot, double profitTarget, int rightZigZag, int sMAPeriod, double stopLoss, int updateAfterChangeZigZagProcent, bool useHighLow)
        {
            return _indicator.AqueGenNinjaBot(input, dayOfSMAValot, deviationType, deviationValue, isDiapasoned, leftZigZag, procentFromMiddleValot, profitTarget, rightZigZag, sMAPeriod, stopLoss, updateAfterChangeZigZagProcent, useHighLow);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.AqueGenNinjaBot AqueGenNinjaBot(int dayOfSMAValot, DeviationType deviationType, double deviationValue, bool isDiapasoned, int leftZigZag, int procentFromMiddleValot, double profitTarget, int rightZigZag, int sMAPeriod, double stopLoss, int updateAfterChangeZigZagProcent, bool useHighLow)
        {
            return _indicator.AqueGenNinjaBot(Input, dayOfSMAValot, deviationType, deviationValue, isDiapasoned, leftZigZag, procentFromMiddleValot, profitTarget, rightZigZag, sMAPeriod, stopLoss, updateAfterChangeZigZagProcent, useHighLow);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.AqueGenNinjaBot AqueGenNinjaBot(Data.IDataSeries input, int dayOfSMAValot, DeviationType deviationType, double deviationValue, bool isDiapasoned, int leftZigZag, int procentFromMiddleValot, double profitTarget, int rightZigZag, int sMAPeriod, double stopLoss, int updateAfterChangeZigZagProcent, bool useHighLow)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.AqueGenNinjaBot(input, dayOfSMAValot, deviationType, deviationValue, isDiapasoned, leftZigZag, procentFromMiddleValot, profitTarget, rightZigZag, sMAPeriod, stopLoss, updateAfterChangeZigZagProcent, useHighLow);
        }
    }
}
#endregion
