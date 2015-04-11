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
using System.Windows;
using System.Linq;
#endregion


// This namespace holds all strategies and is required. Do not change it.
namespace NinjaTrader.Strategy
{
    /// <summary>
    /// Enter the description of your strategy here
    /// </summary>
    /// 

    [Description("Enter the description of your strategy here")]
    public class LevelsStrategy : Strategy
    {
        public const string Version = "2.0.1";
        public const string VerDate = "2012/12/23";
		protected enum StrategyState
		{
            Unknown = -1,
			None = 0,                       //looking for new trades -> trade entry info. 
			OrderFound = 1,                 //trade has been selected (entry, limits and stop is defined), verify we can enter a trade -> place an order
			OrderPlaced = 2,                //order is sitting on the market, verify we want to keep it -> cancel order or do nothing
            OrderCanceling = 3,             //order is currently cancelling
			
            OrderPartialFilled = 4,         //wait for cancel notification -> state None
			PositionOnMarket = 5,           //order executed, position on the market, manage a position -> close a trade
			
            PositionClosing = 6             //wait for trade close -> state None
		};

        struct StrategyParams
        {
            public StrategyState    State;
            public DateTime     StateTime;
            public int          StateBarIdx;

            public OrderAction OrderAction;
            public int Quantity;

            public double LimitPrice;
            public double StopPrice;
            public double TargetPrice;
            public double LossPrice;

            public double High;//Highest bid since we are in position(state)
            public double Low;//Lowest bid sinse we are in position(state)

            public bool BreakevenMode;
            public override string ToString()
            {
                return "{ State: " + State.ToString() + "; Action: " + OrderAction.ToString() +  "; Price: " + LimitPrice.ToString() + "; Target: " + TargetPrice.ToString() + " Stop: " + LossPrice + "; }";
                //return base.ToString();
            }
        }

		#region Variables

		private int mCorrectSupport =0;
		private int mCorrectResistance =0;
		
		private int mLongStopTicks = 10;
		private int mShortStopTicks = 10;
		private int mLongProfitTicks = 70;
		private int mShortProfitTicks = 70;

        private int mLevelMaxWidth = 20; //если ширина уровня больше - делим уровень на 2 или минимизируем до этого значения
        private int mLevelMinWidth = 15; //если ширина уровня меньше - добавляем ширину до этого значения (?)

		
		private double mZigZagDeviation = 0.3;
		private int cnt;
        // User defined variables (add any user defined variables below)
		private IOrder orderEntry = null;
        private IOrder orderExit = null;
        private IOrder orderTarg = null;
        private IOrder orderStop = null;
        private NinjaTrader.Indicator.TrueLevels.trueLevel[] mTrueLevels = null;
		private int mIdxLevelSupport = -1;
		private int mIdxLevelResistance = -1;
        private double mCurSupport = 0;
        private double mCurResistance = 0;
        private DateTime mLevelsUpdTime = DateTime.FromOADate(0);
		
		private int mLevelPrecision = 15;
		private int mTrendPower = 60;

		private double lastVolume = 0;
		private double lastTickVolume = 0;
		
		//private DateTime lastDrawTime;
		private int mTimeFrame;
		private TimeFrameCluster mCurCluster;
        private bool mSAmode = false; //Strategy Analyzer mode
        private StrategyParams mParams;
        #endregion

		
		
        /// <summary>
        /// This method is used to configure the strategy and is called once before any strategy method is called.
        /// </summary>
        protected override void Initialize()
        {
			Add(ZigZag(DeviationType.Points,mZigZagDeviation,false));
			//Print("Initialize");
            CalculateOnBarClose = false;
			mTimeFrame = 60000; //1 minute
            TraceOrders = true;
        }
		
		protected override void OnStartUp() 
		{
			//lastDrawTime = Bars.MarketData.Connection.Now;
			mTimeFrame = Convert.ToInt32(Time[0].Subtract(Time[1]).TotalMilliseconds);
			mCurCluster = new TimeFrameCluster(mTimeFrame,TickSize,2);
			mCurCluster.strategy = this;
		}
		
		#region TimeFrameCluster
		private class TimeFrameCluster
		{
			private struct Tick{
				public double 		Price;
				public int			Volume;
				public DateTime		Time;
			}
			private struct ClusterItem{
				public int		Volume;
				public byte		Power;
			}
			#region ClassVars
			private bool 	mFrozen;
			private long 	mTimeFrame; //timeframe in milliseconds
			
			private double 	mTickSize; //price-delta of 1 tick
			private int 	mClusterTicks; //number of price-levels to group in one cluster
			
			
			private bool 	mbValid;
			
			private Tick[] 	TickArr;
			private int   	lastTickProcessed  = -1; //count of actual elements in the array
			private int   	lastTickRecorded  = -1; //count of actual elements in the array
			private int   	firstTickProcessed  = -1; //count of actual elements in the array
			
			private long 	mLastClusterDrawCnt;
			
			public  StrategyBase 			strategy = null;
			
			private DateTime 				lastClusterCalcTime;
			private SortedDictionary<double,ClusterItem> 	TickCluster;
			private long 	mTotalVolume;
			private double 	mMinPrice;
			private double 	mMaxPrice;
			private int		mMinVolume;
			private int		mMaxVolume;
			
			#endregion
			
			public TimeFrameCluster(): this(100,0.1,2,new DateTime(2000,1,1)){}
			public TimeFrameCluster(int TimeFrame, double TickSize, int ClusterTicks, DateTime ClusterTime){
				TickArr = new Tick[10]; //100K should be enough. if not - we will increase it by 10%
				lastTickProcessed  = -1;
				lastTickRecorded  = -1;
				firstTickProcessed  = -1;
				mTotalVolume = 0;
				
				mTimeFrame = TimeFrame<100?100:TimeFrame;
				mTickSize = TickSize;
				mClusterTicks = ClusterTicks;
				mbValid = false;
				
				TickCluster = new SortedDictionary<double,ClusterItem>();
				
				mLastClusterDrawCnt = 0;
				lastClusterCalcTime = ClusterTime;//new DateTime(2000,1,1);

			}
			public TimeFrameCluster(int TimeFrame, double TickSize, int ClusterTicks)
			{
				TickArr = new Tick[10000]; //100K should be enough. if not - we will increase it by 10%
				lastTickProcessed  = -1;
				lastTickRecorded  = -1;
				firstTickProcessed  = -1;
				mTotalVolume = 0;
				
				mTimeFrame = TimeFrame<100?100:TimeFrame;
				mTickSize = TickSize;
				mClusterTicks = ClusterTicks;
				mbValid = false;
				
				TickCluster = new SortedDictionary<double,ClusterItem>();
				
				mLastClusterDrawCnt = 0;
				lastClusterCalcTime = new DateTime(2000,1,1);
			}
			
			private void CopyCluster(ref TimeFrameCluster toObj){ TimeFrameCluster fromObj = this;  CopyCluster(ref toObj, fromObj, false); }
			private void CopyCluster(ref TimeFrameCluster toObj, TimeFrameCluster fromObj){ CopyCluster(ref toObj, fromObj, false); }
			private void CopyCluster(ref TimeFrameCluster toObj, TimeFrameCluster fromObj, bool bStatic){
				if(toObj == null){
					toObj = new TimeFrameCluster();
				}
				toObj.mTimeFrame = fromObj.mTimeFrame; //timeframe in milliseconds
				
				toObj.mTickSize = fromObj.mTickSize; //price-delta of 1 tick
				toObj.mClusterTicks = fromObj.mClusterTicks; //number of price-levels to group in one cluster
				
				toObj.mTotalVolume = fromObj.mTotalVolume;
				
				toObj.mbValid = fromObj.mbValid;
				
				toObj.lastTickProcessed = fromObj.lastTickProcessed; //count of actual elements in the array
				toObj.lastTickRecorded = fromObj.lastTickRecorded; //count of actual elements in the array
				toObj.firstTickProcessed = fromObj.firstTickProcessed; //count of actual elements in the array
				
				toObj.mLastClusterDrawCnt = fromObj.mLastClusterDrawCnt;
				
				toObj.strategy = fromObj.strategy;
				
				toObj.lastClusterCalcTime = fromObj.lastClusterCalcTime;
				//clone
				
				toObj.TickCluster = new SortedDictionary<double,ClusterItem>();
				foreach(KeyValuePair<double,ClusterItem> kvp in fromObj.TickCluster)
					toObj.TickCluster.Add(kvp.Key,kvp.Value);
				
				toObj.mMinPrice = fromObj.mMinPrice;
				toObj.mMaxPrice = fromObj.mMaxPrice;
				toObj.mMinVolume = fromObj.mMinVolume;
				toObj.mMaxVolume = fromObj.mMaxVolume;
				
				
				if(bStatic){
					toObj.TickArr = new Tick[0];
					toObj.mFrozen = true;
				}else{
					toObj.mFrozen = fromObj.mFrozen;
					toObj.TickArr = (Tick[])fromObj.TickArr.Clone();
				}	
			}
			
			//for static cluster - caller is responsable for time management
			public int AddTickStatic(double Price, int Volume){
				
				//should not be called if cluster is not frozen
				Debug.Assert(mFrozen,"AddTickStatic - should not be called if cluster is not frozen");

				if(Volume==-1){
					CalcClusterProfile();
					return TickCluster.Count;
				}
				
				long t = 0;
				double tt =0;
				ClusterItem ci;
				
				t = (long)Math.Floor(Price / (mTickSize * mClusterTicks));
				tt = t * (mTickSize * mClusterTicks);

				if( TickCluster.ContainsKey(tt) ){
					ci = TickCluster[tt];
					ci.Volume += Volume;
					TickCluster[tt] = ci;
				}else{
					ci.Power = 0;
					ci.Volume = Volume;
					TickCluster.Add(tt,ci);
				}
				t = TickCluster[tt].Volume;
				
				mTotalVolume += Volume;
				
				return TickCluster.Count;

			}
			
			
			//TODO: cluster ticks by 1 tick, then regroup them as needed min = minY max = maxY
			public int AddTick(double Price, int Volume, DateTime When){
				
				//should not be called if cluster is frozen
				Debug.Assert(!mFrozen,"AddTick - should not be called if cluster is frozen");
				if(mFrozen)
					return 0;
				
				if(Volume==-1){
					if(!mbValid)
							strategy.Print("Cluster now is Valid");

					mbValid = true; //manually closed cluster;
					When = TickArr[lastTickRecorded].Time;
					goto recalc_cluster;
				}
				
				int i =0;
				double d;
				//strategy.Print("first:" + firstTickProcessed + " lastProc:" + lastTickProcessed + " lastRec:" + lastTickRecorded);
				//verify if we have enough space in buffer array
				if(TickArr.Length-1<=lastTickRecorded){
					d = firstTickProcessed;
					if(d/TickArr.Length>0.05){//more then 5% of free space at beginning
						//strategy.Print("MoveArray - first:" + firstTickProcessed + ", lastTickRecorded:" + lastTickRecorded + ", lastTickProcessed:" + lastTickProcessed );
						//move all the data to begining of buffer array
						Array.Copy(TickArr,firstTickProcessed,TickArr,0,lastTickRecorded-firstTickProcessed + 1);
						lastTickRecorded-=firstTickProcessed;
						lastTickProcessed-=firstTickProcessed;
						firstTickProcessed=0;
					}else{
						//add 10% at the end of array
						Array.Resize<Tick>(ref TickArr, (int)(TickArr.Length * 1.1 + 10)); // + TickArr%
					}
				}
				
				lastTickRecorded++;
				TickArr[lastTickRecorded].Price = Price;
				TickArr[lastTickRecorded].Time = When;
				TickArr[lastTickRecorded].Volume = Volume;
				
				//if(mMaxPrice==0||mMaxPrice<TickArr[lastTickRecorded].Price)mMaxPrice=TickArr[lastTickRecorded].Price;
				//if(mMinPrice==0||mMinPrice>TickArr[lastTickRecorded].Price)mMinPrice=TickArr[lastTickRecorded].Price;
				
				
				//recalc cluster not on each tick, but each 5 seconds
				//actually, it will be calculated on first tick of each 5 seconds
				//todo: process if some volume accumulated, e.g. 10% of total bar's volume
				if(When.Subtract(lastClusterCalcTime).TotalMilliseconds<5000 && this.Valid) return TickCluster.Count;
				
			recalc_cluster:
				
				//lastClusterCalcTime = TickTimes.Last.Value;
				lastClusterCalcTime = TickArr[lastTickRecorded].Time;
				
				long t = 0;
				double tt =0;
				ClusterItem ci;
				
				for(i=lastTickProcessed+1;i<=lastTickRecorded;i++)
				{
					t = (long)Math.Floor(TickArr[i].Price / (mTickSize * mClusterTicks));
					tt = t * (mTickSize * mClusterTicks);
					if( TickCluster.ContainsKey(tt) ){
						ci = TickCluster[tt];
						ci.Volume += TickArr[i].Volume;
						TickCluster[tt] = ci;
					}else{
						ci.Power = 0;
						ci.Volume = TickArr[i].Volume;
						TickCluster.Add(tt,ci);
					}
					t = TickCluster[tt].Volume;
					
					mTotalVolume += TickArr[i].Volume;
				}
				lastTickProcessed = lastTickRecorded;
				
				if(firstTickProcessed<0)firstTickProcessed = 0;
				
				for(i=firstTickProcessed;i<=lastTickProcessed;i++){
					
					t = (long)When.Subtract(TickArr[i].Time).TotalMilliseconds;
					if(t<=mTimeFrame)
						break;

					t = (long)Math.Floor(TickArr[i].Price / (mTickSize * mClusterTicks));
					tt = t * (mTickSize * mClusterTicks);
					if( TickCluster.ContainsKey(tt) ){
						ci = TickCluster[tt];
						ci.Volume -= TickArr[i].Volume;
						if(ci.Volume>0)
							TickCluster[tt] = ci;
						else
							TickCluster.Remove(tt);
					}else{
						//might be error
						//Print("Error: tick data doesn't exist (1);");
					}
					
					mTotalVolume -= TickArr[i].Volume;
					mbValid = true;
					if(!mbValid)
							strategy.Print("Cluster now is Valid");
				//System.Windows.Forms.MessageBox.Show("mbValid","AddTick");
				}
				
				//now i == number of ticks removed from beginning
				firstTickProcessed += i-firstTickProcessed;
				
				CalcClusterProfile();
				
				return TickCluster.Count;
			}
			
			private void CalcClusterProfile(){
				
				//mark important ticks in the cluster by volume: normalized from 0 to 10
				int i, t;
				ClusterItem ci;

				double RealMaxPrice =0;
				for(i=firstTickProcessed;i<=lastTickProcessed;i++)
					if( RealMaxPrice < TickArr[i].Price )
						RealMaxPrice = TickArr[i].Price;
				strategy.Print(RealMaxPrice);
//				if( RealMaxPrice > 0 && TickCluster.Count > 1 && TickCluster.ContainsKey(RealMaxPrice) ){
//					if(TickCluster.ContainsKey(RealMaxPrice - mTickSize*mClusterTicks)){
//						ci = TickCluster[RealMaxPrice - mTickSize*mClusterTicks];
//						ci.Volume += TickCluster[RealMaxPrice].Volume;
//						TickCluster[RealMaxPrice - mTickSize*mClusterTicks] = ci;
//						TickCluster.Remove(RealMaxPrice);
//					}else{
//						ci = TickCluster[RealMaxPrice];
//						TickCluster.Remove(RealMaxPrice);
//						TickCluster.Add(RealMaxPrice - mTickSize*mClusterTicks,ci);
//					}
//				}

				long midVolume = mTotalVolume/TickCluster.Count;
				mMaxVolume = 0;
				mMinVolume = int.MaxValue;
				mMaxPrice = 0;
				mMinPrice = double.MaxValue;
				
				//todo: учитывать разрыв между min и max
				double[] arr = TickCluster.Keys.ToArray();
				for(i=0;i<arr.Length;i++){
					ci = TickCluster[arr[i]];

					if(mMaxVolume<ci.Volume)	mMaxVolume=ci.Volume;
					if(mMinVolume>ci.Volume)	mMinVolume=ci.Volume;
				//min/max price
					if(mMaxPrice<arr[i]) 		mMaxPrice=arr[i];
					if(mMinPrice>arr[i]) 		mMinPrice=arr[i];
				}
					
				strategy.Print("MinV:" + mMinVolume + " MaxV:" + mMaxVolume + " MinP:" + mMinPrice + " MaxP:" + mMaxPrice );
				
				for(i=0;i<arr.Length;i++){
					ci = TickCluster[arr[i]];
					if(mTotalVolume==0)
						ci.Power = 0;
					else{
						t = ci.Volume - (int)Math.Round(midVolume*0.7);
						if(t<0)t=0;
						ci.Power = (byte)Math.Round((double)t/mMaxVolume*10,0);
					}
						
					TickCluster[arr[i]] = ci;
				}

			}

			public TimeFrameCluster Clone(bool bStatic){
				TimeFrameCluster newCluster = new TimeFrameCluster();
				CopyCluster(ref newCluster,this,bStatic);
				return newCluster;
			}
			
			public DateTime StartTime{
				get { return lastClusterCalcTime.Subtract(TimeSpan.FromMilliseconds(mTimeFrame)); }
			}
			public DateTime EndTime{
				get { return lastClusterCalcTime; }
			}
			
			public bool Frozen{
				get { return mFrozen;}
				set {
					// Debug.Assert(value==false);
					if(value==false)
						return;
					//close the data session
					AddTick(-1,-1,DateTime.MinValue);
					
					//eraze all dynamic data, make the cluster static
					Array.Resize<Tick>(ref TickArr,0);
					TickArr = null;
					mFrozen = true;
				}
			}
			
			public long Volume{
				get { return mTotalVolume;}
			}
			
			public bool Valid{
				get { return mbValid; }
			}
			
			public bool GetMinMax(out double MinPrice, out double MaxPrice,out long MinVolume, out long MaxVolume){
				MinPrice = mMinPrice;
				MaxPrice = mMaxPrice;
				MinVolume = mMinVolume;
				MaxVolume = mMaxVolume;
				return TickCluster.Count > 0;
			}
			
			public void DrawCluster(StrategyBase sb){
				//sb.DrawAndrewsPitchfork
				double mn,mx;
				long mnV,mxV;
				long v  = 0;
				long i  = 0;
				
				if(!GetMinMax(out mn,out mx, out mnV, out mxV)) return;
				
				////sb.DrawLine("ClusterTime",true,TickTimes.Last.Value, mn ,TickTimes.Last.Value,mx + mTickSize*mClusterTicks , Color.Black,DashStyle.Solid,2);
				sb.DrawLine("ClusterTime",true,lastClusterCalcTime, mn ,lastClusterCalcTime,mx + mTickSize*mClusterTicks , Color.Black,DashStyle.Solid,2);
				
				if(mbValid){
					//sb.Print(TickTimes.First.Value.ToString() + " - " + TickTimes. Last.Value.ToString());
					//sb.Print("Cluster.Count: " + TickCluster.Count);
					String s = "";
					foreach(KeyValuePair<double,ClusterItem> kvp in TickCluster){
						s += "{" + kvp.Key + ";" + kvp.Value.Volume + "} ";
						v += kvp.Value.Volume;
						double d = mTimeFrame;
						/////sb.DrawRectangle("Rect" + i++,true,TickTimes.Last.Value,kvp.Key,
						////	TickTimes.Last.Value.AddSeconds(d/1000/mxV*kvp.Value),kvp.Key + mTickSize*mClusterTicks, Color.Transparent,Color.DarkCyan,8);
						
						sb.DrawRectangle("Rect" + i++,true,lastClusterCalcTime,kvp.Key,
							lastClusterCalcTime.AddMilliseconds(d/10*kvp.Value.Power),kvp.Key + mTickSize*mClusterTicks, Color.Transparent,kvp.Value.Power>7?Color.DarkBlue:Color.DarkCyan,8);
					}
					
					//remove rectangels that are not needed
					v = i;
					for(;i<mLastClusterDrawCnt;i++)
						sb.RemoveDrawObject("Rect" + i);
					sb.DrawText("ClusterText",true,lastClusterCalcTime.ToShortTimeString(),lastClusterCalcTime,mx + mTickSize*mClusterTicks,5,Color.Black,null,StringAlignment.Near,Color.Transparent,Color.White,0);
					mLastClusterDrawCnt = v;
				}else
					for(i=0;i<mLastClusterDrawCnt;i++)
						sb.RemoveDrawObject("Rect" + i);

				
			}
		}//end private class TimeFrameCluster
		#endregion
		
        /// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
		/// 

        protected DateTime GetActualTime()
        {
            DateTime tm = DateTime.Now;
            if(Bars.MarketData != null)
                tm = Bars.MarketData.Connection.Now;
            if( tm == DateTime.FromOADate(0) )
                tm = DateTime.Now;
            return tm;
        }

        protected void SetState(StrategyState s)
        {
            if (mParams.State != s)
            {
                if(s == StrategyState.PositionOnMarket || s == StrategyState.OrderPartialFilled)
                    mParams.BreakevenMode = false;
                mParams.State = s;
                mParams.StateTime = GetActualTime();
                mParams.StateBarIdx = CurrentBar;
                mParams.High = Close[0];
                mParams.Low = Close[0];
            }
        }

        #region OnConnectionStatus
        protected override void OnConnectionStatus(ConnectionStatus orderStatus, ConnectionStatus priceStatus)
        {
        }
        #endregion

        #region OnOrderUpdate
        protected override void OnOrderUpdate(IOrder order)
        {
            if (order == null) return;

            //Print("OnOrderUpdate: " + order.ToString());

            if (order == orderEntry)
                switch (order.OrderState)
                {
                    case OrderState.Initialized:
                    case OrderState.PendingSubmit:
                        //don't change strategy state
                        break;
                    case OrderState.Accepted:
                    case OrderState.Working:
                    case OrderState.PendingChange:
                        SetState(StrategyState.OrderPlaced);
                        break;
                    case OrderState.Unknown:
                        //maybe cancel it?
                        break;
                    case OrderState.PendingCancel:
                        if (Position.MarketPosition == MarketPosition.Flat)
                        {
                            //Print("OnOrderUpdate.OrderCanceling");
                            SetState(StrategyState.OrderCanceling);
                        }
                        else
                            SetState(StrategyState.PositionOnMarket);
                        break;
                    case OrderState.Cancelled:
                    case OrderState.Rejected:

                        if (Position.MarketPosition == MarketPosition.Flat) 
                            SetState(StrategyState.None);
                        else 
                            SetState(StrategyState.PositionOnMarket);
                        break;
                    case OrderState.PartFilled:
                        SetState(StrategyState.OrderPartialFilled);
                        break;
                    case OrderState.Filled: //filled
                        SetState(StrategyState.PositionOnMarket);
                        break;
                }

            //redraw strategy
            switch (order.Name)
            {
                case "myPos":
                    if (order.OrderState == OrderState.Working)
                        DrawArrowLine("myPos", 4, order.LimitPrice, 0, order.LimitPrice, order.OrderAction == OrderAction.Buy ? Color.Green : Color.Red);
                    else
                        RemoveDrawObject("myPos");
                    break;
                case "myPosTarg":
                    if(order.OrderState == OrderState.Working)
                        DrawLine("myTarg",4,order.LimitPrice,-5,order.LimitPrice,Color.Blue);
                    else
                        RemoveDrawObject("myTarg");
                    break;
                case "myPosStop":
                    if(order.OrderState == OrderState.Working)
                        DrawLine("myStop", 4, order.LimitPrice, -5, order.LimitPrice, Color.Black);
                    else
                        RemoveDrawObject("myStop");
                    break;
            }
        }
        #endregion

        #region OnExecution
        protected override void OnExecution(IExecution execution)
        {
            // Remember to check the underlying IOrder object for null before trying to access its properties
            IOrder order = execution.Order;

            //if (order != null) Print("OnExecution: " + order.ToString());

            if (order != null && order == orderEntry)
            {
                switch (order.OrderState)
                {
                    case OrderState.Accepted:
                    case OrderState.Unknown:
                    case OrderState.Working:
                    case OrderState.PendingChange:
                    case OrderState.PendingSubmit:
                    case OrderState.Initialized:
                        break;
                    case OrderState.PendingCancel:
                        if(Position.MarketPosition == MarketPosition.Flat)
                            SetState(StrategyState.OrderCanceling);
                        break;
                    case OrderState.Cancelled:
                    case OrderState.Rejected:
                        SetState(StrategyState.None);
                        break;
                    case OrderState.Filled:
                        SetState(StrategyState.PositionOnMarket);
                        break;
                    case OrderState.PartFilled:
                        SetState(StrategyState.OrderPartialFilled);
                        break;
                }
                
            }

            //redraw strategy
            if (order != null)
            {
                switch (order.Name)
                {
                    case "myPos":
                        RemoveDrawObject("myPos");
                        break;
                    case "myPosTarg":
                    case "myPosStop":
                        RemoveDrawObject("myPos");
                        RemoveDrawObject("myTarg");
                        RemoveDrawObject("myStop");
                        break;
                }
            }

        }
        #endregion

        #region OnPositionUpdate
        protected override void OnPositionUpdate(IPosition position)
        {
            if (position.MarketPosition == MarketPosition.Flat )
            {
                //TODO
                SetState(StrategyState.None);
                // Do something like reset some variables here
                /*if (orderEntry != null && orderEntry.OrderState == OrderState.Working) 
                    ;*/
            }

        }
        #endregion

        #region OnMarketData
        //protected override void OnMarketData(MarketDataEventArgs e)
        //{
        //    // Print some data to the Output window
        //    if (e.MarketDataType == MarketDataType.Last)
        //        Print("Last = " + e.Price + " " + e.Volume);


        //    else if (e.MarketDataType == MarketDataType.Ask)
        //        Print("Ask = " + e.Price + " " + e.Volume);
        //    else if (e.MarketDataType == MarketDataType.Bid)
        //        Print("Bid = " + e.Price + " " + e.Volume);
        //}
        #endregion

        #region OnBarUpdate
        protected override void OnBarUpdate()
        {
            
			// Make sure this strategy does not execute against historical data
            if (Historical && StrategyAnalyzerMode == false)
				return;
			//cnt++;
			//Print(string.Format("ZigZag {0}",Inputs[1][0]));
			//Print(string.Format("OnBarUpdate {0}",cnt));

			/*
			if(FirstTickOfBar)
				lastVolume = 0;
			lastTickVolume = Volume[0] - lastVolume;
			lastVolume = Volume[0];
			*/

			//Print("Adding tick: " + Close[0] + ", vol " + lastTickVolume + ", time " + Bars.MarketData.Connection.Now);
//			mCurCluster.AddTick(Close[0],(int)lastTickVolume ,Bars.MarketData.Connection.Now);
//			//redraw once in 5 seconds
//			if(Bars.MarketData.Connection.Now.Subtract(lastDrawTime).TotalSeconds>5){
//				lastDrawTime = Bars.MarketData.Connection.Now;
//				mCurCluster.DrawCluster(this);
//			}
//			if(!mCurCluster.Valid)
//				return;
			
            StrategyParams newSP = new StrategyParams();
			
			//first of all, let's detect our current strategy state
			//this will work even if strategy is interrupted or connection was lost

            //if(Orders.Count<=0 && Position.MarketPosition == MarketPosition.Flat)
            //    mParams.State = StrategyState.Flat;
            //if(Orders.Count<=0 && Position.MarketPosition != MarketPosition.Flat)
            //    mParams.State = StrategyState.OrderFilled;
            //if(Orders.Count>0 && Position.MarketPosition == MarketPosition.Flat)
            //    mParams.State = StrategyState.OrderPlaced;
            //if(Orders.Count>0 && Position.MarketPosition != MarketPosition.Flat)
            //    mParams.State = StrategyState.OrderPartialFilled;


            /*
            if (mParams.State != StrategyState.None || orderEntry != null)
            {
                Print(GetActualTime().ToString() + " Current Bid: " +  Close[0].ToString() );
                Print(mParams.ToString());
                if (orderEntry != null) Print(orderEntry.ToString());
                else Print("orderEntry = null");
            }*/

            if (mParams.Low > Close[0]) mParams.Low = Close[0];
            if (mParams.High < Close[0]) mParams.High = Close[0];

			switch(mParams.State){
				case StrategyState.None:
                //StrategyState_None:
					//Flat, ищем заявку
                    if (FindOrder(ref mParams))
                    {
                        SetState(StrategyState.OrderFound);
                        goto StrategyState_OrderFound;
                    }
					break;
				case StrategyState.OrderFound:
                StrategyState_OrderFound:
                    //подтверждаем заявку, размещаем если подтверждается
                    // (в случае если заявку надо подтверждать)
                    switch (ValidateOrder())
                    {
                        case StrategyState.None:
                            ResetState();
                            break;
                        case StrategyState.OrderPlaced:
                            //если уже поставили Order - не переставляем
                            //Debug.Assert(orderEntry == null);
                            if (orderEntry != null)
                            {
                                Print("!!! Error - StrategyState.OrderPlaced while 'orderEntry != null'");
                                break;
                            }
                            RecalcPositionParams(null, ref mParams);
                            //SetProfitTarget("myPos", CalculationMode.Price, mParams.TargetPrice);
                            //SetStopLoss("myPos", CalculationMode.Price, mParams.LossPrice, false);

                            SetProfitTarget("myPos", CalculationMode.Ticks, mParams.OrderAction == OrderAction.Buy? mLongProfitTicks : mShortProfitTicks);
                            SetStopLoss("myPos", CalculationMode.Ticks, mParams.OrderAction == OrderAction.Buy ? mLongStopTicks : mShortStopTicks, false);
                            orderEntry = PlaceOrder(mParams);
                            if (orderEntry != null)
                            {
                                //RecalcPositionParams(null, ref mParams);
                                //orderTarg = ExitLongLimit(0,true,orderEntry.Quantity, mParams.TargetPrice, "myPosTarg", "myPos");
                                //orderStop = ExitLongStop(0, true, orderEntry.Quantity, mParams.LossPrice, "myPosStop", "myPos");
                                //SetProfitTarget("myPos", CalculationMode.Price, mParams.TargetPrice);
                                //SetStopLoss("myPos", CalculationMode.Price, mParams.LossPrice, false);
                                DrawArrowLine("myPos", 4, mParams.LimitPrice, 0, mParams.LimitPrice, mParams.OrderAction == OrderAction.SellShort ? Color.Red : Color.Green);
                                DrawLine("myStop", 4, mParams.LossPrice, -5, mParams.LossPrice, Color.Black);
                                DrawLine("myTarg",4,mParams.TargetPrice,-5,mParams.TargetPrice,Color.Blue);

                                SetState(StrategyState.OrderPlaced);
                            }
                            
                            //goto StrategyState_OrderPlaced;
                            break;
                        case StrategyState.Unknown:
                            break;
                        default:
                            //error
                            // Debug.Assert(false);
                            break;
                    }
					break;
                case StrategyState.OrderCanceling:
                    //Order был на маркете, но отменен - ждем пока отменится
                    if (orderEntry != null)
                    {
                        if (orderEntry.OrderState == OrderState.Cancelled || orderEntry.OrderState == OrderState.Rejected)
                        {
                            SetState(StrategyState.None);
                            orderEntry = null;
                        }
                    }
                    else
                        SetState(StrategyState.None);

                    break;
                case StrategyState.OrderPartialFilled:
                    //позиция частично заполнена, отменяем дальнейшее заполнение, 
                    // проверяем Order по тем же правилам, затем переходим к обработке позиции
                case StrategyState.OrderPlaced:
					//заявка на маркете. отменяем, переставляем или ждем 
                    newSP = mParams;
                    //newSP.Quantity = orderEntry.Quantity - orderEntry.Filled;//???test
                    switch(RecalcOrderParams(orderEntry, newSP))
                    {
                        case StrategyState.None:
                            ResetState(false);
                            break;
                        case StrategyState.OrderPlaced:
                            if (orderEntry.Quantity != newSP.Quantity || orderEntry.LimitPrice != newSP.LimitPrice)
                            {
                                CancelOrder(orderEntry);
                                PlaceOrder(newSP);
                                mParams.Quantity = newSP.Quantity;
                                mParams.LimitPrice = newSP.LimitPrice;
                            }

                            SetProfitTarget("myPos", CalculationMode.Ticks, mParams.OrderAction == OrderAction.Buy? mLongProfitTicks : mShortProfitTicks);
                            SetStopLoss("myPos", CalculationMode.Ticks, mParams.OrderAction == OrderAction.Buy ? mLongStopTicks : mShortStopTicks, false);
                            //SetProfitTarget("myPos", CalculationMode.Price, mParams.TargetPrice);
                            //SetStopLoss("myPos", CalculationMode.Price, mParams.LossPrice, false);

                            DrawArrowLine("myPos", 4, mParams.LimitPrice, 0, mParams.LimitPrice, mParams.OrderAction == OrderAction.SellShort ? Color.Red : Color.Green);
                            DrawLine("myStop", 4, mParams.LossPrice, -5, mParams.LossPrice, Color.Black);
                            DrawLine("myTarg",4,mParams.TargetPrice,-5,mParams.TargetPrice,Color.Blue);

                            mParams = newSP;
                            break;
                        case StrategyState.Unknown:
                            //ничего не изменяем
                            break;
                        default:
                            //error
                            // Debug.Assert(false);
                            break;
                    }
                    if(mParams.State == StrategyState.OrderPartialFilled)
                        goto StrategyState_OrderFilled;
					break;
				case StrategyState.PositionOnMarket:
                    if (orderEntry != null && orderEntry.OrderState == OrderState.Filled)
                    {
                        orderEntry = null; // Order уже отработан
                    }
                StrategyState_OrderFilled:
                    //мы в позиции. ждем таргета. апдейтим стоп и таргет по надобности
                    newSP = mParams;
                    switch(RecalcPositionParams(Position, ref newSP))
                    {
                        case StrategyState.PositionClosing:
                            orderExit = ClosePosition();
							//PlaceOrder(newSP, "myPos", true);
                            SetState(StrategyState.PositionClosing);
                            break;
                        case StrategyState.PositionOnMarket:
                            //some params like stop|target have been changed
                            //TODO: redraw
                            if (newSP.TargetPrice != 0)
                            {
                                mParams.TargetPrice = newSP.TargetPrice;
                                SetProfitTarget("myPos", CalculationMode.Price, mParams.TargetPrice);
                                DrawLine("myTarg", 4, mParams.TargetPrice, -5, mParams.TargetPrice, Color.Blue);
                            }
                            if (newSP.LossPrice != 0)
                            {
                                mParams.LossPrice = newSP.LossPrice;
                                SetStopLoss("myPos", CalculationMode.Price, mParams.LossPrice, false);
                                DrawLine("myStop", 4, mParams.LossPrice, -5, mParams.LossPrice, Color.Black);
                            }
                            SetState(StrategyState.PositionOnMarket);
                            mParams.StateTime = GetActualTime(); //update time
                            break;
                        case StrategyState.OrderPlaced:
                            //TODO: здесь можно докупаться, если RecalcPositionParams выдаст соотв. информацию,
                            //      тогда следующий стейт - OrderPartialFilled
                        case StrategyState.Unknown:
                        default:
                            // Debug.Assert(false);
                            break;
                    }
					break;
				case StrategyState.PositionClosing:
					//позиция частично закрыта. ждем пока закроется по таргету, переставляем таргет,
					// или закрываемся по маркету
					//??
                    if (Position.MarketPosition == MarketPosition.Flat)
                        SetState(StrategyState.None);
					break;
			}

        }
        #endregion

        private IOrder ClosePosition()
        {
            if (Position.MarketPosition == MarketPosition.Flat) return null;
            if (Position.MarketPosition == MarketPosition.Long) return ExitLong("myPos");
            if (Position.MarketPosition == MarketPosition.Short) return ExitShort("myPos");
            return null;
        }

        private IOrder PlaceOrder(StrategyParams p){ return PlaceOrder(p, "myPos"); }
        private IOrder PlaceOrder(StrategyParams p, string EntrySignal)
        {
            IOrder ret = null;
            switch (p.OrderAction)
            {
                case OrderAction.Buy:
                    ret = EnterLongLimit(0,true,EntriesPerDirection,p.LimitPrice, EntrySignal);
                    break;
                case OrderAction.SellShort:
                    ret = EnterShortLimit(0, true, EntriesPerDirection, p.LimitPrice, EntrySignal);
                    break;
                //??
                case OrderAction.BuyToCover:
                    if (Position.MarketPosition == MarketPosition.Short) ret = ExitShort(EntrySignal);
                    break;
                case OrderAction.Sell:
                    if (Position.MarketPosition == MarketPosition.Long) ret = ExitLong(EntrySignal);
                    break;
            }
            return ret;
        }

        private bool ResetState(){ return ResetState(true); }
        private bool ResetState(bool bResetPositionToo )
        {
            StrategyState ss = StrategyState.None;

            //SetProfitTarget("myPos", CalculationMode.Price, 0);
            //SetStopLoss("myPos", CalculationMode.Price, 0, false);

            if (orderEntry != null )//&& orderEntry.OrderState == OrderState.Working)
            {
                //Print("ResetState.OrderCanceling");
                CancelOrder(orderEntry);
                ss = StrategyState.OrderCanceling;
            }
            
            if (orderStop != null)
                CancelOrder(orderStop);
            
            if (orderTarg != null)
                CancelOrder(orderTarg);

            if (bResetPositionToo)
            {
                orderExit = ClosePosition();
                if (orderExit != null)
                    ss = StrategyState.PositionClosing;
            }

            if (ss != StrategyState.Unknown)
                SetState(ss);
            return true;
        }

        //TODO
        private StrategyState ValidateOrder() { return ValidateOrder(mParams); }
        private StrategyState ValidateOrder(StrategyParams p)
        {
            StrategyState ret = StrategyState.Unknown; //do not change order state - may be we are waiting for some other signal

            //смотрим к какому уровню мы ближе - к саппорту или резистансу
            //выставляем заявку если приблизились к уровню на расстояние 10 тиков

            //TODO: мы вошли в "зону" уровня - здесь можно проверять вторичные признаки, например увеличение объема 
            if (p.OrderAction == OrderAction.SellShort && CrossAbove(High, p.LimitPrice - 10 * TickSize, 1))//подошли ближе чем на 10 тиков к уровню
                return StrategyState.OrderPlaced;

            if (p.OrderAction == OrderAction.Buy && CrossBelow(Low, p.LimitPrice + 10 * TickSize, 1))//подошли ближе чем на 10 тиков к уровню
                return StrategyState.OrderPlaced;

            //if (CurrentBar - mParams.StateBarIdx > 2) //прошло больше 2х баров с того как мы нашли заявку - аннулируем ticket
            if (IsOrderUpdateTime()) //прошло больше n секунд с того как мы нашли заявку - аннулируем ticket
                return StrategyState.None;

            double dySupport = double.MaxValue;
            double dyResistance = double.MaxValue;

            if (mIdxLevelSupport > -1)
            {
                dySupport = Close[0] - mCurSupport;
                //пробили уровень, не успели поставить ордер
                //TODO: может проверять пробой на N тиков
                if (dySupport < 0) return StrategyState.None;
            }
            if (mIdxLevelResistance > -1)
            {
                dyResistance = mCurResistance - Close[0];
                if (dyResistance < 0) return StrategyState.None;
            }


            //ждем шорт, но цена приблизилась к лонгу в 2 раза больше чем к шорту
            if (p.OrderAction == OrderAction.SellShort && dyResistance / 2 > dySupport) return StrategyState.None;

            //ждем лонг, но цена приблизилась к шорту в 2 раза больше чем к лонгу
            if (p.OrderAction == OrderAction.Buy && dySupport / 2 > dyResistance) return StrategyState.None;

            return ret;
        }

        //TODO
        private StrategyState RecalcOrderParams(IOrder orderEntry, StrategyParams newSP)
        {
            StrategyState ret = StrategyState.Unknown; //do not change order state - may be we are waiting for some other signal


            StrategyParams sp = new StrategyParams();
            double ls, lr;

            //если мы близко к текущему уровню (20 тиков) и не пробили стоп - оставляем все как есть
            if(orderEntry.OrderAction == OrderAction.Buy )
            {
                if (Close[0] - orderEntry.LimitPrice < 20 * TickSize && Close[0] > orderEntry.LimitPrice - LongStopTicks * TickSize)
                    return StrategyState.Unknown;

            }else if(orderEntry.OrderAction == OrderAction.SellShort)
            {
                if (orderEntry.LimitPrice - Close[0] < 20 * TickSize && Close[0] < orderEntry.LimitPrice + LongStopTicks * TickSize)
                    return StrategyState.Unknown;

            }

            if (IsOrderUpdateTime())
            {
                if (FindOrder(ref sp, out ls, out lr))
                {
                    if (sp.OrderAction != orderEntry.OrderAction)
                    {
                        ret = StrategyState.None;
                    }
                    else if (sp.LimitPrice != orderEntry.LimitPrice || sp.StopPrice != orderEntry.StopPrice || sp.Quantity != orderEntry.Quantity)
                    {
                        newSP = sp;
                        ret = StrategyState.OrderPlaced; //order will be changed
                    }
                    else
                    {
                        ret = StrategyState.Unknown;
                    }
                }
                else
                {
                    ret = StrategyState.None; //order will be canceled
                }

            }

            return ret;
        }

        //TODO
        private StrategyState RecalcPositionParams(Position pos, ref StrategyParams newSP)
        {
            StrategyState ret = StrategyState.Unknown; //do not change order state - may be we are waiting for some other signal

            //ret = StrategyState.None; //Position will be canceled
            //ret = StrategyState.OrderFilled; //Position params are changed (PL)
            //ret = StrategyState.OrderPlaced; //additional order will be placed (for future use)
            double v;
            if (pos == null)
            {
                //only calc Target\StopLoss 
                v = newSP.LimitPrice + TickSize * ((newSP.OrderAction == OrderAction.Buy) ? mLongProfitTicks : -mShortProfitTicks);
                if (newSP.TargetPrice != v)
                {
                    newSP.TargetPrice = v;
                    ret = StrategyState.Unknown;
                }
                v = newSP.LimitPrice - TickSize * ((newSP.OrderAction == OrderAction.Buy) ? mLongStopTicks : -mShortStopTicks);
                if (newSP.LossPrice != v)
                {
                    newSP.LossPrice = v;
                    ret = StrategyState.Unknown;
                }
                return ret;
            }

            if (!IsPositionUpdateTime()) return ret;

            int mBreakevenTicks = 2;
            int mActivateBreakevenRange = 20; //если цена выходит из этого диапазона - активируем безубыток
            //int mTrailProfitPercent = 80; //разрешаем цене откатиться на 80% от максимума, которого она достигла по сравнению с ценой входа
        mParams_BreakevenMode:
            if (mParams.BreakevenMode)
            {
                //TODO: Trail stop
                //Цена позиции +- количество тиков безубытка
                v = 0;
                if (mParams.OrderAction == OrderAction.Buy) 
                    v = pos.AvgPrice + mBreakevenTicks * TickSize;
                else if (mParams.OrderAction == OrderAction.SellShort) 
                    v = pos.AvgPrice - mBreakevenTicks * TickSize;

                if (v != 0 && v != mParams.LossPrice)
                {
                    mParams.LossPrice = newSP.LossPrice = v;
                    ret = StrategyState.PositionOnMarket;
                }

            }
            else if ((newSP.OrderAction == OrderAction.Buy ? mParams.High - pos.AvgPrice : pos.AvgPrice - mParams.Low) > mActivateBreakevenRange * TickSize)
            {
                Print("Going breakeven => |FillPrice - Current| > 20 ticks");
                newSP.BreakevenMode =  mParams.BreakevenMode = true;
                goto mParams_BreakevenMode;
            }else if (GetActualTime() - mParams.StateTime > TimeSpan.FromSeconds(15 * 60)) //15 minutes, TODO: Strategy param (безубыток): TimeForBreakeven
            {
                //переставляемся в безубыток через n секунд
                Print("Going breakeven => 15 minutes in position");
                mParams.BreakevenMode = true;
                goto mParams_BreakevenMode;
            }

            //TODO: условия досрочного закрытия позиции по маркету/лимиту

            //TODO: что делать с таргетом? когда/куда его двигать
            newSP.TargetPrice = pos.AvgPrice + TickSize * ((newSP.OrderAction == OrderAction.Buy) ? mLongProfitTicks : -mShortProfitTicks);

            return ret;
        }
		
		private double CorrectSupport(double val){
			return val + mCorrectSupport*TickSize;
		}
		private double CorrectResistance(double val){
			return val + mCorrectResistance*TickSize;
		}

        private bool IsOrderUpdateTime()
        {
            bool b = GetActualTime() - mLevelsUpdTime > TimeSpan.FromSeconds( 60 * 5 );
            if(b) return true;
            b = mCurResistance!=0 && CrossAbove(High, mCurResistance, 1);
            if (b) return true;
            b = mCurSupport!=0 && CrossBelow(Low, mCurSupport, 1);
            return b;
        }

        private bool IsPositionUpdateTime()
        {
            bool b = GetActualTime() - mParams.StateTime > TimeSpan.FromSeconds(60); //each minute
            if (b) return true;
            b = CrossAbove(High, mCurResistance, 1);
            if (b) return true;
            b = CrossBelow(Low, mCurSupport, 1);
            return b;
        }

		//"главная" функция в стратегии
		//смотрит уровни и генерирует заявку в зависимости от текущего движения, вызывается до тех пор, пока мы в флет-позиции, постоянно переставляет заявку
		//private bool FindOrder(out bool bShort, out double limitPrice, out double profitTarg, out double stopLoss)
        private bool FindOrder(ref StrategyParams op) { double d; return FindOrder(ref op, out d, out d); }
        private bool FindOrder(ref StrategyParams op, out double LevelSupport, out double LevelResistance)
		{
            op.OrderAction = OrderAction.Buy;
            op.Quantity = 0;
            op.LimitPrice = 0;
            op.StopPrice = 0;
            LevelResistance = mCurResistance;
            LevelSupport = mCurSupport;
			//bShort = false; limitPrice = 0; profitTarg = 0; stopLoss = 0;
			/*
				1) находим уровни, между которыми находится текущая цена (поддержка и сопротивление)
				2) смотрим к какому из них мы ближе находимся и выставляем на этом уровне заявку.
			*/
			int i=0;
			//находим индексы уровней сопротивления и поддержки, между которыми сейчас находится цена
			//т.к. массив отсортирован - вниз от idxLo будут уровни поддержки, вверх от idxHi - уровни сопротивления
			double dySupport = double.MaxValue;
			double dyResistance = double.MaxValue;
			
            

			if(IsOrderUpdateTime() || orderEntry == null)
			{
					TrueLevels(mLevelPrecision,mTrendPower).GetActualSortedLevels(ref mTrueLevels);
					mIdxLevelSupport = -1;
					mIdxLevelResistance = -1;
				    if(mTrueLevels != null)
					    for(i=0;i<mTrueLevels.Length;i++){
						
						    if(mTrueLevels[i].isSupport()){
							    if(CorrectSupport(mTrueLevels[i].PriceLevel2) <Close[0]){
								    mIdxLevelSupport = i;
								    dySupport = Close[0] - CorrectSupport( mTrueLevels[i].PriceLevel2);
							    }
						    }else{
							    if(CorrectResistance( mTrueLevels[i].PriceLevel2)>Close[0] && mIdxLevelResistance == -1){
								    mIdxLevelResistance = i;
								    dyResistance = CorrectResistance( mTrueLevels[i].PriceLevel2) - Close[0];
							    }
						    }
						
						    if(mIdxLevelSupport!=-1 && mIdxLevelResistance!=-1)
							    break;
					    }
					
			}else{
					
				if(mIdxLevelSupport != -1){
					for(i=mIdxLevelSupport;i>=-1;i--){
						if(i==-1){
							mIdxLevelSupport = -1;
							dySupport = double.MaxValue;
							break;
						}
						if(mTrueLevels[i].isSupport() && CorrectSupport( mTrueLevels[i].PriceLevel2)<Close[0]){
							mIdxLevelSupport = i;
							dySupport = Close[0] - CorrectResistance( mTrueLevels[i].PriceLevel2);
							break;
						}
					}
				}
				
				if(mIdxLevelResistance != -1){
					for(i=mIdxLevelResistance;i<=mTrueLevels.Length;i++){
						if(i==mTrueLevels.Length){
							mIdxLevelResistance = -1;
							dyResistance  = double.MaxValue;
							break;
						}
						if(!mTrueLevels[i].isSupport() && CorrectResistance( mTrueLevels[i].PriceLevel2 ) > Close[0]){
							mIdxLevelResistance = i;
							dyResistance = CorrectResistance( mTrueLevels[i].PriceLevel2 ) - Close[0];
							break;
						}
					}
				}else{
					
				}
			}

            mLevelsUpdTime = GetActualTime();
            mCurResistance = mCurSupport = 0;

			if(mIdxLevelResistance == -1 && mIdxLevelSupport == -1)
				return false;
			
            if (mIdxLevelResistance >= 0)
                mCurResistance = LevelResistance = CorrectResistance(mTrueLevels[mIdxLevelResistance].PriceLevel2);
            if(mIdxLevelSupport >= 0)
                mCurSupport = LevelSupport = CorrectSupport(mTrueLevels[mIdxLevelSupport].PriceLevel2);

			//смотрим к какому уровню мы ближе - к саппорту или резистансу
            //выставляем заявку если приблизились к уровню на 20% расстояния между Support & Resistance
            if (dyResistance < dySupport && dyResistance / (dyResistance + dySupport) < 0.2)
            {
                op.LimitPrice = mCurResistance;
                op.OrderAction = OrderAction.SellShort;
                op.Quantity = EntriesPerDirection;
                op.StopPrice = 0; //лимитные заявки
                return true;
            }
            else if (dyResistance > dySupport && dySupport / (dyResistance + dySupport) < 0.2)
            {
                op.LimitPrice = mCurSupport;
                op.OrderAction = OrderAction.Buy;
                op.Quantity = EntriesPerDirection;
                op.StopPrice = 0; //лимитные заявки
                return true;
			}
			return false;
		}

        #region Properties
        [Description("")]
        [GridCategory("Parameters")]
        public bool StrategyAnalyzerMode
        {
            get { return mSAmode; }
            set { mSAmode = value; }
        }

        [Description("LongStopTicks - StopLoss для Long")]
        [GridCategory("Long Params")]
		public int LongStopTicks
		{
            get { return mLongStopTicks; }
            set { mLongStopTicks = value; }
		}
        [Description("ShortStopTicks - StopLoss для Short")]
        [GridCategory("Short Params")]
		public int ShortStopTicks
		{
            get { return mShortStopTicks; }
            set { mShortStopTicks = value; }
		}
		
        [Description("")]
        [GridCategory("Long Params")]
		public int LongProfitTicks
		{
            get { return mLongProfitTicks; }
            set { mLongProfitTicks = value; }
		}
        [Description("")]
        [GridCategory("Short Params")]
		public int ShortProfitTicks
		{
            get { return mShortProfitTicks; }
            set { mShortProfitTicks = value; }
		}


        [Description("Если ширина уровня больше - делим уровень на 2 или минимизируем до этого значения (?)")]
        [GridCategory("Levels Params")]
        public int LevelMaxWidth
        {
            get { return mLevelMaxWidth; }
            set { mLevelMaxWidth = value; }
        }

        [Description("Если ширина уровня меньше - добавляем ширину до этого значения (?)")]
        [GridCategory("Levels Params")]
        public int LevelMinWidth
        {
            get { return mLevelMinWidth; }
            set { mLevelMinWidth = value; }
        }

        [Description("Количество баров вперед/назад в районе Hi|Lo загзага, где искать уровень.")]
        [GridCategory("Levels Params")]
		public int LevelPrecision
		{
            get { return mLevelPrecision; }
            set { mLevelPrecision = value; }
		}
        [Description("Сколько тиков проходит цена для формирования изгиба ZigZag")]
        [GridCategory("Levels Params")]
		public int TrendPower
		{
            get { return mTrendPower; }
            set { mTrendPower = value; }
		}
		
		
		#endregion
    }
}
