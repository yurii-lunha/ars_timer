using System;
using UnityEngine;
using UnityEngine.UI;

#if TEXT_MESH_PRO
using TMPro;
#endif

namespace Ars
{
    public class ArsTimerView : MonoBehaviour
    {
        [Serializable]
        public enum TimerType
        {
            HOURS,
            MINUTES,
            SECONDS
        }

        [Header("Core"), SerializeField, Tooltip("Save time and continue after game restart")]
        bool _realtime;

        [SerializeField] int _uniqIndex = -1;

        [Header("Behaviour"), SerializeField] TimerType _timerType;
        [SerializeField] bool _useTimeScale;
        [SerializeField] bool _autoPlay;
        [SerializeField] Text _timerText;

#if TEXT_MESH_PRO
        [SerializeField] TMP_Text _timerTmpText;
#endif

        [SerializeField] double _countdownHours;
        [SerializeField] double _countdownMinutes;
        [SerializeField] double _countdownSeconds;
        [Header("View"), SerializeField] Color _defaultColor;
        [SerializeField] Color _freezeColor;
        [SerializeField] bool _useLowStyle;
        [SerializeField] int _secondsToLow = 5;
        [SerializeField] Color _lowTimeColor = new Color(0.88f, 0f, 0.11f);

        float _deltaTime;
        bool _isTiming = false;
        DateTime _startTime;
        bool _timeOut;

        TimerData _timerData;
        bool _updateTimer;
        bool _pauseTimer;

        bool _freeze;
        Action _onFreezeComplete;
        float _freezeDuration;

        Color TimerTextColor
        {
            get => _timerText ? _timerText.color : _timerTmpText.color;
            set
            {
                if (_timerTmpText)
                    _timerTmpText.color = value;

                if (_timerText)
                    _timerText.color = value;
            }
        }

        string TimerTextValue
        {
            get => _timerText ? _timerText.text : _timerTmpText.text;
            set
            {
                if (_timerTmpText)
                    _timerTmpText.text = value;

                if (_timerText)
                    _timerText.text = value;
            }
        }

        public int TimerId
        {
            get =>
                _uniqIndex;
        }

        public Text TimerText
        {
            get =>
                _timerText;
        }

        public double CountdownMinutes
        {
            get =>
                _countdownMinutes;
            set
            {
                _countdownMinutes = value;

                UpdateTimerText();
            }
        }

        public double CountdownSeconds
        {
            get =>
                _countdownSeconds;
            set
            {
                _countdownSeconds = value;
                UpdateTimerText();
            }
        }

        public double CountdownHours
        {
            get =>
                _countdownHours;
            set
            {
                _countdownHours = value;
                UpdateTimerText();
            }
        }

        public int SecondsToLow
        {
            get =>
                _secondsToLow;
            set =>
                _secondsToLow = value;
        }

        void Awake()
        {
            ArsTimerUtils.OnTimerViewInit(this);
            InitTimerData();
        }

        void Start()
        {
            if (_autoPlay)
            {
                PlayTimer();
            }
        }

        void Update()
        {
            if (_pauseTimer)
            {
                return;
            }

            if (_freeze)
            {
                if (_freezeDuration > 0f)
                {
                    _freezeDuration -= 1f * Time.deltaTime;
                    return;
                }

                _onFreezeComplete?.Invoke();
                ResumeTimer();
                _freeze = false;
            }

            if (!_useTimeScale)
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

        void FixedUpdate()
        {
            if (_pauseTimer)
            {
                return;
            }

            if (_useTimeScale)
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

        void OnDisable()
        {
            if (_realtime)
            {
                ArsTimerUtils.SetValue(_timerData);
            }
        }

        void InitTimerData()
        {
            if (!_realtime)
            {
                return;
            }

            _timerData = ArsTimerUtils.GetValue(_uniqIndex);

            if (_timerData == null)
            {
                _timerData = new TimerData();
                _timerData.timerIndex = _uniqIndex;
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

        void UpdateRealtimeSeconds()
        {
            if (!_realtime)
            {
                return;
            }

            _timerData.timerSeconds = TimeLeft();
        }

        [ContextMenu("Reset timer")]
        public void DebugResetTimer()
        {
            if (_realtime)
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
            var targetTime = _startTime.AddHours(_countdownHours);
            targetTime = targetTime.AddMinutes(_countdownMinutes);
            targetTime = targetTime.AddSeconds(_countdownSeconds);

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

        public void FreezeTime(float freezeDuration, Action onComplete = null)
        {
            _onFreezeComplete = onComplete;
            _freeze = true;
            _freezeDuration = freezeDuration;

            _updateTimer = false;
            _startTime = _startTime.AddSeconds(freezeDuration);
            TimerTextColor = _freezeColor;
        }

        public void StopTimer() =>
            _updateTimer = false;

        public void PauseTimer()
        {
            _pauseTimer = true;
        }

        public void UnpauseTimer()
        {
            _pauseTimer = false;
        }

        public ArsTimerView RestartTimer(int seconds)
        {
            _startTime = DateTime.Now.AddSeconds(1d);

            if (_realtime)
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

            if (_realtime)
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
            if (_useTimeScale)
            {
                var hours = (int)_countdownHours;
                var minutes = (int)_countdownMinutes;
                var seconds = (int)_countdownSeconds;

                if (_timerType == TimerType.HOURS)
                {
                    TimerTextValue = $"{FormatTime(hours)}:{FormatTime(minutes)}:{FormatTime(seconds)}";
                }
                else if (_timerType == TimerType.MINUTES)
                {
                    TimerTextValue = $"{FormatTime(minutes)}:{FormatTime(seconds)}";
                }
                else
                {
                    TimerTextValue = $"{FormatTime(seconds)}";
                }
            }
            else
            {
                _startTime = _realtime
                    ? _timerData.GetStartDateTime().AddSeconds(1.1d)
                    : DateTime.Now.AddSeconds(1.1d);

                UpdateTimer();
            }
        }

        bool UpdateTimerByTimeScale()
        {
            _deltaTime = 1f * Time.deltaTime;

            _countdownSeconds -= _deltaTime;

            if (_countdownSeconds < 0f)
            {
                if (_countdownMinutes > 0f)
                {
                    _countdownSeconds = 60f;
                    _countdownMinutes--;

                    if (_countdownMinutes < 0f)
                    {
                        if (_countdownHours > 0f)
                        {
                            _countdownMinutes = 60f;
                            _countdownHours--;
                        }
                    }
                }
            }

            var hours = (int)_countdownHours;
            var minutes = (int)_countdownMinutes;
            var seconds = (int)_countdownSeconds;

            if (_timerType == TimerType.HOURS)
            {
                TimerTextValue = $"{FormatTime(hours)}:{FormatTime(minutes)}:{FormatTime(seconds)}";
            }
            else if (_timerType == TimerType.MINUTES)
            {
                TimerTextValue = $"{FormatTime(minutes)}:{FormatTime(seconds)}";
            }
            else
            {
                TimerTextValue = $"{FormatTime(seconds)}";
            }

            UpdateTextStyle();

            return minutes > 0 || seconds > 0 || hours > 0;
        }

        bool UpdateTimer()
        {
            // Hours 
            var targetTime = _startTime.AddHours(_countdownHours);
            // Minutes 
            targetTime = targetTime.AddMinutes(_countdownMinutes);
            // Seconds
            targetTime = targetTime.AddSeconds(_countdownSeconds);

            var now = DateTime.Now.AddSeconds(1d);
            var hours = targetTime.Subtract(now).Hours;
            var minutes = targetTime.Subtract(now).Minutes;
            var seconds = targetTime.Subtract(now).Seconds;

            if (_timerType == TimerType.HOURS)
            {
                TimerTextValue = $"{FormatTime(hours)}:{FormatTime(minutes)}:{FormatTime(seconds)}";
            }
            else if (_timerType == TimerType.MINUTES)
            {
                TimerTextValue = $"{FormatTime(minutes)}:{FormatTime(seconds)}";
            }
            else
            {
                TimerTextValue = $"{FormatTime(seconds)}";
            }

            UpdateTextStyle();

            var canUpdate = minutes > 0 || seconds > 0 || hours > 0;

            return canUpdate;
        }

        void ResumeTimer()
        {
            TimerTextColor = _defaultColor;
            _updateTimer = true;
        }

        void UpdateTextStyle()
        {
            if (!_useLowStyle)
            {
                return;
            }

            if (_countdownSeconds <= SecondsToLow && _countdownHours <= 0 && _countdownMinutes <= 0)
            {
                TimerTextColor = _lowTimeColor;

                LowTime?.Invoke((int)_countdownSeconds);
            }
        }

        public static string FormatTime(int time) =>
            time < 0 ? "00" : time > 9 ? time.ToString() : $"0{time}";
    }
}