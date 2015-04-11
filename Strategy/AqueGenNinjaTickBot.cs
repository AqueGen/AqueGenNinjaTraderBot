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
    public class AqueGenNinjaTickBot : Strategy
    {
        //#region Variables
        // Wizard generated variables
        private int myInput0 = 1; // Default setting for MyInput0
		

		private double _numLow = 0;
		private double _numHigh = 0;
		private double _price = 0;

		private int _period = 20;
		private bool _isLongTrend = true;
		private bool _isLongTrendPreLast = true;
		private bool _isLongTrendStartOrder;
		
		
		private int _waitBarsAfterChangeTrend = 5;
		private int _waitBarsAfterCloseOrder = 10;
      	private bool _isActivateTimeOut = false;
		
      	private int indexBarAfterCloseOrder = 0;
		private int indexBarAfterChangeTrend = 0;
      
		
        private int 	strength 		= 5; 		// Default setting for Strength


		private bool isOrderPresent = false;
		
		private double startOrderPrice = 0;
		
		private string	atmStrategyId		= string.Empty;
		private string	orderId				= string.Empty;
		
		private string openedOrderId;
		
		private bool isFoundTrend = false;
		
      	private int barIndex = 0;
      	private int singleBarIndex = 0;
      
		private int		fast	= 5;
		private int		slow	= 30;
		
		private bool isCanLongOrder = false;
		private bool isCanShortOrder = false;


        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {

            CalculateOnBarClose = true;
			Add(WMA(Fast));
            Add(WMA(Slow));	
			WMA(Fast).Plots[0].Pen.Color = Color.Green;
			WMA(Slow).Plots[0].Pen.Color = Color.Red;

        }

        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		
		
        protected override void OnBarUpdate()
        {	

			barIndex++;
			Print("------------------");
			
          	//CalculateOnBarClose = false;
			double fastPrice = GetWmaPrice(Fast);
            double slowPrice = GetWmaPrice(Slow);
          	//CalculateOnBarClose = true;
          
			if(barIndex % Period == 0){
				if(fastPrice > -1 && slowPrice > -1){
					if(fastPrice > slowPrice){
						_isLongTrendPreLast = IsLongTrend;
						IsLongTrend = true;
						isFoundTrend = true;
					}
					else{
						_isLongTrendPreLast = IsLongTrend;
						IsLongTrend = false;
						isFoundTrend = true;
					}
					Print("_isLongTrendPreLast -> " + _isLongTrendPreLast);
					Print("IsLongTrend -> " + IsLongTrend);
					isCanLongOrder = true;
					isCanShortOrder = true;
				}
				SetLowAndHigh();
			}
          	
			Print("Trend is -> " + IsLongTrend);
			Print("Now Bar is -> " + barIndex);
			
			
        }
		
		private double GetWmaPrice(int period){
		
			if (CurrentBar == 0) {
				//Value.Set(Input[0]);
			}
			else {
				int		back	= Math.Min(period - 1, CurrentBar);
				double	val		= 0;
				int		weight	= 0;
				for (int idx = back; idx >=0; idx--)
				{
					val		+= (idx + 1) * Input[back - idx];
					weight	+= (idx + 1);
				}
				Print(period + " <--> " + (val / weight));
				return (val / weight);
			}
			Print(period + " <--> -1");
			return -1;
		}
		
		private void SetLowAndHigh(){
			NumLow = Low[Period];
			NumHigh = High[Period];
			for(int bar = 0; bar < Period - 1; bar++){
				if(NumLow < Low[bar]){
					NumLow = Low[bar];
					DrawLine("tagNumLow", false, bar, _numLow, -1, _numLow, Color.Green, DashStyle.Solid, 2);
				}
				if(NumHigh > High[bar]){
					NumHigh = High[bar];
					DrawLine("tagNumHigh", false, bar, _numHigh, -1, _numHigh, Color.Red, DashStyle.Solid, 2);
				}
			}
		}
		
		
		
		protected override void OnMarketData(MarketDataEventArgs e)
		{
			if (e.MarketDataType == MarketDataType.Last) {
				Price = e.Price;
								
				BuyOrSell(Price);
			}
		}
		

		private bool IsActivateTimeOutAfterCloseOrder(double startOrderPrice, double price, bool isLongTrend, bool isLongTrendStartOrder){
			if(isLongTrend && isLongTrendStartOrder){
				if(price < startOrderPrice)
					return true;
				else
					return false;
			}
			else
			if(!isLongTrend && !isLongTrendStartOrder){
				if(price > startOrderPrice)
					return true;
				else
					return false;
			}
			return false;
		}
		
		private bool IsActivateTimeOutAfterChangeTrend(bool isLongTrend, bool isLongTrendPreLast){
			return isLongTrend != isLongTrendPreLast;
		}
			
		
		private void BuyOrSell(double price){

			
			if (orderId.Length == 0 && atmStrategyId.Length == 0)
			{

				if(IsActivateTimeOut == true /*&& isOrderPresent == true*/ && IsActivateTimeOutAfterChangeTrend(IsLongTrend, _isLongTrendPreLast)){
					indexBarAfterChangeTrend = barIndex + _waitBarsAfterChangeTrend;
					//isOrderPresent = false;
				}
				
				if(IsActivateTimeOut == true /*&& GetAtmStrategyMarketPosition(atmStrategyId) == MarketPosition.Flat */
					&& isOrderPresent == true && IsActivateTimeOutAfterCloseOrder(startOrderPrice, price, IsLongTrend, _isLongTrendStartOrder)){
					
					indexBarAfterCloseOrder = barIndex + _waitBarsAfterCloseOrder;
					isOrderPresent = false;
				}
				
					Print("Now Bar is -> " + barIndex);
					Print("TimeOut Change Trend New Next Order Time is -> " + indexBarAfterChangeTrend);
					Print("TimeOut Close Order New Next Order Time is -> " + indexBarAfterCloseOrder);
					
				if(isFoundTrend && barIndex > indexBarAfterCloseOrder && barIndex > indexBarAfterChangeTrend){

					if(!IsLongTrend){
						if(price > NumLow && isCanShortOrder == true){// upper then green line	
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Sell, OrderType.Market, 0, 0, TimeInForce.Day, orderId, "AqueGenNinjaStrategy", atmStrategyId);
							Print("OrderAction.Sell");
							_isLongTrendStartOrder = IsLongTrend;
							startOrderPrice = price;
							openedOrderId = orderId;
							isOrderPresent = true;
							
							isCanShortOrder = false;
						}
					}
					else 
					if(IsLongTrend){
						if(price < NumHigh && isCanLongOrder == true){
							atmStrategyId = GetAtmStrategyUniqueId();
							orderId = GetAtmStrategyUniqueId();
							AtmStrategyCreate(OrderAction.Buy, OrderType.Market, 0, 0, TimeInForce.Day, orderId, "AqueGenNinjaStrategy", atmStrategyId);
							Print("OrderAction.Buy");
							_isLongTrendStartOrder = IsLongTrend;
							startOrderPrice = price;
							openedOrderId = orderId;
							isOrderPresent = true;
							
							isCanLongOrder = false;
						}
					}
				}
			
			}
			
			if (orderId.Length > 0)
			{
				string[] status = GetAtmStrategyEntryOrderStatus(orderId);
                
				// If the status call can't find the order specified, the return array length will be zero otherwise it will hold elements
				if (status.GetLength(0) > 0)
				{
					// Print out some information about the order to the output window
					Print("The entry order average fill price is: " + status[0]);
					Print("The entry order filled amount is: " + status[1]);
					Print("The entry order order state is: " + status[2]);

					// If the order state is terminal, reset the order id value
					if (status[2] == "Filled" || status[2] == "Cancelled" || status[2] == "Rejected")
						orderId = string.Empty;
				}
			} // If the strategy has terminated reset the strategy id
			else if (atmStrategyId.Length > 0 && GetAtmStrategyMarketPosition(atmStrategyId) == Cbi.MarketPosition.Flat)
				atmStrategyId = string.Empty;
				
		}
		
		
        #region Properties	
		
		[Description("Period for fast MA")]
		[GridCategory("Parameters")]
		public int Fast
		{
			get { return fast; }
			set { fast = Math.Max(1, value); }
		}

		[Description("Period for slow MA")]
		[GridCategory("Parameters")]
		public int Slow
		{
			get { return slow; }
			set { slow = Math.Max(1, value); }
		}

		
		
		[Description("Number of bars used  for calculations")]
        [GridCategory("Parameters")]
        public int Period
        {
            get { return _period; }
            set { _period = Math.Max(1, value); }
        }
	
		
		
 		[Description("Number of bars to wait after each trade")]
        [Category("Parameters")]
        public int WaitBarsAfterCloseOrder 
        {
           get{return _waitBarsAfterCloseOrder;}
           set{_waitBarsAfterCloseOrder = value;}
        }
		
		[Description("Number of bars to wait after each trade")]
        [Category("Parameters")]
        public int WaitBarsAfterChangeTrend 
        {
           get{return _waitBarsAfterChangeTrend;}
           set{_waitBarsAfterChangeTrend = value;}
        }
		
      
        [Description("Is wait after each trade")]
        [Category("Parameters")]
        public bool IsActivateTimeOut 
        {
          get{return _isActivateTimeOut;}
          set{_isActivateTimeOut = value;}
        }
       
		//[Description("Is wait after each trade")]
       // [Category("Parameters")]
       // public int PariodForChangeTrend 
       // {
       //   get; set;
       // }
		
		
		
        private bool IsLongTrend
        {
            get { return _isLongTrend; }
            set { _isLongTrend = value; }
        }
		
		private double NumLow {
			get{return _numLow;} 
			set{_numLow = value;}
		}
			
		private double NumHigh {
			get{return _numHigh;} 
			set{_numHigh = value;}
		}
		
		private double Price {
			get{return _price;} 
			set{_price = value;}
		}
		
		
		
        #endregion
    }
}
