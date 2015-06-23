//###
//### Universal Renko Bar Type.  
//###	
//### Created: Gaston, June 2012
//###	
using System;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace NinjaTrader.Data
{
    public class UniRenkoBarsType : BarsType
    {
        public UniRenkoBarsType() : base(PeriodType.Custom5) { }

        static bool registered = Register(new UniRenkoBarsType());
        double barOpen;
        double barMax;
        double barMin;
		double fakeOpen=0;

		int    barDirection=0;
		double openOffset=0;
		double trendOffset=0;
		double reversalOffset=0;

		bool   maxExceeded=false;
		bool   minExceeded=false;

		double tickSize=0.01;

        public override void Add(Bars bars, double open, double high, double low, double close, DateTime time, long volume, bool isRealtime)
        {
            	//### First Bar
            if ((bars.Count == 0) || bars.IsNewSession(time, isRealtime))
            {
				tickSize = bars.Instrument.MasterInstrument.TickSize;

                trendOffset    = bars.Period.Value  * bars.Instrument.MasterInstrument.TickSize;
                reversalOffset = bars.Period.Value2 * bars.Instrument.MasterInstrument.TickSize;
				//bars.Period.BasePeriodValue = bars.Period.Value;	//### Remove to customize OpenOffset
				openOffset = Math.Ceiling((double)bars.Period.BasePeriodValue * 1) * bars.Instrument.MasterInstrument.TickSize;

                barOpen = close;
                barMax  = barOpen + (trendOffset * barDirection);
                barMin  = barOpen - (trendOffset * barDirection);

                AddBar(bars, barOpen, barOpen, barOpen, barOpen, time, volume, isRealtime);
            }
            	//### Subsequent Bars
            else
            {
                Data.Bar bar = (Bar)bars.Get(bars.Count - 1);
                maxExceeded  = bars.Instrument.MasterInstrument.Compare(close, barMax) > 0 ? true : false;
                minExceeded  = bars.Instrument.MasterInstrument.Compare(close, barMin) < 0 ? true : false;

                	//### Defined Range Exceeded?
                if ( maxExceeded || minExceeded )
                {
                    double thisClose = maxExceeded ? Math.Min(close, barMax) : minExceeded ? Math.Max(close, barMin) : close;
                    barDirection     = maxExceeded ? 1 : minExceeded ? -1 : 0;
                    fakeOpen = thisClose - (openOffset * barDirection);		//### Fake Open is halfway down the bar

                    	//### Close Current Bar
                    UpdateBar(bars, bar.Open, (maxExceeded ? thisClose : bar.High), (minExceeded ? thisClose : bar.Low), thisClose, time, volume, isRealtime);

                    	//### Add New Bar
					barOpen = close;
					barMax  = thisClose + ((barDirection>0 ? trendOffset : reversalOffset));
					barMin  = thisClose - ((barDirection>0 ? reversalOffset : trendOffset));

					AddBar(bars, fakeOpen, (maxExceeded ? thisClose : fakeOpen), (minExceeded ? thisClose : fakeOpen), thisClose, time, volume, isRealtime);
                }
                	//### Current Bar Still Developing
                else
                {
                    UpdateBar(bars, bar.Open, (close > bar.High ? close : bar.High), (close < bar.Low ? close : bar.Low), close, time, volume, isRealtime);
                }
            }

            bars.LastPrice = close;
        }

        public override PropertyDescriptorCollection GetProperties(PropertyDescriptor propertyDescriptor, Period period, Attribute[] attributes)
        {
            PropertyDescriptorCollection properties = base.GetProperties(propertyDescriptor, period, attributes);

            properties.Remove(properties.Find("BasePeriodType",  true));
            properties.Remove(properties.Find("PointAndFigurePriceType", true));
            properties.Remove(properties.Find("ReversalType", true));
            //properties.Remove(properties.Find("BasePeriodValue", true));	//### Remove to customize OpenOffset

            Gui.Design.DisplayNameAttribute.SetDisplayName(properties, "Value2", "\r\rTick Reversal");
            Gui.Design.DisplayNameAttribute.SetDisplayName(properties, "Value",  "\r\rTick \rTrend");
            Gui.Design.DisplayNameAttribute.SetDisplayName(properties, "BasePeriodValue",  "\r\rOpen Offset");

            return properties;
        }

        public override void ApplyDefaults(Gui.Chart.BarsData barsData)
        {
            barsData.Period.Value  = 2;				//### Trend    Value
            barsData.Period.Value2 = 4;				//### Reversal Value
            barsData.Period.BasePeriodValue = 2;	//### Open Offset Value
            barsData.DaysBack      = 2;
        }

        public override string ToString(Period period)
        {
			//return period.Value + " UniRenko T" +period.Value +"R" +period.Value2;	//### Remove for OpenOffset
			return period.Value + " UniRenko T" +period.Value +"R" +period.Value2 +"O" +period.BasePeriodValue;
        }

        public override PeriodType BuiltFrom
        {
            get { return PeriodType.Tick; }
        }

        public override string ChartDataBoxDate(DateTime time)
        {
            return time.ToString(Cbi.Globals.CurrentCulture.DateTimeFormat.ShortDatePattern);
        }

        public override string ChartLabel(Gui.Chart.ChartControl chartControl, DateTime time)
        {
            return time.ToString(chartControl.LabelFormatTick, Cbi.Globals.CurrentCulture);
        }

        public override object Clone()
        {
            return new UniRenkoBarsType();
        }

        public override int GetInitialLookBackDays(Period period, int barsBack)
        {
            return new RangeBarsType().GetInitialLookBackDays(period, barsBack);
        }

        public override int DefaultValue
        {
            get { return 4; }
        }

        public override string DisplayName
        {
            get { return "UniRenko"; }
        }

        public override double GetPercentComplete(Bars bars, DateTime now)
        {
          return 0;
        }
		
        public override bool IsIntraday
        {
            get { return true; }
        }
    }
}