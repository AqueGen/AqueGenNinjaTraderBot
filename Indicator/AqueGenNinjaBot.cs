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

		private int _period = 20;	
		

		
		private int _waitBarsAfterChangeTrend = 5;
		private int _waitBarsAfterCloseOrder = 10;
      	private bool _isActivateTimeOut = false;
		
      	private int indexBarAfterCloseOrder = 0;
		private int indexBarAfterChangeTrend = 0;
      
	
		private bool isOrderPresent = false;
		
		private double startOrderPrice = 0;
		
		private string	atmStrategyId		= string.Empty;
		private string	orderId				= string.Empty;
		
		private string openedOrderId;
		
		private bool isFoundTrend = false;
		
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
		private double firstLevelPrice = 0;
		private double secondLevelPrice = 0;
		private double thirdLevelPrice = 0;
		
		private int highBar = 0;
		private int lowBar = 0;
		private int highBarPeriod = 0;
		private int lowBarPeriod = 0;
		
		private int periodOfCalculate = 0;

		
		//RSI
		private DataSeries					avgUp;
		private DataSeries					avgDown;
		private DataSeries					down;
		private int								period	= 14;
		private int								smooth	= 1;
		private DataSeries					up;
		
		private bool isCanBuyRSI = false;
		private bool isCanSellRSI = false;
		
		private int _lowRSI = 35;
		private int _highRSI = 65;
		
		private double rsiAvg;
		private double rsi;
		
			
		//Diapason
		private int firstDiapason = 40;
		private int thirtDiapason = 40;
		
		private double firstDiapasonPriceDifferent = 100;
		private double thirtDiapasonPriceDifferent = 100;
		
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
            //Overlay				= true;
			//PaintPriceMarkers	= false;
			
			//RSI
			avgUp				= new DataSeries(this);
			avgDown				= new DataSeries(this);
			down				= new DataSeries(this);
			up					= new DataSeries(this);
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
    {
		
		if(CurrentBar < 5)
			return;
           
			barIndex++;
			//Print("------------------");
			
			RSIUpdateOnBar();
			ZigZagUpdateOnBar();
		
			if(lowBarPeriod == 0 || highBarPeriod == 0)
				return;
		
			//Print("lowBar -> " + lowBar);
			//Print("highBar -> " + highBar);
			
			//Print("lowBarPeriod -> " + lowBarPeriod);
			//Print("highBarPeriod -> " + highBarPeriod);
			
			
			//Print("Now Bar is -> " + CurrentBar);
			
			int startBar = 0;
			int endBar = 0;
			
			
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
			
			
			//Print("StartPeriod -> " + startBar);
			DrawVerticalLine("StartPeriod", CurrentBar - startBar, Color.Black);
			//Print("EndPeriod -> " + endBar);
			DrawVerticalLine("EndPeriod", CurrentBar - endBar, Color.Black);
			

			
			DrawVerticalLine("firstPeriod", CurrentBar - startBar - GetProcentValue(periodOfCalculate, FirstDiapason), Color.Orange);
			DrawVerticalLine("secondPeriod", CurrentBar - endBar + GetProcentValue(periodOfCalculate, ThirtDiapason), Color.Orange);
			
			
			int firstDiapasonEnd = startBar + GetProcentValue(periodOfCalculate, FirstDiapason);
			int thirtDiapasoneStart = endBar - GetProcentValue(periodOfCalculate, ThirtDiapason);
			
			double firstDiapasoneApex;
			double thirtDiapasoneApex;
			
			if(startBar < endBar){
				if(isTrendOnPeriodDown){
					firstDiapasoneApex = GetApex(true, startBar, firstDiapasonEnd);
					thirtDiapasoneApex = GetApex(false, thirtDiapasoneStart, endBar);
					
					firstLevelPrice = GetLowOrHighPriceOfBar(true, startBar, firstDiapasonEnd);
					secondLevelPrice = GetLowOrHighPriceOfBar(true, firstDiapasonEnd, thirtDiapasoneStart);
					thirdLevelPrice = GetLowOrHighPriceOfBar(false, thirtDiapasoneStart, endBar);
					
				}
				else{
					firstDiapasoneApex = GetApex(false, startBar, firstDiapasonEnd);
					thirtDiapasoneApex = GetApex(true, thirtDiapasoneStart, endBar);
					
					firstLevelPrice = GetLowOrHighPriceOfBar(false, startBar, firstDiapasonEnd);
					secondLevelPrice = GetLowOrHighPriceOfBar(false, firstDiapasonEnd, thirtDiapasoneStart);
					thirdLevelPrice = GetLowOrHighPriceOfBar(true, thirtDiapasoneStart, endBar);
				}
				firstLevelPrice = GetAverageBetweenLevel(firstDiapasoneApex, firstLevelPrice, FirstDiapasonPriceDifferent);
				thirdLevelPrice = GetAverageBetweenLevel(thirtDiapasoneApex, thirdLevelPrice, ThirtDiapasonPriceDifferent);
			}

			
			//Print("firstLevelPrice -> " + firstLevelPrice);
			DrawHorizontalLine("firstLevelPrice", firstLevelPrice, Color.Red);
			
			//Print("secondLevelPrice -> " + secondLevelPrice);
			DrawHorizontalLine("secondLevelPrice", secondLevelPrice, Color.Gold);
			
			//Print("thirdLevelPrice -> " + thirdLevelPrice);
			DrawHorizontalLine("thirdLevelPrice", thirdLevelPrice, Color.Green);
			
		}
		
		private double GetAverageBetweenLevel(double apex, double level, double priceDifferent){
			if(Math.Abs(apex - level) > priceDifferent)
				return Math.Round((apex + level) / 2, 1);
			else
				return level;
		}
	
	
		private int GetProcentValue(double value, int procent){
			return Convert.ToInt32((value/100) * procent);
		}
	
		private double GetApex(bool isFoundLowPriceOnBar, int startBar, int endBar){
			
			double low = Low[CurrentBar - startBar];
			double high = High[CurrentBar - startBar];
			int start = CurrentBar - startBar;
			int end = CurrentBar - endBar;

			for(;start > end; start--){
				if(isFoundLowPriceOnBar){
					if(low > Low[start]){
						low = Low[start];
					}
				}
				else{
					if(high < High[start]){
						high = High[start];
					}
				}
			}
			
			if(isFoundLowPriceOnBar)
				return low;
			else
				return high;
		}
		
	
		private double GetLowOrHighPriceOfBar(bool isFoundLowPriceOnBar, int startBar, int endBar){

			double low = Low[CurrentBar - startBar];
			double high = High[CurrentBar - startBar];
			int start = CurrentBar - startBar;
			int end = CurrentBar - endBar;
			//Print("Start -> " + start);
			//Print("End -> " + end);
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
		
		
		private void RSIUpdateOnBar(){
			if (CurrentBar == 0)
			{
				down.Set(0);
				up.Set(0);

                if (Period < 3)
                    //Avg.Set(50);
				return;
			}

			down.Set(Math.Max(Input[1] - Input[0], 0));
			up.Set(Math.Max(Input[0] - Input[1], 0));

			if ((CurrentBar + 1) < Period) 
			{
				if ((CurrentBar + 1) == (Period - 1))
					//Avg.Set(50);
				return;
			}

			if ((CurrentBar + 1) == Period) 
			{
				// First averages 
				avgDown.Set(SMA(down, Period)[0]);
				avgUp.Set(SMA(up, Period)[0]);
			}  
			else 
			{
				// Rest of averages are smoothed
				avgDown.Set((avgDown[1] * (Period - 1) + down[0]) / Period);
				avgUp.Set((avgUp[1] * (Period - 1) + up[0]) / Period);
			}

		 	rsi	  = avgDown[0] == 0 ? 100 : 100 - 100 / (1 + avgUp[0] / avgDown[0]);
			rsiAvg = (2.0 / (1 + Smooth)) * rsi + (1 - (2.0 / (1 + Smooth))) * rsiAvg;
			
			if((rsiAvg < 70 && rsi < 70) && (rsiAvg > 30 && rsi > 30)){
				isCanSellRSI = false;
				isCanBuyRSI = false;
			}
			else if(rsiAvg > 70 && rsi > 70){
				isCanSellRSI = true;
				isCanBuyRSI = false;
			}
			else if(rsiAvg < 30 && rsi < 30){
				isCanSellRSI = false;
				isCanBuyRSI = true;
			}

			//Avg.Set(rsiAvg);
			//Value.Set(rsi);
		}
		
		
		
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

			//Print("--------------");
			
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
						lowBarPeriod = lowBar;
						highBar = CurrentBar;
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
						highBarPeriod = highBar;
						lowBar = CurrentBar;
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

		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			
			
		}

		
		
       #region Properties	
		

				
		[Description("Установить нижний уровень RSA")]
		[GridCategory("_RSI")]
		public int LowRSI
		{
			get{return _lowRSI;}
			set{_lowRSI = value;}
		}
		
		[Description("Установить нижний уровень RSA")]
		[GridCategory("_RSI")]
		public int HighRSI
		{
			get{return _highRSI;}
			set{_highRSI = value;}
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

		
		
		[Description("Установить период подсчёта")]
        [GridCategory("_RSI")]
        public int Period
        {
            get { return _period; }
            set { _period = Math.Max(1, value); }
        }
	
		[Description("Установить степень сглаживания. Чем меньше тем ближе линии друг к другу.")]
		[GridCategory("_RSI")]
		public int Smooth
		{
			get { return smooth; }
			set { smooth = Math.Max(1, value); }
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
        public AqueGenNinjaBot AqueGenNinjaBot(DeviationType deviationType, double deviationValue, int highRSI, int leftZigZag, int lowRSI, int period, int rightZigZag, int smooth, bool useHighLow)
        {
            return AqueGenNinjaBot(Input, deviationType, deviationValue, highRSI, leftZigZag, lowRSI, period, rightZigZag, smooth, useHighLow);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public AqueGenNinjaBot AqueGenNinjaBot(Data.IDataSeries input, DeviationType deviationType, double deviationValue, int highRSI, int leftZigZag, int lowRSI, int period, int rightZigZag, int smooth, bool useHighLow)
        {
            if (cacheAqueGenNinjaBot != null)
                for (int idx = 0; idx < cacheAqueGenNinjaBot.Length; idx++)
                    if (cacheAqueGenNinjaBot[idx].DeviationType == deviationType && Math.Abs(cacheAqueGenNinjaBot[idx].DeviationValue - deviationValue) <= double.Epsilon && cacheAqueGenNinjaBot[idx].HighRSI == highRSI && cacheAqueGenNinjaBot[idx].LeftZigZag == leftZigZag && cacheAqueGenNinjaBot[idx].LowRSI == lowRSI && cacheAqueGenNinjaBot[idx].Period == period && cacheAqueGenNinjaBot[idx].RightZigZag == rightZigZag && cacheAqueGenNinjaBot[idx].Smooth == smooth && cacheAqueGenNinjaBot[idx].UseHighLow == useHighLow && cacheAqueGenNinjaBot[idx].EqualsInput(input))
                        return cacheAqueGenNinjaBot[idx];

            lock (checkAqueGenNinjaBot)
            {
                checkAqueGenNinjaBot.DeviationType = deviationType;
                deviationType = checkAqueGenNinjaBot.DeviationType;
                checkAqueGenNinjaBot.DeviationValue = deviationValue;
                deviationValue = checkAqueGenNinjaBot.DeviationValue;
                checkAqueGenNinjaBot.HighRSI = highRSI;
                highRSI = checkAqueGenNinjaBot.HighRSI;
                checkAqueGenNinjaBot.LeftZigZag = leftZigZag;
                leftZigZag = checkAqueGenNinjaBot.LeftZigZag;
                checkAqueGenNinjaBot.LowRSI = lowRSI;
                lowRSI = checkAqueGenNinjaBot.LowRSI;
                checkAqueGenNinjaBot.Period = period;
                period = checkAqueGenNinjaBot.Period;
                checkAqueGenNinjaBot.RightZigZag = rightZigZag;
                rightZigZag = checkAqueGenNinjaBot.RightZigZag;
                checkAqueGenNinjaBot.Smooth = smooth;
                smooth = checkAqueGenNinjaBot.Smooth;
                checkAqueGenNinjaBot.UseHighLow = useHighLow;
                useHighLow = checkAqueGenNinjaBot.UseHighLow;

                if (cacheAqueGenNinjaBot != null)
                    for (int idx = 0; idx < cacheAqueGenNinjaBot.Length; idx++)
                        if (cacheAqueGenNinjaBot[idx].DeviationType == deviationType && Math.Abs(cacheAqueGenNinjaBot[idx].DeviationValue - deviationValue) <= double.Epsilon && cacheAqueGenNinjaBot[idx].HighRSI == highRSI && cacheAqueGenNinjaBot[idx].LeftZigZag == leftZigZag && cacheAqueGenNinjaBot[idx].LowRSI == lowRSI && cacheAqueGenNinjaBot[idx].Period == period && cacheAqueGenNinjaBot[idx].RightZigZag == rightZigZag && cacheAqueGenNinjaBot[idx].Smooth == smooth && cacheAqueGenNinjaBot[idx].UseHighLow == useHighLow && cacheAqueGenNinjaBot[idx].EqualsInput(input))
                            return cacheAqueGenNinjaBot[idx];

                AqueGenNinjaBot indicator = new AqueGenNinjaBot();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.DeviationType = deviationType;
                indicator.DeviationValue = deviationValue;
                indicator.HighRSI = highRSI;
                indicator.LeftZigZag = leftZigZag;
                indicator.LowRSI = lowRSI;
                indicator.Period = period;
                indicator.RightZigZag = rightZigZag;
                indicator.Smooth = smooth;
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
        public Indicator.AqueGenNinjaBot AqueGenNinjaBot(DeviationType deviationType, double deviationValue, int highRSI, int leftZigZag, int lowRSI, int period, int rightZigZag, int smooth, bool useHighLow)
        {
            return _indicator.AqueGenNinjaBot(Input, deviationType, deviationValue, highRSI, leftZigZag, lowRSI, period, rightZigZag, smooth, useHighLow);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.AqueGenNinjaBot AqueGenNinjaBot(Data.IDataSeries input, DeviationType deviationType, double deviationValue, int highRSI, int leftZigZag, int lowRSI, int period, int rightZigZag, int smooth, bool useHighLow)
        {
            return _indicator.AqueGenNinjaBot(input, deviationType, deviationValue, highRSI, leftZigZag, lowRSI, period, rightZigZag, smooth, useHighLow);
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
        public Indicator.AqueGenNinjaBot AqueGenNinjaBot(DeviationType deviationType, double deviationValue, int highRSI, int leftZigZag, int lowRSI, int period, int rightZigZag, int smooth, bool useHighLow)
        {
            return _indicator.AqueGenNinjaBot(Input, deviationType, deviationValue, highRSI, leftZigZag, lowRSI, period, rightZigZag, smooth, useHighLow);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.AqueGenNinjaBot AqueGenNinjaBot(Data.IDataSeries input, DeviationType deviationType, double deviationValue, int highRSI, int leftZigZag, int lowRSI, int period, int rightZigZag, int smooth, bool useHighLow)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.AqueGenNinjaBot(input, deviationType, deviationValue, highRSI, leftZigZag, lowRSI, period, rightZigZag, smooth, useHighLow);
        }
    }
}
#endregion
