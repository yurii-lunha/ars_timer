using System;
using UnityEngine;
using UnityEngine.UI;

namespace Ars
{
    public class ArsTimerView : MonoBehaviour
    {
        [Serializable]
        public enum TimerType
        {
            Hours,
            Minutes,
            Seconds
        }

        [Header("Core"), SerializeField, Tooltip("Save time and continue after game restart")]
        private bool realtime;

        [SerializeField]
        private int uniqIndex = -1;

        [Header("Behaviour"), SerializeField]
        private TimerType timerType;

        [SerializeField]
        private bool useTimeScale;

        [SerializeField]
        private bool autoPlay;

        [SerializeField]
        private Text timerText;

        [SerializeField]
        private double countdownHours;

        [SerializeField]
        private double countdownMinutes;

        [SerializeField]
        private double countdownSeconds;

        [Header("View"), SerializeField]
        private Color defaultColor;

        [SerializeField]
        private Color freezeColor;

        [SerializeField]
        private bool useLowStyle;

        [SerializeField]
        private int secondsToLow = 5;

        [SerializeField]
        private Color lowTimeColor = new Color(0.88f, 0f, 0.11f);

        private float _deltaTime;
        private bool _isTiming = false;
        private DateTime _startTime;
        private bool _timeOut;

        private TimerData _timerData;
        private bool _updateTimer;

        public int TimerId
        {
            get =>
                uniqIndex;
        }

        public Text TimerText
        {
            get =>
                timerText;
        }

        public double CountdownMinutes
        {
            get =>
                countdownMinutes;
            set
            {
                countdownMinutes = value;

                UpdateTimerText();
            }
        }

        public double CountdownSeconds
        {
            get =>
                countdownSeconds;
            set
            {
                countdownSeconds = value;
                UpdateTimerText();
            }
        }

        public double CountdownHours
        {
            get =>
                countdownHours;
            set
            {
                countdownHours = value;
                UpdateTimerText();
            }
        }

        public int SecondsToLow
        {
            get =>
                secondsToLow;
            set =>
                secondsToLow = value;
        }

        private void Awake()
        {
            ArsTimerUtils.OnTimerViewInit(this);
            InitTimerData();
        }

        private void Start()
        {
            if (autoPlay)
            {
                PlayTimer();
            }
        }

        private void Update()
        {
            if (!useTimeScale)
            {
                return;
            }

            if (!_updateTimer || _timeOut)
            {
                return;
            }

            if (!UpdateTimerByTimeScale())
            {
                _timeOut = true;
                LocalTimeOut?.Invoke();
                TimeOut?.Invoke();
            }
        }

        private void FixedUpdate()
        {
            if (useTimeScale)
            {
                return;
            }

            if (!_updateTimer || _timeOut)
            {
                return;
            }

            if (!UpdateTimer())
            {
                _timeOut = true;
                LocalTimeOut?.Invoke();
                TimeOut?.Invoke();
            }
        }

        private void OnDisable()
        {
            if (realtime)
            {
                ArsTimerUtils.SetValue(_timerData);
            }
        }

        private void InitTimerData()
        {
            if (!realtime)
            {
                return;
            }

            _timerData = ArsTimerUtils.GetValue(uniqIndex);

            if (_timerData == null)
            {
                _timerData = new TimerData();
                _timerData.timerIndex = uniqIndex;
            }
        }

        public int TimeLeft()
        {
            var h = CountdownHours > 0 ? CountdownHours * 60 * 60 : 0;
            var m = CountdownMinutes > 0 ? CountdownMinutes * 60 : 0;
            var s = CountdownSeconds;

            return (int)s + (int)m + (int)h;
        }

        public static event Action TimeOut;
        public event Action LocalTimeOut;
        public event Action<int> LowTime;

        private void UpdateRealtimeSeconds()
        {
            if (!realtime)
            {
                return;
            }

            _timerData.timerSeconds = TimeLeft();
        }

        [ContextMenu("Reset timer")]
        public void DebugResetTimer()
        {
            if (realtime)
            {
                _timerData.dataIsReady = true;

                _timerData.SetStartDateTime(DateTime.Now.AddSeconds(1d));
                _startTime = DateTime.Now.AddSeconds(1d);

                UpdateRealtimeSeconds();

                SetTime(_timerData.timerSeconds);
            }
            else
            {
                _startTime = DateTime.Now.AddSeconds(1d);
            }

            _deltaTime = 0f;
            _updateTimer = true;
            _timeOut = false;
        }

        public bool IsTimerOut()
        {
            var targetTime = _startTime.AddHours(countdownHours);
            targetTime = targetTime.AddMinutes(countdownMinutes);
            targetTime = targetTime.AddSeconds(countdownSeconds);

            var now = DateTime.Now.AddSeconds(1d);
            var hours = targetTime.Subtract(now).Hours;
            var minutes = targetTime.Subtract(now).Minutes;
            var seconds = targetTime.Subtract(now).Seconds;

            return !(minutes > 0 || seconds > 0 || hours > 0);
        }

        public void SetTime(int seconds)
        {
            var h = 0f;
            var m = 0f;
            float s = seconds;

            if (s > 60f)
            {
                m = s / 60f;
                s %= 60f;
                m -= s / 60f;
            }

            if (m > 60f)
            {
                h = m / 60f;
                m %= 60f;
                h -= m / 60f;
            }

            CountdownHours = h;
            CountdownMinutes = m;
            CountdownSeconds = s;
        }

        public void FreezeTime(float freezeDuration)
        {
            _updateTimer = false;
            _startTime = _startTime.AddSeconds(freezeDuration);
            timerText.color = freezeColor;

            Invoke(nameof(ResumeTimer), freezeDuration);
        }

        public void StopTimer() =>
            _updateTimer = false;

        public ArsTimerView RestartTimer(int seconds)
        {
            _startTime = DateTime.Now.AddSeconds(1d);

            if (realtime)
            {
                _timerData.SetStartDateTime(_startTime);
            }

            _deltaTime = 0f;
            _updateTimer = false;
            _timeOut = false;

            SetTime(seconds);
            UpdateRealtimeSeconds();

            return this;
        }

        public void PlayTimer()
        {
            if (_updateTimer)
            {
                return;
            }

            if (realtime)
            {
                InitTimerData();

                if (!_timerData.dataIsReady)
                {
                    _timerData.dataIsReady = true;
                    _timerData.SetStartDateTime(DateTime.Now.AddSeconds(1d));

                    UpdateRealtimeSeconds();
                }

                _startTime = _timerData.GetStartDateTime();
                SetTime(_timerData.timerSeconds);
            }
            else
            {
                _startTime = DateTime.Now.AddSeconds(1d);
            }

            _deltaTime = 0f;
            _updateTimer = true;
            _timeOut = false;
        }

        public void UpdateTimerText()
        {
            if (useTimeScale)
            {
                var hours = (int)countdownHours;
                var minutes = (int)countdownMinutes;
                var seconds = (int)countdownSeconds;

                if (timerType == TimerType.Hours)
                {
                    timerText.text = $"{FormatTime(hours)}:{FormatTime(minutes)}:{FormatTime(seconds)}";
                }
                else if (timerType == TimerType.Minutes)
                {
                    timerText.text = $"{FormatTime(minutes)}:{FormatTime(seconds)}";
                }
                else
                {
                    timerText.text = $"{FormatTime(seconds)}";
                }
            }
            else
            {
                _startTime = realtime
                    ? _timerData.GetStartDateTime().AddSeconds(1.1d)
                    : DateTime.Now.AddSeconds(1.1d);

                UpdateTimer();
            }
        }

        private bool UpdateTimerByTimeScale()
        {
            _deltaTime = 1f * Time.deltaTime;

            countdownSeconds -= _deltaTime;

            if (countdownSeconds < 0f)
            {
                if (countdownMinutes > 0f)
                {
                    countdownSeconds = 60f;
                    countdownMinutes--;

                    if (countdownMinutes < 0f)
                    {
                        if (countdownHours > 0f)
                        {
                            countdownMinutes = 60f;
                            countdownHours--;
                        }
                    }
                }
            }

            var hours = (int)countdownHours;
            var minutes = (int)countdownMinutes;
            var seconds = (int)countdownSeconds;

            if (timerType == TimerType.Hours)
            {
                timerText.text = $"{FormatTime(hours)}:{FormatTime(minutes)}:{FormatTime(seconds)}";
            }
            else if (timerType == TimerType.Minutes)
            {
                timerText.text = $"{FormatTime(minutes)}:{FormatTime(seconds)}";
            }
            else
            {
                timerText.text = $"{FormatTime(seconds)}";
            }

            UpdateTextStyle();

            return minutes > 0 || seconds > 0 || hours > 0;
        }

        private bool UpdateTimer()
        {
            // Hours 
            var targetTime = _startTime.AddHours(countdownHours);
            // Minutes 
            targetTime = targetTime.AddMinutes(countdownMinutes);
            // Seconds
            targetTime = targetTime.AddSeconds(countdownSeconds);

            var now = DateTime.Now.AddSeconds(1d);
            var hours = targetTime.Subtract(now).Hours;
            var minutes = targetTime.Subtract(now).Minutes;
            var seconds = targetTime.Subtract(now).Seconds;

            if (timerType == TimerType.Hours)
            {
                timerText.text = $"{FormatTime(hours)}:{FormatTime(minutes)}:{FormatTime(seconds)}";
            }
            else if (timerType == TimerType.Minutes)
            {
                timerText.text = $"{FormatTime(minutes)}:{FormatTime(seconds)}";
            }
            else
            {
                timerText.text = $"{FormatTime(seconds)}";
            }

            UpdateTextStyle();

            var canUpdate = minutes > 0 || seconds > 0 || hours > 0;

            return canUpdate;
        }

        private void ResumeTimer()
        {
            timerText.color = defaultColor;
            _updateTimer = true;
        }

        private void UpdateTextStyle()
        {
            if (!useLowStyle)
            {
                return;
            }

            if (countdownSeconds <= SecondsToLow && countdownHours <= 0 && countdownMinutes <= 0)
            {
                timerText.color = lowTimeColor;

                LowTime?.Invoke((int)countdownSeconds);
            }
        }

        public static string FormatTime(int time) =>
            time < 0 ? "00" : time > 9 ? time.ToString() : $"0{time}";
    }
}