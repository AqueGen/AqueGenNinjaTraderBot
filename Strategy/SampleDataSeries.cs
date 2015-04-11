// 
// Copyright (C) 2007, NinjaTrader LLC <www.ninjatrader.com>.
// NinjaTrader reserves the right to modify or overwrite this NinjaScript component with each release.
//
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
using NinjaTrader.Strategy;
#endregion

// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Sample demonstrates how to make DataSeries objects synced to different bar periods.
    /// </summary>
    [Description("Sample demonstrates how to make DataSeries objects synced to different bar periods.")]
    public class SampleDataSeries : Strategy
    {
        #region Variables
		// Declare two DataSeries objects
		private DataSeries primarySeries;
		private DataSeries secondarySeries;
        #endregion

        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
			// Adds a secondary bar object to the strategy.
			Add(PeriodType.Minute, 5);
			
			// Syncs a DataSeries object to the primary bar object
			primarySeries = new DataSeries(this);
			
            // Stop-loss orders are placed 5 ticks below average entry price
			SetStopLoss(CalculationMode.Ticks, 5);
			// Profit target orders are placed 10 ticks above average entry price
			SetProfitTarget(CalculationMode.Ticks, 10);
			
			CalculateOnBarClose = true;
        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
			/* Only need to sync DataSeries objects once. We couldn't do this sync in the Initialize() method because we
			cannot access the BarsArray property there. */
			if (secondarySeries == null)
			{
				/* Syncs another DataSeries object to the secondary bar object.
				We use an arbitrary indicator overloaded with an IDataSeries input to achieve the sync.
				The indicator can be any indicator. The DataSeries will be synced to whatever the
				BarsArray[] is provided.*/
				secondarySeries = new DataSeries(SMA(BarsArray[1], 50));
			}
			
			// Executed on primary bar updates only
			if (BarsInProgress == 0)
			{
				// Set DataSeries object to store the trading range of the primary bar
				primarySeries.Set(Close[0] - Open[0]);
			}
			
			// Executed on secondary bar updates only
			if (BarsInProgress == 1)
			{
				// Set the DataSeries object to store the trading range of the secondary bar
				secondarySeries.Set(Close[0] - Open[0]);
			}
			
			// When both trading ranges of the current bars on both time frames are positive, enter long
			if (primarySeries[0] > 0 && secondarySeries[0] > 0)
				EnterLong();
        }

        #region Properties
        #endregion
    }
}
