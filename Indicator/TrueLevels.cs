#region Using declarations
using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Xml.Serialization;
using NinjaTrader.Cbi;
using NinjaTrader.Data;
using NinjaTrader.Gui.Chart;
//using PriceActionSwing.Utility;
#endregion

// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    /// <summary>
    /// Enter the description of your new custom indicator here
    /// </summary>
    [Description("Enter the description of your new custom indicator here")]
    public class TrueLevels : Indicator
    {
        #region Variables
        // Wizard generated variables
            private int period = 10; // Default setting for Period
            private int mFactor = 12; // Default setting for MFactor
		 	private int nTrendPower = 120; // Default setting for MFactor		

		// User defined variables (add any user defined variables below)
        #endregion
		string ProgramVersion = "1.14";
		string ProgramVersionDate = "25.09.2012"; 
		int HLPeriod=10;
		int TF=5;
		int PeriodAVG;
		int cnt=0;
		int BarH=1, BarL=1;
		bool AllowDraw = true;
		int LastBarID = 0;
		

		public struct trueLevel {
			public int Bar;
			public int BarLast;				// Bar на котором уровень перестал быть актуальным, цена пересекла оба левела и закрепилась там в течении ?? баров
			public double PriceLevel1 ;
			public double PriceLevel2;
			public readonly int ID;
		public trueLevel(int Bar, double PriceLevel1 , double PriceLevel2) {
				this.Bar = Bar;
				this.PriceLevel1 = PriceLevel1;
				this.PriceLevel2 = PriceLevel2;
				this.BarLast = 0;
				Random rnd = new Random();
				this.ID = rnd.Next(0,100000);				
			}

			// возвращает актуален ли уровень 
			public bool isActual() {
				return (BarLast == 0);
			}

			// возвращает суппорт ето или резистанс
			public bool isSupport() {
				return (PriceLevel1 < PriceLevel2);
			}
		}
		
		List <trueLevel> Levels = new List <trueLevel>();
		
		private double BarRangeStd; // Default setting for MFactor

        /// <summary>
        /// This method is used to configure the indicator and is called once before any bar data is loaded.
        /// </summary>
        protected override void Initialize()
        {
			ClearOutputWindow();
			
			// ADD Indicators			
			
			Print ("Version: " + ProgramVersion);
            Overlay				= true;
						
		}
        
		protected override void OnStartUp() {
			//BarRangeStd = GetAvgBarRange (233);
			//Print ("STD BarRange: " +  Low[233]);			
			if (BarsPeriod.BasePeriodType!=PeriodType.Minute)  {
					Print ("Invalid timeframe");
					return ;
			}
			else TF = BarsPeriod.Value;						
			PeriodAVG = 24*60/TF * 2; //3 days
			Print ("Timeframe: " + TF);
			Print ("Ticksize: " + TickSize);
			
			HLPeriod = period;
		}
		
		/// <summary>
        /// Called on each bar update event (incoming tick)
        /// </summary>
        protected override void OnBarUpdate()
        {
            double p1=0, p2=0;
			int Bar = 0;		
			int tmp = 0;

			if (CurrentBar < 3) return;
			if (CurrentBar < Period) return;
			if (CurrentBar < 2*HLPeriod+1) return;
			//if (CurrentBar < PeriodAVG) return;

			if (CurrentBar >PeriodAVG) 
				BarRangeStd = GetAvgBarRange(1,PeriodAVG); 
			else 
				BarRangeStd = GetAvgBarRange(1,CurrentBar-PeriodAVG);
									
			cnt++;			

			//Print ("Bar: " + CurrentBar + " AVG: " + GetAvgBarRange(0, Period)  + " STD: " + BarRangeStd + " Range: " + GetBarRange(0) + " R2: " + GetAvgBarRange(1,1)); 

			//UP bar	
			Bar = ZigZag(DeviationType.Points,TickSize*nTrendPower,false).LowBar(0,1,0);
			if (Bar>0) {
				BarL = CurrentBar - Bar;
				if (BarH>0) {		// save LOW
					RemoveLevel(LastBarID);
					Bar = GetHighestBar(CurrentBar-BarH, 3);	// ищей хай на расстоянии в 3 бара
					//Print("U: " + CurrentBar + " " + (CurrentBar-BarH).ToString() + " " + Bar);
					DrawDiamond("DMD_U:" + (CurrentBar-Bar).ToString(), true, Bar, High[Bar] + 2*TickSize, Color.Red);					
					BarColorSeries[Bar] = Color.Red;
					if (GetLevel(Bar, false, out p1, out p2)) AddLevel(CurrentBar - Bar, p1, p2);					
				} else {
					// если ето промежуточный хай то рисуем его, потом удалим
					Bar = GetLowestBar(CurrentBar-BarL, 3);		// ищей лоу на расстоянии в 3 бара
					BarColorSeries[Bar] = Color.Orange;					
					DrawDiamond("DMD_I:" + (CurrentBar-Bar).ToString(), true, Bar, Low[Bar] - 2*TickSize, Color.Orange);
					if (GetLevel(Bar, true, out p1, out p2)) tmp = AddLevel(CurrentBar-Bar, p1, p2);
					if (tmp > 0) {						
						RemoveLevel(LastBarID);
						LastBarID = tmp;
					}
				}
			}						

			//DOWN bar
			Bar = ZigZag(DeviationType.Points,TickSize*nTrendPower ,false).HighBar(0,1,0);
			if (Bar>0) {
				BarH = CurrentBar - Bar;
				if (BarL>0) {		// save HIGH					
					RemoveLevel(LastBarID);
					Bar = GetLowestBar(CurrentBar-BarL, 3);		// ищей лоу на расстоянии в 3 бара
					//Print("D: " +CurrentBar +" " + (CurrentBar-BarL).ToString() + " " + Bar);
					DrawDiamond("DMD_D:" + (CurrentBar-Bar).ToString(), true, Bar, Low[Bar] - 2*TickSize, Color.Green);
					BarColorSeries[Bar] = Color.Green;					
					if (GetLevel(Bar, true, out p1, out p2)) AddLevel(CurrentBar-Bar, p1, p2);
				} else {
					// если ето промежуточный уровень то рисуем его, потом удалим
					Bar = GetHighestBar(CurrentBar-BarH, 3);
					BarColorSeries[Bar] = Color.Orange;					
					DrawDiamond("DMD_I:" + (CurrentBar-Bar).ToString(), true, Bar, High[Bar] + 2*TickSize, Color.Orange);					
					if (GetLevel(Bar, true, out p1, out p2)) tmp = AddLevel(CurrentBar-Bar, p1, p2);
					if (tmp > 0) {						
						RemoveLevel(LastBarID);
						LastBarID = tmp;
					}
				}
			} 

			#region comments
//			if (GetBarRange(1) > 0) {
//				double factor = GetBarRange(0) / GetBarRange(1);
//				
//				if 	((factor > mFactor )  								// если есть вынос на баре
//					&& (GetBarRange(0) > GetAvgBarRange(0, Period)) 	// если бар больше чeм средний бар за Period
//					&& (GetBarRange(0) > 1.2*BarRangeStd) 				// если бар больше чeм средний бар за несколько дней
//					&& ((Low[1]<=Low[0]) || (High[1]>=High[0]))			// если предыдущий бар выше/ниже выноса
//					&& (cnt>3)											// следующий вынос  не сразу, а спустя ?? баров после накопления
//					&& (false)){
//
//						cnt=0;
//						
//						// DOWN
//						if ((High[0]>Low[0]) && (Low[0]<Low[1]) && (IsUpBar(0)==false)) {
//							if (GetLevel(0, false, out p1, out p2)) {
//								//Print ("P1: " + p1 + " P2: " + p2 + " L: " + Low[0] + " H: " + High[0] + " is: " +IsUpBar(0));
//								
//								Levels.Add (new trueLevel(CurrentBar, p1, p2)); 
//								//DrawLine("LN" + (CurrentBar).ToString(), 0, p1, -100, p1, Color.Red);						
//								//DrawLine("LN" + (CurrentBar).ToString(), 0, p2, -100, p2, Color.Red);						
//								
//								//DrawLine("LN1" + CurrentBar, true, 1, p1, -50, p1, Color.Red, DashStyle.Solid, 1);							
//								//DrawLine("LN2" + CurrentBar, true, 1, p2, -50, p2, Color.Red, DashStyle.Solid, 1);
//							
//								//DrawDiamond("DMD" + (CurrentBar).ToString(), true, 0, High[0]+ 2*TickSize, Color.Red);
//							//DrawLine("LN" + (CurrentBar).ToString(),0, High[0], -100,High[0], Color.Red);						
//							}
//						}
//
//						// UP
//						if ((Low[0]<High[0]) && (High[0]>High[1]) && (IsUpBar(0) == true)) {
//							if (GetLevel(0, true, out p1, out p2)) {
//								//Print ("P1: " + p1 + " P2: " + p2 + " L: " + Low[0] + " H: " + High[0] + " is: " + IsUpBar(0));
//								
//								Levels.Add (new trueLevel(CurrentBar, p1, p2)); 
//								//Print ("LEVEL: " + Bar + " P1: " + p1+ " P2: " + p2);
//								//DrawLine("LN1" + CurrentBar, true, 1, p1, -50, p1, Color.Green, DashStyle.Solid, 1);							
//								//DrawLine("LN2" + CurrentBar, true, 1, p2, -50, p2, Color.Green, DashStyle.Solid, 1);
//																
//								//DrawDiamond("DMD" + (CurrentBar).ToString(), true, 0, Low[0]- 2*TickSize, Color.Green);
//							//DrawLine("LN" + (CurrentBar).ToString(),0, Low[0], -100,Low[0], Color.Green);						
//							}
//						}
//						BarColorSeries[0] = Color.LightGreen; 			
//						//Print ("DrawDiamand: " +  (CurrentBar).ToString() + " Factor: " + factor.ToString() + " AVG: " + BarRangeStd) ;
//					}
//			}

//			int n = 3;
//			int i = 0;
//			while (NBarsUp(n, true, false, false)[i] > 0) i++;
//			if (i>0)  {
//				Bar = n-1+i-1;
//				//DrawDiamond("DMD_U" + CurrentBar, true, Bar, Low[Bar] - TickSize, Color.Green);
//				if (GetLevel(Bar, out p1, out p2)) {
//					Levels.Add (new trueLevel(CurrentBar-Bar, p1, p2)); 								
//					Print ("LEVEL: " + (CurrentBar).ToString() + " R2: " + GetAvgBarRange(Bar-n,n).ToString());
//				}
//			}
//			
//			i = 0;
//			while (NBarsDown(n, true, false, false)[i] > 0) i++;
//			if (i>0) {
//				Bar = n-1+i-1;				
//				//DrawDiamond("DMD_D" + (CurrentBar-Bar).ToString(), true, Bar, High[Bar]+ TickSize, Color.Red);											
//				if (GetLevel(Bar, out p1, out p2))  {
//					Print ("LEVEL: " + (CurrentBar).ToString() + " R2: " + GetAvgBarRange(Bar-n,n).ToString());
//					Levels.Add (new trueLevel(CurrentBar-Bar, p1, p2)); 								
//				}
//			}

			//Print("Bar: " + CurrentBar + " Total: " + Bars.Count + " Swing: " + Swing(HLPeriod).SwingHighBar(1,1,255));

//			if (CurrentBar == Bars.Count-2) 
//				for (int i=0;i<CurrentBar;i++) {
//					
//					BarH = HighestBar(High, i+BarL);
//					BarColorSeries[BarH] = Color.Blue;
//		
//
//					BarL = LowestBar(Low, i+BarH);
//					BarColorSeries[BarL] = Color.Magenta;			
//			}




/*
			Bar = ZigZag(DeviationType.Points,10,false).LowBar(1,1,100);			
			if (Bar>0)  {
				BarH = Bar;
				Print ("Bar: " + CurrentBar + " LowBar: " +Bar + " Value: " + ZigZag(DeviationType.Points,10,false).ZigZagLow[0]);
				//Bar = Bar -1;
				DrawDiamond("DMD_U:" + CurrentBar, true, Bar, Low[Bar] - 2*TickSize, Color.Green);
				BarColorSeries[Bar] = Color.LimeGreen;
				if (GetLevel(Bar, true, out p1, out p2)) {
					Levels.Add (new trueLevel(CurrentBar-Bar, p1, p2)); 								
				}

			}

		//DOWN
			//Bar = IsTrendSeries(4,false,false,false,true);
			Bar = ZigZag(DeviationType.Points,10,false).HighBar(BarH+1,1,BarH);						
			if (Bar>0) { 
				BarH = Bar;
				Print ("Bar: " + CurrentBar + " HighBar: " +Bar + " Value: " + ZigZag(DeviationType.Points,10,false).ZigZagHigh [0]);
				//Bar = Bar -1;
				p1 = ZigZag(DeviationType.Points,5,false).ZigZagHigh[Bar];			
				DrawDiamond("DMD_D:" + CurrentBar + " " +  p1 , true, Bar, High[Bar]+ 2*TickSize, Color.Red);											
				BarColorSeries[Bar] = Color.OrangeRed;
				if (GetLevel(Bar, false, out p1, out p2)) {
					Levels.Add (new trueLevel(CurrentBar-Bar, p1, p2)); 								
				}
			}
*/
			//int n = ZigZag(DeviationType.Percent,0.15,false).LowBar(0,1,100);
			//if (n>0)  BarColorSeries[n] = Color.OrangeRed;
						
//***********LAST workong version
//			if (Swing(HLPeriod).SwingHighBar(0,1,1)==HLPeriod+1) {
//				BarColorSeries[HLPeriod+1] = Color.Magenta;	
//				if (GetLevel(HLPeriod+1, false, out p1, out p2)) Levels.Add (new trueLevel(CurrentBar - HLPeriod-1, p1, p2)); 
//			}

//			if (Swing(HLPeriod).SwingLowBar(0,1,1)==HLPeriod+1) {
//				BarColorSeries[HLPeriod+1] = Color.Green;	
//				if (GetLevel(HLPeriod+1, true, out p1, out p2)) Levels.Add (new trueLevel(CurrentBar - HLPeriod-1, p1, p2)); 	
//			}
//
//******************
			//			bool b=true;
//			for (int i=1; i<HLPeriod; i++){
//				if (High[i]>=High[HLPeriod]) {
//					b=false;
//					break;
//				}
//				if (High[HLPeriod+i]>=High[HLPeriod]) {
//					b=false;
//					break;
//				}
//			}
//			if (b==true) {
//				BarColorSeries[HLPeriod] = Color.Magenta;	
//				if (GetLevel(HLPeriod, false, out p1, out p2)) Levels.Add (new trueLevel(CurrentBar - HLPeriod, p1, p2)); 								
//			}

//
//			b=true;
//			for (int i=1; i<HLPeriod; i++){
//				if (Low[i] <= Low[HLPeriod]) {
//					b=false;
//					break;
//				}
//				if (Low[HLPeriod+i] <= Low[HLPeriod]) {
//					b=false;
//					break;
//				}
//			}
//			if (b==true)  {
//				BarColorSeries[HLPeriod] = Color.Green;	
//				if (GetLevel(HLPeriod, true, out p1, out p2)) Levels.Add (new trueLevel(CurrentBar - HLPeriod, p1, p2)); 								
//			}
//			
			#endregion
			
			// redraw all
			if (AllowDraw) DrawLevels();
		}
		

		// находит на расстоянии BarCount от CurrentBar самый High бар; CurrentBar - index бара
		int GetHighestBar(int BarIndex, int BarCount) {
			int Bar = BarIndex;					
			if (CurrentBar > BarIndex+ BarCount) for(int i=1; i <= BarCount; i++) if (High[BarIndex+i] > High[Bar]) Bar = BarIndex+i;
			for(int i=1; i <= Math.Min(BarCount,BarIndex); i++) 	  if (High[BarIndex-i] > High[Bar]) Bar = BarIndex-i;
			return Bar;
		}
			
		// находит на расстоянии BarCount от CurrentBar самый Low бар; CurrentBar - index бара
		int GetLowestBar(int BarIndex, int BarCount) {
			int Bar = BarIndex;
			if (CurrentBar > BarIndex + BarCount) for (int i=1; i <= BarCount; i++) if (Low[BarIndex+i]<Low[Bar]) Bar = BarIndex+i;
			for (int i=1; i <= Math.Min(BarCount,BarIndex); i++) if (Low[BarIndex-i]<Low[Bar]) Bar = BarIndex-i;
			return Bar;
		}		

		// находит на расстоянии BarCount от CurrentBar самый высокий LOW
		int GetHighestLowBar(int BarIndex, int BarCount) {
			int Bar = BarIndex;					
			if (CurrentBar > BarIndex+ BarCount) for(int i=1; i <= BarCount; i++) if (Low[BarIndex+i] > Low[Bar]) Bar = BarIndex+i;
			for(int i=1; i <= Math.Min(BarCount,BarIndex); i++) if (Low[BarIndex-i] > Low[Bar]) Bar = BarIndex-i;
			return Bar;
		}

		// находит на расстоянии BarCount от CurrentBar самый низкий High
		int GetLowestHighBar(int BarIndex, int BarCount) {
			int Bar = BarIndex;
			if (CurrentBar > BarIndex + BarCount) for (int i=1; i <= BarCount; i++) if (High[BarIndex+i]<High[Bar]) Bar = BarIndex+i;
			for (int i=1; i <= Math.Min(BarCount,BarIndex); i++) if (High[BarIndex-i]<High[Bar]) Bar = BarIndex-i;
			return Bar;
		}

		// находит на расстоянии BarCount от CurrentBar второй по величине High после BarIndex
		int GetNextHighBar(int BarIndex, int BarCount) {
			int Bar = BarIndex+1;
			if (CurrentBar > BarIndex + BarCount) for (int i=2; i <= BarCount; i++) if ((High[BarIndex+i]>High[Bar]) && (High[BarIndex+i]<High[BarIndex])) Bar = BarIndex+i;
			for (int i=2; i <= Math.Min(BarCount,BarIndex); i++) if ((High[BarIndex-i]>High[Bar])&& (High[BarIndex-i]<High[BarIndex])) Bar = BarIndex-i;
			return Bar;
		}
		// находит на расстоянии BarCount от CurrentBar самый высокий LOW
		int GetNextLowBar(int BarIndex, int BarCount) {
			int Bar = BarIndex+1;					
			if (CurrentBar > BarIndex + BarCount) for(int i=2; i <= BarCount; i++) if ((Low[BarIndex+i] < Low[Bar]) && (Low[BarIndex+i] > Low[BarIndex])) Bar = BarIndex+i;
			for(int i=2; i <= Math.Min(BarCount,BarIndex); i++) if ((Low[BarIndex-i] < Low[Bar]) && (Low[BarIndex-i] > Low[BarIndex])) Bar = BarIndex-i;
			return Bar;
		}
		
		
		private static int CompareLevels(trueLevel x, trueLevel y){ 
        	int i = Math.Sign(x.PriceLevel2 - y.PriceLevel2);  
            return i; 
        } 

		           
        public int GetActualSortedLevels(ref trueLevel[] l) { 
           Update(); 
           int i=0,j=0; 
           if(Levels.Count>0) { 
                l = new trueLevel[Levels.Count]; 
                int c =0; 
                for(i =0;i<Levels.Count;i++) 
                { 
                     if(Levels[i].isActual()){ 
                          l[c] = Levels[i]; 
                          c++; 
                     }      
                } 
                Array.Resize<trueLevel>(ref l,c); 
                //sort the array by Level2 price 
                Array.Sort<trueLevel>(l,CompareLevels); 
                return c; 
           } 
           return 0; 
		}
		
		//#region  CheckLevels
		///проверяет уровни, удаляет те, которые не подходят под правила		
		private void CheckLevels() {
			trueLevel lvl;
			for (int i=0; i<Levels.Count; i++)
				if (Levels[i].isActual()) {
				for (int j=Levels[i].Bar+5; j<CurrentBar; j++) {		// проверяет проколы с 5-го бара после екстремума
					if (Levels[i].isSupport()) {											
						if (Low[CurrentBar-j] < Levels[i].PriceLevel1) {
							lvl=Levels[i];
							lvl.BarLast = j;
							Levels.RemoveAt(i); 
							Levels.Add(lvl);
							break;
						}
					} else 
						if (High[CurrentBar-j] > Levels[i].PriceLevel1) {
							lvl = Levels[i];
							lvl.BarLast = j;
							Levels.RemoveAt(i); 
							Levels.Add(lvl);
							break;
						}								
				}
			}					
		}
		
	//#endregion

//		int GetZigZagHiLo(int Bar, bool Direction) {
//			int n, cBar;
//			for (int i = Bar; i<CurrentBar - Bar; i++) {
//				if (Direction) n = ZigZag(DeviationType.Percent,0.15,false).HighBar(i,1,100);
//					    else  n = ZigZag(DeviationType.Percent,0.15,false).LowBar(i,1,100);				
//				
//			}
//			if (Direction)  {// UP 
//			
//			}
//			Bar = ZigZag(DeviationType.Percent,0.15,false).LowBar(0,1,100);
//			return Bar;
//		}
			
		// DRAW ALL LEVELS		
		private void DrawLevels() {
			Color cArea, cBorder;
			int BarsAgo;
			
			CheckLevels();

			for (int i=0; i<Levels.Count; i++) {
				BarsAgo = CurrentBar - Levels[i].Bar + 1;
				if // ((BarsAgo < PeriodAVG ) && 									// проверка попадания в период
					//(BarsAgo < Bars.BarsSinceSession) && 	//только за текущую сессию					
					//(Time[BarsAgo] > Time[0].AddHours(-4*24*TF/15)) &&
					((true)) {					

					//color
					if  (Levels[i].PriceLevel1  > Levels[i].PriceLevel2) {
						//down
						cArea = cDownAreaColor;	//Color.LightPink;
						cBorder = cDownBorderColor; //Color.IndianRed;
					} else { // up
						cArea = cUpAreaColor;	// Color.DarkSeaGreen;
						cBorder = cUpBorderColor;	// Color.DarkOliveGreen;						
					}					 
															
					//debug
					//Print ("Bar: " + CurrentBar + " Level: " + i + " ID: " + Levels[i].ID + " StartBar: " + Levels[i].Bar + " LastBar: " + Levels[i].BarLast + "  P1: " + Levels[i].PriceLevel1 + " P2: " + Levels[i].PriceLevel2);
					
					if (Levels[i].PriceLevel1 != Levels[i].PriceLevel2)
						DrawRectangle("RECT " + Levels[i].Bar, false, CurrentBar - Levels[i].Bar, Levels[i].PriceLevel1, Levels[i].BarLast>0?CurrentBar - Levels[i].BarLast:-100 ,Levels[i].PriceLevel2,cBorder, cArea,1);
					else
						DrawLine("RECT " + Levels[i].Bar, false,CurrentBar - Levels[i].Bar+1, Levels[i].PriceLevel1 , Levels[i].BarLast>0?CurrentBar - Levels[i].BarLast:-100,Levels[i].PriceLevel2,cBorder,DashStyle.Solid, 1);
					
					//if (Levels[i].isActual()) 
						DrawText("TXT " + Levels[i].Bar, GetRangeTicks (Levels[i].PriceLevel1 , Levels[i].PriceLevel2).ToString(), Levels[i].BarLast>0?CurrentBar - Levels[i].BarLast+5:0, Levels[i].PriceLevel1  + (Levels[i].PriceLevel2-Levels[i].PriceLevel1 )/2, Color.Gray);
					//else RemoveDrawObject("TXT" + Levels[i].Bar);
				} else  {					
					Levels.RemoveAt(i);	
				}
			}
			DrawTextFixed("Info", "Version: " + ProgramVersion  + "; Levels count: " + Levels.Count,TextPosition.BottomLeft);			
		}

		//удаляет уровень по ID
		private bool RemoveLevel(int ID) {
			if (ID <= 0) return false;
			for (int i=0; i<Levels.Count; i++)
				if (Levels[i].ID == ID) {
					RemoveDrawObject ("RECT " + Levels[i].Bar);
					RemoveDrawObject ("TXT " + Levels[i].Bar);
					Levels.RemoveAt(i);				
					return true;
				}			
			return false;
		}

		//математика, выдает уровни после вылета
		private bool GetLevel(int Bar, bool UpTrend, out double l1, out double l2) {
			// ограничиваем ширину уровня сверху, половиной среднего ширины среднего бара за весь период

			l1=0;l2=0;
			if (UpTrend) { // UP  -  support level												 
				l1 = Low[GetNextLowBar(Bar,nLevelPrecision)];
				l2 = High[GetLowestHighBar(Bar,nLevelPrecision)];

				//switch levels
				if (l1 > l2) {
					double tmp = l2;
					l2 = l1;
					l1 = tmp;
				}

			} else { // DOWN - resistance level								
				l1 = High[GetNextHighBar(Bar ,nLevelPrecision)];
				l2 = Low[GetHighestLowBar(Bar, nLevelPrecision)];
				
				//switch
				if (l1 < l2) {
					double tmp = l2;
					l2 = l1;
					l1 = tmp;
				}
				//if (GetRangeTicks(l1, l2)>BarWidth)  l2 = l1-BarWidth*TickSize;
				}		
			return true;
		}		

		#region GetLEvel_obsolete
		private bool GetLevel_obs(int Bar, bool UpTrend, out double l1, out double l2) {
			// ограничиваем ширину уровня сверху, половиной среднего ширины среднего бара за весь период
			//int BarWidth = GetAvgBarRange(Bar,CurrentBar)/2;
			l1=0;l2=0;
			//if (ADX(14)[Bar+1]  < 30) return false;
			if (UpTrend) { // UP TREND
				
				if (High[Bar] < High[Bar-1] ) { // НЕТ ПОГЛОЩЕНИЯ
					l1 = Math.Min(Low[Bar-1], Low[Bar+1]);					
					l2 = High[Bar];
				} else {  // ПОГЛОЩЕНИЕ
					l1 = Math.Max(Low[Bar-1], Low[Bar-2]);
					l2 = Math.Min(High[Bar-1], High[Bar-2]);
				}						

				//switch
				if (l1 > l2) {
					double tmp = l2;
					l2 = l1;
					l1 = tmp;
				}

			} else { // DOWN TREND				
				// проверка на поглощение след бара
				if (Low[Bar] > Low[Bar-1] ) { // НЕТ ПОГЛОЩЕНИЯ
					l1 = Math.Max(High[Bar-1], High[Bar+1]);					
					l2 = Low[Bar];
				} else {  // ПОГЛОЩЕНИЕ
					l1 = Math.Min(High[Bar-1], High[Bar-2]);
					l2 = Math.Max(Low[Bar-1], Low[Bar-2]);
				}						
				
				//switch
				if (l1 < l2) {
					double tmp = l2;
					l2 = l1;
					l1 = tmp;
				}
				//if (GetRangeTicks(l1, l2)>BarWidth)  l2 = l1-BarWidth*TickSize;
				}		
			return true;
		}
		#endregion
		
		// добавляет новый уровень, проверят чтобы не было дублей
		private int AddLevel(int Bar, double Price1, double Price2) {		
			trueLevel tl;
			for (int i=0; i<Levels.Count; i++) {
				if (Levels[i].Bar == Bar) return 0;
			}
			
			tl  = new trueLevel(Bar, Price1, Price2);
			Levels.Add(tl); 							
			return tl.ID;
		}
			
		int IsTrendSeries(int barCount, bool UpTrend, bool checkOpenClose, bool checkHigh, bool checkLow)  {			
			int i = 0;
			int retVal = 0;
			for (i = 0; i < CurrentBar; i++) {
				if (UpTrend) {
					if (checkOpenClose && !(Open[i] < Close[i])) break;					
					if (checkHigh && !(High[i] > High[i+1])) break;					
					if (checkLow && !(Low[i] > Low[i+1])) break;					
				} else {
					if (checkOpenClose && !(Open[i] > Close[i])) break;					
					if (checkHigh && !(High[i] < High[i+1])) break;					
					if (checkLow && !(Low[i] < Low[i+1])) break;					
				}					
			}
			if (i >= barCount-1) retVal = i;
			
			//проверяем чтобы ето был min или max
			if (UpTrend) {
				if (!(Low[i]<=Low[i-1] && Low[i]<=Low[i+1])) retVal=0;
			} else 
				if (!(High[i]>=High[i-1] && High[i]>=High[i+1])) retVal=0;
			
			//проверяем силу тренда
//			if (retVal>0) {
//				double w;
//				if (UpTrend) w = High[0]-Low[i]; else  w = High[i]-Low[0];
//				w = w / TickSize;
//				Print(Math.Round(w,0));
//				if (w<120*TF/15) retVal=0;
//			}
			return retVal;
		}

		bool IsUpBar(int Bar)  {
			return (Open[Bar] < Close[Bar]);
		}

		bool IsDownBar (int Bar)  {
			return (Open[Bar] > Close[Bar]);
		}

		int GetAvgBarRange(int Bar, int BarsCnt) {			
			int BarRange=0;
			if (BarsCnt==0) return 0;

			for (int i=Bar; i<=BarsCnt; i++) BarRange += GetBarRange(i);							
			return (int)(BarRange/BarsCnt);
		}

//		int GetAvgBarSeriesHeight(int Bar, int BarsCnt) {			
//			int BarRange=0;
//			if (BarsCnt==0) return 0;			
//	
//			for (int i=Bar;i<BarsCnt;i++) BarRange +=GetBarRange(i);							
//			return (int)(BarRange / BarsCnt);
//		}

		int GetRangeTicks(double Price1, double Price2){			
			return (int)(Math.Round(Math.Abs(Price1 - Price2)/TickSize,10));			
		}

		int GetBarRange(int Bar) {
			return (int)(Math.Round(Math.Abs(High[Bar]-Low[Bar])/TickSize,10));			
		}
        #region Properties
        [Browsable(false)]	// this line prevents the data series from being displayed in the indicator properties dialog, do not remove
        [XmlIgnore()]		// this line ensures that the indicator can be saved/recovered as part of a chart template, do not remove
        public DataSeries Plot0
        {
            get { return Values[0]; }
        }

        [Description("")]
        [GridCategory("Parameters")]
        private int Period
        {
            get { return period; }
            set { period = Math.Max(1, value); }
        }

        [Description("")]
        [GridCategory("Parameters")]
        private int MFactor
        {
            get { return mFactor; }
            set { mFactor = Math.Max(1, value); }
        }

		/************************************/		
		private int nLevelPrecision=15;
		[Description("Number of bars to find LH and HL values. Default value is 10 bars.")]
        [GridCategory("Parameters")]        
		public int LevelPrecision
        {
            get { return nLevelPrecision; }
            set { nLevelPrecision= Math.Max(1, value); }
        }
		
		/************************************/		
		[Description("Number of trend ticks. Example: 120 ticks is optimal for 60min chart for GC")]
        [GridCategory("Parameters")]		
        public int TrendPower
        {
            get { return nTrendPower; }
            set { nTrendPower = Math.Max(1, value); }
        }
						
		/************************************/		
		private Color cDownAreaColor = Color.LightPink;				
		[Description("Area Color")]
        [Category("Colors")]
		[Gui.Design.DisplayName("Down Area Color")]
		[XmlIgnore()]
		public Color DownAreaColor
        {
            get { return cDownAreaColor; }
            set { cDownAreaColor = value; }
        }
		/************************************/
		private Color cDownBorderColor = Color.IndianRed;		
		[XmlIgnore()]
		[Description("Border Color")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("Down Border Color")]
		public Color DownBorderColor
        {
            get { return cDownBorderColor; }
            set { cDownBorderColor = value; }
        }
		/************************************/

		/************************************/
		private Color cUpAreaColor = Color.DarkSeaGreen;		
		[Description("Area Color")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("Up Area Color")]
		public Color UpAreaColor
        {
            get { return cUpAreaColor; }
            set { cUpAreaColor = value; }
        }
		/************************************/
		private Color cUpBorderColor = Color.DarkOliveGreen;		
		[Description("Border Color")]
        [Category("Colors")]
		[Gui.Design.DisplayNameAttribute("Up Border Color")]		
		public Color UpBorderColor
        {
            get { return cUpBorderColor; }
            set { cUpBorderColor = value; }
        }
		/************************************/		
		#endregion

    }
}

#region NinjaScript generated code. Neither change nor remove.
// This namespace holds all indicators and is required. Do not change it.
namespace NinjaTrader.Indicator
{
    public partial class Indicator : IndicatorBase
    {
        private TrueLevels[] cacheTrueLevels = null;

        private static TrueLevels checkTrueLevels = new TrueLevels();

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public TrueLevels TrueLevels(int levelPrecision, int trendPower)
        {
            return TrueLevels(Input, levelPrecision, trendPower);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public TrueLevels TrueLevels(Data.IDataSeries input, int levelPrecision, int trendPower)
        {
            if (cacheTrueLevels != null)
                for (int idx = 0; idx < cacheTrueLevels.Length; idx++)
                    if (cacheTrueLevels[idx].LevelPrecision == levelPrecision && cacheTrueLevels[idx].TrendPower == trendPower && cacheTrueLevels[idx].EqualsInput(input))
                        return cacheTrueLevels[idx];

            lock (checkTrueLevels)
            {
                checkTrueLevels.LevelPrecision = levelPrecision;
                levelPrecision = checkTrueLevels.LevelPrecision;
                checkTrueLevels.TrendPower = trendPower;
                trendPower = checkTrueLevels.TrendPower;

                if (cacheTrueLevels != null)
                    for (int idx = 0; idx < cacheTrueLevels.Length; idx++)
                        if (cacheTrueLevels[idx].LevelPrecision == levelPrecision && cacheTrueLevels[idx].TrendPower == trendPower && cacheTrueLevels[idx].EqualsInput(input))
                            return cacheTrueLevels[idx];

                TrueLevels indicator = new TrueLevels();
                indicator.BarsRequired = BarsRequired;
                indicator.CalculateOnBarClose = CalculateOnBarClose;
#if NT7
                indicator.ForceMaximumBarsLookBack256 = ForceMaximumBarsLookBack256;
                indicator.MaximumBarsLookBack = MaximumBarsLookBack;
#endif
                indicator.Input = input;
                indicator.LevelPrecision = levelPrecision;
                indicator.TrendPower = trendPower;
                Indicators.Add(indicator);
                indicator.SetUp();

                TrueLevels[] tmp = new TrueLevels[cacheTrueLevels == null ? 1 : cacheTrueLevels.Length + 1];
                if (cacheTrueLevels != null)
                    cacheTrueLevels.CopyTo(tmp, 0);
                tmp[tmp.Length - 1] = indicator;
                cacheTrueLevels = tmp;
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
        public Indicator.TrueLevels TrueLevels(int levelPrecision, int trendPower)
        {
            return _indicator.TrueLevels(Input, levelPrecision, trendPower);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.TrueLevels TrueLevels(Data.IDataSeries input, int levelPrecision, int trendPower)
        {
            return _indicator.TrueLevels(input, levelPrecision, trendPower);
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
        public Indicator.TrueLevels TrueLevels(int levelPrecision, int trendPower)
        {
            return _indicator.TrueLevels(Input, levelPrecision, trendPower);
        }

        /// <summary>
        /// Enter the description of your new custom indicator here
        /// </summary>
        /// <returns></returns>
        public Indicator.TrueLevels TrueLevels(Data.IDataSeries input, int levelPrecision, int trendPower)
        {
            if (InInitialize && input == null)
                throw new ArgumentException("You only can access an indicator with the default input/bar series from within the 'Initialize()' method");

            return _indicator.TrueLevels(input, levelPrecision, trendPower);
        }
    }
}
#endregion
