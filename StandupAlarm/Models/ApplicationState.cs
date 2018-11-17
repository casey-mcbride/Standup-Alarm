using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using Java.Util;
using StandupAlarm.Activities;
using StandupAlarm.Persistance;

namespace StandupAlarm.Models
{
	/// <summary>
	/// Contains the shared state and information for the application.
	/// </summary>
	class ApplicationState
	{
		// TODOS
		// On/off switch by hitting the side buttons?
		// Add cell tower filter

		#region Constants

		/// <summary>
		/// How much notice the user gets before the alarm actually goes off.
		/// </summary>
		public static readonly TimeSpan SHUT_OFF_WARNING_TIME
#if DEBUG
			= TimeSpan.FromSeconds(3);
#else
			= TimeSpan.FromSeconds(15);
#endif

		/// <summary>
		/// When the alarm goes off from the start of the day.
		/// </summary>
		private static readonly TimeSpan ALARM_START_TIME_OF_DAY = TimeSpan.FromHours(11);

		/// <summary>
		/// Represents information about the next alarm date.
		/// </summary>
		private struct AlarmDateOffset
		{
			public int DaysToNext { get; private set; }
			
			public DayOfWeek Day { get; private set; }

			public AlarmDateOffset(DayOfWeek day, int daysToNext)
			{
				this.Day = day;
				this.DaysToNext = daysToNext;
			}
		}

		private static readonly Dictionary<DayOfWeek, AlarmDateOffset> DAY_OF_WEEK_TO_NEXT_ALARM_DAY = new Dictionary<DayOfWeek, AlarmDateOffset>()
		{
			{ DayOfWeek.Monday, new AlarmDateOffset(DayOfWeek.Tuesday, 1 ) },
			{ DayOfWeek.Tuesday, new AlarmDateOffset(DayOfWeek.Wednesday, 1) },
			{ DayOfWeek.Wednesday, new AlarmDateOffset(DayOfWeek.Thursday, 1) },
			{ DayOfWeek.Thursday, new AlarmDateOffset(DayOfWeek.Friday, 1) },
			{ DayOfWeek.Friday, new AlarmDateOffset(DayOfWeek.Monday, 3) },
			{ DayOfWeek.Saturday, new AlarmDateOffset(DayOfWeek.Monday, 2) },
			{ DayOfWeek.Sunday, new AlarmDateOffset(DayOfWeek.Monday, 1) },
		};

		public static string[] Permissions 
		{
			get
			{
				return new string[]
				{
					Manifest.Permission.ReceiveBootCompleted,
					Manifest.Permission.SetAlarm,
					Manifest.Permission.DevicePower,
					Manifest.Permission.SystemAlertWindow,
					Manifest.Permission.Vibrate,
					Manifest.Permission.WakeLock,
				};
			}
		}

		#endregion

		#region Fields

		private readonly Context applicationContext;

		#endregion

		#region Properties

		private static ApplicationState instance;

		#endregion

		#region Initializers

		private ApplicationState(Context applicationContext)
		{
			this.applicationContext = applicationContext;
		}

		#endregion

		#region Methods

		/// <summary>
		/// Refreshes the state of the next alarm.
		/// </summary>
		public void SyncNextAlarm ()
		{
			if (Settings.GetIsAlarmOn(applicationContext))
				StartAlarm();
			else
				ResetAlarms();
		}

		public static ApplicationState GetInstance(Context applicationContext)
		{
			if (instance == null)
				instance = new ApplicationState(applicationContext.ApplicationContext);

			return instance;
		}

		public void StartAlarm()
		{
			setAlarmDate(determineNextAlarmTime());
		}

		/// <summary>
		/// Cancel any pending test alarms, and if the main alarm is on restarts it.
		/// </summary>
		public void ResetAlarms()
		{
			if (Settings.GetIsAlarmOn(this.applicationContext))
			{
				// Overwrite alarms with the main one
				StartAlarm();
				Settings.AddLogMessage("Alarm Started", applicationContext);
			}
			else
			{
				//Gets or creates an intent if it exists, then cancels it
				Intent intent = createAlarmViewIntent();
				PendingIntent.GetActivity(applicationContext, 0, intent, PendingIntentFlags.UpdateCurrent).Cancel();

				StandupAlarm.Persistance.Settings.SetNextAlarmTime(null, applicationContext);

				Settings.AddLogMessage("Alarm Cancelled", applicationContext);
			}
		}

		private Intent createAlarmViewIntent()
		{
			Intent intent = new Intent(applicationContext, typeof(StopAlarmActivity));
			intent.SetFlags(ActivityFlags.NewTask);
			return intent;
		}

		public void SetAlarmTimer(TimeSpan timeSpan)
		{
			setAlarmDate(DateTime.Now + timeSpan);
		}

		public void setAlarmDate(DateTime alarmTime)
		{
			Java.Util.Calendar calendar = Java.Util.Calendar.Instance;
			calendar.Set(CalendarField.Year, alarmTime.Year);
			calendar.Set(CalendarField.DayOfYear, alarmTime.DayOfYear);
			calendar.Set(CalendarField.HourOfDay, alarmTime.Hour);
			calendar.Set(CalendarField.Minute, alarmTime.Minute);
			calendar.Set(CalendarField.Second, alarmTime.Second);

			StandupAlarm.Persistance.Settings.SetNextAlarmTime(alarmTime, applicationContext);

			PendingIntent pendingIntent = PendingIntent.GetActivity(applicationContext, 0, createAlarmViewIntent(), PendingIntentFlags.CancelCurrent);
			AlarmManager.FromContext(applicationContext).SetExactAndAllowWhileIdle(AlarmType.RtcWakeup, calendar.TimeInMillis , pendingIntent);

			Settings.AddLogMessage(applicationContext, "Alarm target: {0}. Current time: {1}",
				 alarmTime, DateTime.Now);
		}

		private static DateTime determineNextAlarmTime()
		{
			DateTime alarmDate = DateTime.Now;

			DayOfWeek day = alarmDate.DayOfWeek;

			// If past the alarm time (or on a weekend) pick the next day
			if (alarmDate.TimeOfDay > ALARM_START_TIME_OF_DAY || day == DayOfWeek.Saturday || day == DayOfWeek.Sunday)
			{
				AlarmDateOffset nextAlarmDay = DAY_OF_WEEK_TO_NEXT_ALARM_DAY[alarmDate.DayOfWeek];
				alarmDate = alarmDate.AddDays(nextAlarmDay.DaysToNext);
			}

			// Strip the time
			alarmDate = alarmDate.Date;

			// If 2 weeks from the forbidden day, skip it
			if((alarmDate - Settings.GetSkippedDate(instance.applicationContext)).Days % 14 == 0)
			{
				AlarmDateOffset nextAlarmDay = DAY_OF_WEEK_TO_NEXT_ALARM_DAY[alarmDate.DayOfWeek];
				alarmDate = alarmDate.AddDays(nextAlarmDay.DaysToNext);
			}

			alarmDate = alarmDate.Add(ALARM_START_TIME_OF_DAY);

			return alarmDate;
		}

		#endregion
	}
}