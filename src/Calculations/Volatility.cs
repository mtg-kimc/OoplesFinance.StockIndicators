﻿//     Ooples Finance Stock Indicator Library
//     https://ooples.github.io/OoplesFinance.StockIndicators/
//
//     Copyright © Franklin Moormann, 2020-2022
//     cheatcountry@gmail.com
//
//     This library is free software and it uses the Apache 2.0 license
//     so if you are going to re-use or modify my code then I just ask
//     that you include my copyright info and my contact info in a comment

namespace OoplesFinance.StockIndicators;

public static partial class Calculations
{
    /// <summary>
    /// Calculates the Standard Deviation Volatility
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateStandardDeviationVolatility(this StockData stockData, MovingAvgType maType = MovingAvgType.SimpleMovingAverage,
        int length = 20)
    {
        List<double> stdDevVolatilityList = new();
        List<double> deviationSquaredList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var smaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentSma = smaList[i];
            double currentDeviation = currentValue - currentSma;

            double deviationSquared = Pow(currentDeviation, 2);
            deviationSquaredList.AddRounded(deviationSquared);
        }

        var divisionOfSumList = GetMovingAverageList(stockData, maType, length, deviationSquaredList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double divisionOfSum = divisionOfSumList[i];

            double stdDevVolatility = Sqrt(divisionOfSum);
            stdDevVolatilityList.AddRounded(stdDevVolatility);
        }

        var stdDevSmaList = GetMovingAverageList(stockData, maType, length, stdDevVolatilityList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentSma = smaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevSma = i >= 1 ? smaList[i - 1] : 0;
            double stdDev = stdDevVolatilityList[i];
            double stdDevMa = stdDevSmaList[i];

            Signal signal = GetVolatilitySignal(currentValue - currentSma, prevValue - prevSma, stdDev, stdDevMa);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "StdDev", stdDevVolatilityList },
            { "Variance", divisionOfSumList },
            { "Signal", stdDevSmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = stdDevVolatilityList;
        stockData.IndicatorName = IndicatorName.StandardDeviationVolatility;

        return stockData;
    }

    /// <summary>
    /// Calculates the historical volatility.
    /// </summary>
    /// <param name="stockData">The stock data.</param>
    /// <param name="maType">Type of the ma.</param>
    /// <param name="length">The length.</param>
    /// <returns></returns>
    public static StockData CalculateHistoricalVolatility(this StockData stockData, MovingAvgType maType = MovingAvgType.ExponentialMovingAverage, int length = 20)
    {
        List<double> hvList = new();
        List<double> tempLogList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        double annualSqrt = Sqrt(365);

        var emaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double temp = prevValue != 0 ? currentValue / prevValue : 0;

            double tempLog = temp > 0 ? Math.Log(temp) : 0;
            tempLogList.AddRounded(tempLog);
        }

        stockData.CustomValuesList = tempLogList;
        var stdDevLogList = CalculateStandardDeviationVolatility(stockData, maType, length).CustomValuesList;
        for (int i = 0; i < stockData.Count; i++)
        {
            var stdDevLog = stdDevLogList[i];
            double currentEma = emaList[i];
            double prevEma = i >= 1 ? emaList[i - 1] : 0;
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;

            double prevHv = hvList.LastOrDefault();
            double hv = 100 * stdDevLog * annualSqrt;
            hvList.AddRounded(hv);

            var signal = GetVolatilitySignal(currentValue - currentEma, prevValue - prevEma, hv, prevHv);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Hv", hvList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = hvList;
        stockData.IndicatorName = IndicatorName.HistoricalVolatility;

        return stockData;
    }

    /// <summary>
    /// Calculates the Moving Average BandWidth
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="fastLength"></param>
    /// <param name="slowLength"></param>
    /// <param name="mult"></param>
    /// <returns></returns>
    public static StockData CalculateMovingAverageBandWidth(this StockData stockData, MovingAvgType maType = MovingAvgType.ExponentialMovingAverage, 
        int fastLength = 10, int slowLength = 50, double mult = 1)
    {
        List<double> mabwList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var mabList = CalculateMovingAverageBands(stockData, maType, fastLength, slowLength, mult);
        var ubList = mabList.OutputValues["UpperBand"];
        var lbList = mabList.OutputValues["LowerBand"];
        var maList = mabList.OutputValues["MiddleBand"];

        for (int i = 0; i < stockData.Count; i++)
        {
            double mb = maList[i];
            double ub = ubList[i];
            double lb = lbList[i];
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevMb = i >= 1 ? maList[i - 1] : 0;
            double prevUb = i >= 1 ? ubList[i - 1] : 0;
            double prevLb = i >= 1 ? lbList[i - 1] : 0;

            double mabw = mb != 0 ? (ub - lb) / mb * 100 : 0;
            mabwList.AddRounded(mabw);

            var signal = GetBollingerBandsSignal(currentValue - mb, prevValue - prevMb, currentValue, prevValue, ub, prevUb, lb, prevLb);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Mabw", mabwList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = mabwList;
        stockData.IndicatorName = IndicatorName.MovingAverageBandWidth;

        return stockData;
    }

    /// <summary>
    /// Calculates the Moving Average Adaptive Filter
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="length"></param>
    /// <param name="filter"></param>
    /// <param name="fastAlpha"></param>
    /// <param name="slowAlpha"></param>
    /// <returns></returns>
    public static StockData CalculateMovingAverageAdaptiveFilter(this StockData stockData, int length = 10, double filter = 0.15, 
        double fastAlpha = 0.667, double slowAlpha = 0.0645)
    {
        List<double> amaList = new();
        List<double> amaDiffList = new();
        List<double> maafList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var erList = CalculateKaufmanAdaptiveMovingAverage(stockData, length: length).OutputValues["Er"];
        var emaList = GetMovingAverageList(stockData, MovingAvgType.ExponentialMovingAverage, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double prevAma = i >= 1 ? amaList[i - 1] : currentValue;
            double er = erList[i];
            double sm = Pow((er * (fastAlpha - slowAlpha)) + slowAlpha, 2);

            double ama = prevAma + (sm * (currentValue - prevAma));
            amaList.AddRounded(ama);

            double amaDiff = ama - prevAma;
            amaDiffList.AddRounded(amaDiff);
        }

        stockData.CustomValuesList = amaDiffList;
        var stdDevList = CalculateStandardDeviationVolatility(stockData, length: length).CustomValuesList;
        for (int i = 0; i < stockData.Count; i++)
        {
            double stdDev = stdDevList[i];
            double currentValue = inputList[i];
            double ema = emaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;

            double prevMaaf = maafList.LastOrDefault();
            double maaf = stdDev * filter;
            maafList.AddRounded(maaf);

            var signal = GetVolatilitySignal(currentValue - ema, prevValue - prevEma, maaf, prevMaaf);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Maaf", maafList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = maafList;
        stockData.IndicatorName = IndicatorName.MovingAverageAdaptiveFilter;

        return stockData;
    }

    /// <summary>
    /// Calculates the Relative Normalized Volatility
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="marketData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateRelativeNormalizedVolatility(this StockData stockData, StockData marketData,
        MovingAvgType maType = MovingAvgType.SimpleMovingAverage, int length = 14)
    {
        List<double> absZsrcList = new();
        List<double> absZspList = new();
        List<double> rList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);
        var (spInputList, _, _, _, _) = GetInputValuesList(marketData);

        if (stockData.Count == marketData.Count)
        {
            var emaList = GetMovingAverageList(stockData, maType, length, inputList);
            var stdDevList = CalculateStandardDeviationVolatility(stockData, maType, length).CustomValuesList;
            var spStdDevList = CalculateStandardDeviationVolatility(marketData, maType, length).CustomValuesList;

            for (int i = 0; i < stockData.Count; i++)
            {
                double currentValue = inputList[i];
                double prevValue = i >= 1 ? inputList[i - 1] : 0;
                double spValue = spInputList[i];
                double prevSpValue = i >= 1 ? spInputList[i - 1] : 0;
                double stdDev = stdDevList[i];
                double spStdDev = spStdDevList[i];
                double d = MinPastValues(i, 1, currentValue - prevValue);
                double sp = spValue - prevSpValue;
                double zsrc = stdDev != 0 ? d / stdDev : 0;
                double zsp = spStdDev != 0 ? sp / spStdDev : 0;

                double absZsrc = Math.Abs(zsrc);
                absZsrcList.AddRounded(absZsrc);

                double absZsp = Math.Abs(zsp);
                absZspList.AddRounded(absZsp);
            }

            var absZsrcSmaList = GetMovingAverageList(stockData, maType, length, absZsrcList);
            var absZspSmaList = GetMovingAverageList(marketData, maType, length, absZspList);
            for (int i = 0; i < stockData.Count; i++)
            {
                double currentValue = inputList[i];
                double currentEma = emaList[i];
                double absZsrcSma = absZsrcSmaList[i];
                double absZspSma = absZspSmaList[i];
                double prevValue = i >= 1 ? inputList[i - 1] : 0;
                double prevEma = i >= 1 ? emaList[i - 1] : 0;

                double r = absZspSma != 0 ? absZsrcSma / absZspSma : 0;
                rList.AddRounded(r);

                var signal = GetVolatilitySignal(currentValue - currentEma, prevValue - prevEma, r, 1);
                signalsList.Add(signal);
            }
        }

        stockData.OutputValues = new()
        {
            { "Rnv", rList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = rList;
        stockData.IndicatorName = IndicatorName.RelativeNormalizedVolatility;

        return stockData;
    }

    /// <summary>
    /// Calculates the Reversal Points
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateReversalPoints(this StockData stockData, MovingAvgType maType = MovingAvgType.ExponentialMovingAverage,
        int length = 100)
    {
        List<double> aList = new();
        List<double> bList = new();
        List<double> bSumList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        double c = length + (length / Sqrt(length) / 2);
        int length1 = MinOrMax((int)Math.Ceiling((double)length / 2));

        var emaList = GetMovingAverageList(stockData, maType, length1, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double max = Math.Max(currentValue, prevValue);
            double min = Math.Min(currentValue, prevValue);

            double a = max - min;
            aList.AddRounded(a);
        }

        var aEma1List = GetMovingAverageList(stockData, maType, length1, aList);
        var aEma2List = GetMovingAverageList(stockData, maType, length1, aEma1List);
        for (int i = 0; i < stockData.Count; i++)
        {
            double aEma1 = aEma1List[i];
            double aEma2 = aEma2List[i];
            double currentValue = inputList[i];
            double ema = emaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;

            double b = aEma2 != 0 ? aEma1 / aEma2 : 0;
            bList.AddRounded(b);

            double bSum = bList.TakeLastExt(length).Sum();
            bSumList.AddRounded(bSum);

            var signal = GetVolatilitySignal(currentValue - ema, prevValue - prevEma, bSum, c);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Rp", bSumList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = bSumList;
        stockData.IndicatorName = IndicatorName.ReversalPoints;

        return stockData;
    }

    /// <summary>
    /// Calculates the Mayer Multiple
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <param name="threshold"></param>
    /// <returns></returns>
    public static StockData CalculateMayerMultiple(this StockData stockData, MovingAvgType maType = MovingAvgType.SimpleMovingAverage, int length = 200,
        double threshold = 2.4)
    {
        List<double> mmList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var smaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentSma = smaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevSma = i >= 1 ? smaList[i - 1] : 0;

            double mm = currentSma != 0 ? currentValue / currentSma : 0;
            mmList.AddRounded(mm);

            var signal = GetVolatilitySignal(currentValue - currentSma, prevValue - prevSma, mm, threshold);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Mm", mmList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = mmList;
        stockData.IndicatorName = IndicatorName.MayerMultiple;

        return stockData;
    }

    /// <summary>
    /// Calculates the Motion Smoothness Index
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateMotionSmoothnessIndex(this StockData stockData, int length = 50)
    {
        List<double> bList = new();
        List<double> chgList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var stdDevList = CalculateStandardDeviationVolatility(stockData, length: length).CustomValuesList;
        var emaList = GetMovingAverageList(stockData, MovingAvgType.ExponentialMovingAverage, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;

            double chg = MinPastValues(i, 1, currentValue - prevValue);
            chgList.AddRounded(chg);
        }

        stockData.CustomValuesList = chgList;
        var aChgStdDevList = CalculateStandardDeviationVolatility(stockData, length: length).CustomValuesList;
        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentEma = emaList[i];
            double aChgStdDev = aChgStdDevList[i];
            double stdDev = stdDevList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;

            double b = stdDev != 0 ? aChgStdDev / stdDev : 0;
            bList.AddRounded(b);

            var signal = GetVolatilitySignal(currentValue - currentEma, prevValue - prevEma, b, 0.5);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Msi", bList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = bList;
        stockData.IndicatorName = IndicatorName.MotionSmoothnessIndex;

        return stockData;
    }

    /// <summary>
    /// Calculates the Market Meanness Index
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateMarketMeannessIndex(this StockData stockData, MovingAvgType maType = MovingAvgType.EhlersNoiseEliminationTechnology, int length = 100)
    {
        List<double> mmiList = new();
        List<double> tempList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var maList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            tempList.AddRounded(currentValue);

            double median = tempList.TakeLastExt(length).Median();
            int nl = 0, nh = 0;
            for (int j = 1; j < length; j++)
            {
                var value1 = i >= j - 1 ? tempList[i - (j - 1)] : 0;
                var value2 = i >= j ? tempList[i - j] : 0;

                if (value1 > median && value1 > value2)
                {
                    nl++;
                }
                else if (value1 < median && value1 < value2)
                {
                    nh++;
                }
            }

            double mmi = length != 1 ? 100 * (nl + nh) / (length - 1) : 0;
            mmiList.AddRounded(mmi);
        }

        var mmiFilterList = GetMovingAverageList(stockData, maType, length, mmiList);
        for (int i = 0; i < stockData.Count; i++)
        {
            var mmiFilt = mmiFilterList[i];
            var prevMmiFilt1 = i >= 1 ? mmiFilterList[i - 1] : 0;
            var prevMmiFilt2 = i >= 2 ? mmiFilterList[i - 2] : 0;
            var currentValue = inputList[i];
            var currentMa = maList[i];

            Signal signal = GetConditionSignal(currentValue < currentMa && ((mmiFilt > prevMmiFilt1 && prevMmiFilt1 < prevMmiFilt2) || (mmiFilt < prevMmiFilt1 && prevMmiFilt1 > prevMmiFilt2)), currentValue < currentMa && ((mmiFilt > prevMmiFilt1 && prevMmiFilt1 < prevMmiFilt2) || (mmiFilt < prevMmiFilt1 && prevMmiFilt1 > prevMmiFilt2)));
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Mmi", mmiList },
            { "MmiSmoothed", mmiFilterList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = mmiList;
        stockData.IndicatorName = IndicatorName.MarketMeannessIndex;

        return stockData;
    }

    /// <summary>
    /// Calculates the choppiness index.
    /// </summary>
    /// <param name="stockData">The stock data.</param>
    /// <param name="maType">Type of the ma.</param>
    /// <param name="length">The length.</param>
    /// <returns></returns>
    public static StockData CalculateChoppinessIndex(this StockData stockData, MovingAvgType maType = MovingAvgType.ExponentialMovingAverage,
        int length = 14)
    {
        List<double> ciList = new();
        List<double> trList = new();
        List<Signal> signalsList = new();

        var (inputList, highList, lowList, _, _) = GetInputValuesList(stockData);
        var (highestHighList, lowestLowList) = GetMaxAndMinValuesList(highList, lowList, length);
        var emaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentEma = emaList[i];
            double currentHigh = highList[i];
            double currentLow = lowList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;
            double highestHigh = highestHighList[i];
            double lowestLow = lowestLowList[i];
            double range = highestHigh - lowestLow;

            double tr = CalculateTrueRange(currentHigh, currentLow, prevValue);
            trList.AddRounded(tr);

            double trSum = trList.TakeLastExt(length).Sum();
            double ci = range > 0 ? 100 * Math.Log10(trSum / range) / Math.Log10(length) : 0;
            ciList.AddRounded(ci);

            var signal = GetVolatilitySignal(currentValue - currentEma, prevValue - prevEma, ci, 38.2);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Ci", ciList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = ciList;
        stockData.IndicatorName = IndicatorName.ChoppinessIndex;

        return stockData;
    }

    /// <summary>
    /// Calculates the Ultimate Volatility Indicator
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateUltimateVolatilityIndicator(this StockData stockData, MovingAvgType maType = MovingAvgType.ExponentialMovingAverage, 
        int length = 14)
    {
        List<double> uviList = new();
        List<double> absList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, openList, _) = GetInputValuesList(stockData);

        var maList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentOpen = openList[i];
            double currentClose = inputList[i];
            double currentMa = maList[i];
            double prevClose = i >= 1 ? inputList[i - 1] : 0;
            double prevMa = i >= 1 ? maList[i - 1] : 0;

            double abs = Math.Abs(currentClose - currentOpen);
            absList.AddRounded(abs);

            double uvi = (double)1 / length * absList.TakeLastExt(length).Sum();
            uviList.AddRounded(uvi);

            var signal = GetVolatilitySignal(currentClose - currentMa, prevClose - prevMa, uvi, 1);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Uvi", uviList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = uviList;
        stockData.IndicatorName = IndicatorName.UltimateVolatilityIndicator;

        return stockData;
    }

    /// <summary>
    /// Calculates the Qma Sma Difference
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateQmaSmaDifference(this StockData stockData, int length = 14)
    {
        List<double> cList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var qmaList = CalculateQuadraticMovingAverage(stockData, length).CustomValuesList;
        var smaList = CalculateSimpleMovingAverage(stockData, length).CustomValuesList;
        var emaList = CalculateExponentialMovingAverage(stockData, length).CustomValuesList;

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentEma = emaList[i];
            double sma = smaList[i];
            double qma = qmaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;

            double prevC = cList.LastOrDefault();
            double c = qma - sma;
            cList.AddRounded(c);

            var signal = GetVolatilitySignal(currentValue - currentEma, prevValue - prevEma, c, prevC);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "QmaSmaDiff", cList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = cList;
        stockData.IndicatorName = IndicatorName.QmaSmaDifference;

        return stockData;
    }

    /// <summary>
    /// Calculates the Garman Klass Volatility
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <param name="signalLength"></param>
    /// <returns></returns>
    public static StockData CalculateGarmanKlassVolatility(this StockData stockData, MovingAvgType maType = MovingAvgType.WeightedMovingAverage,
        int length = 14, int signalLength = 7)
    {
        List<double> gcvList = new();
        List<double> logList = new();
        List<Signal> signalsList = new();
        var (inputList, highList, lowList, openList, _) = GetInputValuesList(stockData);

        var wmaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentHigh = highList[i];
            double currentLow = lowList[i];
            double currentOpen = openList[i];
            double currentClose = inputList[i];
            double logHl = currentLow != 0 ? Math.Log(currentHigh / currentLow) : 0;
            double logCo = currentOpen != 0 ? Math.Log(currentClose / currentOpen) : 0;

            double log = (0.5 * Pow(logHl, 2)) - (((2 * Math.Log(2)) - 1) * Pow(logCo, 2));
            logList.AddRounded(log);

            double logSum = logList.TakeLastExt(length).Sum();
            double gcv = length != 0 && logSum != 0 ? Sqrt((double)i / length * logSum) : 0;
            gcvList.AddRounded(gcv);
        }

        var gcvWmaList = GetMovingAverageList(stockData, maType, signalLength, gcvList);
        for (int i = 0; i < stockData.Count; i++)
        {
            var currentClose = inputList[i];
            var wma = wmaList[i];
            var prevClose = i >= 1 ? inputList[i - 1] : 0;
            var prevWma = i >= 1 ? wmaList[i - 1] : 0;
            var gcv = gcvList[i];
            var gcvWma = i >= 1 ? gcvWmaList[i - 1] : 0;

            var signal = GetVolatilitySignal(currentClose - wma, prevClose - prevWma, gcv, gcvWma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Gcv", gcvList },
            { "Signal", gcvWmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = gcvList;
        stockData.IndicatorName = IndicatorName.GarmanKlassVolatility;

        return stockData;
    }

    /// <summary>
    /// Calculates the Gopalakrishnan Range Index
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateGopalakrishnanRangeIndex(this StockData stockData, MovingAvgType maType = MovingAvgType.WeightedMovingAverage,
        int length = 5)
    {
        List<double> gapoList = new();
        List<double> gapoEmaList = new();
        List<Signal> signalsList = new();
        var (inputList, highList, lowList, _, _) = GetInputValuesList(stockData);
        var (highestList, lowestList) = GetMaxAndMinValuesList(highList, lowList, length);

        var wmaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double highestHigh = highestList[i];
            double lowestLow = lowestList[i];
            double range = highestHigh - lowestLow;
            double rangeLog = range > 0 ? Math.Log(range) : 0;

            double gapo = rangeLog / Math.Log(length);
            gapoList.AddRounded(gapo);
        }

        var gapoWmaList = GetMovingAverageList(stockData, maType, length, gapoList);
        for (int i = 0; i < stockData.Count; i++)
        {
            var gapoWma = gapoWmaList[i];
            var prevGapoWma = i >= 1 ? gapoWmaList[i - 1] : 0;
            var currentValue = inputList[i];
            var prevValue = i >= 1 ? inputList[i - 1] : 0;
            var currentWma = wmaList[i];
            var prevWma = i >= 1 ? wmaList[i - 1] : 0;

            var signal = GetVolatilitySignal(currentValue - currentWma, prevValue - prevWma, gapoWma, prevGapoWma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Gapo", gapoList },
            { "Signal", gapoEmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = gapoList;
        stockData.IndicatorName = IndicatorName.GopalakrishnanRangeIndex;

        return stockData;
    }

    /// <summary>
    /// Calculates the Historical Volatility Percentile
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <param name="annualLength"></param>
    /// <returns></returns>
    public static StockData CalculateHistoricalVolatilityPercentile(this StockData stockData,
        MovingAvgType maType = MovingAvgType.ExponentialMovingAverage, int length = 21, int annualLength = 252)
    {
        List<double> devLogSqList = new();
        List<double> devLogSqAvgList = new();
        List<double> hvList = new();
        List<double> hvpList = new();
        List<double> tempLogList = new();
        List<double> stdDevLogList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var emaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentEma = emaList[i];
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double temp = prevValue != 0 ? currentValue / prevValue : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;

            double tempLog = temp > 0 ? Math.Log(temp) : 0;
            tempLogList.AddRounded(tempLog);

            double avgLog = tempLogList.TakeLastExt(length).Average();
            double devLogSq = Pow(tempLog - avgLog, 2);
            devLogSqList.AddRounded(devLogSq);

            double devLogSqAvg = devLogSqList.TakeLastExt(length).Sum() / (length - 1);
            double stdDevLog = devLogSqAvg >= 0 ? Sqrt(devLogSqAvg) : 0;

            double hv = stdDevLog * Sqrt(annualLength);
            hvList.AddRounded(hv);

            double count = hvList.TakeLastExt(annualLength).Where(i => i < hv).Count();
            double hvp = count / annualLength * 100;
            hvpList.AddRounded(hvp);
        }

        var hvpEmaList = GetMovingAverageList(stockData, maType, length, hvpList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double currentEma = emaList[i];
            double prevEma = i >= 1 ? emaList[i - 1] : 0;
            double hvp = hvpList[i];
            double hvpEma = hvpEmaList[i];

            var signal = GetVolatilitySignal(currentValue - currentEma, prevValue - prevEma, hvp, hvpEma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Hvp", hvpList },
            { "Signal", hvpEmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = hvpList;
        stockData.IndicatorName = IndicatorName.HistoricalVolatilityPercentile;

        return stockData;
    }

    /// <summary>
    /// Calculates the Fast Z Score
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateFastZScore(this StockData stockData, MovingAvgType maType = MovingAvgType.SimpleMovingAverage, int length = 200)
    {
        List<double> gsList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        int length2 = MinOrMax((int)Math.Ceiling((double)length / 2));

        var smaList = GetMovingAverageList(stockData, maType, length, inputList);
        stockData.CustomValuesList = smaList;
        var smaLinregList = CalculateLinearRegression(stockData, length).CustomValuesList;
        stockData.CustomValuesList = smaList;
        var linreg2List = CalculateLinearRegression(stockData, length2).CustomValuesList;
        stockData.CustomValuesList = smaList;
        var smaStdDevList = CalculateStandardDeviationVolatility(stockData, maType, length).CustomValuesList;

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double sma = smaList[i];
            double stdDev = smaStdDevList[i];
            double linreg = smaLinregList[i];
            double linreg2 = linreg2List[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevSma = i >= 1 ? smaList[i - 1] : 0;

            double gs = stdDev != 0 ? (linreg2 - linreg) / stdDev / 2 : 0;
            gsList.AddRounded(gs);

            var signal = GetVolatilitySignal(currentValue - sma, prevValue - prevSma, gs, 0);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Fzs", gsList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = gsList;
        stockData.IndicatorName = IndicatorName.FastZScore;

        return stockData;
    }

    /// <summary>
    /// Calculates the Volatility Switch Indicator
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateVolatilitySwitchIndicator(this StockData stockData, MovingAvgType maType = MovingAvgType.WeightedMovingAverage,
        int length = 14)
    {
        List<double> drList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;

            double rocSma = (currentValue + prevValue) / 2;
            double dr = rocSma != 0 ? MinPastValues(i, 1, currentValue - prevValue) / rocSma : 0;
            drList.AddRounded(dr);
        }

        stockData.CustomValuesList = drList;
        var volaList = CalculateStandardDeviationVolatility(stockData, maType, length).CustomValuesList;
        var vswitchList = GetMovingAverageList(stockData, maType, length, volaList);
        var wmaList = GetMovingAverageList(stockData, maType, length, inputList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentWma = wmaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevWma = i >= 1 ? wmaList[i - 1] : 0;
            double vswitch14 = vswitchList[i];

            var signal = GetVolatilitySignal(currentValue - currentWma, prevValue - prevWma, vswitch14, 0.5);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Vsi", vswitchList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = vswitchList;
        stockData.IndicatorName = IndicatorName.VolatilitySwitchIndicator;

        return stockData;
    }

    /// <summary>
    /// Calculates the Vertical Horizontal Filter
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <param name="signalLength"></param>
    /// <returns></returns>
    public static StockData CalculateVerticalHorizontalFilter(this StockData stockData, MovingAvgType maType = MovingAvgType.WeightedMovingAverage,
        int length = 18, int signalLength = 6)
    {
        List<double> vhfList = new();
        List<double> changeList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);
        var (highestList, lowestList) = GetMaxAndMinValuesList(inputList, length);

        var wmaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double currentValue = inputList[i];
            double highestPrice = highestList[i];
            double lowestPrice = lowestList[i];
            double numerator = Math.Abs(highestPrice - lowestPrice);

            double priceChange = Math.Abs(MinPastValues(i, 1, currentValue - prevValue));
            changeList.AddRounded(priceChange);

            double denominator = changeList.TakeLastExt(length).Sum();
            double vhf = denominator != 0 ? numerator / denominator : 0;
            vhfList.AddRounded(vhf);
        }

        var vhfWmaList = GetMovingAverageList(stockData, maType, signalLength, vhfList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double currentWma = wmaList[i];
            double prevWma = i >= 1 ? wmaList[i - 1] : 0;
            double vhfWma = vhfWmaList[i];
            double vhf = vhfList[i];

            var signal = GetVolatilitySignal(currentValue - currentWma, prevValue - prevWma, vhf, vhfWma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Vhf", vhfList },
            { "Signal", vhfWmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = vhfList;
        stockData.IndicatorName = IndicatorName.VerticalHorizontalFilter;

        return stockData;
    }

    /// <summary>
    /// Calculates the Closed Form Distance Volatility
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateClosedFormDistanceVolatility(this StockData stockData,
        MovingAvgType maType = MovingAvgType.ExponentialMovingAverage, int length = 14)
    {
        List<double> tempHighList = new();
        List<double> tempLowList = new();
        List<double> hvList = new();
        List<Signal> signalsList = new();
        var (inputList, highList, lowList, _, _) = GetInputValuesList(stockData);

        var emaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double ema = emaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;

            double currentHigh = highList[i];
            tempHighList.AddRounded(currentHigh);

            double currentLow = lowList[i];
            tempLowList.AddRounded(currentLow);

            double a = tempHighList.TakeLastExt(length).Sum();
            double b = tempLowList.TakeLastExt(length).Sum();
            double abAvg = (a + b) / 2;

            double prevHv = hvList.LastOrDefault();
            double hv = abAvg != 0 && a != b ? Sqrt(1 - (Pow(a, 0.25) * Pow(b, 0.25) / Pow(abAvg, 0.5))) : 0;
            hvList.AddRounded(hv);

            var signal = GetVolatilitySignal(currentValue - ema, prevValue - prevEma, hv, prevHv);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Cfdv", hvList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = hvList;
        stockData.IndicatorName = IndicatorName.ClosedFormDistanceVolatility;

        return stockData;
    }

    /// <summary>
    /// Calculates the Projection Oscillator
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <param name="smoothLength"></param>
    /// <returns></returns>
    public static StockData CalculateProjectionOscillator(this StockData stockData, MovingAvgType maType = MovingAvgType.WeightedMovingAverage,
        int length = 14, int smoothLength = 4)
    {
        List<double> pboList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var projectionBandsList = CalculateProjectionBands(stockData, length);
        var puList = projectionBandsList.OutputValues["UpperBand"];
        var plList = projectionBandsList.OutputValues["LowerBand"];
        var wmaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double pl = plList[i];
            double pu = puList[i];

            double pbo = pu - pl != 0 ? 100 * (currentValue - pl) / (pu - pl) : 0;
            pboList.AddRounded(pbo);
        }

        var pboSignalList = GetMovingAverageList(stockData, maType, smoothLength, pboList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double pbo = pboSignalList[i];
            double prevPbo = i >= 1 ? pboSignalList[i - 1] : 0;
            double wma = wmaList[i];
            double currentValue = inputList[i];
            double prevWma = i >= 1 ? wmaList[i - 1] : 0;
            double prevValue = i >= 1 ? inputList[i - 1] : 0;

            var signal = GetVolatilitySignal(currentValue - wma, prevValue - prevWma, pbo, prevPbo);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Pbo", pboList },
            { "Signal", pboSignalList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = pboList;
        stockData.IndicatorName = IndicatorName.ProjectionOscillator;

        return stockData;
    }

    /// <summary>
    /// Calculates the Projection Bandwidth
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateProjectionBandwidth(this StockData stockData, MovingAvgType maType = MovingAvgType.WeightedMovingAverage,
        int length = 14)
    {
        List<double> pbwList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var projectionBandsList = CalculateProjectionBands(stockData, length);
        var puList = projectionBandsList.OutputValues["UpperBand"];
        var plList = projectionBandsList.OutputValues["LowerBand"];
        var wmaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double pu = puList[i];
            double pl = plList[i];

            double pbw = pu + pl != 0 ? 200 * (pu - pl) / (pu + pl) : 0;
            pbwList.AddRounded(pbw);
        }

        var pbwSignalList = GetMovingAverageList(stockData, maType, length, pbwList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double pbw = pbwList[i];
            double pbwSignal = pbwSignalList[i];
            double wma = wmaList[i];
            double currentValue = inputList[i];
            double prevWma = i >= 1 ? wmaList[i - 1] : 0;
            double prevValue = i >= 1 ? inputList[i - 1] : 0;

            var signal = GetVolatilitySignal(currentValue - wma, prevValue - prevWma, pbw, pbwSignal);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Pbw", pbwList },
            { "Signal", pbwSignalList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = pbwList;
        stockData.IndicatorName = IndicatorName.ProjectionBandwidth;

        return stockData;
    }

    /// <summary>
    /// Calculates the Donchian Channel Width
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <param name="smoothLength"></param>
    /// <returns></returns>
    public static StockData CalculateDonchianChannelWidth(this StockData stockData, MovingAvgType maType = MovingAvgType.SimpleMovingAverage, int length = 20,
        int smoothLength = 22)
    {
        List<double> donchianWidthList = new();
        List<Signal> signalsList = new();
        var (inputList, highList, lowList, _, _) = GetInputValuesList(stockData);
        var (highestList, lowestList) = GetMaxAndMinValuesList(highList, lowList, length);

        var smaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double upper = highestList[i];
            double lower = lowestList[i];

            double donchianWidth = upper - lower;
            donchianWidthList.AddRounded(donchianWidth);
        }

        var donchianWidthSmaList = GetMovingAverageList(stockData, maType, smoothLength, donchianWidthList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentSma = smaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevSma = i >= 1 ? smaList[i - 1] : 0;
            double donchianWidth = donchianWidthList[i];
            double donchianWidthSma = donchianWidthSmaList[i];

            var signal = GetVolatilitySignal(currentValue - currentSma, prevValue - prevSma, donchianWidth, donchianWidthSma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Dcw", donchianWidthList },
            { "Signal", donchianWidthSmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = donchianWidthList;
        stockData.IndicatorName = IndicatorName.DonchianChannelWidth;

        return stockData;
    }

    /// <summary>
    /// Calculates the Statistical Volatility
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length1"></param>
    /// <param name="length2"></param>
    /// <returns></returns>
    public static StockData CalculateStatisticalVolatility(this StockData stockData, MovingAvgType maType = MovingAvgType.ExponentialMovingAverage, 
        int length1 = 30, int length2 = 253)
    {
        List<double> volList = new();
        List<Signal> signalsList = new();
        var (inputList, highList, lowList, _, _) = GetInputValuesList(stockData);
        var (highestList1, lowestList1) = GetMaxAndMinValuesList(inputList, length1);
        var (highestList2, lowestList2) = GetMaxAndMinValuesList(highList, lowList, length1);

        double annualSqrt = Sqrt((double)length2 / length1);

        var emaList = GetMovingAverageList(stockData, maType, length1, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double maxC = highestList1[i];
            double minC = lowestList1[i];
            double maxH = highestList2[i];
            double minL = lowestList2[i];
            double cLog = minC != 0 ? Math.Log(maxC / minC) : 0;
            double hlLog = minL != 0 ? Math.Log(maxH / minL) : 0;

            double vol = MinOrMax(((0.6 * cLog * annualSqrt) + (0.6 * hlLog * annualSqrt)) * 0.5, 2.99, 0);
            volList.AddRounded(vol);
        }

        var volEmaList = GetMovingAverageList(stockData, maType, length1, volList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double currentEma = emaList[i];
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;
            double vol = volList[i];
            double volEma = volEmaList[i];

            var signal = GetVolatilitySignal(currentValue - currentEma, prevValue - prevEma, vol, volEma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Sv", volList },
            { "Signal", volEmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = volList;
        stockData.IndicatorName = IndicatorName.StatisticalVolatility;

        return stockData;
    }

    /// <summary>
    /// Calculates the Standard Deviation
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateStandardDevation(this StockData stockData, MovingAvgType maType = MovingAvgType.SimpleMovingAverage, int length = 14)
    {
        List<double> cList = new();
        List<double> powList = new();
        List<double> tempList = new();
        List<double> sumList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var emaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            tempList.AddRounded(currentValue);

            double sum = tempList.TakeLastExt(length).Sum();
            double sumPow = Pow(sum, 2);
            sumList.AddRounded(sumPow);

            double pow = Pow(currentValue, 2);
            powList.AddRounded(pow);
        }

        var powSmaList = GetMovingAverageList(stockData, maType, length, powList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double a = powSmaList[i];
            double sum = sumList[i];
            double b = sum / Pow(length, 2);

            double c = a - b >= 0 ? Sqrt(a - b) : 0;
            cList.AddRounded(c);
        }

        var cSmaList = GetMovingAverageList(stockData, maType, length, cList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double currentEma = emaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;
            double c = cList[i];
            double cSma = cSmaList[i];

            var signal = GetVolatilitySignal(currentValue - currentEma, prevValue - prevEma, c, cSma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Std", cList },
            { "Signal", cSmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = cList;
        stockData.IndicatorName = IndicatorName.StandardDeviation;

        return stockData;
    }

    /// <summary>
    /// Calculates the Volatility Based Momentum
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length1"></param>
    /// <param name="length2"></param>
    /// <returns></returns>
    public static StockData CalculateVolatilityBasedMomentum(this StockData stockData, MovingAvgType maType = MovingAvgType.WildersSmoothingMethod,
        int length1 = 22, int length2 = 65)
    {
        List<double> vbmList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var atrList = CalculateAverageTrueRange(stockData, maType, length2).CustomValuesList;

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentAtr = atrList[i];
            double currentValue = inputList[i];
            double prevValue = i >= length1 ? inputList[i - length1] : 0;
            double rateOfChange = MinPastValues(i, length1, currentValue - prevValue);

            double vbm = currentAtr != 0 ? rateOfChange / currentAtr : 0;
            vbmList.AddRounded(vbm);
        }

        var vbmEmaList = GetMovingAverageList(stockData, maType, length1, vbmList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double vbm = vbmList[i];
            double vbmEma = vbmEmaList[i];
            double prevVbm = i >= 1 ? vbmList[i - 1] : 0;
            double prevVbmEma = i >= 1 ? vbmEmaList[i - 1] : 0;

            var signal = GetCompareSignal(vbm - vbmEma, prevVbm - prevVbmEma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Vbm", vbmList },
            { "Signal", vbmEmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = vbmList;
        stockData.IndicatorName = IndicatorName.VolatilityBasedMomentum;

        return stockData;
    }

    /// <summary>
    /// Calculates the Volatility Quality Index
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="fastLength"></param>
    /// <param name="slowLength"></param>
    /// <returns></returns>
    public static StockData CalculateVolatilityQualityIndex(this StockData stockData, MovingAvgType maType = MovingAvgType.SimpleMovingAverage,
        int fastLength = 9, int slowLength = 200)
    {
        List<double> vqiList = new();
        List<double> vqiSumList = new();
        List<double> vqiTList = new();
        List<Signal> signalsList = new();
        var (inputList, highList, lowList, openList, _) = GetInputValuesList(stockData);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentHigh = highList[i];
            double currentLow = lowList[i];
            double currentClose = inputList[i];
            double currentOpen = openList[i];
            double prevClose = i >= 1 ? inputList[i - 1] : 0;
            double trueRange = CalculateTrueRange(currentHigh, currentLow, prevClose);

            double prevVqiT = vqiTList.LastOrDefault();
            double vqiT = trueRange != 0 && currentHigh - currentLow != 0 ?
                (((currentClose - prevClose) / trueRange) + ((currentClose - currentOpen) / (currentHigh - currentLow))) * 0.5 : prevVqiT;
            vqiTList.AddRounded(vqiT);

            double vqi = Math.Abs(vqiT) * ((currentClose - prevClose + (currentClose - currentOpen)) * 0.5);
            vqiList.AddRounded(vqi);

            double vqiSum = vqiList.Sum();
            vqiSumList.AddRounded(vqiSum);
        }

        var vqiSumFastSmaList = GetMovingAverageList(stockData, maType, fastLength, vqiSumList);
        var vqiSumSlowSmaList = GetMovingAverageList(stockData, maType, slowLength, vqiSumList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double vqiSum = vqiSumList[i];
            double vqiSumFastSma = vqiSumFastSmaList[i];
            double prevVqiSum = i >= 1 ? vqiSumList[i - 1] : 0;
            double prevVqiSumFastSma = i >= 1 ? vqiSumFastSmaList[i - 1] : 0;

            var signal = GetCompareSignal(vqiSum - vqiSumFastSma, prevVqiSum - prevVqiSumFastSma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Vqi", vqiSumList },
            { "FastSignal", vqiSumFastSmaList },
            { "SlowSignal", vqiSumSlowSmaList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = vqiSumList;
        stockData.IndicatorName = IndicatorName.VolatilityQualityIndex;

        return stockData;
    }

    /// <summary>
    /// Calculates the Sigma Spikes
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateSigmaSpikes(this StockData stockData, MovingAvgType maType = MovingAvgType.ExponentialMovingAverage, int length = 20)
    {
        List<double> retList = new();
        List<double> sigmaList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;

            double ret = prevValue != 0 ? (currentValue / prevValue) - 1 : 0;
            retList.AddRounded(ret);
        }

        stockData.CustomValuesList = retList;
        var stdList = CalculateStandardDeviationVolatility(stockData, maType, length).CustomValuesList;
        for (int i = 0; i < stockData.Count; i++)
        {
            double prevStd = i >= 1 ? stdList[i - 1] : 0;
            double ret = retList[i];

            double sigma = prevStd != 0 ? ret / prevStd : 0;
            sigmaList.AddRounded(sigma);
        }

        var ssList = GetMovingAverageList(stockData, maType, length, sigmaList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double ss = ssList[i];
            double prevSs = i >= 1 ? ssList[i - 1] : 0;

            var signal = GetCompareSignal(ss, prevSs);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Ss", sigmaList },
            { "Signal", ssList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = sigmaList;
        stockData.IndicatorName = IndicatorName.SigmaSpikes;

        return stockData;
    }

    /// <summary>
    /// Calculates the Surface Roughness Estimator
    /// </summary>
    /// <param name="stockData"></param>
    /// <param name="maType"></param>
    /// <param name="length"></param>
    /// <returns></returns>
    public static StockData CalculateSurfaceRoughnessEstimator(this StockData stockData, MovingAvgType maType = MovingAvgType.ExponentialMovingAverage,
        int length = 100)
    {
        List<double> aList = new();
        List<double> corrList = new();
        List<double> tempList = new();
        List<double> prevList = new();
        List<Signal> signalsList = new();
        var (inputList, _, _, _, _) = GetInputValuesList(stockData);

        var emaList = GetMovingAverageList(stockData, maType, length, inputList);

        for (int i = 0; i < stockData.Count; i++)
        {
            double currentValue = inputList[i];
            tempList.AddRounded(currentValue);

            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            prevList.AddRounded(prevValue);

            var corr = GoodnessOfFit.R(prevList.TakeLastExt(length).Select(x => (double)x), tempList.TakeLastExt(length).Select(x => (double)x));
            corr = IsValueNullOrInfinity(corr) ? 0 : corr;
            double a = 1 - (((double)corr + 1) / 2);
            aList.AddRounded(a);
        }

        var aEmaList = GetMovingAverageList(stockData, maType, length, aList);
        for (int i = 0; i < stockData.Count; i++)
        {
            double corr = corrList[i];
            double currentValue = inputList[i];
            double ema = emaList[i];
            double prevValue = i >= 1 ? inputList[i - 1] : 0;
            double prevEma = i >= 1 ? emaList[i - 1] : 0;
            double a = aList[i];
            double aEma = aEmaList[i];

            var signal = GetVolatilitySignal(currentValue - ema, prevValue - prevEma, a, aEma);
            signalsList.Add(signal);
        }

        stockData.OutputValues = new()
        {
            { "Sre", aList }
        };
        stockData.SignalsList = signalsList;
        stockData.CustomValuesList = aList;
        stockData.IndicatorName = IndicatorName.SurfaceRoughnessEstimator;

        return stockData;
    }
}