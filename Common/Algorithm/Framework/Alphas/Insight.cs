﻿/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using QuantConnect.Algorithm.Framework.Alphas.Serialization;
using QuantConnect.Interfaces;
using QuantConnect.Securities;
using static QuantConnect.StringExtensions;

namespace QuantConnect.Algorithm.Framework.Alphas
{
    /// <summary>
    /// Defines a alpha prediction for a single symbol generated by the algorithm
    /// </summary>
    /// <remarks>
    /// Serialization of this type is delegated to the <see cref="InsightJsonConverter"/> which uses the <see cref="SerializedInsight"/> as a model.
    /// </remarks>
    [JsonConverter(typeof(InsightJsonConverter))]
    public class Insight
    {
        private readonly IPeriodSpecification _periodSpecification;

        /// <summary>
        /// Gets the unique identifier for this insight
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Gets the group id this insight belongs to, null if not in a group
        /// </summary>
        public Guid? GroupId { get; private set; }

        /// <summary>
        /// Gets an identifier for the source model that generated this insight.
        /// </summary>
        public string SourceModel { get; set; }

        /// <summary>
        /// Gets the utc time this insight was generated
        /// </summary>
        /// <remarks>
        /// The algorithm framework handles setting this value appropriately.
        /// If providing custom <see cref="Insight"/> implementation, be sure
        /// to set this value to algorithm.UtcTime when the insight is generated.
        /// </remarks>
        public DateTime GeneratedTimeUtc { get; set; }

        /// <summary>
        /// Gets the insight's prediction end time. This is the time when this
        /// insight prediction is expected to be fulfilled. This time takes into
        /// account market hours, weekends, as well as the symbol's data resolution
        /// </summary>
        public DateTime CloseTimeUtc { get; set; }

        /// <summary>
        /// Gets the symbol this insight is for
        /// </summary>
        public Symbol Symbol { get; private set; }

        /// <summary>
        /// Gets the type of insight, for example, price insight or volatility insight
        /// </summary>
        public InsightType Type { get; private set; }

        /// <summary>
        /// Gets the initial reference value this insight is predicting against. The value is dependent on the specified <see cref="InsightType"/>
        /// </summary>
        public decimal ReferenceValue { get; set; }

        /// <summary>
        /// Gets the final reference value, used for scoring, this insight is predicting against. The value is dependent on the specified <see cref="InsightType"/>
        /// </summary>
        public decimal ReferenceValueFinal { get; set; }

        /// <summary>
        /// Gets the predicted direction, down, flat or up
        /// </summary>
        public InsightDirection Direction { get; private set; }

        /// <summary>
        /// Gets the period over which this insight is expected to come to fruition
        /// </summary>
        public TimeSpan Period { get; internal set; }

        /// <summary>
        /// Gets the predicted percent change in the insight type (price/volatility)
        /// </summary>
        public double? Magnitude { get; private set; }

        /// <summary>
        /// Gets the confidence in this insight
        /// </summary>
        public double? Confidence { get; private set; }

        /// <summary>
        /// Gets the portfolio weight of this insight
        /// </summary>
        public double? Weight { get; private set; }

        /// <summary>
        /// Gets the most recent scores for this insight
        /// </summary>
        public InsightScore Score { get; private set; }

        /// <summary>
        /// Gets the estimated value of this insight in the account currency
        /// </summary>
        public decimal EstimatedValue { get; internal set; }

        /// <summary>
        /// Enum indicating the Insight creation moment.
        /// </summary>
        public InsightSource Source { get; set; }

        /// <summary>
        /// Determines whether or not this insight is considered expired at the specified <paramref name="utcTime"/>
        /// </summary>
        /// <param name="utcTime">The algorithm's current time in UTC. See <see cref="IAlgorithm.UtcTime"/></param>
        /// <returns>True if this insight is expired, false otherwise</returns>
        public bool IsExpired(DateTime utcTime)
        {
            return CloseTimeUtc < utcTime;
        }

        /// <summary>
        /// Determines whether or not this insight is considered active at the specified <paramref name="utcTime"/>
        /// </summary>
        /// <param name="utcTime">The algorithm's current time in UTC. See <see cref="IAlgorithm.UtcTime"/></param>
        /// <returns>True if this insight is active, false otherwise</returns>
        public bool IsActive(DateTime utcTime)
        {
            return !IsExpired(utcTime);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Insight"/> class
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="period">The period over which the prediction will come true</param>
        /// <param name="type">The type of insight, price/volatility</param>
        /// <param name="direction">The predicted direction</param>
        public Insight(Symbol symbol, TimeSpan period, InsightType type, InsightDirection direction)
            : this(symbol, period, type, direction, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Insight"/> class
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="period">The period over which the prediction will come true</param>
        /// <param name="type">The type of insight, price/volatility</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="magnitude">The predicted magnitude as a percentage change</param>
        /// <param name="confidence">The confidence in this insight</param>
        /// <param name="sourceModel">An identifier defining the model that generated this insight</param>
        /// <param name="weight">The portfolio weight of this insight</param>
        public Insight(Symbol symbol, TimeSpan period, InsightType type, InsightDirection direction, double? magnitude, double? confidence, string sourceModel = null, double? weight = null)
        {
            Id = Guid.NewGuid();
            Score = new InsightScore();
            SourceModel = sourceModel;

            Symbol = symbol;
            Type = type;
            Direction = direction;
            Period = period;

            // Optional
            Magnitude = magnitude;
            Confidence = confidence;
            Weight = weight;

            _periodSpecification = new TimeSpanPeriodSpecification(period);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Insight"/> class
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="expiryFunc">Func that defines the expiry time</param>
        /// <param name="type">The type of insight, price/volatility</param>
        /// <param name="direction">The predicted direction</param>
        public Insight(Symbol symbol, Func<DateTime, DateTime> expiryFunc, InsightType type, InsightDirection direction)
            : this(symbol, expiryFunc, type, direction, null, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Insight"/> class
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="expiryFunc">Func that defines the expiry time</param>
        /// <param name="type">The type of insight, price/volatility</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="magnitude">The predicted magnitude as a percentage change</param>
        /// <param name="confidence">The confidence in this insight</param>
        /// <param name="sourceModel">An identifier defining the model that generated this insight</param>
        /// <param name="weight">The portfolio weight of this insight</param>
        public Insight(Symbol symbol, Func<DateTime, DateTime> expiryFunc, InsightType type, InsightDirection direction, double? magnitude, double? confidence, string sourceModel = null, double? weight = null)
            : this(symbol, new FuncPeriodSpecification(expiryFunc), type, direction, magnitude, confidence, sourceModel, weight)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Insight"/> class.
        /// This constructor is provided mostly for testing purposes. When running inside an algorithm,
        /// the generated and close times are set based on the algorithm's time.
        /// </summary>
        /// <param name="generatedTimeUtc">The time this insight was generated in utc</param>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="period">The period over which the prediction will come true</param>
        /// <param name="type">The type of insight, price/volatility</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="magnitude">The predicted magnitude as a percentage change</param>
        /// <param name="confidence">The confidence in this insight</param>
        /// <param name="sourceModel">An identifier defining the model that generated this insight</param>
        /// <param name="weight">The portfolio weight of this insight</param>
        public Insight(DateTime generatedTimeUtc, Symbol symbol, TimeSpan period, InsightType type, InsightDirection direction, double? magnitude, double? confidence, string sourceModel = null, double? weight = null)
            : this(symbol, period, type, direction, magnitude, confidence, sourceModel, weight)
        {
            GeneratedTimeUtc = generatedTimeUtc;
            CloseTimeUtc = generatedTimeUtc + period;
        }

        /// <summary>
        /// Private constructor used to keep track of how a user defined the insight period.
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="periodSpec">A specification defining how the insight's period was defined, via time span, via resolution/barcount, via close time</param>
        /// <param name="type">The type of insight, price/volatility</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="magnitude">The predicted magnitude as a percentage change</param>
        /// <param name="confidence">The confidence in this insight</param>
        /// <param name="sourceModel">An identifier defining the model that generated this insight</param>
        /// <param name="weight">The portfolio weight of this insight</param>
        private Insight(Symbol symbol, IPeriodSpecification periodSpec, InsightType type, InsightDirection direction, double? magnitude, double? confidence, string sourceModel = null, double? weight = null)
        {
            Id = Guid.NewGuid();
            Score = new InsightScore();
            SourceModel = sourceModel;

            Symbol = symbol;
            Type = type;
            Direction = direction;

            // Optional
            Magnitude = magnitude;
            Confidence = confidence;
            Weight = weight;

            _periodSpecification = periodSpec;

            // keep existing behavior of Insight.Price such that we set the period immediately
            var period = (periodSpec as TimeSpanPeriodSpecification)?.Period;
            if (period != null)
            {
                Period = period.Value;
            }
        }

        /// <summary>
        /// Sets the insight period and close times if they have not already been set.
        /// </summary>
        /// <param name="exchangeHours">The insight's security exchange hours</param>
        public void SetPeriodAndCloseTime(SecurityExchangeHours exchangeHours)
        {
            if (GeneratedTimeUtc == default(DateTime))
            {
                throw new InvalidOperationException($"The insight's '{nameof(GeneratedTimeUtc)}' " +
                    $"property must be set before calling {nameof(SetPeriodAndCloseTime)}.");
            }

            _periodSpecification.SetPeriodAndCloseTime(this, exchangeHours);
        }

        /// <summary>
        /// Creates a deep clone of this insight instance
        /// </summary>
        /// <returns>A new insight with identical values, but new instances</returns>
        public virtual Insight Clone()
        {
            return new Insight(Symbol, Period, Type, Direction, Magnitude, Confidence, weight:Weight)
            {
                GeneratedTimeUtc = GeneratedTimeUtc,
                CloseTimeUtc = CloseTimeUtc,
                Score = Score,
                Id = Id,
                EstimatedValue = EstimatedValue,
                ReferenceValue = ReferenceValue,
                ReferenceValueFinal = ReferenceValueFinal,
                SourceModel = SourceModel,
                GroupId = GroupId
            };
        }

        /// <summary>
        /// Creates a new insight for predicting the percent change in price over the specified period
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="resolution">The resolution used to define the insight's period and also used to determine the insight's close time</param>
        /// <param name="barCount">The number of resolution time steps to make in market hours to compute the insight's closing time</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="magnitude">The predicted magnitude as a percent change</param>
        /// <param name="confidence">The confidence in this insight</param>
        /// <param name="sourceModel">The model generating this insight</param>
        /// <param name="weight">The portfolio weight of this insight</param>
        /// <returns>A new insight object for the specified parameters</returns>
        public static Insight Price(Symbol symbol, Resolution resolution, int barCount, InsightDirection direction, double? magnitude = null, double? confidence = null, string sourceModel = null, double? weight = null)
        {
            if (barCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(barCount), "Insight barCount must be greater than zero.");
            }

            var spec = new ResolutionBarCountPeriodSpecification(resolution, barCount);
            return new Insight(symbol, spec, InsightType.Price, direction, magnitude, confidence, sourceModel, weight);
        }

        /// <summary>
        /// Creates a new insight for predicting the percent change in price over the specified period
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="closeTimeLocal">The insight's closing time in the security's exchange time zone</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="magnitude">The predicted magnitude as a percent change</param>
        /// <param name="confidence">The confidence in this insight</param>
        /// <param name="sourceModel">The model generating this insight</param>
        /// <param name="weight">The portfolio weight of this insight</param>
        /// <returns>A new insight object for the specified parameters</returns>
        public static Insight Price(Symbol symbol, DateTime closeTimeLocal, InsightDirection direction, double? magnitude = null, double? confidence = null, string sourceModel = null, double? weight = null)
        {
            var spec = closeTimeLocal == Time.EndOfTime ? (IPeriodSpecification)
                new EndOfTimeCloseTimePeriodSpecification() : new CloseTimePeriodSpecification(closeTimeLocal);
            return new Insight(symbol, spec, InsightType.Price, direction, magnitude, confidence, sourceModel, weight);
        }

        /// <summary>
        /// Creates a new insight for predicting the percent change in price over the specified period
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="period">The period over which the prediction will come true</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="magnitude">The predicted magnitude as a percent change</param>
        /// <param name="confidence">The confidence in this insight</param>
        /// <param name="sourceModel">The model generating this insight</param>
        /// <param name="weight">The portfolio weight of this insight</param>
        /// <returns>A new insight object for the specified parameters</returns>
        public static Insight Price(Symbol symbol, TimeSpan period, InsightDirection direction, double? magnitude = null, double? confidence = null, string sourceModel = null, double? weight = null)
        {
            if (period < Time.OneSecond)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Insight period must be greater than or equal to 1 second.");
            }

            var spec = period == Time.EndOfTimeTimeSpan ? (IPeriodSpecification)
                new EndOfTimeCloseTimePeriodSpecification() : new TimeSpanPeriodSpecification(period);
            return new Insight(symbol, spec, InsightType.Price, direction, magnitude, confidence, sourceModel, weight);
        }

        /// <summary>
        /// Creates a new insight for predicting the percent change in price over the specified period
        /// </summary>
        /// <param name="symbol">The symbol this insight is for</param>
        /// <param name="expiryFunc">Func that defines the expiry time</param>
        /// <param name="direction">The predicted direction</param>
        /// <param name="magnitude">The predicted magnitude as a percent change</param>
        /// <param name="confidence">The confidence in this insight</param>
        /// <param name="sourceModel">The model generating this insight</param>
        /// <param name="weight">The portfolio weight of this insight</param>
        /// <returns>A new insight object for the specified parameters</returns>
        public static Insight Price(Symbol symbol, Func<DateTime, DateTime> expiryFunc, InsightDirection direction, double? magnitude = null, double? confidence = null, string sourceModel = null, double? weight = null)
        {
            return new Insight(symbol, expiryFunc, InsightType.Price, direction, magnitude, confidence, sourceModel, weight);
        }

        /// <summary>
        /// Creates a new, unique group id and sets it on each insight
        /// </summary>
        /// <param name="insights">The insights to be grouped</param>
        public static IEnumerable<Insight> Group(params Insight[] insights)
        {
            if (insights == null)
            {
                throw new ArgumentNullException(nameof(insights));
            }

            var groupId = Guid.NewGuid();
            foreach (var insight in insights)
            {
                if (insight.GroupId.HasValue)
                {
                    throw new InvalidOperationException($"Unable to set group id on insight {insight} because it has already been assigned to a group.");
                }

                insight.GroupId = groupId;
            }
            return insights;
        }

        /// <summary>
        /// Creates a new, unique group id and sets it on each insight
        /// </summary>
        /// <param name="insight">The insight to be grouped</param>
        public static IEnumerable<Insight> Group(Insight insight) => Group(new[] {insight});

        /// <summary>
        /// Creates a new <see cref="Insight"/> object from the specified serialized form
        /// </summary>
        /// <param name="serializedInsight">The insight DTO</param>
        /// <returns>A new insight containing the information specified</returns>
        public static Insight FromSerializedInsight(SerializedInsight serializedInsight)
        {
            var insight = new Insight(
                Time.UnixTimeStampToDateTime(serializedInsight.CreatedTime),
                new Symbol(SecurityIdentifier.Parse(serializedInsight.Symbol), serializedInsight.Ticker),
                TimeSpan.FromSeconds(serializedInsight.Period),
                serializedInsight.Type,
                serializedInsight.Direction,
                serializedInsight.Magnitude,
                serializedInsight.Confidence,
                serializedInsight.SourceModel,
                serializedInsight.Weight
            )
            {
                Id = Guid.Parse(serializedInsight.Id),
                CloseTimeUtc = Time.UnixTimeStampToDateTime(serializedInsight.CloseTime),
                EstimatedValue = serializedInsight.EstimatedValue,
                ReferenceValue = serializedInsight.ReferenceValue,
                ReferenceValueFinal = serializedInsight.ReferenceValueFinal,
                GroupId = string.IsNullOrEmpty(serializedInsight.GroupId) ? (Guid?) null : Guid.Parse(serializedInsight.GroupId),
                Source = serializedInsight.Source
            };

            // only set score values if non-zero or if they're the final scores
            if (serializedInsight.ScoreIsFinal)
            {
                insight.Score.SetScore(InsightScoreType.Magnitude, serializedInsight.ScoreMagnitude, insight.CloseTimeUtc);
                insight.Score.SetScore(InsightScoreType.Direction, serializedInsight.ScoreDirection, insight.CloseTimeUtc);
                insight.Score.Finalize(insight.CloseTimeUtc);
            }
            else
            {
                if (serializedInsight.ScoreMagnitude != 0)
                {
                    insight.Score.SetScore(InsightScoreType.Magnitude, serializedInsight.ScoreMagnitude, insight.CloseTimeUtc);
                }

                if (serializedInsight.ScoreDirection != 0)
                {
                    insight.Score.SetScore(InsightScoreType.Direction, serializedInsight.ScoreDirection, insight.CloseTimeUtc);
                }
            }

            return insight;
        }

        /// <summary>
        /// Computes the insight closing time from the given generated time, resolution and bar count.
        /// This will step through market hours using the given resolution, respecting holidays, early closes, weekends, etc..
        /// </summary>
        /// <param name="exchangeHours">The exchange hours of the insight's security</param>
        /// <param name="generatedTimeUtc">The insight's generated time in utc</param>
        /// <param name="resolution">The resolution used to 'step-through' market hours to compute a reasonable close time</param>
        /// <param name="barCount">The number of resolution steps to take</param>
        /// <returns>The insight's closing time in utc</returns>
        public static DateTime ComputeCloseTime(SecurityExchangeHours exchangeHours, DateTime generatedTimeUtc, Resolution resolution, int barCount)
        {
            if (barCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(barCount), "Insight barCount must be greater than zero.");
            }

            // remap ticks to seconds
            resolution = resolution == Resolution.Tick ? Resolution.Second : resolution;
            if (resolution == Resolution.Hour)
            {
                // remap hours to minutes to avoid complications w/ stepping through
                // for example 9->10 is an hour step but market opens at 9:30
                barCount *= 60;
                resolution = Resolution.Minute;
            }

            var barSize = resolution.ToTimeSpan();
            var startTimeLocal = generatedTimeUtc.ConvertFromUtc(exchangeHours.TimeZone);
            var closeTimeLocal = Time.GetEndTimeForTradeBars(exchangeHours, startTimeLocal, barSize, barCount, false);
            return closeTimeLocal.ConvertToUtc(exchangeHours.TimeZone);
        }

        /// <summary>
        /// computs the insight closing time from the given generated time and period
        /// </summary>
        /// <param name="exchangeHours">The exchange hours of the insight's security</param>
        /// <param name="generatedTimeUtc">The insight's generated time in utc</param>
        /// <param name="period">The insight's period</param>
        /// <returns>The insight's closing time in utc</returns>
        public static DateTime ComputeCloseTime(SecurityExchangeHours exchangeHours, DateTime generatedTimeUtc, TimeSpan period)
        {
            if (period < Time.OneSecond)
            {
                throw new ArgumentOutOfRangeException(nameof(period), "Insight periods must be greater than or equal to 1 second.");
            }

            var barSize = period.ToHigherResolutionEquivalent(false);
            // remap ticks to seconds
            barSize = barSize == Resolution.Tick ? Resolution.Second : barSize;
            // remap hours to minutes to avoid complications w/ stepping through, for example 9->10 is an hour step but market opens at 9:30
            barSize = barSize == Resolution.Hour ? Resolution.Minute : barSize;
            var barCount = (int)(period.Ticks / barSize.ToTimeSpan().Ticks);
            var closeTimeUtc = ComputeCloseTime(exchangeHours, generatedTimeUtc, barSize, barCount);
            if (closeTimeUtc == generatedTimeUtc)
            {
                return ComputeCloseTime(exchangeHours, generatedTimeUtc, Resolution.Second, 1);
            }

            var totalPeriodUsed = barSize.ToTimeSpan().Multiply(barCount);
            if (totalPeriodUsed != period)
            {
                var delta = period - totalPeriodUsed;

                // interpret the remainder as fractional trading days
                if (barSize == Resolution.Daily)
                {
                    var percentOfDay = delta.Ticks / (double) Time.OneDay.Ticks;
                    delta = exchangeHours.RegularMarketDuration.Multiply(percentOfDay);
                }

                if (delta != TimeSpan.Zero)
                {
                    // continue stepping forward using minute resolution for the remainder
                    barCount = (int) (delta.Ticks / Time.OneMinute.Ticks);
                    if (barCount > 0)
                    {
                        closeTimeUtc = ComputeCloseTime(exchangeHours, closeTimeUtc, Resolution.Minute, barCount);
                    }
                }
            }

            return closeTimeUtc;
        }

        /// <summary>
        /// Computes the insight period from the given generated and close times
        /// </summary>
        /// <param name="exchangeHours">The exchange hours of the insight's security</param>
        /// <param name="generatedTimeUtc">The insight's generated time in utc</param>
        /// <param name="closeTimeUtc">The insight's close time in utc</param>
        /// <returns>The insight's period</returns>
        public static TimeSpan ComputePeriod(SecurityExchangeHours exchangeHours, DateTime generatedTimeUtc, DateTime closeTimeUtc)
        {
            if (generatedTimeUtc > closeTimeUtc)
            {
                throw new ArgumentOutOfRangeException(nameof(closeTimeUtc), "Insight closeTimeUtc must be greater than generatedTimeUtc.");
            }

            var generatedTimeLocal = generatedTimeUtc.ConvertFromUtc(exchangeHours.TimeZone);
            var closeTimeLocal = closeTimeUtc.ConvertFromUtc(exchangeHours.TimeZone);

            if (generatedTimeLocal.Date == closeTimeLocal.Date)
            {
                return closeTimeLocal - generatedTimeLocal;
            }

            var choices = new[]
            {
                // don't use hourly since it causes issues with non-round open/close times
                new {barSize = Time.OneDay, count = 0, delta = TimeSpan.MaxValue},
                new {barSize = Time.OneMinute, count = 0, delta = TimeSpan.MaxValue}
            };

            for (int i = 0; i < choices.Length; i++)
            {
                var barSize = choices[i].barSize;
                var count = Time.GetNumberOfTradeBarsInInterval(exchangeHours, generatedTimeLocal, closeTimeLocal, barSize);
                var closeTime = Time.GetEndTimeForTradeBars(exchangeHours, generatedTimeLocal, barSize, count, false);
                var delta = (closeTimeLocal - closeTime).Abs();

                if (delta == TimeSpan.Zero)
                {
                    // exact match found
                    return barSize.Multiply(count);
                }

                choices[i] = new {barSize, count, delta};
            }

            // no exact match, return the one with the least error
            var choiceWithLeastError = choices.OrderBy(c => c.delta).First();
            return choiceWithLeastError.barSize.Multiply(choiceWithLeastError.count);
        }

        /// <summary>Returns a string that represents the current object.</summary>
        /// <returns>A string that represents the current object.</returns>
        /// <filterpriority>2</filterpriority>
        public override string ToString()
        {
            var str = Invariant($"{Id:N}: {Symbol} {Type} {Direction} within {Period}");

            if (Magnitude.HasValue)
            {
                str += Invariant($" by {Magnitude.Value}%");
            }
            if (Confidence.HasValue)
            {
                str += Invariant($" with {Math.Round(100 * Confidence.Value, 1)}% confidence");
            }
            if (Weight.HasValue)
            {
                str += Invariant($" and {Math.Round(100 * Weight.Value, 1)}% weight");
            }

            return str;
        }

        /// <summary>
        /// Distinguishes between the different ways an insight's period/close times can be specified
        /// This was really only required since we can't properly acces certain data from within a static
        /// context (such as Insight.Price) or from within a constructor w/out requiring the users to properly
        /// fetch the required data and supply it as an argument.
        /// </summary>
        private interface IPeriodSpecification
        {
            void SetPeriodAndCloseTime(Insight insight, SecurityExchangeHours exchangeHours);
        }

        /// <summary>
        /// User defined the insight's period using a time span
        /// </summary>
        private class TimeSpanPeriodSpecification : IPeriodSpecification
        {
            public readonly TimeSpan Period;

            public TimeSpanPeriodSpecification(TimeSpan period)
            {
                if (period == TimeSpan.Zero)
                {
                    period = Time.OneSecond;
                }

                Period = period;
            }

            public void SetPeriodAndCloseTime(Insight insight, SecurityExchangeHours exchangeHours)
            {
                insight.Period = Period;
                insight.CloseTimeUtc = ComputeCloseTime(exchangeHours, insight.GeneratedTimeUtc, Period);
            }
        }

        /// <summary>
        /// User defined insight's period using a resolution and bar count
        /// </summary>
        private class ResolutionBarCountPeriodSpecification : IPeriodSpecification
        {
            public readonly Resolution Resolution;
            public readonly int BarCount;

            public ResolutionBarCountPeriodSpecification(Resolution resolution, int barCount)
            {
                if (resolution == Resolution.Tick)
                {
                    resolution = Resolution.Second;
                }

                if (resolution == Resolution.Hour)
                {
                    // remap hours to minutes to avoid errors w/ half hours, for example, 9:30 open
                    barCount *= 60;
                    resolution = Resolution.Minute;
                }

                Resolution = resolution;
                BarCount = barCount;
            }

            public void SetPeriodAndCloseTime(Insight insight, SecurityExchangeHours exchangeHours)
            {
                insight.Period = Resolution.ToTimeSpan().Multiply(BarCount);
                insight.CloseTimeUtc = ComputeCloseTime(exchangeHours, insight.GeneratedTimeUtc, Resolution, BarCount);
            }
        }

        /// <summary>
        /// User defined the insight's local closing time
        /// </summary>
        private class CloseTimePeriodSpecification : IPeriodSpecification
        {
            public readonly DateTime CloseTimeLocal;

            public CloseTimePeriodSpecification(DateTime closeTimeLocal)
            {
                CloseTimeLocal = closeTimeLocal;
            }

            public void SetPeriodAndCloseTime(Insight insight, SecurityExchangeHours exchangeHours)
            {
                insight.CloseTimeUtc = CloseTimeLocal.ConvertToUtc(exchangeHours.TimeZone);
                if (insight.GeneratedTimeUtc > insight.CloseTimeUtc)
                {
                    throw new ArgumentOutOfRangeException("closeTimeLocal", $"Insight closeTimeLocal must not be in the past.");
                }

                insight.Period = ComputePeriod(exchangeHours, insight.GeneratedTimeUtc, insight.CloseTimeUtc);
            }
        }

        /// <summary>
        /// Special case for insights which close time is defined by a function
        /// and want insights to expiry with calendar rules
        /// </summary>
        private class FuncPeriodSpecification : IPeriodSpecification
        {
            public readonly Func<DateTime, DateTime> _expiryFunc;

            public FuncPeriodSpecification(Func<DateTime, DateTime> expiryFunc)
            {
                _expiryFunc = expiryFunc;
            }

            public void SetPeriodAndCloseTime(Insight insight, SecurityExchangeHours exchangeHours)
            {
                var closeTimeLocal = insight.GeneratedTimeUtc.ConvertFromUtc(exchangeHours.TimeZone);
                closeTimeLocal = _expiryFunc(closeTimeLocal);

                // Prevent close time to be defined to a date/time in closed market
                if (!exchangeHours.IsOpen(closeTimeLocal, false))
                {
                    closeTimeLocal = exchangeHours.GetNextMarketOpen(closeTimeLocal, false);
                }

                insight.CloseTimeUtc = closeTimeLocal.ConvertToUtc(exchangeHours.TimeZone);
                insight.Period = insight.CloseTimeUtc - insight.GeneratedTimeUtc;
            }
        }

        /// <summary>
        /// Special case for insights where we do not know whats the
        /// <see cref="Period"/> or <see cref="CloseTimeUtc"/>.
        /// </summary>
        private class EndOfTimeCloseTimePeriodSpecification : IPeriodSpecification
        {
            public void SetPeriodAndCloseTime(Insight insight, SecurityExchangeHours exchangeHours)
            {
                insight.Period = Time.EndOfTimeTimeSpan;
                insight.CloseTimeUtc = Time.EndOfTime;
            }
        }
    }
}