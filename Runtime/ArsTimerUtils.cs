using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Ars
{
    public static class ArsTimerUtils
    {
        private const string TimersPrefsName = "realtime_timers";

        private static TimersSave _timersSave;

        private static readonly List<ArsTimerView> _timers = new List<ArsTimerView>();

        public static void RemoveAllTimers()
        {
            foreach (var timer in _timers)
            {
                timer.StopTimer();
                timer.RestartTimer(0);
            }

            PlayerPrefs.SetString(TimersPrefsName, "");
            _timersSave = null;
        }

        public static void OnTimerViewInit(ArsTimerView timerView)
        {
            if (!_timers.Contains(timerView))
            {
                _timers.Add(timerView);
            }
        }

        private static void SetTimerSave(TimersSave timersSave)
        {
            _timersSave = timersSave;

            var json = JsonUtility.ToJson(timersSave);
            PlayerPrefs.SetString(TimersPrefsName, json);
        }

        private static TimersSave GetTimerSave()
        {
            if (_timersSave != null)
            {
                return _timersSave;
            }

            var json = PlayerPrefs.GetString(TimersPrefsName);

            _timersSave = JsonUtility.FromJson<TimersSave>(json);

            if (_timersSave == null)
            {
                _timersSave = new TimersSave();
                _timersSave.timersData = new List<TimerData>();
            }

            return _timersSave;
        }

        public static TimerData GetValue(int timerIndex)
        {
            var timerSave = GetTimerSave();

            return timerSave.GetTimerData(timerIndex);
        }

        public static void SetValue(TimerData timerData)
        {
            var timerSave = GetTimerSave();
            timerSave.SetTimerData(timerData);

            SetTimerSave(timerSave);
        }

        public static bool IsTimeOut(int timerIndex)
        {
            var timerSave = GetTimerSave();
            var timerData = timerSave.GetTimerData(timerIndex);

            if (timerData != null)
            {
                var now = DateTime.Now.AddSeconds(1d);
                var seconds = timerData.GetStartDateTime().Subtract(now).Seconds;

                return seconds <= 0;
            }

            return false;
        }

        #region InSeconds

        public const int WeekInSeconds = 604799;

        public const int DayInSeconds = 86399;
        public const int HalfDayInSeconds = 43199;

        public const int HourInSeconds = 3599;
        public const int MinuteInSeconds = 60;

        public const int FiveMinuteInSeconds = 300;

        #endregion
    }

    [Serializable]
    public class TimerData
    {
        private const string DateTimeFormat = "MM/dd/yyyy HH:mm:ss";

        // TODO: rename to id
        public int timerIndex;
        public string startDateTimeStr;
        public int timerSeconds;
        public bool dataIsReady;

        public void SetStartDateTime(DateTime dateTime) =>
            startDateTimeStr = dateTime.ToString(DateTimeFormat);

        public DateTime GetStartDateTime() =>
            string.IsNullOrEmpty(startDateTimeStr)
                ? DateTime.Now
                : DateTime.ParseExact(startDateTimeStr, DateTimeFormat, null);
    }

    [Serializable]
    public class TimersSave
    {
        public List<TimerData> timersData = new List<TimerData>();

        public TimerData GetTimerData(int index) =>
            timersData.FirstOrDefault(i => i.timerIndex == index);

        public void SetTimerData(TimerData timerData)
        {
            var value = GetTimerData(timerData.timerIndex);

            if (value != null)
            {
                timersData[timersData.IndexOf(value)] = timerData;
            }
            else
            {
                timersData.Add(timerData);
            }
        }
    }
}