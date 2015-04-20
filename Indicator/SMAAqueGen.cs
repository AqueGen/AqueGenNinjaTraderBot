// 
// Copyright (C) 2006, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//

#region Using declarations
using System;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.ComponentModel;
using System.Xml.Serialization;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
	/// <summary>
	/// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
	/// </summary>
	[Description("The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.")]
	public class SMAAqueGen : Indicator
	{
		#region Variables
		private int		smaPeriod	= 300;
		
		private double middleValot = 0;
		private int dayOfSMAValot = 5;
		
		private DataSeries					up;
		private DataSeries					down;
		
		#endregion

		/// <summary>
		/// This method is used to configure the indicator and is called once before any bar data is loaded.
		/// </summary>
		protected override void Initialize()
		{
			Add(new Plot(Color.Orange, "SMA"));
			
			Overlay = true;
			
			Add(new Plot(Color.Orange, "Up"));
			Add(new Plot(Color.Green, "Down"));
			
			up		= new DataSeries(this);
			down	= new DataSeries(this);
			
			
			Add(PeriodType.Day, 1);
		}

		/// <summary>
		/// Called on each bar update event (incoming tick).
		/// </summary>
		protected override void OnBarUpdate()
		{
			if(BarsInProgress == 0){
				if (CurrentBar == 0)
					Value.Set(Input[0]);
				else
				{
					double last = Value[1] * Math.Min(CurrentBar, SMAPeriod);

					if (CurrentBar >= SMAPeriod)
						Value.Set((last + Input[0] - Input[SMAPeriod]) / Math.Min(CurrentBar, SMAPeriod));
					else
						Value.Set((last + Input[0]) / (Math.Min(CurrentBar, SMAPeriod) + 1));
					
					up.Set(Value[0] + middleValot / 2);
					down.Set(Value[0] - middleValot / 2);
				}
			}
			if(BarsInProgress == 1){
				middleValot = 0;
				
				for(int i = 0; i < DayOfSMAValot; i++){
					middleValot = middleValot + Highs[2][i] - Lows[2][i];
				}
				middleValot = middleValot / DayOfSMAValot;
				Print("Day middleValot " + middleValot);	
			}
			
		}

		#region Properties
		/// <summary>
		/// </summary>
		
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
		
        #endregion
	}
}

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private SMAAqueGen[] cacheSMAAqueGen = null;

        private static SMAAqueGen checkSMAAqueGen = new SMAAqueGen();

        /// <summary>
        /// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
        /// </summary>
        /// <returns></returns>
        public SMAAqueGen SMAAqueGen(int dayOfSMAValot, int sMAPeriod)
        {
            return SMAAqueGen(Input, dayOfSMAValot, sMAPeriod);
        }

        /// <summary>
        /// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
        /// </summary>
        /// <returns></returns>
        public SMAAqueGen SMAAqueGen(Data.IDataSeries input, int dayOfSMAValot, int sMAPeriod)
        {
            if (cacheSMAAqueGen != null)
                for (int idx = 0; idx < cacheSMAAqueGen.Length; idx++)
                    if (cacheSMAAqueGen[idx].DayOfSMAValot == dayOfSMAValot && cacheSMAAqueGen[idx].SMAPeriod == sMAPeriod && cacheSMAAqueGen[idx].EqualsInput(input))
                        return cacheSMAAqueGen[idx];

            lock (checkSMAAqueGen)
            {
                checkSMAAqueGen.DayOfSMAValot = dayOfSMAValot;
                dayOfSMAValot = checkSMAAqueGen.DayOfSMAValot;
                checkSMAAqueGen.SMAPeriod = sMAPeriod;
                sMAPeriod = checkSMAAqueGen.SMAPeriod;

                if (cacheSMAAqueGen != null)
                    for (int idx = 0; idx < cacheSMAAqueGen.Length; idx++)
                        if (cacheSMAAqueGen[idx].DayOfSMAValot == dayOfSMAValot && cacheSMAAqueGen[idx].SMAPeriod == sMAPeriod && cacheSMAAqueGen[idx].EqualsInput(input))
                            return cacheSMAAqueGen[idx];

                SMAAqueGen indicator = new SMAAqueGen();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.DayOfSMAValot = dayOfSMAValot;
                indicator.SMAPeriod = sMAPeriod;
                Indicators.Add(indicator);
                indicator.SetUp();

                SMAAqueGen[] tmp = new SMAAqueGen[cacheSMAAqueGen == null ? 1 : cacheSMAAqueGen.Length + 1];
                if (cacheSMAAqueGen != null)
                    cacheSMAAqueGen.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheSMAAqueGen = tmp;
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
        /// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.SMAAqueGen SMAAqueGen(int dayOfSMAValot, int sMAPeriod)
        {
            return _indicator.SMAAqueGen(Input, dayOfSMAValot, sMAPeriod);
        }

        /// <summary>
        /// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
        /// </summary>
        /// <returns></returns>
        public Indicator.SMAAqueGen SMAAqueGen(Data.IDataSeries input, int dayOfSMAValot, int sMAPeriod)
        {
            return _indicator.SMAAqueGen(input, dayOfSMAValot, sMAPeriod);
        }
    }
}

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    public partial class Strategy : StrategyBase
    {
        /// <summary>
        /// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
        /// </summary>
        /// <returns></returns>
        [Gui.Design.WizardCondition("Indicator")]
        public Indicator.SMAAqueGen SMAAqueGen(int dayOfSMAValot, int sMAPeriod)
        {
            return _indicator.SMAAqueGen(Input, dayOfSMAValot, sMAPeriod);
        }

        /// <summary>
        /// The SMA (Simple Moving Average) is an indicator that shows the average value of a security's price over a period of time.
        /// </summary>
        /// <returns></returns>
        public Indicator.SMAAqueGen SMAAqueGen(Data.IDataSeries input, int dayOfSMAValot, int sMAPeriod)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.SMAAqueGen(input, dayOfSMAValot, sMAPeriod);
        }
    }
}
#endregion
