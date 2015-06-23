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
using System.Collections.Generic;

#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    [Description("Enter the description of your strategy here")]
    public class AGZigZagOrders : Strategy
    {
  

        private int myInput0 = 1; // Default setting for MyInput0
  
		
		//zigzag
		private double			currentZigZagHigh	= 0;
		private double			currentZigZagLow	= 0;
		private DeviationType	deviationType		= DeviationType.Percent;
		private double			deviationValue		= 0.8;
		private DataSeries		_zigZagHighZigZags; 
		private DataSeries		_zigZagLowZigZags; 
		private DataSeries		_zigZagHighSeries; 
		private DataSeries		_zigZagLowSeries; 
		private int				lastSwingIdx		= -1;
		private double			lastSwingPrice		= 0.0;
		private int				trendDir			= 0; // 1 = trend up, -1 = trend down, init = 0
		private bool			useHighLow			= false;
      

		private int highBar = 0;
		private int lowBar = 0;
		
		private int highBarPeriod = 0;
		private int lowBarPeriod = 0;
		private int lastHighBarPeriod = 0;
		private int lastLowBarPeriod = 0;
		
		private double highZigZagPrice = 0;
		private double lowZigZagPrice = 0;

		private bool isChangePeriod = false;
		private Trend zigZagTrend;
		
		private ZigZag zigZagObject1;
		private ZigZag zigZagObject2;
		public enum Trend
		{
			UP,
			DOWN
		}
		private bool isSameZigZagApex = false;
		private double _stopLoss = 0;
		private double _takeProfit = 0;
		
		private bool _isBuyOrder = false;
		
		private double startOrderPrice = 0;
		
		private bool isCanEnterOrder = false;
		
		private int BarsInDay
		{
			get
			{
				return 60 / MainTimeFrame * 24;
			}
		}
		
		private List<double> _dayVolume = new List<double>();
		
		private double MaxVolumeOfDay = 0;
		private void DayVolumeResize(List<double> list)
		{
			if(list.Count >= BarsInDay)
			{
				list.RemoveAt(0);
			}
		}
		
		private double GetMaxVolumeOfDay(List<double> list)
		{
			double max = 0;
			foreach(double value in list)
			{
				if(max < value)
				{
					max = value;
				}
			}
			return max;
		}
		
		
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
            CalculateOnBarClose = true;
			
			//zigzag
			_zigZagHighSeries	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			_zigZagHighZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			_zigZagLowSeries	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			_zigZagLowZigZags	= new DataSeries(this, MaximumBarsLookBack.Infinite); 
			
			Add(PeriodType.Tick, 1);
			

        }
		
		protected override void OnStartUp()
		{
			zigZagObject1 = new ZigZag(TickSize, _zigZagHighZigZags,_zigZagLowZigZags, _zigZagHighSeries, _zigZagLowSeries, DeviationType1, DeviationValue1, UseHighLow1);
			zigZagObject2 = new ZigZag(TickSize, _zigZagHighZigZags,_zigZagLowZigZags, _zigZagHighSeries, _zigZagLowSeries, DeviationType2, DeviationValue2, UseHighLow2);
			zigZagObject1.IsChangePeriod = false;
			zigZagObject2.IsChangePeriod = false;
			
			_stopLoss = StopLoss * TickSize;
			_takeProfit = StopLoss * TakeProfitIndex * TickSize;
			
		}

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			if(CurrentBars[0] < 10 || CurrentBars[1] < 10)
				return;
			
			if(BarsInProgress == 0)
			{
				zigZagObject1.UpdateOnBar(this, CurrentBar, High, Low, Input[0], Input);
				zigZagObject2.UpdateOnBar(this, CurrentBar, High, Low, Input[0], Input);
				
				_dayVolume.Add(VOL()[0]);
				DayVolumeResize(_dayVolume);
				MaxVolumeOfDay = GetMaxVolumeOfDay(_dayVolume);
				
				

				
				
				
				
				if(zigZagObject1.LastBarPosition == 0)
					return;
					
					
				double crossZigZagsVolume = VOL()[CurrentBar - zigZagObject1.LastBarPosition + 1];
				
				if(zigZagObject1.LastPricePosition == zigZagObject2.HighZigZagPrice || zigZagObject1.LastPricePosition == zigZagObject2.LowZigZagPrice)
				{
					if(zigZagObject2.IsChangePeriod)
					{
						zigZagObject2.IsChangePeriod = false;
						
						
						Print("--------------");
						Print(Time[0]);
						Print("CurrentBar -> " + CurrentBars[0]);
						Print("zigZagObject1 LastPosition-> " + zigZagObject1.LastPricePosition);
						Print("zigZagObject2 HighZigZagPrice-> " + zigZagObject2.HighZigZagPrice);
						Print("zigZagObject1 LastPosition-> " + zigZagObject1.LastPricePosition);
						Print("zigZagObject2 LowZigZagPrice-> " + zigZagObject2.LowZigZagPrice);
						
						Print("zigZagObject1.LastBarPosition  -> " + zigZagObject1.LastBarPosition);
						
						Print("MaxVolumeOfDay -> " + MaxVolumeOfDay);
						Print("crossZigZagsVolume -> " + crossZigZagsVolume);
						if(MaxVolumeOfDay < crossZigZagsVolume)
						{
							isCanEnterOrder = true;
						}
						
					}
				}
				
				
			}
			else if(BarsInProgress == 1)
			{
				if(isCanEnterOrder)
				{
					double price = Opens[1][0];
					if(Position.MarketPosition == MarketPosition.Flat)
					{
						if(zigZagObject2.IsTrendDown == -1)
						{
							_isBuyOrder = false;
							startOrderPrice = Opens[1][0];
							EnterShort("ShortOrder");
						}
						else if(zigZagObject2.IsTrendDown == 1)
						{
							_isBuyOrder = true;
							startOrderPrice = Opens[1][0];
							EnterLong("LongOrder");
						}
					}
					else
					{
						if(_isBuyOrder)
						{
							if((price < startOrderPrice - _stopLoss) || (price > startOrderPrice + _takeProfit))
							{
								ExitLong("LongOrder");
								isCanEnterOrder = false;
							}
						}
						else
						{
							if((price > startOrderPrice + _stopLoss) || (price < startOrderPrice - _takeProfit))
							{
								ExitShort("ShortOrder");
								isCanEnterOrder = false;
							}
						}
					}
				}
			}
        }
		
		
		
		

		
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
			
			
			public int IsTrendDown
			{
				get
				{
					return _trendDir;
				}
			}
			
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
			
			public double LastPricePosition
			{get; private set;}
			
			public int LastBarPosition
			{get; private set;}
			
			
			public bool IsChangePeriod
			{get; set;}
			
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
			
			public void UpdateOnBar(AGZigZagOrders strategy, int currentBar, IDataSeries highSeries, IDataSeries lowSeries, double input0, IDataSeries input)
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
				}	
				else if (_trendDir >= 0 && isSwingLow && isOverLowDeviation)
				{	
					saveValue	= lowSeries[1];
					addLow		= true;
					_trendDir	= -1;
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
						
						if (!_useHighLow){
							LastPricePosition = strategy.Close[currentBar - LowBarPeriod + 1];
							LastBarPosition = currentBar;
						}
						else
						{
							LastPricePosition = strategy.Low[currentBar - LowBarPeriod + 1];
							LastBarPosition = currentBar;
						}
						
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
						
						if (!_useHighLow){
							LastPricePosition = strategy.Close[currentBar - HighBarPeriod + 1];
							LastBarPosition = currentBar;
						}
						else
						{
							LastPricePosition = strategy.High[currentBar - HighBarPeriod + 1];
							LastBarPosition = currentBar;
						}
						
						
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
					LastPricePosition = _lastSwingPrice;
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
		
		
		[Description("Deviation in percent or points regarding on the deviation type")]
        [GridCategory("ZigZag1")]
		[Gui.Design.DisplayName("Deviation value")]
        public double DeviationValue1
        {get;set;}

        [Description("Type of the deviation value")]
        [GridCategory("ZigZag1")]
		[Gui.Design.DisplayName("Deviation type")]
        public DeviationType DeviationType1
        {get;set;}

        [Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("ZigZag1")]
		[Gui.Design.DisplayName("Use high and low")]
		[RefreshProperties(RefreshProperties.All)]
        public bool UseHighLow1
        {get;set;}
		
		[Description("Deviation in percent or points regarding on the deviation type")]
        [GridCategory("ZigZag2")]
		[Gui.Design.DisplayName("Deviation value")]
        public double DeviationValue2
        {get;set;}

        [Description("Type of the deviation value")]
        [GridCategory("ZigZag2")]
		[Gui.Design.DisplayName("Deviation type")]
        public DeviationType DeviationType2
        {get;set;}

        [Description("If true, high and low instead of selected price type is used to plot indicator.")]
        [GridCategory("ZigZag2")]
		[Gui.Design.DisplayName("Use high and low")]
		[RefreshProperties(RefreshProperties.All)]
        public bool UseHighLow2
        {get;set;}
		
		[GridCategory("Orders")]
        public int MainTimeFrame
        {get;set;}
		
		[GridCategory("Orders")]
        public int StopLoss
        {get;set;}
		
		[GridCategory("Orders")]
        public int TakeProfitIndex
        {get;set;}
		
		
    }
}
